using System;
using System.Collections.Generic;

namespace LLB.Models.ViewModel
{
    public class SecretaryDashboardViewModel
    {
        public List<SecretaryDashboardApplicationItemViewModel> Applications { get; set; } = new();
        public List<SecretaryDashboardPostFormationItemViewModel> PostFormations { get; set; } = new();
    }

    public class SecretaryDashboardApplicationItemViewModel
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string TradingName { get; set; } = "N/A";
        public string OperatingAddress { get; set; } = "N/A";
        public DateTime ApplicationDate { get; set; }
        public string LicenseName { get; set; } = "N/A";
        public string RegionName { get; set; } = "N/A";
        public string Status { get; set; } = "Unknown";
        public string ReviewUrl { get; set; } = "#";
    }

    public class SecretaryDashboardPostFormationItemViewModel
    {
        public string RecordId { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string TradingName { get; set; } = "N/A";
        public string OperatingAddress { get; set; } = "N/A";
        public DateTime SubmittedDate { get; set; }
        public string LicenseName { get; set; } = "N/A";
        public string RegionName { get; set; } = "N/A";
        public string Status { get; set; } = "Unknown";
        public string ReviewUrl { get; set; } = "#";
    }
}
