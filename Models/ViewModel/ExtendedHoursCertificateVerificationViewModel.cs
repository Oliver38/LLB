namespace LLB.Models.ViewModel
{
    public class ExtendedHoursCertificateVerificationViewModel
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CertificateReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TradingName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string LLBNumber { get; set; } = string.Empty;
        public string LicenseName { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string Council { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime? ExtendedHoursDate { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public string Justification { get; set; } = string.Empty;
    }
}
