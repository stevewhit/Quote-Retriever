using System;
using StockMarket.DataModel;
using System.Linq;
using StockMarket.Generic.Downloaders;
using System.Collections.Generic;
using Framework.Generic.Utility;
using System.Threading.Tasks;

namespace QR.Business.Services
{
    public interface IMarketService<C, Q> : IDisposable where C : Company where Q : Quote
    {
        /// <summary>
        /// Creates and returns a new company by downloading all relevant information for the <paramref name="tickerSymbol"/>.
        /// </summary>
        /// <param name="tickerSymbol">The ticker symbol of the company to download.</param>
        /// <returns>Returns a new company after downloading all relevant information for the <paramref name="tickerSymbol"/>.</returns>
        Task<C> GetCompanyDetailsAsync(string tickerSymbol);

        /// <summary>
        /// Updates the company details for all companies that are marked to be updated by downloading all relevant company information
        /// and updating the existing entity object.
        /// </summary>
        Task UpdateAllCompanyDetailsAsync();

        /// <summary>
        /// Downloads and stores quotes for all active companies, up to max range.
        /// </summary>
        Task UpdateAllCompaniesWithLatestQuotesAsync();

        /// <summary>
        /// Asycronously downloads and returns quotes for a given company <paramref name="tickerSymbol"/>, up to a max range. 
        /// </summary>
        /// <param name="tickerSymbol">The symbol of the company to store the quotes for.</param>
        /// <param name="startDate">The beginning date of the day quotes that are returned.</param>
        /// <returns>Returns the downloaded quotes for the given company <paramref name="tickerSymbol"/>.</returns>
        Task<IEnumerable<Q>> GetDayQuotesForCompanyAsync(string tickerSymbol, DateTime startDate);

        /// <summary>
        /// Asycronously downloads and returns minute quotes for a given company <paramref name="tickerSymbol"/>.
        /// </summary>
        /// <param name="tickerSymbol">The company symbol to download the quotes for.</param>
        /// <returns>Returns the downloaded minute quotes for the given company <paramref name="tickerSymbol"/>.</returns>
        Task<IEnumerable<Q>> GetMinuteQuotesForCompanyAsync(string tickerSymbol);
    }

    public class MarketService<C, Q> : IMarketService<C, Q> where C : Company where Q : Quote
    {
        private const int MAX_MONTHS_TO_DOWNLOAD = 6;

        private readonly ICompanyService<C> _companyService;
        private readonly IQuoteService<Q> _quoteService;
        private readonly IMarketDownloader<C, Q> _downloader;

        private bool _isDisposed = false;
        private readonly static TimeSpan _marketOpen = new TimeSpan(9, 30, 0);
        private readonly static TimeSpan _marketClose = new TimeSpan(15, 59, 0);

        public MarketService(ICompanyService<C> companyService, IQuoteService<Q> quoteService, IMarketDownloader<C, Q> downloader)
        {
            _companyService = companyService ?? throw new ArgumentNullException("companyService");
            _quoteService = quoteService ?? throw new ArgumentNullException("quoteService");
            _downloader = downloader ?? throw new ArgumentNullException("downloader");
        }

        #region IMarketService<C, Q>

        /// <summary>
        /// Asycronously creates and returns a new company by downloading all relevant information for the <paramref name="tickerSymbol"/>.
        /// </summary>
        /// <param name="tickerSymbol">The ticker symbol of the company to download.</param>
        /// <returns>Returns a new company after downloading all relevant information for the <paramref name="tickerSymbol"/>.</returns>
        public async Task<C> GetCompanyDetailsAsync(string tickerSymbol)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");

            if (string.IsNullOrEmpty(tickerSymbol))
                throw new ArgumentNullException(nameof(tickerSymbol));

            return await Task.Run(() =>
            {
                return _downloader.DownloadCompanyDetails(tickerSymbol);
            });
        }

        /// <summary>
        /// Asycronously updates the company details for all companies that are marked to be updated, by downloading all relevant company information
        /// and updating the existing entity object.
        /// </summary>
        public async Task UpdateAllCompanyDetailsAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");

            // Asyncronously download details for each company
            var runningTasks = _companyService.GetCompanies().Where(c => c.DownloadDetailsFlag).ToList().Select(c => GetCompanyDetailsAsync(c.Symbol)).ToList();
            var taskExceptions = new List<Exception>();

            while (runningTasks.Any())
            {
                try
                {
                    var completedTask = await Task.WhenAny(runningTasks);
                    runningTasks.Remove(completedTask);

                    // Process any completed task by updating the existing company with new data.
                    var downloadedCompany = await completedTask;

                    // Copy the relevant details to the existing company
                    var company = _companyService.FindCompany(downloadedCompany.Symbol);
                    company.CompanyName = downloadedCompany.CompanyName;
                    company.Exchange = downloadedCompany.Exchange;
                    company.Industry = downloadedCompany.Industry;
                    company.Website = downloadedCompany.Website;
                    company.Description = downloadedCompany.Description;
                    company.CEO = downloadedCompany.CEO;
                    company.SecurityName = downloadedCompany.SecurityName;
                    company.IssueType = downloadedCompany.IssueType;
                    company.Sector = downloadedCompany.Sector;
                    company.NumEmployees = downloadedCompany.NumEmployees;
                    company.Tags = downloadedCompany.Tags;
                    company.DownloadDetailsFlag = false;

                    _companyService.Update(company);
                }
                catch (Exception e)
                {
                    taskExceptions.Add(e);
                }
            }

