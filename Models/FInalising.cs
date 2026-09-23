using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class Finalising
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? UserId { get; set; }
        public string? ManagersInfo { get; set; }
        public string? DirectorsInfo { get; set; }
        public string? OutletInfo { get; set; }
        public string? DocumentInfo { get; set; }
        public string? ApplicantInfo { get; set; }
        public bool IsApplicantInformationComplete { get; set; }
        public bool IsOutletInformationComplete { get; set; }
        public bool IsManagerInformationComplete { get; set; }
        public bool IsDocumentInformationComplete { get; set; }
        public bool AllRequiredSectionsComplete =>
            IsApplicantInformationComplete
            && IsOutletInformationComplete
            && IsManagerInformationComplete
            && IsDocumentInformationComplete;
        public double? ManagersPrice { get; set; }
        public double? LicencePrice { get; set; }
        public double? Total { get; set; }
        public double? ManagersTotal { get; set; }
        public int? ManagersCount { get; set; }
        public string? Status { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }



    }
}
