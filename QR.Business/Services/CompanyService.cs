using Framework.Generic.EntityFramework;
using System;
using System.Data.Entity;
using StockMarket.DataModel;
using System.Linq;

namespace QR.Business.Services
{
    public interface ICompanyService<T> : IDisposable where T : Company
    {
        IDbSet<T> GetCompanies();
        IQueryable<T> GetCompaniesForQuoteDownload();
        T FindCompany(int id);
        DateTime? GetMostRecentQuoteDate(T company);
        void Add(T company);
        void Update(T company);
        void Delete(int id);
        void Delete(T company);
    }

    public class CompanyService<T> : ICompanyService<T> where T : Company
    {
        private IEfRepository<T> _repository;
        private bool _isDisposed = false;

        public CompanyService(IEfRepository<T> repository)
        {
            if (repository == null)
                throw new ArgumentNullException("repository");

            _repository = repository;
        }

        /// <summary>
        /// Returns companies stored in the repository.
        /// </summary>
        /// <returns>Returns companies stored in the repository.</returns>
        public IDbSet<T> GetCompanies()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Repository", "The repository has been disposed.");

            return _repository.GetEntities();
        }

        /// <summary>
        /// Returns companies that are available for quote downloading by checking the RetrieveQuotesFlag.
        /// </summary>
        /// <returns>Returns companies that are available for quote downloading.</returns>
        public IQueryable<T> GetCompaniesForQuoteDownload()
        {
            return GetCompanies().Where(c => c.RetrieveQuotesFlag);
        }

        /// <summary>
        /// Finds and returns the company from the repository with the matching id.
        /// </summary>
        /// <param name="id">The id of the company to return.</param>
        /// <returns>Returns the company with the matching id.</returns>
        public T FindCompany(int id)
        {
            return GetCompanies().FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Returns the date of the most recently stored quote for a given <paramref name="company"/>. 
        /// </summary>
        /// <returns>The date of the most recently stored quote for a given <paramref name="company"/>; Otherwise, null.</returns>
        public DateTime? GetMostRecentQuoteDate(T company)
        {
            if (company == null)
                throw new ArgumentNullException("company");

            if (company.Quotes == null)
                throw new InvalidOperationException("The company's quotes haven't been initialized.");

            return company.Quotes.OrderByDescending(q => q.Date).FirstOrDefault()?.Date;
        }

        /// <summary>
        /// Adds the supplied <paramref name="company"/> to the repository.
        /// </summary>
        /// <param name="company">The company that is to be added.</param>
        public void Add(T company)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Repository", "The repository has been disposed.");

            if (company == null)
                throw new ArgumentNullException("company");

            _repository.Create(company);
            _repository.SaveChanges();
        }

        /// <summary>
        /// Updates the supplied <paramref name="company"/> in the repository.
        /// </summary>
        /// <param name="company">The company that is to be updated.</param>
        public void Update(T company)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Repository", "The repository has been disposed.");

            if (company == null)
                throw new ArgumentNullException("company");

            _repository.Update(company);
            _repository.SaveChanges();
        }

        /// <summary>
        /// Finds and deletes an existing company in the repository by using its <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id of company to be deleted.</param>
        public void Delete(int id)
        {
            var company = FindCompany(id);

            if (company == null)
                throw new ArgumentException($"A company with the supplied id doesn't exist: {id}.");

            Delete(company);
        }

        /// <summary>
        /// Deletes the supplied <paramref name="company"/> from the repository.
        /// </summary>
        /// <param name="company">The company that is to be deleted.</param>
        public void Delete(T company)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Repository", "The repository has been disposed.");

            if (company == null)
                throw new ArgumentNullException("company");

            _repository.Delete(company);
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
