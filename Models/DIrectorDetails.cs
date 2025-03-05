using System;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace LLB.Models
{
    public class DirectorDetails
    {
        [Key]

        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? ApplicationId { get; set; }
        public string? Surname { get; set; }
        public string? NationalId { get; set; }
        public string? Address { get; set; }
        public string? Status { get; set; }
        public string? NatId { get; set; }
        public string? Form55 { get; set; }
        public string? FingerPrints { get; set; }

        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }


    }
}