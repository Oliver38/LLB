using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LLB.Models.ViewModel
{
    public class LoginViewModel
    {
        
        [EmailAddress]
        
        [Required(ErrorMessage = "Email Is Required")]
        public string Email { get; set; }
       
        [Required(ErrorMessage = "Correct Password Required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
