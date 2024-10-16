using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class PaymentStatus
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? Status { get; set; }  
        public decimal? Amount { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseArea { get; set; }
        public string? ApplicationRefNum { get; set; }
        public string? PaymentId { get; set; }


        public string? PopDoc { get; set; }
        public DateTime? DatePaid { get; set; }




    }
}
