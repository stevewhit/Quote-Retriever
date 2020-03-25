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
using StockMarket.DataModel.Test.Builders.Objects;
using Framework.Generic.Utility;
using System.Collections.Generic;

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

        private static TimeSpan _marketOpen = new TimeSpan(9, 30, 0);
        private static TimeSpan _marketClose = new TimeSpan(15, 59, 0);

        private static readonly DateTime _todayDateBeforeMarketOpen = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 8, 0, 0);
        private static readonly DateTime _todayDateAfterMarketClose = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 18, 0, 0);

        private int DaysUntil(DateTime endDate) => (int)(SystemTime.Now().Date - endDate.Date).TotalDays;

        [TestInitialize]
        public void Initialize()
        {
            SystemTime.ResetDateTime();
            FakeQuotesBuilder.CreatesValidQuotes = true;

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
        #region Testing Task<C> GetCompanyDetailsAsync(string tickerSymbol)

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void GetCompanyDetailsAsync_WithDisposedService_ThrowsException()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            _marketService.Dispose();
            _marketService.GetCompanyDetailsAsync(company.Symbol).Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void GetCompanyDetailsAsync_WithNullTickerSymbol_ThrowsException()
        {
            // Act
            _marketService.GetCompanyDetailsAsync(null).Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void GetCompanyDetailsAsync_WithEmptyTickerSymbol_ThrowsException()
        {
            // Act
            _marketService.GetCompanyDetailsAsync(string.Empty).Wait();
        }

        [TestMethod]
        public void GetCompanyDetailsAsync_WithValidTickerSymbol_ReturnsCompanyDetails()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            var downloadTask = _marketService.GetCompanyDetailsAsync(company.Symbol);
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
        [ExpectedException(typeof(AggregateException))]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithInvalidCompany_ThrowsException()
        {
            // Arrange
            var company = new TestCompany()
            {
                Symbol = "Invalid",
                RetrieveQuotesFlag = true
            };

            _companyService.Add(company);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithCompaniesSetToRetrieveQuotes_DownloadsQuotesForOneCompanies()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 1, _marketClose);
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);        
            _companyService.Add(company);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var quotes = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id).ToList();

            // Assert
            Assert.IsTrue(quotes.Count() > companyQuotes.Count());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithCompaniesSetToRetrieveQuotes_DownloadsQuotesForMultipleCompanies()
        {
            // Arrange
            var company1 = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company1.RetrieveQuotesFlag = true;
            var company1Quotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company1, 1, _marketClose);
            company1.Quotes = company1Quotes.ToList<Quote>();
            _quoteService.AddRange(company1Quotes);
            _companyService.Add(company1);

            var company2 = FakeCompaniesBuilder.CreateFakeCompanyGPRO();
            company2.RetrieveQuotesFlag = true;
            var company2Quotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company2, 1, _marketClose);
            company2.Quotes = company2Quotes.ToList<Quote>();
            _quoteService.AddRange(company2Quotes);
            _companyService.Add(company2);

            var company3 = FakeCompaniesBuilder.CreateFakeCompanyGOOG();
            company3.RetrieveQuotesFlag = false;
            var company3Quotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company3, 1, _marketClose);
            company3.Quotes = company3Quotes.ToList<Quote>();
            _quoteService.AddRange(company3Quotes);
            _companyService.Add(company3);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var company1QuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company1.Id).ToList();
            var company2QuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company2.Id).ToList();
            var company3QuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company3.Id).ToList();

            // Assert
            Assert.IsTrue(company1QuotesAfter.Count() > company1Quotes.Count());
            Assert.IsTrue(company2QuotesAfter.Count() > company2Quotes.Count());
            Assert.IsTrue(company3QuotesAfter.Count() == company3Quotes.Count());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithPreExistingMinuteQuotes_DownloadsOnlyLatestQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 1, _marketClose);
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var maxInitialMinuteQuoteDate = companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Minute).Select(cq => cq.Date).Max();
            var minuteQuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id && q.TypeId == (int)QuoteTypeEnum.Minute).ToList();
            var existingMinuteQuoteDates = new HashSet<DateTime>(companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Minute).Select(q => q.Date));
            var newMinuteQuotes = minuteQuotesAfter.Where(q => !existingMinuteQuoteDates.Contains(q.Date));

            // Assert
            Assert.IsTrue(newMinuteQuotes.All(q => q.Date > maxInitialMinuteQuoteDate));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithPreExistingDayQuotes_DownloadsOnlyLatestQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 1, _marketClose);
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var maxInitialDayQuoteDate = companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Day).Select(cq => cq.Date).Max();
            var dayQuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id && q.TypeId == (int)QuoteTypeEnum.Day).ToList();
            var existingDayQuoteDates = new HashSet<DateTime>(companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Day).Select(q => q.Date));
            var newDayQuotes = dayQuotesAfter.Where(q => !existingDayQuoteDates.Contains(q.Date));

            // Assert
            Assert.IsTrue(newDayQuotes.All(q => q.Date > maxInitialDayQuoteDate));
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_BeforeMarketOpen_DoesNotDownloadMinuteData()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 1, _marketClose);
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);
            
            SystemTime.SetDateTime(_todayDateBeforeMarketOpen);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var maxInitialMinuteQuoteDate = companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Minute).Select(cq => cq.Date).Max();
            var minuteQuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id && q.TypeId == (int)QuoteTypeEnum.Minute).ToList();
            var existingMinuteQuoteDates = new HashSet<DateTime>(companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Minute).Select(q => q.Date));
            var newMinuteQuotes = minuteQuotesAfter.Where(q => !existingMinuteQuoteDates.Contains(q.Date));

            // Assert
            Assert.IsTrue(newMinuteQuotes.Count() == 0);
            Assert.IsTrue(existingMinuteQuoteDates.Count() == minuteQuotesAfter.Count());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_BeforeMarketOpen_DownloadsMissingDayQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 1, _marketClose);
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);

            SystemTime.SetDateTime(_todayDateBeforeMarketOpen);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var maxInitialDayQuoteDate = companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Day).Select(cq => cq.Date).Max();
            var dayQuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id && q.TypeId == (int)QuoteTypeEnum.Day).ToList();
            var existingDayQuoteDates = new HashSet<DateTime>(companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Day).Select(q => q.Date));
            var newDayQuotes = dayQuotesAfter.Where(q => !existingDayQuoteDates.Contains(q.Date));

            // Assert
            Assert.IsTrue(newDayQuotes.Count() == 1);
            Assert.IsTrue(dayQuotesAfter.Count() == existingDayQuoteDates.Count() + 1);
        }
        
        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_DuringMarketHours_DoesntDownloadIfWithinSameMinute()
        {
            // Arrange
            var todayDuringMarketOpen = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0, 500);

            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 0, todayDuringMarketOpen.TimeOfDay);
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);

            // Set time to 500ms later than the quotes we have (still within same second).
            SystemTime.SetDateTime(todayDuringMarketOpen.Add(new TimeSpan(0, 0, 0, 0, 500)));

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            
            var quotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id).ToList();
            var existingQuoteDates = new HashSet<DateTime>(companyQuotes.Select(q => q.Date));
            var newQuotes = quotesAfter.Where(q => !existingQuoteDates.Contains(q.Date));

            // Assert
            Assert.IsTrue(newQuotes.Count() == 0);
            Assert.IsTrue(existingQuoteDates.Count() == quotesAfter.Count());
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_DuringMarketHours_DownloadsOnlyLatestMinuteQuotes()
        {
            // Arrange
            var todayDuringMarketOpen = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0, 500);

            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 0, todayDuringMarketOpen.TimeOfDay);
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);

            var addedSystemTime = new TimeSpan(2, 0, 0);
            SystemTime.SetDateTime(todayDuringMarketOpen.Add(addedSystemTime));

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var maxInitialMinuteQuoteDate = companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Minute).Select(cq => cq.Date).Max();
            var minuteQuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id && q.TypeId == (int)QuoteTypeEnum.Minute).ToList();
            var existingMinuteQuoteDates = new HashSet<DateTime>(companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Minute).Select(q => q.Date));
            var newMinuteQuotes = minuteQuotesAfter.Where(q => !existingMinuteQuoteDates.Contains(q.Date));

            // Assert
            Assert.IsTrue(newMinuteQuotes.Count() == addedSystemTime.TotalMinutes);
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_DuringMarketHours_DownloadsOnlyNewDayQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 1, _marketClose);
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);
            
            var todayDuringMarketOpen = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0, 500);
            SystemTime.SetDateTime(todayDuringMarketOpen);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var maxInitialDayQuoteDate = companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Day).Select(cq => cq.Date).Max();
            var dayQuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id && q.TypeId == (int)QuoteTypeEnum.Day).ToList();
            var existingDayQuoteDates = new HashSet<DateTime>(companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Day).Select(q => q.Date));
            var newDayQuotes = dayQuotesAfter.Where(q => !existingDayQuoteDates.Contains(q.Date));

            // Assert
            Assert.IsTrue(newDayQuotes.Count() == 1);
            Assert.IsTrue(dayQuotesAfter.Count() == existingDayQuoteDates.Count() + 1);
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_AfterMarketCloses_DownloadsOnlyMissingMinuteQuotes()
        {
            // Arrange
            var timeUntilClose = new TimeSpan(2, 0, 0);

            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 0, _marketClose.Subtract(timeUntilClose));
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);
            
            SystemTime.SetDateTime(_todayDateAfterMarketClose);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var maxInitialMinuteQuoteDate = companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Minute).Select(cq => cq.Date).Max();
            var minuteQuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id && q.TypeId == (int)QuoteTypeEnum.Minute).ToList();
            var existingMinuteQuoteDates = new HashSet<DateTime>(companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Minute).Select(q => q.Date));
            var newMinuteQuotes = minuteQuotesAfter.Where(q => !existingMinuteQuoteDates.Contains(q.Date));

            // Assert
            Assert.IsTrue(newMinuteQuotes.Count() == timeUntilClose.TotalMinutes);
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_AfterMarketCloses_DownloadsOnlyMissingDayQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 1, _marketClose);
            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);
            
            SystemTime.SetDateTime(_todayDateAfterMarketClose);

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var maxInitialDayQuoteDate = companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Day).Select(cq => cq.Date).Max();
            var dayQuotesAfter = _quoteService.GetQuotes().Where(q => q.CompanyId == company.Id && q.TypeId == (int)QuoteTypeEnum.Day).ToList();
            var existingDayQuoteDates = new HashSet<DateTime>(companyQuotes.Where(q => q.TypeId == (int)QuoteTypeEnum.Day).Select(q => q.Date));
            var newDayQuotes = dayQuotesAfter.Where(q => !existingDayQuoteDates.Contains(q.Date));

            // Assert
            Assert.IsTrue(newDayQuotes.Count() == 1);
            Assert.IsTrue(dayQuotesAfter.Count() == existingDayQuoteDates.Count() + 1);
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithInvalidExistingQuoteAndInvalidNewQuote_DoesntUpdateQuote()
        {
            // Arrange
            var todayDuringMarketOpen = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);

            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 0, todayDuringMarketOpen.TimeOfDay).ToList();

            // Invalidate one of the quotes
            var quoteBeforeUpdate = companyQuotes.First(q => q.QuoteType == QuoteTypeEnum.Minute);
            var highBefore = quoteBeforeUpdate.High;
            var lowBefore = quoteBeforeUpdate.Low;
            var openBefore = quoteBeforeUpdate.Open;
            var closeBefore = quoteBeforeUpdate.Close;
            var volumeBefore = quoteBeforeUpdate.Volume;

            companyQuotes.Remove(quoteBeforeUpdate);
            quoteBeforeUpdate.IsValid = false;
            companyQuotes.Add(quoteBeforeUpdate);

            company.Quotes = companyQuotes.ToList<Quote>();
            
            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);

            SystemTime.SetDateTime(todayDuringMarketOpen.AddHours(2));

            // Update quotes builder so that is creates invalid quotes.
            FakeQuotesBuilder.CreatesValidQuotes = false;

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();
            
            var updatedQuote = _quoteService.FindQuote(quoteBeforeUpdate.Id);

            // Assert
            Assert.IsTrue(updatedQuote.High == highBefore);
            Assert.IsTrue(updatedQuote.Low == lowBefore);
            Assert.IsTrue(updatedQuote.Open == openBefore);
            Assert.IsTrue(updatedQuote.Close == closeBefore);
            Assert.IsTrue(updatedQuote.Volume == volumeBefore);
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithInvalidExistingQuoteAndValidNewQuote_UpdatesQuote()
        {
            // Arrange
            var todayDuringMarketOpen = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);

            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 0, todayDuringMarketOpen.TimeOfDay).ToList();

            // Invalidate one of the quotes
            var quoteBeforeUpdate = companyQuotes.First(q => q.QuoteType == QuoteTypeEnum.Minute);
            var highBefore = quoteBeforeUpdate.High;
            var lowBefore = quoteBeforeUpdate.Low;
            var openBefore = quoteBeforeUpdate.Open;
            var closeBefore = quoteBeforeUpdate.Close;
            var volumeBefore = quoteBeforeUpdate.Volume;
            
            companyQuotes.Remove(quoteBeforeUpdate);
            quoteBeforeUpdate.IsValid = false;
            companyQuotes.Add(quoteBeforeUpdate);

            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);

            SystemTime.SetDateTime(todayDuringMarketOpen.AddHours(2));

            // Update quotes builder so that is creates invalid quotes.
            FakeQuotesBuilder.CreatesValidQuotes = true;

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var updatedQuote = _quoteService.FindQuote(quoteBeforeUpdate.Id);

            // Assert
            Assert.IsTrue(updatedQuote.High != highBefore);
            Assert.IsTrue(updatedQuote.Low != lowBefore);
            Assert.IsTrue(updatedQuote.Open != openBefore);
            Assert.IsTrue(updatedQuote.Close != closeBefore);
            Assert.IsTrue(updatedQuote.Volume != volumeBefore);
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidExistingQuoteAndInvalidNewQuote_DoesntUpdateQuote()
        {
            // Arrange
            var todayDuringMarketOpen = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);

            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 0, todayDuringMarketOpen.TimeOfDay).ToList();

            // Invalidate one of the quotes
            var quoteBeforeUpdate = companyQuotes.First(q => q.QuoteType == QuoteTypeEnum.Minute);
            var highBefore = quoteBeforeUpdate.High;
            var lowBefore = quoteBeforeUpdate.Low;
            var openBefore = quoteBeforeUpdate.Open;
            var closeBefore = quoteBeforeUpdate.Close;
            var volumeBefore = quoteBeforeUpdate.Volume;

            companyQuotes.Remove(quoteBeforeUpdate);
            quoteBeforeUpdate.IsValid = true;
            companyQuotes.Add(quoteBeforeUpdate);

            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);

            SystemTime.SetDateTime(todayDuringMarketOpen.AddHours(2));

            // Update quotes builder so that is creates invalid quotes.
            FakeQuotesBuilder.CreatesValidQuotes = false;

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var updatedQuote = _quoteService.FindQuote(quoteBeforeUpdate.Id);

            // Assert
            Assert.IsTrue(updatedQuote.High == highBefore);
            Assert.IsTrue(updatedQuote.Low == lowBefore);
            Assert.IsTrue(updatedQuote.Open == openBefore);
            Assert.IsTrue(updatedQuote.Close == closeBefore);
            Assert.IsTrue(updatedQuote.Volume == volumeBefore);
        }

        [TestMethod]
        public void UpdateAllCompaniesWithLatestQuotesAsync_WithValidExistingQuoteAndValidNewQuote_DoesntUpdateQuote()
        {
            // Arrange
            var todayDuringMarketOpen = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 0, 0);

            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var companyQuotes = FakeQuotesBuilder.CreateFakeDayMinuteQuotes(company, 0, todayDuringMarketOpen.TimeOfDay).ToList();

            // Invalidate one of the quotes
            var quoteBeforeUpdate = companyQuotes.First(q => q.QuoteType == QuoteTypeEnum.Minute);
            var highBefore = quoteBeforeUpdate.High;
            var lowBefore = quoteBeforeUpdate.Low;
            var openBefore = quoteBeforeUpdate.Open;
            var closeBefore = quoteBeforeUpdate.Close;
            var volumeBefore = quoteBeforeUpdate.Volume;

            companyQuotes.Remove(quoteBeforeUpdate);
            quoteBeforeUpdate.IsValid = true;
            companyQuotes.Add(quoteBeforeUpdate);

            company.Quotes = companyQuotes.ToList<Quote>();

            _quoteService.AddRange(companyQuotes);
            _companyService.Add(company);

            SystemTime.SetDateTime(todayDuringMarketOpen.AddHours(2));

            // Update quotes builder so that is creates invalid quotes.
            FakeQuotesBuilder.CreatesValidQuotes = true;

            // Act
            _marketService.UpdateAllCompaniesWithLatestQuotesAsync().Wait();

            var updatedQuote = _quoteService.FindQuote(quoteBeforeUpdate.Id);

            // Assert
            Assert.IsTrue(updatedQuote.High == highBefore);
            Assert.IsTrue(updatedQuote.Low == lowBefore);
            Assert.IsTrue(updatedQuote.Open == openBefore);
            Assert.IsTrue(updatedQuote.Close == closeBefore);
            Assert.IsTrue(updatedQuote.Volume == volumeBefore);
        }

        #endregion
        #region Testing Task<IEnumerable<Q>> GetDayQuotesForCompanyAsync(string tickerSymbol, DateTime startDate)

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void GetDayQuotesForCompanyAsync_WithDisposedService_ThrowsException()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            _marketService.Dispose();
            _marketService.GetDayQuotesForCompanyAsync(company.Symbol, DateTime.Now).Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void GetDayQuotesForCompanyAsync_WithNullTickerSymbol_ThrowsException()
        {
            // Act
            _marketService.GetDayQuotesForCompanyAsync(null, DateTime.Now).Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void GetDayQuotesForCompanyAsync_WithInvalidFutureDate_ThrowsException()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            _marketService.GetDayQuotesForCompanyAsync(company.Symbol, DateTime.Now.AddDays(1)).Wait();
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_DownloadsMaxQuotesForCompany()
        {
            // Arrange
            var startDate = DateTime.Now.AddYears(-3);
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            _companyService.Add(company);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddYears(-2)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads2YearQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddYears(-2).AddDays(-1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;
            
            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddYears(-2)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads2YearQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddYears(-2);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddYears(-2)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads2YearQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddYears(-2).AddDays(1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddYears(-2)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads2YearQuotesForCompany4()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddYears(-1).AddDays(-1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddYears(-2)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads1YearQuotesForCompany()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddYears(-1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddYears(-1)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads1YearQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddYears(-1).AddDays(1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddYears(-1)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads1YearQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;
            
            var startDate = DateTime.Now.AddMonths(-5).AddDays(-1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddYears(-1)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads5MonthQuotesForCompany0()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddMonths(-5);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddMonths(-5)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads5MonthQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddMonths(-5).AddDays(1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddMonths(-5)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads5MonthQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddMonths(-3).AddDays(-1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddMonths(-5)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads3MonthQuotesForCompany0()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddMonths(-3);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddMonths(-3)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads3MonthQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddMonths(-3).AddDays(1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddMonths(-3)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads3MonthQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddMonths(-1).AddDays(-1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddMonths(-3)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads1MonthQuotesForCompany0()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddMonths(-1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddMonths(-1)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads1MonthQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddMonths(-1).AddDays(1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddMonths(-1)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads1MonthQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddDays(-6);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddMonths(-1)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads5DaysQuotesForCompany0()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddDays(-5);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddDays(-5)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads5DaysQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddDays(-4);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddDays(-5)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads5DaysQuotesForCompany2()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddDays(-2);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddDays(-5)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads5DaysQuotesForCompany3()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddDays(-2);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddDays(-5)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidStartDate_Downloads1DaysQuotesForCompany1()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            company.RetrieveQuotesFlag = true;

            var startDate = DateTime.Now.AddDays(-1);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;

            // Assert
            Assert.IsTrue(downloadedQuotes.Count() == DaysUntil(DateTime.Now.AddDays(-1)));
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithInvalidCompany_StoresNullCompanyInReturnedQuotes()
        {
            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync("GOOG", DateTime.Now.AddDays(-1)).Result;
            var firstQuote = downloadedQuotes.FirstOrDefault();

            // Assert
            Assert.IsNotNull(firstQuote);
            Assert.IsNull(firstQuote.Company);
        }

        [TestMethod]
        public void GetDayQuotesForCompanyAsync_WithValidCompany_StoresCompanyInReturnedQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            var startDate = DateTime.Now.AddDays(-2);
            var daysToSkip = DaysUntil(startDate);
            var fakequotes = FakeQuotesBuilder.CreateFakeDayQuotes(company, 1, daysToSkip).ToList();

            company.Quotes = fakequotes.Cast<Quote>().ToList();

            _companyService.Add(company);
            _quoteService.AddRange(fakequotes);

            // Act
            var downloadedQuotes = _marketService.GetDayQuotesForCompanyAsync(company.Symbol, startDate).Result;
            var firstQuote = downloadedQuotes.FirstOrDefault();

            // Assert
            Assert.IsNotNull(firstQuote);
            Assert.IsTrue(firstQuote.Company == company);
        }

        #endregion
        #region Testing Task<IEnumerable<Q>> GetMinuteQuotesForCompanyAsync(string tickerSymbol)

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void GetMinuteQuotesForCompanyAsync_WithDisposedService_ThrowsException()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            // Act
            _marketService.Dispose();
            _marketService.GetMinuteQuotesForCompanyAsync(company.Symbol).Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void GetMinuteQuotesForCompanyAsync_WithNullTickerSymbol_ThrowsException()
        {
            // Act
            _marketService.GetMinuteQuotesForCompanyAsync(null).Wait();
        }

        [TestMethod]
        [ExpectedException(typeof(AggregateException))]
        public void GetMinuteQuotesForCompanyAsync_WithEmptyTickerSymbol_ThrowsException()
        {
            // Act
            _marketService.GetMinuteQuotesForCompanyAsync(string.Empty).Wait();
        }

        [TestMethod]
        public void GetMinuteQuotesForCompanyAsync_BeforeMarketOpen_ReturnsEmpty()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            
            SystemTime.SetDateTime(_todayDateBeforeMarketOpen);

            // Act
            var downloadTask = _marketService.GetMinuteQuotesForCompanyAsync(company.Symbol);
            downloadTask.Wait();

            var quotes = downloadTask.Result.ToList();

            // Assert
            Assert.IsNotNull(quotes);
            Assert.IsTrue(quotes.Count() == 0);
        }

        [TestMethod]
        public void GetMinuteQuotesForCompanyAsync_DuringMarketHours_ReturnsCorrectNumberOfQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            var todayDuringMarketHours = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 30, 0);

            SystemTime.SetDateTime(todayDuringMarketHours);

            // Act
            var downloadTask = _marketService.GetMinuteQuotesForCompanyAsync(company.Symbol);
            downloadTask.Wait();

            var quotes = downloadTask.Result.ToList();
            var expectedNumberOfQuotes = (int)(todayDuringMarketHours.TimeOfDay - _marketOpen).TotalMinutes + 1;

            // Assert
            Assert.IsNotNull(quotes);
            Assert.IsTrue(quotes.Count() == expectedNumberOfQuotes);
        }

        [TestMethod]
        public void GetMinuteQuotesForCompanyAsync_AfterMarketHours_ReturnsFullDayOfQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();

            SystemTime.SetDateTime(_todayDateAfterMarketClose);

            // Act
            var downloadTask = _marketService.GetMinuteQuotesForCompanyAsync(company.Symbol);
            downloadTask.Wait();

            var quotes = downloadTask.Result.ToList();
            var expectedNumberOfQuotes = (int)(_marketClose - _marketOpen).TotalMinutes + 1;

            // Assert
            Assert.IsNotNull(quotes);
            Assert.IsTrue(quotes.Count() == expectedNumberOfQuotes);
        }

        [TestMethod]
        public void GetMinuteQuotesForCompanyAsync_WithValidTickerSymbol_ReturnsMinuteQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            
            var todayDateDuringMarketHours = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 12, 30, 0);
            SystemTime.SetDateTime(todayDateDuringMarketHours);

            // Act
            var downloadTask = _marketService.GetMinuteQuotesForCompanyAsync(company.Symbol);
            downloadTask.Wait();

            var quotes = downloadTask.Result.ToList();
            var expectedNumQuotes = SystemTime.Now().TimeOfDay > _marketOpen ?
                                                Math.Min((int)(SystemTime.Now().TimeOfDay - _marketOpen).TotalMinutes, (int)(_marketClose - _marketOpen).TotalMinutes) + 1 :
                                                0;
            // Assert
            Assert.IsNotNull(quotes);
            Assert.IsTrue(quotes.Count() == expectedNumQuotes);
            Assert.IsTrue(quotes.First().QuoteType == QuoteTypeEnum.Minute);
        }

        [TestMethod]
        public void GetMinuteQuotesForCompanyAsync_WithInvalidCompany_StoresNullCompanyInReturnedQuotes()
        {
            // Act
            var downloadedQuotes = _marketService.GetMinuteQuotesForCompanyAsync("GOOG").Result;
            var firstQuote = downloadedQuotes.FirstOrDefault();

            // Assert
            Assert.IsNotNull(firstQuote);
            Assert.IsNull(firstQuote.Company);
        }

        [TestMethod]
        public void GetMinuteQuotesForCompanyAsync_WithValidCompany_StoresCompanyInReturnedQuotes()
        {
            // Arrange
            var company = FakeCompaniesBuilder.CreateFakeCompanyAAPL();
            _companyService.Add(company);

            // Act
            var downloadedQuotes = _marketService.GetMinuteQuotesForCompanyAsync(company.Symbol).Result;
            var firstQuote = downloadedQuotes.FirstOrDefault();

            // Assert
            Assert.IsNotNull(firstQuote);
            Assert.IsTrue(firstQuote.Company == company);
        }

        #endregion
        #region Testing void Dispose(bool disposing)

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
