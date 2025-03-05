using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class OutletInfo
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? UserId { get; set; }
        public string? TradingName { get; set; }
        public string? Province { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? DirectorNames { get; set; }
        public string? Status { get; set; }
        public string? ApplicationType { get; set; }
        //public string BusinessName { get; set; }
        public string? LicenseTypeID { get; set; }
        public string? Council { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }



    }
}
