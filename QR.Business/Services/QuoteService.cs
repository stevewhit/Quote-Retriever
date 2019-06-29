using Framework.Generic.EntityFramework;
using System;
using System.Data.Entity;
using StockMarket.DataModel;
using System.Linq;
using System.Collections.Generic;

namespace QR.Business.Services
{
    public interface IQuoteService<T> : IDisposable where T : Quote
    {
        IDbSet<T> GetQuotes();
        T FindQuote(int id);
        void Add(T quote);
        void Add(IEnumerable<T> quotes);
        void Update(T quote);
        void Delete(int id);
        void Delete(T quote);
    }

    public class QuoteService<T> : IQuoteService<T> where T : Quote
    {
        private readonly IEfRepository<T> _repository;
        private bool _isDisposed = false;

        public QuoteService(IEfRepository<T> repository)
        {
            _repository = repository ?? throw new ArgumentNullException("repository");
        }

        #region IQuoteService<T>

        /// <summary>
        /// Returns quotes stored in the repository.
        /// </summary>
        /// <returns>Returns quotes stored in the repository.</returns>
        public IDbSet<T> GetQuotes()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("QuoteService", "The service has been disposed.");

            return _repository.GetEntities();
        }

        /// <summary>
        /// Finds and returns the quote from the repository with the matching id.
        /// </summary>
        /// <param name="id">The id of the quote to return.</param>
        /// <returns>Returns the quote with the matching id.</returns>
        public T FindQuote(int id)
        {
            return GetQuotes().FirstOrDefault(c => c.Id == id);
        }
        
        /// <summary>
        /// Adds the supplied <paramref name="quote"/> to the repository.
        /// </summary>
        /// <param name="quote">The quote that is to be added.</param>
        public void Add(T quote)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("QuoteService", "The service has been disposed.");

            if (quote == null)
                throw new ArgumentNullException("quote");
            
            _repository.Create(quote);
            _repository.SaveChanges();
        }

        /// <summary>
        /// Adds the supplied <paramref name="quotes"/> to the repository.
        /// </summary>
        /// <param name="quotes">The quotes that are to be added.</param>
        public void Add(IEnumerable<T> quotes)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("QuoteService", "The service has been disposed.");

            if (quotes == null)
                throw new ArgumentNullException("quotes");

            foreach (var quote in quotes)
            {
                if (quote == null)
                    throw new ArgumentNullException("quote");

                _repository.Create(quote);
            }
            
            _repository.SaveChanges();
        }

        /// <summary>
        /// Updates the supplied <paramref name="quote"/> in the repository.
        /// </summary>
        /// <param name="quote">The quote that is to be updated.</param>
        public void Update(T quote)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("QuoteService", "The service has been disposed.");

            if (quote == null)
                throw new ArgumentNullException("quote");
            
            _repository.Update(quote);
            _repository.SaveChanges();
        }

        /// <summary>
        /// Finds and deletes an existing quote in the repository by using its <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id of quote to be deleted.</param>
        public void Delete(int id)
        {
            var quote = FindQuote(id);

            if (quote == null)
                throw new ArgumentException($"A quote with the supplied id doesn't exist: {id}.");

            Delete(quote);
        }

        /// <summary>
        /// Deletes the supplied <paramref name="quote"/> from the repository.
        /// </summary>
        /// <param name="quote">The quote that is to be deleted.</param>
        public void Delete(T quote)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("QuoteService", "The service has been disposed.");

            if (quote == null)
                throw new ArgumentNullException("quote");

            _repository.Delete(quote);
            _repository.SaveChanges();
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
                    _repository.Dispose();
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
