using Framework.Generic.EntityFramework;
using Framework.Generic.Tests.Builders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QR.Business.Services;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using StockMarket.Generic.Test.Builders;
using StockMarket.Generic.Downloaders;
using StockMarket.DataModel.Test.Builders;
using StockMarket.DataModel;

namespace QR.Business.Tests.Services
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class MarketServiceTest
    {
        private ICompanyService<TestCompany> _companyService;
        private IQuoteService<TestQuote> _quoteService;
        private IMarketDownloader<TestCompany, TestQuote> _downloader;
        private IMarketService<TestCompany, TestQuote> _marketService;

        private int DaysUntil(DateTime endDate) => (int)(DateTime.Now.Date - endDate.Date).TotalDays;

        [TestInitialize]
        public void Initialize()
        {
            var mockContext = new MockEfContext(new[] { typeof(TestQuote), typeof(TestCompany) } );
            var quoteRepository = new EfRepository<TestQuote>(mockContext.Object);
            var companyRepository = new EfRepository<TestCompany>(mockContext.Object);

            _quoteService = new QuoteService<TestQuote>(quoteRepository);
            _companyService = new CompanyService<TestCompany>(companyRepository);
            _downloader = new MockMarketDownloader().Object;

            _marketService = new MarketService<TestCompany, TestQuote>(_companyService, _quoteService, _downloader);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _marketService.Dispose();
            _companyService.Dispose();
            _quoteService.Dispose();
            _downloader.Dispose();
        }

        #region Testing MarketService(ICompanyService<C> companyService, IQuoteService<Q> quoteService, IMarketDownloader<C, Q> downloader)

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MarketService_WithNullCompanyService_ThrowsException()
        {
            // Act
            _marketService = new MarketService<TestCompany,TestQuote>(null, _quoteService, _downloader);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MarketService_WithNullQuoteService_ThrowsException()
        {
            // Act
            _marketService = new MarketService<TestCompany, TestQuote>(_companyService, null, _downloader);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MarketService_WithNullDownloader_ThrowsException()
        {
            // Act
            _marketService = new MarketService<TestCompany, TestQuote>(_companyService, _quoteService, null);
        }

        [TestMethod]
        public void MarketService_WithValidServices_InitializesServicesProperly()
        {
            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotes();
        }

        #endregion
        #region Testing C DownloadCompany(string tickerSymbol)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void DownloadCompany_WithDisposedService_ThrowsException()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            _marketService.Dispose();
            _marketService.DownloadCompany(company.Symbol);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DownloadCompany_WithNullTickerSymbol_ThrowsException()
        {
            // Act
            _marketService.DownloadCompany(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DownloadCompany_WithEmptyTickerSymbol_ThrowsException()
        {
            // Act
            _marketService.DownloadCompany(string.Empty);
        }

        [TestMethod]
        public void DownloadCompany_WithValidTickerSymbol_ReturnsCompanyDetails()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            var details = _marketService.DownloadCompany(company.Symbol);

            // Assert
            Assert.IsNotNull(details);
            Assert.IsTrue(details.Id == company.Id);
        }

        #endregion
        #region Testing void UpdateCompanyWithLatestQuotes(int id)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void UpdateCompanyWithLatestQuotes_WithDisposedService_ThrowsException()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            _marketService.Dispose();
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void UpdateCompanyWithLatestQuotes_WithInvalidCompanyId_ThrowsException()
        {
            // Act
            _marketService.UpdateCompanyWithLatestQuotes(778899);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndNoQuotes_UpdatesMaxQuotesForCompany()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            _companyService.Add(company);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            var maxDays = DaysUntil(DateTime.Now.AddYears(-2));
            
            // Assert
            Assert.IsTrue(quotesCountAfter == maxDays);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates2YearQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-2)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates2YearQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-2));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            
            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates2YearQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-2)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes); 

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            
            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates1YearQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-1)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            
            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates1YearQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-1));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes); 

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates1YearQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-1)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes); 

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates5MonthQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-5)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates5MonthQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-5));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates5MonthQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-5)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates3MonthQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-3)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates3MonthQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-3));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates3MonthQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-3)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates1MonthQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-1)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates1MonthQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-1));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates1MonthQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-1)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates5DaysQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-5)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates5DaysQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-5));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates5DaysQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-5)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates1DaysQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-1)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates1DaysQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-1));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        [TestMethod]
        public void UpdateCompanyWithLatestQuotes_WithValidCompanyIdAndQuotes_Updates1DaysQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-1)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.Add(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateCompanyWithLatestQuotes(company.Id);
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == daysToSkip + 1);
        }

        #endregion
        #region Testing void UpdateAllCompaniesWithLatestQuotes()

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void UpdateAllCompaniesWithLatestQuotes_WithDisposedService_ThrowsException()
        {
            // Arrange
            _marketService.Dispose();

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotes();
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotes_WithNoCompanies_DoesNothing()
        {
            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotes();

            var quotes = _quoteService.GetQuotes().ToList();

            // Assert
            Assert.IsFalse(quotes.Any());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotes_WithCompaniesNotSetToRetrieveQuotes_DoesNothing()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = false;

            _companyService.Add(company);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotes();

            var quotes = _quoteService.GetQuotes().ToList();

            // Assert
            Assert.IsFalse(quotes.Any());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotes_WithCompaniesSetToRetrieveQuotes_DownloadsQuotesForOneCompanies()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            _companyService.Add(company);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotes();

            var quotes = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id).ToList();

            // Assert
            Assert.IsTrue(quotes.Any());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotes_WithCompaniesSetToRetrieveQuotes_DownloadsQuotesForMultipleCompanies()
        {
            // Arrange
            var company1 = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company1.RetrieveQuotesFlag = true;

            var company2 = FakeCompaniesBuilder.CreateFakeCompanyGPRO();
            company2.RetrieveQuotesFlag = true;

            var company3 = FakeCompaniesBuilder.CreateFakeCompanyGOOG();
            company3.RetrieveQuotesFlag = false;

            _companyService.Add(company1);
            _companyService.Add(company2);
            _companyService.Add(company3);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotes();

            var aaplQuotes = _quoteService.GetQuotes().Where(q => q.CompanyId == company1.Id).ToList();
            var gproQuotes = _quoteService.GetQuotes().Where(q => q.CompanyId == company2.Id).ToList();
            var googQuotes = _quoteService.GetQuotes().Where(q => q.CompanyId == company3.Id).ToList();

            // Assert
            Assert.IsTrue(aaplQuotes.Any());
            Assert.IsTrue(gproQuotes.Any());
            Assert.IsFalse(googQuotes.Any());
        }

        #endregion
        #region Testing Dispose...

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Dispose_AndThenTryToReuseService_ThrowsException1()
        {
            // Act
            _marketService.Dispose();
            _marketService.UpdateAllCompaniesWithLatestQuotes();
        }
        
        #endregion
    }
}
