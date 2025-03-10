using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class Renewals
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? UserId { get; set; }
        public DateTime? PreviousExpiry { get; set; }
        public string? FeePaid { get; set; }
        public string? PenaltyPaid { get; set; }
        public string? PaymentStatus { get; set; }
        public string? LLBNumber { get; set; }
        
        public string? Service { get; set; }
        public string? HealthCert { get; set; }
        public string? CertifiedLicense { get; set; }
        public string? Status { get; set; }
        public string? Verifier { get; set; }
        public string? Inspector { get; set; }
        public DateTime DateApplied { get; set; }
        public DateTime DateUpdated { get; set; }



    }
}
