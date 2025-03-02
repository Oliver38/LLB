
//using System;
using System.ComponentModel.DataAnnotations;
//using System.Net.NetworkInformation;

namespace LLB.Models
{
    public class ApplicationInfo
    {
        [Key]
        public string? Id { get; set; }
        // * ApplicationID /Id
        public string? UserID { get; set; }
        public string? ApplicationType { get; set; }
        //public string BusinessName { get; set; }
        public string? LicenseTypeID { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentId { get; set; }
        public string? RefNum { get; set; }
        public string? ExaminationStatus { get; set; }
        public decimal? PaymentFee { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? DateofEntryIntoZimbabwe { get; set; }
        public string? PlaceOfEntry { get; set; }
        public string? OperationAddress { get; set; }//Place of operation
        public string? LLBNum { get; set; }//Place of operation

        public string? BusinessName { get; set; }//Place of operation
        public string? Nationality { get; set; }//Place of operation
        public string? IdPass { get; set; }//Place of operation
        //public string? Foreigner  { get; set; }//Place of operation


        public string? Status { get; set; }
        public DateTime ApplicationDate { get; set; }
        public DateTime DateUpdated { get; set; }
        public string? InspectorID { get; set; }

        public DateTime InspectionDate { get; set; }
        public string? Secretary { get; set; }
        public DateTime ApprovedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? RejectionReason { get; set; }
        //spublic DateTime DateCreated { get; set; }
        public bool rejectionFlag { get; set; }
        public string? rejectionFlagComment { get; set; }
        public string? FlaggerUserId { get; set; }
        public string? RenewalStatus { get; set; }
        public DateTime RenewalDate { get; set; }

        public string? rejectionUnflagComment { get; set; }

    }
}
