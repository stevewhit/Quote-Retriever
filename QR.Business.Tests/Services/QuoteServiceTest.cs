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

        [TestMethod]
        public void Fail()
        {
            Assert.Fail();
        }
    }
}
