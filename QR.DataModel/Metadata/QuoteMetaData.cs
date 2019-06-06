using System;
using System.ComponentModel.DataAnnotations;
using Framework.Generic.Utility;

namespace QR.DataModel
{
    [MetadataType(typeof(QuoteMetaData))]
    [Serializable]
    public partial class Quote : ICloneable
    {
        public Quote Clone()
        {
            return this.CopyObject();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
    
    public class QuoteMetaData
    {
        [Key]
        [Range(0, int.MaxValue)]
        public int Id { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int CompanyId { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString="{0:MMM/dd/yyyy HH:mm}")]
        public DateTime Date { get; set; }

        [Range(0.0, int.MaxValue)]
        [DataType(DataType.Currency)]
        public Nullable<decimal> Open { get; set; }

        [Required]
        [Range(0.0, int.MaxValue)]
        [DataType(DataType.Currency)]
        public decimal Close { get; set; }

        [Range(0.0, int.MaxValue)]
        [DataType(DataType.Currency)]
        public Nullable<decimal> High { get; set; }

        [Range(0.0, int.MaxValue)]
        [DataType(DataType.Currency)]
        public Nullable<decimal> Low { get; set; }

        [Range(0.0, long.MaxValue)]
        [DataType(DataType.Currency)]
        public Nullable<long> Volume { get; set; }

        public virtual Company Company { get; set; }
    }
}
