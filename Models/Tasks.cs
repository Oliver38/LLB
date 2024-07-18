using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class Tasks
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? AssignerId { get; set; }  
        public string? VerifierId { get; set; }  
        public DateTime? VerificationDate { get; set; }  
        public string? RecommenderId { get; set; }  
        public DateTime? RecommendationDate { get; set; }
        public string? ApproverId { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? Status { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }



    }
}
