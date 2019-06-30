using Framework.Generic.EntityFramework;
using System;
using System.Data.Entity;
using StockMarket.DataModel;
using System.Linq;

namespace QR.Business.Services
{
    public interface ICompanyService<T> : IDisposable where T : Company
    {
        /// <summary>
        /// Returns stored companies.
        /// </summary>
        /// <returns>Returns companies stored in the repository.</returns>
        IDbSet<T> GetCompanies();

        /// <summary>
        /// Finds and returns the company with the matching id.
        /// </summary>
        /// <param name="id">The id of the company to return.</param>
        /// <returns>Returns the company with the matching id.</returns>
        T FindCompany(int id);

        /// <summary>
        /// Adds the supplied <paramref name="company"/>.
        /// </summary>
        /// <param name="company">The company that is to be added.</param>
        void Add(T company);

        /// <summary>
        /// Updates the supplied <paramref name="company"/>.
        /// </summary>
        /// <param name="company">The company that is to be updated.</param>
        void Update(T company);

        /// <summary>
        /// Finds and deletes an existing company by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id of company to be deleted.</param>
        void Delete(int id);

        /// <summary>
        /// Deletes the supplied <paramref name="company"/>.
        /// </summary>
        /// <param name="company">The company that is to be deleted.</param>
        void Delete(T company);
    }

    public class CompanyService<T> : ICompanyService<T> where T : Company
    {
        private readonly IEfRepository<T> _repository;
        private bool _isDisposed = false;

        public CompanyService(IEfRepository<T> repository)
        {
            _repository = repository ?? throw new ArgumentNullException("repository");
        }

        #region ICompanyService<C>

        /// <summary>
        /// Returns stored companies.
        /// </summary>
        /// <returns>Returns companies stored in the repository.</returns>
        public IDbSet<T> GetCompanies()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("CompanyService", "The service has been disposed.");

            return _repository.GetEntities();
        }

        /// <summary>
        /// Finds and returns the company with the matching id.
        /// </summary>
        /// <param name="id">The id of the company to return.</param>
        /// <returns>Returns the company with the matching id.</returns>
        public T FindCompany(int id)
        {
            return GetCompanies().FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Adds the supplied <paramref name="company"/>.
        /// </summary>
        /// <param name="company">The company that is to be added.</param>
        public void Add(T company)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("CompanyService", "The service has been disposed.");

            if (company == null)
                throw new ArgumentNullException("company");

            _repository.Add(company);
            _repository.SaveChanges();
        }

        /// <summary>
        /// Updates the supplied <paramref name="company"/>.
        /// </summary>
        /// <param name="company">The company that is to be updated.</param>
        public void Update(T company)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("CompanyService", "The service has been disposed.");

            if (company == null)
                throw new ArgumentNullException("company");

            _repository.Update(company);
            _repository.SaveChanges();
        }

        /// <summary>
        /// Finds and deletes an existing company by <paramref name="id"/>.
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
        /// Deletes the supplied <paramref name="company"/>.
        /// </summary>
        /// <param name="company">The company that is to be deleted.</param>
        public void Delete(T company)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("CompanyService", "The service has been disposed.");

            if (company == null)
                throw new ArgumentNullException("company");

            _repository.Delete(company);
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
