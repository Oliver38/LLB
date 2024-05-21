
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Net.NetworkInformation;

namespace LLB.Models
{
    public class AttachmentInfo
    {
        [Key]
        
        public string? Id { get; set; }

        public string? UserId { get; set; }
        public string? ApplicationId { get; set; }
        public string? DocumentTitle { get; set; }
        public string? DocumentLocation { get; set; }
        public string? Status { get; set; }
        public DateTime? DateAdded { get; set; }
        public DateTime? DateUpdated { get; set; }


    }
}
