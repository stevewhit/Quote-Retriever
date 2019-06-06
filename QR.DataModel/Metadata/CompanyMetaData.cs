using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Framework.Generic.Utility;

namespace QR.DataModel
{
    [MetadataType(typeof(CompanyMetaData))]
    [Serializable]
    public partial class Company : ICloneable
    {
        public Company Clone()
        {
            return this.CopyObject();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
    
    public class CompanyMetaData
    {
        [Key]
        [Range(0, int.MaxValue)]
        public int Id { get; set; }

        [StringLength(20)]
        public string Symbol { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        [StringLength(20)]
        public string Exchange { get; set; }
        
        [StringLength(25)]
        public string Industry { get; set; }
        
        [StringLength(50)]
        public string Website { get; set; }
        
        [StringLength(255)]
        public string Description { get; set; }
        
        [StringLength(50)]
        public string Sector { get; set; }
        
        [StringLength(1000)]
        public string Tags { get; set; }

        public virtual ICollection<Quote> Quotes { get; set; }
    }
}
