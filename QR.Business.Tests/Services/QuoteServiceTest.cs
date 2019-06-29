using Framework.Generic.EntityFramework;
using Framework.Generic.Tests.Builders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QR.Business.Services;
using StockMarket.DataModel.Test.Builders;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using System.Collections.Generic;

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
        public void GetQuotes_AfterDisposed_ThrowsException()
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
            var quoteToAdd = new TestQuote(999);
            _repository.Create(quoteToAdd);
            
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
        public void FindQuote_AfterDisposed_ThrowsException()
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
            var quoteToAdd = new TestQuote()
            {
                Id = id
            };
            
            _repository.Create(quoteToAdd);

            // Act
            var quote = _service.FindQuote(id);

            // Assert
            Assert.IsNotNull(quote);
            Assert.IsTrue(quote == quoteToAdd);
        }

        [TestMethod]
        public void FindQuotes_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var quoteToAdd = new TestQuote()
            {
                Id = 123
            };

            _repository.Create(quoteToAdd);

            // Act
            var quote = _service.FindQuote(234);

            // Assert
            Assert.IsNull(quote);
        }

        #endregion
        #region Testing void Add(T quote)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Add_Quote_AfterDisposed_ThrowsException()
        {
            // Arrange
            var quoteToAdd = new TestQuote()
            {
                Id = 123
            };

            _service.Dispose();

            // Act
            _service.Add(quoteToAdd);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_Quote_WithNullQuote_ThrowsException()
        {
            // Act
            _service.Add(quote:null);
        }
        
        [TestMethod]
        public void Add_Quote_WithValidQuote_AddsQuoteToRepository()
        {
            // Arrange
            var quoteToAdd = new TestQuote()
            {
                Id = 123
            };

            // Act
            _service.Add(quoteToAdd);

            var addedQuote = _service.FindQuote(quoteToAdd.Id);

            // Assert
            Assert.IsTrue(addedQuote == quoteToAdd);
        }

        #endregion
        #region Testing void Add(IEnumerable<T> quotes)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Add_Quotes_AfterDisposed_ThrowsException()
        {
            // Arrange
            var quotesToAdd = new List<TestQuote>
            {
                new TestQuote()
            };
            
            _service.Dispose();

            // Act
            _service.Add(quotesToAdd);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_Quotes_WithNullQuotes_ThrowsException()
        {
            // Act
            _service.Add(quotes:null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_Quotes_ContainingNullQuote_ThrowsException()
        {
            // Arrange
            var quote1 = new TestQuote();
            var quote2 = new TestQuote();
            var quote3 = new TestQuote();
            TestQuote quote4 = null;

            var quotesToAdd = new List<TestQuote>
            {
                quote1, quote2, quote3, quote4
            };

            // Act
            _service.Add(quotesToAdd);
        }

        [TestMethod]
        public void Add_Quotes_ContainingValidQuotes_AddsQuotes()
        {
            // Arrange
            var quote1 = new TestQuote() { Id = 111 };
            var quote2 = new TestQuote() { Id = 222 };
            var quote3 = new TestQuote() { Id = 333 };
            var quote4 = new TestQuote() { Id = 444 };

            var quotesToAdd = new List<TestQuote>
            {
                quote1, quote2, quote3, quote4
            };

            // Act
            _service.Add(quotesToAdd);

            var addedQuote1 = _service.FindQuote(quote1.Id);
            var addedQuote2 = _service.FindQuote(quote2.Id);
            var addedQuote3 = _service.FindQuote(quote3.Id);
            var addedQuote4 = _service.FindQuote(quote4.Id);

            // Assert
            Assert.IsTrue(addedQuote1 == quote1);
            Assert.IsTrue(addedQuote2 == quote2);
            Assert.IsTrue(addedQuote3 == quote3);
            Assert.IsTrue(addedQuote4 == quote4);
        }

        #endregion
        #region Testing void Update(T quote)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Update_AfterDisposed_ThrowsException()
        {
            // Arrange
            var quoteToUpdate = new TestQuote()
            {
                Id = 123
            };

            _service.Dispose();

            // Act
            _service.Update(quoteToUpdate);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Update_WithNullQuote_ThrowsException()
        {
            // Act
            _service.Update(null);
        }
        
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void Update_WithNonExistingQuote_ThrowsException()
        {
            // Arrange
            var quoteToUpdate = new TestQuote()
            {
                Id = 123
            };

            // Act
            _service.Update(quoteToUpdate);
        }

        [TestMethod]
        public void Update_WithExistingValidQuote_UpdatesQuoteInRepository()
        {
            // Arrange
            var quoteToUpdate = new TestQuote()
            {
                Id = 123
            };

            _service.Add(quoteToUpdate);

            // Act
            quoteToUpdate.Id = 234;
            _service.Update(quoteToUpdate);

            var updatedQuote = _service.FindQuote(quoteToUpdate.Id);

            // Assert
            Assert.IsTrue(quoteToUpdate.Id == 234);
        }

        #endregion
        #region Testing void Delete(int id)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Delete_Id_AfterDisposed_ThrowsException()
        {
            // Arrange
            var quoteToDelete = new TestQuote()
            {
                Id = 123
            };

            _service.Dispose();

            // Act
            _service.Delete(quoteToDelete.Id);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Delete_Id_WithNonExistingQuoteId_ThrowsException()
        {
            // Arrange
            var quoteToDelete = new TestQuote()
            {
                Id = 123
            };

            // Act
            _service.Delete(quoteToDelete.Id);
        }

        [TestMethod]
        public void Delete_Id_WithValidQuoteId_DeletesQuoteInRepository()
        {
            // Arrange
            var quoteToDelete = new TestQuote()
            {
                Id = 123
            };

            _service.Add(quoteToDelete);

            // Act
            _service.Delete(quoteToDelete.Id);

            var deletedQuote = _service.FindQuote(quoteToDelete.Id);

            // Assert
            Assert.IsNull(deletedQuote);
        }

        #endregion
        #region Testing void Delete(T quote)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Delete_T_AfterDisposed_ThrowsException()
        {
            // Arrange
            var quoteToDelete = new TestQuote()
            {
                Id = 123
            };

            _service.Dispose();

            // Act
            _service.Delete(quoteToDelete);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Delete_T_WithNullQuote_ThrowsException()
        {
            // Act
            _service.Delete(null);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void Delete_T_WithNonExistingQuoteId_ThrowsException()
        {
            // Arrange
            var quoteToDelete = new TestQuote()
            {
                Id = 123
            };

            // Act
            _service.Delete(quoteToDelete);
        }

        [TestMethod]
        public void Delete_T_WithValidQuoteId_DeletesQuoteInRepository()
        {
            // Arrange
            var quoteToDelete = new TestQuote()
            {
                Id = 123
            };

            _service.Add(quoteToDelete);

            // Act
            _service.Delete(quoteToDelete);

            var deletedQuote = _service.FindQuote(quoteToDelete.Id);

            // Assert
            Assert.IsNull(deletedQuote);
        }

        #endregion
        #region Testing void Dispose()

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Dispose_DisposesRepository()
        {
            // Act
            _service.Dispose();
            var quotes = _service.GetQuotes();
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Dispose_AfterDisposal_KeepsRepositoryDisposed()
        {
            // Act
            _service.Dispose();
            _service.Dispose();
            var quotes = _service.GetQuotes();
        }
        
        #endregion
    }
}
