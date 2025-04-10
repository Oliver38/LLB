﻿using Microsoft.AspNetCore.Identity;

namespace LLB.Models
{
    public class ApplicationUser : IdentityUser
    {
         //  public required string Id { get; set; }
      //  public string? UserId { get; set; }
                     
        public string? Name { get; set; }   
        public string? LastName { get; set; }
        public string? PhysicalAddress { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhoneNumber { get; set; }
        public string? NatID { get; set; }
        public string DOB { get; set; }
        public string? Nationality { get; set; }
        public string? CountryOfResidence { get; set; }
        public DateTime DateOfApplication { get; set; }

        public string? ApplicationBy { get; set; }

        public string? Gender { get; set; }
        public string? Province { get; set; }
        public string? LeaveStatus { get; set; }

        public bool IsActive { get; set; }

        public static implicit operator string?(ApplicationUser? v)
        {
            throw new NotImplementedException();
        }
    }
}
