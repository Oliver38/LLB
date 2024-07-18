using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class Queries
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? InspectorId { get; set; }
        public string? Stage { get; set; }
        public string? Query { get; set; }
        public string? Status { get; set; }
        public string? TaskId { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }



    }
}
