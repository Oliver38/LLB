﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace LLB.Models
{
    public class ManagersParticulars
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
        public string? Attachment { get; set; }
        public string? Fingerprints { get; set; }
        public string? Form55 { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime DissmisalDate { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }


    }
}