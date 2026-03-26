namespace LLB.Models.ViewModel
{
    public class DuplicateDownloadViewModel
    {
        public string? ApplicationId { get; set; }
        public string? TaskId { get; set; }
        public string? LLBNumber { get; set; }
        public string? TradingName { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseRegion { get; set; }
        public string? DownloadStatus { get; set; }
        public string? DuplicateStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? RequestedOn { get; set; }
    }
}
