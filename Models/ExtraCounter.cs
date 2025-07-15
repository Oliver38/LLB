using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Grpc.Core;

namespace LLB.Models
{
    public class ExtraCounter
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? Status  { get; set; }
        public string? Reference { get; set; }

        public string? ApplicationId { get; set; }
        public string? PreviousPlanPath { get; set; }
        public string? NewPlanPath { get; set; }
        public string? ExtracounterReason { get; set; }

        public double? PaidFee { get; set; }
        public string? PaymentStatus { get; set; }
       
        public string? ApproverId { get; set; }
        public DateTime? DateOfApproval { get; set; }

        public string? VerifierId { get; set; }
        public DateTime? DateVerified { get; set; }

        public string? RecommenderId { get; set; }
        public DateTime? DateRecommended { get; set; }

        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }



    }


    public class ExtraCounterView
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? TaskId { get; set; }
        public string? Status { get; set; }
        public string? Reference { get; set; }

        public string? ApplicationId { get; set; }


        public double? PaidFee { get; set; }
        public string? PaymentStatus { get; set; }
        public string? ReasonForExtention { get; set; }
        public string? HoursOfExtension { get; set; }
        public string? ApproverId { get; set; }
        public DateTime? DateOfApproval { get; set; }
        public DateTime ExtendedHoursDate { get; set; }

        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }



    }

}
