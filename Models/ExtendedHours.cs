using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Grpc.Core;

namespace LLB.Models
{
    public class ExtendedHours
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? Status  { get; set; }
        public string? Reference { get; set; }

        public string? ApplicationId { get; set; }


        public double? PaidFee { get; set; }
        public string? PaymentStatus { get; set; }
        public string? ReasonForExtention { get; set; }
        public string? HoursOfExtension { get; set; }
        public DateTime ExtendedHoursDate { get; set; }

        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }



    }

    

}
