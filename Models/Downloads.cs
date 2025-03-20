using Grpc.Core;
using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class Downloads
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? LLBNUM { get; set; }
        public string? Status { get; set; }
        public string? DocumentType { get; set; }
        public string? PaymentRef { get; set; }
        public string? PaymentStatus { get; set; }
        public string? UserId { get; set; }
        public Int64? DownloadCount { get; set; }
        public DateTime? DateApplied { get; set; }
        public DateTime? DateUpdated { get; set; }
        

    }
}
