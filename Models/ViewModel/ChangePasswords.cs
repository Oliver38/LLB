using System.ComponentModel.DataAnnotations;

namespace LLB.Models.ViewModel
{
    public class ChangePasswords
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "New Passwords does not match.")]
        public string ConfirmPassword { get; set; }
    }
}
