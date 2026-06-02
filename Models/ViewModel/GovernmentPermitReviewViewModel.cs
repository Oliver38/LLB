namespace LLB.Models.ViewModel
{
    public class GovernmentPermitReviewViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? LLBNumber { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseRegion { get; set; }
        public string? TitleOfAuthority { get; set; }
        public string? Ministry { get; set; }
        public string? LocationName { get; set; }
        public string? Address { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Council { get; set; }
        public double? Payment { get; set; }
        public string? PaymentStatus { get; set; }
        public string? LG30 { get; set; }
        public string? LetterFromTheSuperior { get; set; }
        public DateTime RequestedOn { get; set; }
        public string ReviewStageLabel { get; set; } = "Review";
        public string ApproveButtonLabel { get; set; } = "Approve";
        public bool CanReviewAction { get; set; }
    }
}
