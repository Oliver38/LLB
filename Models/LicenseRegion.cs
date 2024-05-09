
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace LLB.Models
{
    public class LicenseRegion
    {
        [Key]
        public string Id { get; set; }

       public string  RegionName { get; set; }
        public string Description { get; set; }
        public string status { get; set; }
        public DateTime DateAdded { get; set; }
        


    }
}
