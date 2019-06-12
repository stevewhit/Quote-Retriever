using Framework.Generic.Tests.Builders;
using StockMarket.DataModel;
using System;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;

namespace QR.Business.Tests.Builders
{
    [ExcludeFromCodeCoverage]
    public class TestQuote : Quote, ITestEntity
    {
        public Guid TestId { get; private set; }
        public int StoredValue { get; set; }
        public int CurrentValue { get; set; }
        public EntityState State { get; set; }
        public bool IsVirtual { get; set; }

        public TestQuote() : this(0, false) { }
        public TestQuote(bool isVirtual = false)
        {
            TestId = Guid.NewGuid();
            State = EntityState.Unchanged;
            IsVirtual = isVirtual;
        }

        public TestQuote(int value, bool isVirtual = false) : this(isVirtual)
        {
            StoredValue = value;
            CurrentValue = value;
        }
    }
}
