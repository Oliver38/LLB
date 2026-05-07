namespace LLB.Models.ViewModel
{
    public class ClientPostFormationListingViewModel
    {
        public string RecordId { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string TradingName { get; set; } = "N/A";
        public string LLBNumber { get; set; } = "N/A";
        public string LicenseName { get; set; } = "N/A";
        public string RegionName { get; set; } = "N/A";
        public string ServiceName { get; set; } = string.Empty;
        public string Status { get; set; } = "Unknown";
        public DateTime? SubmittedDate { get; set; }
        public DateTime? EventDate { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        public string ActionLabel { get; set; } = "Open";
    }
}
