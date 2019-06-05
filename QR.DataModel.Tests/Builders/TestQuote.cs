using System;
using Framework.Generic.Tests.Builders;
using System.Data.Entity;
using QR.DataModel;
using System.Diagnostics.CodeAnalysis;

namespace QA.DataModel.Tests.Builders
{
    [ExcludeFromCodeCoverage]
    public class TestQuote : Quote, ITestEntity
    {
        public Guid TestId { get; private set; }
        public int StoredValue { get; set; }
        public int CurrentValue { get; set; }
        public EntityState State { get; set; }
        public bool IsVirtual { get; set; }
    }
}
