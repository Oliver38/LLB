namespace LLB.Models.ViewModel
{
    public class SecretaryReportsViewModel
    {
        public string? ProvinceFilter { get; set; }
        public string? CouncilFilter { get; set; }
        public string? RegionFilter { get; set; }
        public List<string> ProvinceOptions { get; set; } = new();
        public List<string> CouncilOptions { get; set; } = new();
        public List<string> RegionOptions { get; set; } = new();
        public int TotalApplications { get; set; }
        public int FilteredApplications { get; set; }
        public int DistinctProvinces { get; set; }
        public int DistinctCouncils { get; set; }
        public int DistinctRegions { get; set; }
        public List<SecretaryReportSummaryItemViewModel> ProvinceBreakdown { get; set; } = new();
        public List<SecretaryReportSummaryItemViewModel> CouncilBreakdown { get; set; } = new();
        public List<SecretaryReportSummaryItemViewModel> RegionBreakdown { get; set; } = new();
        public List<SecretaryReportRowViewModel> Applications { get; set; } = new();
    }

    public class SecretaryReportSummaryItemViewModel
    {
        public string Name { get; set; } = "Unspecified";
        public int Count { get; set; }
    }

    public class SecretaryReportRowViewModel
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string TradingName { get; set; } = "N/A";
        public string OperatingAddress { get; set; } = "N/A";
        public string Province { get; set; } = "Unspecified";
        public string Council { get; set; } = "Unspecified";
        public string Region { get; set; } = "Unspecified";
        public string LicenseName { get; set; } = "N/A";
        public string Status { get; set; } = "Unknown";
        public DateTime ApplicationDate { get; set; }
    }
}
