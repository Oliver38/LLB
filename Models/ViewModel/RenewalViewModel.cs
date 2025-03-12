using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace LLB.Models.ViewModel
{
    public class RenewalViewModel
    {

        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? UserId { get; set; }
        public DateTime? PreviousExpiry { get; set; }
        public string? FeePaid { get; set; }
        public string? PenaltyPaid { get; set; }
        public string? PaymentStatus { get; set; }
        public string? LLBNumber { get; set; }
        public string? TradingName { get; set; }
        public string? Licensetype { get; set; }
        public string? LicenseRegion { get; set; }
        public string? OutletName { get; set; }
        public string? TaskId { get; set; }

        public string? Service { get; set; }
        public string? HealthCert { get; set; }
        public string? CertifiedLicense { get; set; }
        public string? Status { get; set; }
        public string? Verifier { get; set; }
        public DateTime DateApplied { get; set; }
        public DateTime DateUpdated { get; set; }

    }
}
