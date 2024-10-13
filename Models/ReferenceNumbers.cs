using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class ReferenceNumbers
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public int? Number { get; set; }
        //public string? District { get; set; }
//public string? DistrictCode { get; set; }
       



    }
}
