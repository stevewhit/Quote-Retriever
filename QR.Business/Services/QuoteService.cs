﻿using Framework.Generic.EntityFramework;
using System;
using System.Data.Entity;
using QR.DataModel;
using System.Linq;

namespace QR.Business.Services
{
    public interface IQuoteService<T> : IDisposable where T : Quote
    {
        IDbSet<T> GetQuotes();
        T FindQuote(int id);
        void Add(T quote);        
        void Update(T quote);
        void Delete(int id);
        void Delete(T quote);
    }

    public class QuoteService<T> : IQuoteService<T> where T : Quote
    {
        private IEfRepository<T> _repository;
        private bool _isDisposed = false;

        public QuoteService(IEfRepository<T> repository)
        {
            _repository = repository;
        }
                
        /// <summary>
        /// Returns quotes stored in the repository.
        /// </summary>
        /// <returns>Returns quotes stored in the repository.</returns>
        public IDbSet<T> GetQuotes()
        {
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
            _repository.Create(quote);
            _repository.SaveChanges();
        }
        
        /// <summary>
        /// Updates the supplied <paramref name="quote"/> in the repository.
        /// </summary>
        /// <param name="quote">The quote that is to be updated.</param>
        public void Update(T quote)
        {
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
            _repository.Delete(quote);
            _repository.SaveChanges();
        }

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
                    _repository = null;
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
    }
}
