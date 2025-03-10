using Humanizer;
using System;
using System.ComponentModel.DataAnnotations;

namespace LLB.Models
{
    public class District
    {
        [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Province { get; set; }
        public string? ProvinceId { get; set; }
       

        public string? UserId { get; set; }
        public string? Description { get; set; }
        public DateTime? DateAdded { get; set; }
        public string? Status { get; set; }
        public DateTime DateUpdated { get; set; }



    }
}
