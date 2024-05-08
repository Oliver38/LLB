using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace LLB.Models
{
    public class ApplicationInfo
    {
        [Key]
        public string Id { get; set; }
        // * ApplicationID /Id
        public string UserID { get; set; }
        public string ApplicationType { get; set; }
        public string LicenseTypeID { get; set; }
        //* Name**
        //* Surname**
        //* DOB**
        //* Gender**
        public string PlaceOfBirth { get; set; }
        public string DateofEntryIntoZimbabwe { get; set; }
        public string PlaceOfEntry { get; set; }
        public string OperationAddress { get; set; }//Place of operation// 
        public string Status { get; set; }
        public string ApplicationDate { get; set; }
        public string InspectorID { get; set; }
        public string InspectionDate { get; set; }
        public string Secretary { get; set; }
        public string ApprovedDate { get; set; }
        public string RejectionReason { get; set; }

    }
}
