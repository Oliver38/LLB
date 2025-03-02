using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class Payments
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? UserId { get; set; }
        public string? PaynowRef { get; set; }
        public string? SystemRef { get; set; }
        public string? PollUrl { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal? Amount { get; set; }
        public string? Status { get; set; }
        public string? PopDoc { get; set; }
        public string? Service { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }



    }
}
