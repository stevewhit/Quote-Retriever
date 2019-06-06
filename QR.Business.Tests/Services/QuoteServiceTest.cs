using Framework.Generic.EntityFramework;
using Framework.Generic.Tests.Builders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QR.Business.Services;
using QR.Business.Tests.Builders;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;

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
        #region Testing IDbSet<T> GetQuotes()

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void GetQuotes_WithDisposedRepository_ThrowsException()
        {
            // Arrange
            _service.Dispose();

            // Act
            var quotes = _service.GetQuotes();
        }

        [TestMethod]
        public void GetQuotes_WithValidRepository_ReturnsQuotes()
        {
            // Arrange
            var entityToAdd = new TestQuote(999);
            _repository.Create(entityToAdd);
            
            // Act
            var quotes = _service.GetQuotes();

            // Assert
            Assert.IsNotNull(quotes);
            Assert.IsTrue(quotes.Count() == 1);
        }

        #endregion
        #region Testing T FindQuote(int id)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void FindQuote_WithDisposedRepository_ThrowsException()
        {
            // Arrange
            _service.Dispose();

            // Act
            var quote = _service.FindQuote(1);
        }

        [TestMethod]
        public void FindQuotes_WithValidId_ReturnsQuote()
        {
            // Arrange
            var id = 123;
            var entityToAdd = new TestQuote()
            {
                Id = id
            };
            
            _repository.Create(entityToAdd);

            // Act
            var quote = _service.FindQuote(id);

            // Assert
            Assert.IsNotNull(quote);
            Assert.IsTrue(quote == entityToAdd);
        }

        [TestMethod]
        public void FindQuotes_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var entityToAdd = new TestQuote()
            {
                Id = 123
            };

            _repository.Create(entityToAdd);

            // Act
            var quote = _service.FindQuote(234);

            // Assert
            Assert.IsNull(quote);
        }

        #endregion
    }
}
