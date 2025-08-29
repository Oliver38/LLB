
//using System;
using System.ComponentModel.DataAnnotations;
using Grpc.Core;
//using System.Net.NetworkInformation;

namespace LLB.Models
{
    public class ChangeManaager
    {
        [Key]
        
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public string? NewManagersCount { get; set; }
        public string? PaidFee { get; set; }
        public string? PaymentStatus { get; set; }
        public string? DateApplied { get; set; }
        public string? DateUpdated { get; set; }

    }
}
