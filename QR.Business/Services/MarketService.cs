using System;
using StockMarket.DataModel;
using System.Linq;
using StockMarket.Generic.Downloaders;

namespace QR.Business.Services
{
    public interface IMarketService<C, Q> : IDisposable where C : Company where Q : Quote
    {
        /// <summary>
        /// Creates and returns a new company by downloading all relevant information for the <paramref name="tickerSymbol"/>.
        /// </summary>
        /// <param name="tickerSymbol">The ticker symbol of the company to download.</param>
        /// <returns>Returns a new company after downloading all relevant information for the <paramref name="tickerSymbol"/>.</returns>
        C DownloadCompany(string tickerSymbol);

        /// <summary>
        /// Downloads and stores quotes for a given company, up to a max range. 
        /// NOTE: Does not override existing quotes.
        /// </summary>
        /// <param name="id">The ID of the company to store the quotes for.</param> 
        void UpdateCompanyWithLatestQuotes(int id);

        /// <summary>
        /// Downloads and stores quotes for all active companies, up to max range.
        /// </summary>
        void UpdateAllCompaniesWithLatestQuotes();
    }

    public class MarketService<C, Q> : IMarketService<C, Q> where C : Company where Q : Quote
    {
        private const int MAX_MONTHS_TO_DOWNLOAD = 24;

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
        /// Creates and returns a new company by downloading all relevant information for the <paramref name="tickerSymbol"/>.
        /// </summary>
        /// <param name="tickerSymbol">The ticker symbol of the company to download.</param>
        /// <returns>Returns a new company after downloading all relevant information for the <paramref name="tickerSymbol"/>.</returns>
        public C DownloadCompany(string tickerSymbol)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");

            if (string.IsNullOrEmpty(tickerSymbol))
                throw new ArgumentNullException("tickerSymbol");

            return _downloader.DownloadCompanyDetails(tickerSymbol);
        }

        /// <summary>
        /// Downloads and stores quotes for a given company, up to a max range. 
        /// NOTE: Does not override existing quotes.
        /// </summary>
        /// <param name="id">The ID of the company to store the quotes for.</param>
        public void UpdateCompanyWithLatestQuotes(int id)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");
            
            var company = _companyService.FindCompany(id) ?? throw new ArgumentException($"A company with the id='{id}' does not exist.");

            UpdateCompanyWithLatestQuotes(company);
        }

        /// <summary>
        /// Downloads and stores quotes for all active companies up to max range of quotes.
        /// </summary>
        public void UpdateAllCompaniesWithLatestQuotes()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("MarketService", "The service has been disposed.");

            foreach (var company in _companyService.GetCompanies().Where(c => c.RetrieveQuotesFlag))
                UpdateCompanyWithLatestQuotes(company);
        }

        /// <summary>
        /// Downloads and stores quotes for a given company, up to a max range. 
        /// NOTE: Does not override existing quotes.
        /// </summary>
        /// <param name="company">The company to store the quotes for.</param>
        private void UpdateCompanyWithLatestQuotes(C company)
        {
            // If quotes are stored for this company, return the last date a quote was stored for; 
            // otherwise return today's date - MAX_MONTHS_TO_DOWNLOAD
            var lastStoredQuoteDate = company.Quotes.Any() ?
                        company.Quotes.Max(q => q.Date).Date : DateTime.Now.AddMonths(-1 * MAX_MONTHS_TO_DOWNLOAD).AddDays(-1).Date;
            var todaysDate = DateTime.Now.Date;

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
            quotes.ForEach(q => { q.Company = company; q.CompanyId = company.Id; });

            _quoteService.Add(quotes);
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
