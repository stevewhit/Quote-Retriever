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
        Task<C> DownloadCompanyAsync(string tickerSymbol);

        /// <summary>
        /// Updates the company details for all companies that are marked to be updated by downloading all relevant company information
        /// and updating the existing entity object.
        /// </summary>
        Task UpdateAllCompanyDetailsAsync();

        /// <summary>
        /// Downloads and stores quotes for all active companies, up to max range.
        /// </summary>
        Task UpdateAllCompaniesWithLatestQuotesAsync();
    }

    public class MarketService<C, Q> : IMarketService<C, Q> where C : Company where Q : Quote
    {
        private const int MAX_MONTHS_TO_DOWNLOAD = 1;

        private readonly ICompanyService<C> _companyService;
        private readonly IQuoteService<Q> _quoteService;
        private readonly IMarketDownloader<C, Q> _downloader;

        private bool _isDisposed = false;

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
        public async Task<C> DownloadCompanyAsync(string tickerSymbol)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");

            if (string.IsNullOrEmpty(tickerSymbol))
                throw new ArgumentNullException("tickerSymbol");

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
            var downloadTasks = _companyService.GetCompanies().Where(c => c.DownloadDetailsFlag).ToList().Select(c => GetCompanyDetailsAsync(c));
            var updatedCompanies = await Task.WhenAll(downloadTasks);
            
            updatedCompanies.ForEach(c =>
            {
                c.DownloadDetailsFlag = false;
                _companyService.Update(c);
            });
        }

        /// <summary>
        /// Asycronously downloads details for a given company.
        /// </summary>
        /// <param name="company">The company to download details for.</param>
        /// <returns>Returns the updated company object after it has been updated with the downloaded details.</returns>
        private async Task<C> GetCompanyDetailsAsync(C company)
        {
            return await Task.Run(() =>
            {
                var downloadedCompany = _downloader.DownloadCompanyDetails(company.Symbol);
                CopyDownloadedCompanyDetails(company, downloadedCompany);
                
                return company;
            });
        }

        /// <summary>
        /// Copies all relevant company properties from a downloaded company to an existing company.
        /// </summary>
        /// <param name="toCompany">The company that the downloaded details will be copied to.</param>
        /// <param name="fromCompany">The downloaded company.</param>
        private void CopyDownloadedCompanyDetails(C toCompany, C fromCompany)
        {
            toCompany.CompanyName = fromCompany.CompanyName;
            toCompany.Exchange = fromCompany.Exchange;
            toCompany.Industry = fromCompany.Industry;
            toCompany.Website = fromCompany.Website;
            toCompany.Description = fromCompany.Description;
            toCompany.CEO = fromCompany.CEO;
            toCompany.SecurityName = fromCompany.SecurityName;
            toCompany.IssueType = fromCompany.IssueType;
            toCompany.Sector = fromCompany.Sector;
            toCompany.NumEmployees = fromCompany.NumEmployees;
            toCompany.Tags = fromCompany.Tags;
        }

        /// <summary>
        /// Downloads and stores quotes for all active companies up to max range of quotes.
        /// </summary>
        public async Task UpdateAllCompaniesWithLatestQuotesAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");

            // Start the async downloads for each company and add the new
            // quote data as the tasks finish.
            var downloadTasks = _companyService.GetCompanies().Where(c => c.RetrieveQuotesFlag).ToList().Select(c => GetLatestQuotesForCompanyAsync(c)).ToList();
            while (downloadTasks.Count() > 0)
            {
                var completedTask = await Task.WhenAny(downloadTasks);

                downloadTasks.Remove(completedTask);
                
                _quoteService.AddRange(await completedTask);
            }
        }

        /// <summary>
        /// Asycronously downloads and returns quotes for a given company, up to a max range. 
        /// </summary>
        /// <param name="company">The company to store the quotes for.</param>
        /// <returns>Returns the downloaded quotes for the given company.</returns>
        private async Task<IEnumerable<Q>> GetLatestQuotesForCompanyAsync(C company)
        {
            // If quotes are stored for this company, return the last date a quote was stored for; 
            // otherwise return today's date - MAX_MONTHS_TO_DOWNLOAD
            var lastStoredQuoteDate = company.Quotes.Any() ?
                        company.Quotes.Max(q => q.Date).Date : DateTime.Now.AddMonths(-1 * MAX_MONTHS_TO_DOWNLOAD).AddDays(-1).Date;

            var todaysDate = DateTime.Now.Date;

            return await Task.Run(() =>
            {   
                // Download quotes in chuncks based off the date difference between the last stored quote date and today's date.
                var downloadedQuotes = lastStoredQuoteDate.AddDays(1) >= todaysDate ? new[] { _downloader.DownloadPreviousDayQuote(company.Symbol) } :
                                       lastStoredQuoteDate.AddDays(5) >= todaysDate ? _downloader.DownloadQuotesFiveDays(company.Symbol) :
                                       lastStoredQuoteDate.AddMonths(1) >= todaysDate ? _downloader.DownloadQuotesOneMonth(company.Symbol) :
                                       lastStoredQuoteDate.AddMonths(3) >= todaysDate ? _downloader.DownloadQuotesThreeMonths(company.Symbol) :
                                       lastStoredQuoteDate.AddMonths(5) >= todaysDate ? _downloader.DownloadQuotesFiveMonths(company.Symbol) :
                                       lastStoredQuoteDate.AddYears(1) >= todaysDate ? _downloader.DownloadQuotesOneYear(company.Symbol) :
                                       lastStoredQuoteDate.AddYears(2) >= todaysDate ? _downloader.DownloadQuotesTwoYears(company.Symbol) :
                                       _downloader.DownloadQuotesTwoYears(company.Symbol);

                // Remove any duplicate dates
                var quotes = downloadedQuotes.Where(q => q.Date > lastStoredQuoteDate && !company.Quotes.Any(cq => cq.Date == q.Date)).OrderBy(q => q.Date).ToList();

                // Store company for each quote.
                return quotes.ForEach<Q>(q => { q.Company = company; q.CompanyId = company.Id; });
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
