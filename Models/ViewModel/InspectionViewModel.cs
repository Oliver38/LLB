using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace LLB.Models.ViewModel
{
    public class InspectionViewModel
    {

        // Liquor Outlet Info

        public string? Id { get; set; }
        public string? Service { get; set; }
        public string? TradingName { get; set; }
        public string? LLBNumber { get; set; }
        public string? Application { get; set; }
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public string? ApplicationId { get; set; }
        public string? InspectorId { get; set; }
        public string? LicenseType { get; set; }
        public string? LicenseRegion { get; set; }
        public string? TaskId { get; set; }
        public DateTime InspectionDate { get; set; }
        public DateTime DateApplied { get; set; }
        public DateTime DateUpdate { get; set; }


    }
}
