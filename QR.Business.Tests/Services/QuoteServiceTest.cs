using Framework.Generic.EntityFramework;
using Framework.Generic.Tests.Builders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QR.Business.Services;
using StockMarket.DataModel.Test.Builders;
using System.Diagnostics.CodeAnalysis;
using System;

namespace QR.Business.Tests.Services
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class QuoteServiceTest
    {
        private MockEfContext _mockContext;
        private IEfRepository<TestQuote> _repository;
        private IQuoteService<TestQuote> _service;

        [TestInitialize]
        public void Initialize()
        {
            _mockContext = new MockEfContext(typeof(TestQuote));
            _repository = new EfRepository<TestQuote>(_mockContext.Object);
            _service = new QuoteService<TestQuote>(_repository);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _mockContext.Object.Dispose();
            _repository.Dispose();
            _service.Dispose();
        }
        
        #region Testing QuoteService(IEfRepository<T> repository)

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void QuoteService_WithNullRepository_ThrowsException()
        {
            // Act
            _service = new QuoteService<TestQuote>(null);
        }

        [TestMethod]
        public void QuoteService_WithValidRepository_StoresRepository()
        {
            // Act
            var quotes = _service.GetQuotes();

            // Assert
            Assert.IsNotNull(quotes);
        }

        #endregion
    }
}
