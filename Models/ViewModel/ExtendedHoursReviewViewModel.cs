namespace LLB.Models.ViewModel
{
    public class ExtendedHoursReviewViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string? TradingName { get; set; }
        public string? LLBNumber { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseRegion { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaynowReference { get; set; }
        public string? ReasonForExtention { get; set; }
        public double? PaidFee { get; set; }
        public DateTime ExtendedHoursDate { get; set; }
        public DateTime RequestedOn { get; set; }
    }
}
