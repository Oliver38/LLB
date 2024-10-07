using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class DistrictCodes
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? DistrictCode { get; set; }
       



    }
}
