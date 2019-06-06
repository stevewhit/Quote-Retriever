﻿using Framework.Generic.Tests.Builders;
using QR.DataModel;
using System;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;

namespace QR.Business.Tests.Builders
{
    [ExcludeFromCodeCoverage]
    public class TestCompany : Company, ITestEntity
    {
        public Guid TestId { get; private set; }
        public int StoredValue { get; set; }
        public int CurrentValue { get; set; }
        public EntityState State { get; set; }
        public bool IsVirtual { get; set; }
    }
}
