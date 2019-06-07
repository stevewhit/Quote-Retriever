//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace QR.DataModel
{
    using System;
    using System.Collections.Generic;
    
    public partial class Quote
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public System.DateTime Date { get; set; }
        public Nullable<decimal> Open { get; set; }
        public decimal Close { get; set; }
        public Nullable<decimal> High { get; set; }
        public Nullable<decimal> Low { get; set; }
        public Nullable<long> Volume { get; set; }
        public Nullable<System.DateTime> LastModifiedDate { get; set; }
    
        public virtual Company Company { get; set; }
    }
}
