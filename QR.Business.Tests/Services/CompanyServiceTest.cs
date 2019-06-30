using Framework.Generic.EntityFramework;
using Framework.Generic.Tests.Builders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QR.Business.Services;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using StockMarket.DataModel.Test.Builders;

namespace QR.Business.Tests.Services
{
    /// <summary>
    /// Summary description for CompanyServiceTest
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class CompanyServiceTest
    {
        private MockEfContext _mockContext;
        private IEfRepository<TestCompany> _repository;
        private ICompanyService<TestCompany> _service;

        [TestInitialize]
        public void Initialize()
        {
            _mockContext = new MockEfContext(typeof(TestCompany));
            _repository = new EfRepository<TestCompany>(_mockContext.Object);
            _service = new CompanyService<TestCompany>(_repository);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _mockContext.Object.Dispose();
            _repository.Dispose();
            _service.Dispose();
        }

        #region Testing CompanyService(IEfRepository<T> repository)

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CompanyService_WithNullRepository_ThrowsException()
        {
            // Act
            _service = new CompanyService<TestCompany>(null);
        }

        [TestMethod]
        public void CompanyService_WithValidRepository_StoresRepository()
        {
            // Act
            var companies = _service.GetCompanies();

            // Assert
            Assert.IsNotNull(companies);
        }

        #endregion
        #region Testing IDbSet<T> GetCompanies()

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void GetCompanies_AfterDisposed_ThrowsException()
        {
            // Arrange
            _service.Dispose();

            // Act
            var companies = _service.GetCompanies();
        }

        [TestMethod]
        public void GetCompanies_WithEmptyRepository_ReturnsEmptyEnumerable()
        {
            // Act
            var companies = _service.GetCompanies();

            // Assert
            Assert.IsNotNull(companies);
            Assert.IsFalse(companies.Any());
        }

        [TestMethod]
        public void GetCompanies_WithValidRepository_ReturnsCompanies()
        {
            // Arrange
            var companyToAdd = new TestCompany(999);
            _repository.Add(companyToAdd);

            // Act
            var companies = _service.GetCompanies();

            // Assert
            Assert.IsNotNull(companies);
            Assert.IsTrue(companies.Count() == 1);
        }

        #endregion
        #region Testing T FindCompany(int id)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void FindCompany_AfterDisposed_ThrowsException()
        {
            // Arrange
            _service.Dispose();

            // Act
            var company = _service.FindCompany(1);
        }

        [TestMethod]
        public void FindCompany_WithValidId_ReturnsCompany()
        {
            // Arrange
            var id = 123;
            var companyToAdd = new TestCompany()
            {
                Id = id
            };

            _repository.Add(companyToAdd);

            // Act
            var company = _service.FindCompany(id);

            // Assert
            Assert.IsNotNull(company);
            Assert.IsTrue(company == companyToAdd);
        }

        [TestMethod]
        public void FindCompany_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var companyToAdd = new TestCompany()
            {
                Id = 123
            };

            _repository.Add(companyToAdd);

            // Act
            var company = _service.FindCompany(234);

            // Assert
            Assert.IsNull(company);
        }

        #endregion
        #region Testing void Add(T company)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Add_AfterDisposed_ThrowsException()
        {
            // Arrange
            var companyToAdd = new TestCompany()
            {
                Id = 123
            };

            _service.Dispose();

            // Act
            _service.Add(companyToAdd);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_WithNullCompany_ThrowsException()
        {
            // Act
            _service.Add(null);
        }

        [TestMethod]
        public void Add_WithValidCompany_AddsCompanyToRepository()
        {
            // Arrange
            var companyToAdd = new TestCompany()
            {
                Id = 123
            };

            // Act
            _service.Add(companyToAdd);

            var addedCompany = _service.FindCompany(companyToAdd.Id);

            // Assert
            Assert.IsTrue(addedCompany == companyToAdd);
        }

        #endregion
        #region Testing void Update(T company)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Update_AfterDisposed_ThrowsException()
        {
            // Arrange
            var companyToUpdate = new TestCompany()
            {
                Id = 123
            };

            _service.Dispose();

            // Act
            _service.Update(companyToUpdate);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Update_WithNullCompany_ThrowsException()
        {
            // Act
            _service.Update(null);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void Update_WithNonExistingCompany_ThrowsException()
        {
            // Arrange
            var companyToUpdate = new TestCompany()
            {
                Id = 123
            };

            // Act
            _service.Update(companyToUpdate);
        }

        [TestMethod]
        public void Update_WithExistingValidCompany_UpdatesCompanyInRepository()
        {
            // Arrange
            var companyToUpdate = new TestCompany()
            {
                Id = 123
            };

            _service.Add(companyToUpdate);

            // Act
            companyToUpdate.Id = 234;
            _service.Update(companyToUpdate);

            var updatedCompany = _service.FindCompany(companyToUpdate.Id);

            // Assert
            Assert.IsTrue(companyToUpdate.Id == 234);
        }

        #endregion
        #region Testing void Delete(int id)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Delete_Id_AfterDisposed_ThrowsException()
        {
            // Arrange
            var companyToDelete = new TestCompany()
            {
                Id = 123
            };

            _service.Dispose();

            // Act
            _service.Delete(companyToDelete.Id);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Delete_Id_WithNonExistingCompanyId_ThrowsException()
        {
            // Arrange
            var companyToDelete = new TestCompany()
            {
                Id = 123
            };

            // Act
            _service.Delete(companyToDelete.Id);
        }

        [TestMethod]
        public void Delete_Id_WithValidCompanyId_DeletesCompanyInRepository()
        {
            // Arrange
            var companyToDelete = new TestCompany()
            {
                Id = 123
            };

            _service.Add(companyToDelete);

            // Act
            _service.Delete(companyToDelete.Id);

            var deletedCompany = _service.FindCompany(companyToDelete.Id);

            // Assert
            Assert.IsNull(deletedCompany);
        }

        #endregion
        #region Testing void Delete(T company)

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Delete_T_AfterDisposed_ThrowsException()
        {
            // Arrange
            var companyToDelete = new TestCompany()
            {
                Id = 123
            };

            _service.Dispose();

            // Act
            _service.Delete(companyToDelete);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Delete_T_WithNullCompany_ThrowsException()
        {
            // Act
            _service.Delete(null);
        }

        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void Delete_T_WithNonExistingCompanyId_ThrowsException()
        {
            // Arrange
            var companyToDelete = new TestCompany()
            {
                Id = 123
            };

            // Act
            _service.Delete(companyToDelete);
        }

        [TestMethod]
        public void Delete_T_WithValidCompanyId_DeletesCompanyInRepository()
        {
            // Arrange
            var companyToDelete = new TestCompany()
            {
                Id = 123
            };

            _service.Add(companyToDelete);

            // Act
            _service.Delete(companyToDelete);

            var deletedCompany = _service.FindCompany(companyToDelete.Id);

            // Assert
            Assert.IsNull(deletedCompany);
        }

        #endregion
        #region Testing void Dispose()

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Dispose_DisposesRepository()
        {
            // Act
            _service.Dispose();
            var companies = _service.GetCompanies();
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Dispose_AfterDisposal_KeepsRepositoryDisposed()
        {
            // Act
            _service.Dispose();
            _service.Dispose();
            var companies = _service.GetCompanies();
        }

        #endregion
    }
}
