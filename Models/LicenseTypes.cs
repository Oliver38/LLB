using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class LicenseTypes
    {
        [Key]
        public string Id { get; set; }
        public string LicenseTypeNameId { get; set; }
        public string DescriptionId { get; set; }
        public string Status { get; set; }
        public double FeeId { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdates { get; set; }
        
    }
}
