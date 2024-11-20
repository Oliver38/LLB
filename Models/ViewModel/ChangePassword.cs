using System.ComponentModel.DataAnnotations;

namespace LLB.Models.ViewModel
{
    public class ChangePassword
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
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; }
    }

}
