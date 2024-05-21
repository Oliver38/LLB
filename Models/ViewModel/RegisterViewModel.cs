using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace LLB.Models.ViewModel
{
    public class RegisterViewModel
    {
        
        public string? Email { get; set; }
        
        public string? Password { get; set; }
       
        public string? ConfirmPassword { get; set; }
       
        public string? Name { get; set; }
        
        public string? LastName { get; set; }
       
        public string? PhysicalAddress { get; set; }
       
        public string? PhoneNumber { get; set; }
        
        public string? NatID { get; set; }
        
        public DateTime DOB { get; set; }
        
        public string? Nationality { get; set; }
      
        public string? CountryOfResidence { get; set; }

        public DateTime DateOfApplication { get; set; }

       // public string? ApplicationBy { get; set; }
    
        public string? Gender { get; set; }
       
        public string? Province { get; set; }

        public bool IsActive { get; set; }

    }
}
