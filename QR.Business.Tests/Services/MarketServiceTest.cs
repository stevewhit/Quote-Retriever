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
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync();
        }

        #endregion
        #region Testing Task<C> DownloadCompanyAsync(string tickerSymbol)

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void DownloadCompanyAsync_WithDisposedService_ThrowsException()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            _marketService.Dispose();
            _marketService.DownloadCompanyAsync(company.Symbol).Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void DownloadCompanyAsync_WithNullTickerSymbol_ThrowsException()
        {
            // Act
            _marketService.DownloadCompanyAsync(null).Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void DownloadCompanyAsync_WithEmptyTickerSymbol_ThrowsException()
        {
            // Act
            _marketService.DownloadCompanyAsync(string.Empty).Wait();
        }

        [TestMethod]
        public void DownloadCompanyAsync_WithValidTickerSymbol_ReturnsCompanyDetails()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            var downloadTask = _marketService.DownloadCompanyAsync(company.Symbol);
            downloadTask.Wait();

            var details = downloadTask.Result;

            // Assert
            Assert.IsNotNull(details);
            Assert.IsTrue(details.Id == company.Id);
        }

        #endregion
        #region Testing Task UpdateAllCompanyDetailsAsync()

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void UpdateAllCompanyDetailsAsync_WithDisposedService_ThrowsException()
        {
            // Arrange
            _marketService.Dispose();

            // Act
            _marketService.UpdateAllCompanyDetailsAsync().Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void UpdateAllCompanyDetailsAsync_WithInvalidCompany_ThrowsException()
        {
            // Arrange
            var invalidCompany = new TestCompany()
            {
                Symbol = "INVALID",
                DownloadDetailsFlag = true
            };

            _companyService.Add(invalidCompany);

            // Act
            _marketService.UpdateAllCompanyDetailsAsync().Wait();
        }

        [TestMethod]
        public void UpdateAllCompanyDetailsAsync_WithoutCompanies_DoesNothing()
        {
            // Act
            _marketService.UpdateAllCompanyDetailsAsync().Wait();

            var companies = _companyService.GetCompanies();

            // Assert
            Assert.IsTrue(companies.Count() == 0);
        }

        [TestMethod]
        public void UpdateAllCompanyDetailsAsync_WithCompaniesNotDownloadable_DoesNothing()
        {
            // Arrange
            var testCompany1 = FakeCompaniesBuilder.CreateFakeCompanyAMZNIncomplete();
            testCompany1.DownloadDetailsFlag = false;

            var testCompany2 = FakeCompaniesBuilder.CreateFakeCompanyGOOGIncomplete();
            testCompany2.DownloadDetailsFlag = false;

            _companyService.Add(testCompany1);
            _companyService.Add(testCompany2);

            // Act
            _marketService.UpdateAllCompanyDetailsAsync().Wait();

            var company1 = _companyService.GetCompanies().First(c => c.Id == testCompany1.Id);
            var company2 = _companyService.GetCompanies().First(c => c.Id == testCompany2.Id);

            // Assert
            Assert.IsTrue(string.IsNullOrEmpty(company1.CompanyName));
            Assert.IsTrue(string.IsNullOrEmpty(company2.CompanyName));
        }

        [TestMethod]
        public void UpdateAllCompanyDetailsAsync_WithCompanyDownloadable_UpdatesCompanyWithData()
        {
            // Arrange
            var testCompany1 = FakeCompaniesBuilder.CreateFakeCompanyAMZNIncomplete();
            testCompany1.DownloadDetailsFlag = false;

            var testCompany2 = FakeCompaniesBuilder.CreateFakeCompanyGOOGIncomplete();
            testCompany2.DownloadDetailsFlag = true;

            _companyService.Add(testCompany1);
            _companyService.Add(testCompany2);

            // Act
            _marketService.UpdateAllCompanyDetailsAsync().Wait();

            var company1 = _companyService.GetCompanies().First(c => c.Id == testCompany1.Id);
            var company2 = _companyService.GetCompanies().First(c => c.Id == testCompany2.Id);

            // Assert
            Assert.IsTrue(string.IsNullOrEmpty(company1.CompanyName));
            Assert.IsTrue(company2.CompanyName.Equals("Google", StringComparison.CurrentCultureIgnoreCase));
        }

        [TestMethod]
        public void UpdateAllCompanyDetailsAsync_WithCompaniesDownloadable_OverridesStoredCompanyData()
        {
            // Arrange
            var testCompany1 = FakeCompaniesBuilder.CreateFakeCompanyAMZNIncomplete();
            testCompany1.DownloadDetailsFlag = true;

            var testCompany2 = FakeCompaniesBuilder.CreateFakeCompanyGOOGIncomplete();
            testCompany2.DownloadDetailsFlag = true;

            var testCompany3 = FakeCompaniesBuilder.CreateFakeCompanyGPRO();
            testCompany3.CompanyName = "FakeName";
            testCompany3.DownloadDetailsFlag = true;

            _companyService.Add(testCompany1);
            _companyService.Add(testCompany2);
            _companyService.Add(testCompany3);

            // Act
            _marketService.UpdateAllCompanyDetailsAsync().Wait();

            var company1 = _companyService.GetCompanies().First(c => c.Id == testCompany1.Id);
            var company2 = _companyService.GetCompanies().First(c => c.Id == testCompany2.Id);
            var company3 = _companyService.GetCompanies().First(c => c.Id == testCompany3.Id);

            // Assert
            Assert.IsTrue(company1.CompanyName.Equals("Amazon", StringComparison.CurrentCultureIgnoreCase));
            Assert.IsTrue(company2.CompanyName.Equals("Google", StringComparison.CurrentCultureIgnoreCase));
            Assert.IsTrue(company3.CompanyName.Equals("GoPro", StringComparison.CurrentCultureIgnoreCase));
        }

        #endregion
        #region Testing Task UpdateAllCompaniesWithLatestQuotesAsync()
        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithDisposedService_ThrowsException()
        {
            // Arrange
            _marketService.Dispose();

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithInvalidCompany_ThrowsException()
        {
            // Arrange
            var invalidCompany = new TestCompany()
            {
                Symbol = "INVALID",
                RetrieveQuotesFlag = true
            };

            _companyService.Add(invalidCompany);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithNoCompanies_DoesNothing()
        {
            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var quotes = _quoteService.GetQuotes().ToList();

            // Assert
            Assert.IsFalse(quotes.Any());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithCompaniesNotSetToRetrieveQuotes_DoesNothing()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = false;

            _companyService.Add(company);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var quotes = _quoteService.GetQuotes().ToList();

            // Assert
            Assert.IsFalse(quotes.Any());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithCompaniesSetToRetrieveQuotes_DownloadsQuotesForOneCompanies()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            _companyService.Add(company);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var quotes = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id).ToList();

            // Assert
            Assert.IsTrue(quotes.Any());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithCompaniesSetToRetrieveQuotes_DownloadsQuotesForMultipleCompanies()
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
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var aaplQuotes = _quoteService.GetQuotes().Where(q => q.CompanyId == company1.Id).ToList();
            var gproQuotes = _quoteService.GetQuotes().Where(q => q.CompanyId == company2.Id).ToList();
            var googQuotes = _quoteService.GetQuotes().Where(q => q.CompanyId == company3.Id).ToList();

            // Assert
            Assert.IsTrue(aaplQuotes.Any());
            Assert.IsTrue(gproQuotes.Any());
            Assert.IsFalse(googQuotes.Any());
        }
        
        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndNoQuotes_UpdatesMaxQuotesForCompany()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            _companyService.Add(company);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddYears(-2)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates2YearQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-2)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddYears(-2)) + 1);
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates2YearQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-2));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddYears(-2)) + 1);
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates2YearQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-2)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddYears(-2)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates2YearQuotesForCompany4()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-1)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddYears(-2)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates2YearsQuotesForCompany5()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-1));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddYears(-2)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates1YearQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddYears(-1)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddYears(-1)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates1YearQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-5)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddYears(-1)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates1YearQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-5));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddYears(-1)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates5MonthQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-5)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddMonths(-5)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates5MonthQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-3)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddMonths(-5)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates5MonthQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-3));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddMonths(-5)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates3MonthQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-3)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddMonths(-3)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates3MonthQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-1)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddMonths(-3)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates3MonthsQuotesForCompany4()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-1));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddMonths(-3)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates1MonthQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddMonths(-1)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddMonths(-1)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates1MonthQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-5)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddMonths(-1)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates1MonthQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-5));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddMonths(-1)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates5DaysQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-5)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddDays(-5)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates5DaysQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-1)) + 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddDays(-5)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates5DaysQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-1));
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddDays(-5)));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidCompanyIdAndQuotes_Updates1DaysQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var daysToSkip = DaysUntil(DateTime.Now.AddDays(-1)) - 1;
            var fakequotes = FakeQuotesBuilder.CreateFakeQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var quotesCountBefore = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            var quotesCountAfter = _quoteService.GetQuotes().Count(q => q.CompanyId == company.Id);

            // Assert
            Assert.IsTrue(quotesCountAfter == DaysUntil(DateTime.Now.AddDays(-1)));
        }

        #endregion
        #region Testing Dispose...

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void Dispose_AndThenTryToReuseService_ThrowsException1()
        {
            // Act
            _marketService.Dispose();
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
        }
        
        #endregion
    }
}
