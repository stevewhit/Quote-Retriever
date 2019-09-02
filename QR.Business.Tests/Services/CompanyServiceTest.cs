using Framework.Generic.EntityFramework;
using Framework.Generic.Tests.Builders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QR.Business.Services;
using System.Diagnostics.CodeAnalysis;
using System;
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
    }
}