            if (taskExceptions.Any())
                throw new AggregateException(taskExceptions);
        }
        
        /// <summary>
        /// Downloads and stores quotes for all active companies up to max range of quotes.
        /// </summary>
        public async Task UpdateAllCompaniesWithLatestQuotesAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");

            var currentDate = SystemTime.Now();
            var runningTasks = new List<Task<IEnumerable<Q>>>();
            var taskExceptions = new List<Exception>();

            // Foreach company that is marked to accept new downloaded data,
            // download quotes for each range of time.
            foreach (var company in _companyService.GetCompanies().Where(c => c.RetrieveQuotesFlag))
            {
                var lastMinuteQuoteDate = company.Quotes.Any() ? company.Quotes.Where(q => q.QuoteType == QuoteTypeEnum.Minute).Max(q => q.Date) : DateTime.MinValue.Date;
                var lastDayQuoteDate = company.Quotes.Any() ? company.Quotes.Where(q => q.QuoteType == QuoteTypeEnum.Day).Max(q => q.Date) : SystemTime.Now().AddMonths(-1 * MAX_MONTHS_TO_DOWNLOAD).AddDays(-1).Date;
                                
                // If market is open
                if (currentDate.TimeOfDay >= _marketOpen && currentDate.TimeOfDay <= _marketClose)
                {
                    // If there isn't any minute data for today, or if it's been at least 1 minute since the last stored MINUTE quote
                    if (lastMinuteQuoteDate.Date < currentDate.Date || lastMinuteQuoteDate.TimeOfDay.Add(new TimeSpan(0, 1, 0)) < SystemTime.Now().TimeOfDay)
                        runningTasks.Add(GetMinuteQuotesForCompanyAsync(company.Symbol));

                    // If it's been at least 2 days since the last stored DAY quote
                    if (lastDayQuoteDate.Date < currentDate.Date.AddDays(-1))
                        runningTasks.Add(GetDayQuotesForCompanyAsync(company.Symbol, lastDayQuoteDate));
                }

                // If it's currently AFTER market closed
                else if (currentDate.TimeOfDay > _marketClose)
                {
                    // If there is missing MINUTE data from today
                    if (lastMinuteQuoteDate.Date < currentDate.Date || (lastMinuteQuoteDate.TimeOfDay < _marketClose))
                        runningTasks.Add(GetMinuteQuotesForCompanyAsync(company.Symbol));

                    // If today's DAY data is missing
                    if (lastDayQuoteDate.Date < currentDate.Date)
                        runningTasks.Add(GetDayQuotesForCompanyAsync(company.Symbol, lastDayQuoteDate));
                }

                // If it's currently BEFORE the market opens
                else if (currentDate.TimeOfDay < _marketOpen)
                {
                    // If yesterday's DAY data is missing
                    if (lastDayQuoteDate.Date < currentDate.Date)
                        runningTasks.Add(GetDayQuotesForCompanyAsync(company.Symbol, lastDayQuoteDate));
                }
            }
            
            while (runningTasks.Any())
            {
                try
                {
                    var completedTask = await Task.WhenAny(runningTasks);
                    runningTasks.Remove(completedTask);
                    
                    var downloadedQuotes = await completedTask;
                    if (downloadedQuotes.Any())
                    {
                        var firstDownloadedQuote = downloadedQuotes.First();
                        var storedCompanyQuotes = _quoteService.GetQuotes().ToList().Where(q => q.CompanyId == firstDownloadedQuote.CompanyId && q.TypeId == firstDownloadedQuote.TypeId);

                        // Hashsets of the quote dates to indicate which dates contain valid/invalid quotes.
                        // Note: Use hashsets for better performance when identifying if a list item is contained in a separate list.
                        var allStoredCompanyQuoteDates = new HashSet<DateTime>(storedCompanyQuotes.Select(q => q.Date));
                        var invalidStoredCompanyQuoteDates = new HashSet<DateTime>(storedCompanyQuotes.Where(q => !q.IsValid).Select(q => q.Date));

                        // Update any invalid stored quotes if the new downloaded quote is valid.
                        var quotesToUpdate = downloadedQuotes.Where(dq => dq.IsValid && invalidStoredCompanyQuoteDates.Contains(dq.Date)).Select((downloadedQuote) =>
                        {
                            var storedQuoteToUpdate = storedCompanyQuotes.First(q => q.Date == downloadedQuote.Date);
                            storedQuoteToUpdate.High = downloadedQuote.High;
                            storedQuoteToUpdate.Low = downloadedQuote.Low;
                            storedQuoteToUpdate.Open = downloadedQuote.Open;
                            storedQuoteToUpdate.Close = downloadedQuote.Close;
                            storedQuoteToUpdate.Volume = downloadedQuote.Volume;
                            storedQuoteToUpdate.IsValid = true;

                            return storedQuoteToUpdate;
                        });

                        _quoteService.UpdateRange(quotesToUpdate);

                        // Add non-duplicate quotes
                        var quotesToAdd = downloadedQuotes.Where(q => !allStoredCompanyQuoteDates.Contains(q.Date));
                        _quoteService.AddRange(quotesToAdd);
                    }
                }
                catch(Exception e)
                {
                    taskExceptions.Add(e);
                }
            }

            if (taskExceptions.Any())
                throw new AggregateException(taskExceptions);
        }

        /// <summary>
        /// Asycronously downloads and returns quotes for a given company <paramref name="tickerSymbol"/>, up to a max range. 
        /// </summary>
        /// <param name="tickerSymbol">The symbol of the company to store the quotes for.</param>
        /// <param name="startDate">The beginning date of the day quotes that are returned.</param>
        /// <returns>Returns the downloaded quotes for the given company <paramref name="tickerSymbol"/>.</returns>
        public async Task<IEnumerable<Q>> GetDayQuotesForCompanyAsync(string tickerSymbol, DateTime startDate)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");

            if (string.IsNullOrEmpty(tickerSymbol))
                throw new ArgumentNullException(nameof(tickerSymbol));

            var currentDate = SystemTime.Now().Date;
            if (startDate > currentDate)
                throw new ArgumentException("Invalid start date supplied. Start date cannot be greater than the current date.");

            // Retrieve the existing company using the ticker symbol so each quote can be updated with it.
            var existingCompany = _companyService.FindCompany(tickerSymbol);

            return await Task.Run(() =>
            {
                // Download quotes in chunks based off the date difference between the last stored quote date and today's date.
                var downloadedQuotes = startDate.AddDays(1) >= currentDate ? new[] { _downloader.DownloadPreviousDayQuote(tickerSymbol) } :
                                       startDate.AddDays(5) >= currentDate ? _downloader.DownloadQuotesFiveDays(tickerSymbol) :
                                       startDate.AddMonths(1) >= currentDate ? _downloader.DownloadQuotesOneMonth(tickerSymbol) :
                                       startDate.AddMonths(3) >= currentDate ? _downloader.DownloadQuotesThreeMonths(tickerSymbol) :
                                       startDate.AddMonths(5) >= currentDate ? _downloader.DownloadQuotesFiveMonths(tickerSymbol) :
                                       startDate.AddYears(1) >= currentDate ? _downloader.DownloadQuotesOneYear(tickerSymbol) :
                                       startDate.AddYears(2) >= currentDate ? _downloader.DownloadQuotesTwoYears(tickerSymbol) :
                                       _downloader.DownloadQuotesTwoYears(tickerSymbol);

                // Attach the company to the newly downloaded quotes if it exists.
                if (existingCompany != null)
                    downloadedQuotes = downloadedQuotes.ToList().ForEach<Q>(q => { q.Company = existingCompany; q.CompanyId = existingCompany.Id; });

                return downloadedQuotes.OrderBy(q => q.Date);
            });
        }

        /// <summary>
        /// Asycronously downloads and returns minute quotes for a given company <paramref name="tickerSymbol"/>.
        /// </summary>
        /// <param name="tickerSymbol">The company symbol to download the quotes for.</param>
        /// <returns>Returns the downloaded minute quotes for the given company <paramref name="tickerSymbol"/>.</returns>
        public async Task<IEnumerable<Q>> GetMinuteQuotesForCompanyAsync(string tickerSymbol)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");

            if (string.IsNullOrEmpty(tickerSymbol))
                throw new ArgumentNullException(nameof(tickerSymbol));

            // Retrieve the existing company using the ticker symbol so each quote can be updated with it.
            var existingCompany = _companyService.FindCompany(tickerSymbol);

            return await Task.Run(() =>
            {
                var downloadedQuotes = _downloader.DownloadIntradayMinuteQuotes(tickerSymbol);

                // Attach the company to the newly downloaded quotes if it exists.
                if (existingCompany != null)
                    downloadedQuotes = downloadedQuotes.ToList().ForEach<Q>(q => { q.Company = existingCompany; q.CompanyId = existingCompany.Id; });

                return downloadedQuotes.OrderBy(q => q.Date);
            });
        }

        #endregion
        #region IDisposable

        /// <summary>
        /// Disposes this object and properly cleans up resources. 
        /// </summary>
        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _companyService.Dispose();
                    _quoteService.Dispose();
                    _downloader.Dispose();
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Disposes this object and properly cleans up resources. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        #endregion
    }
}
