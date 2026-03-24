using System.ComponentModel.DataAnnotations;

namespace LLB.Models.ViewModel
{
    public class AdminUserDirectoryViewModel
    {
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
        public string? StatusFilter { get; set; } = "all";
        public string? ScopeFilter { get; set; } = "all";

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int LockedUsers { get; set; }

        public List<string> AvailableRoles { get; set; } = new();
        public List<AdminUserListItemViewModel> Users { get; set; } = new();
    }

    public class AdminUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PrimaryRole { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string Scope { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsLockedOut { get; set; }
        public string LeaveStatus { get; set; } = "Available";
        public DateTime? DateOfApplication { get; set; }
    }

    public class AdminUserUpsertViewModel
    {
        public string? Id { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? LastName { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? PhysicalAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? NatID { get; set; }
        public DateTime? DOB { get; set; }
        public string? Nationality { get; set; }
        public string? CountryOfResidence { get; set; }
        public string? Gender { get; set; }
        public string? Province { get; set; }
        public string? LeaveStatus { get; set; } = "Available";
        public bool IsActive { get; set; } = true;

        public List<string> SelectedRoles { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
        public List<string> LeaveStatusOptions { get; set; } = new();
    }

    public class AdminUserDetailViewModel : AdminUserUpsertViewModel
    {
        public string? UserName { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public DateTime? DateOfApplication { get; set; }
        public string? ApplicationBy { get; set; }

        public bool IsLockedOut =>
            LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    }
}
