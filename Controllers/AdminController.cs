using LLB.Data;
using LLB.Helpers;
using LLB.Models;
using LLB.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LLB.Controllers
{
    [Authorize(Roles = "admin,super user")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private static readonly string[] LeaveStatuses =
        {
            "Available",
            "On Leave",
            "Annual Leave",
            "Sick Leave",
            "Study Leave",
            "Suspended"
        };

        private static readonly HashSet<string> ExternalRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "client",
            "external"
        };

        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext db;

        public AdminController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            this.db = db;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        [HttpGet("RegisterUser")]
        public async Task<IActionResult> RegisterUser()
        {
            var model = new AdminUserUpsertViewModel
            {
                IsActive = true,
                LeaveStatus = "Available",
                AvailableRoles = await GetAllRoleNamesAsync(),
                LeaveStatusOptions = LeaveStatuses.ToList()
            };

            ViewData["Title"] = "Create User";
            ViewData["Subtitle"] = "Provision a new portal account and assign roles";
            return View(model);
        }

        [HttpPost("RegisterUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(AdminUserUpsertViewModel model)
        {
            await PopulateReferenceDataAsync(model);
            ValidateUserForm(model, isCreate: true);

            if (await userManager.FindByEmailAsync(model.Email ?? string.Empty) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "That email address is already in use.");
            }

            var selectedRoles = await NormalizeSelectedRolesAsync(model.SelectedRoles);
            if (selectedRoles.Count == 0)
            {
                ModelState.AddModelError(nameof(model.SelectedRoles), "Select at least one role for the user.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Create User";
                ViewData["Subtitle"] = "Provision a new portal account and assign roles";
                return View(model);
            }

            var password = string.IsNullOrWhiteSpace(model.Password)
                ? PasswordHelper.GenerateStrongPassword()
                : model.Password!.Trim();

            var user = new ApplicationUser
            {
                Name = model.Name?.Trim(),
                LastName = model.LastName?.Trim(),
                Email = model.Email?.Trim(),
                UserEmail = model.Email?.Trim(),
                UserName = model.Email?.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                UserPhoneNumber = model.PhoneNumber?.Trim(),
                PhysicalAddress = model.PhysicalAddress?.Trim(),
                NatID = model.NatID?.Trim(),
                DOB = model.DOB?.ToString("yyyy-MM-dd") ?? string.Empty,
                Nationality = model.Nationality?.Trim(),
                CountryOfResidence = model.CountryOfResidence?.Trim(),
                Gender = model.Gender?.Trim(),
                Province = model.Province?.Trim(),
                LeaveStatus = NormalizeLeaveStatus(model.LeaveStatus),
                IsActive = model.IsActive,
                DateOfApplication = DateTime.Now,
                ApplicationBy = User.Identity?.Name ?? "admin",
                LockoutEnabled = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                AddIdentityErrors(createResult);
                ViewData["Title"] = "Create User";
                ViewData["Subtitle"] = "Provision a new portal account and assign roles";
                return View(model);
            }

            var roleResult = await userManager.AddToRolesAsync(user, selectedRoles);
            if (!roleResult.Succeeded)
            {
                TempData["error"] = string.Join(" ", roleResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(ViewUser), new { id = user.Id });
            }

            if (!model.IsActive)
            {
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }

            TempData["success"] = "User created successfully.";
            TempData["generatedPassword"] = password;
            return RedirectToAction(nameof(ViewUser), new { id = user.Id });
        }

        [HttpGet("InternalUsers")]
        public async Task<IActionResult> InternalUsers(
            string? searchTerm,
            string? role,
            string? status = "all",
            string? scope = "all")
        {
            var model = await BuildDirectoryViewModelAsync(searchTerm, role, status, scope);

            ViewData["Title"] = "User Management";
            ViewData["Subtitle"] = "Manage access, roles, status, and account support actions";
            return View(model);
        }

        [HttpPost("InternalUsers")]
        [ValidateAntiForgeryToken]
        public IActionResult InternalUsers(AdminUserDirectoryViewModel model)
        {
            return RedirectToAction(nameof(InternalUsers), new
            {
                searchTerm = model.SearchTerm,
                role = model.RoleFilter,
                status = model.StatusFilter,
                scope = model.ScopeFilter
            });
        }

        [HttpGet("ViewUser")]
        public async Task<IActionResult> ViewUser(string id)
        {
            var model = await BuildDetailViewModelAsync(id);
            if (model == null)
            {
                TempData["error"] = "The requested user could not be found.";
                return RedirectToAction(nameof(InternalUsers));
            }

            ViewData["Title"] = "Manage User";
            ViewData["Subtitle"] = $"Review and update {model.Name} {model.LastName}";
            return View(model);
        }

        [HttpPost("UpdateUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(AdminUserUpsertViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Id))
            {
                TempData["error"] = "User identifier is missing.";
                return RedirectToAction(nameof(InternalUsers));
            }

            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["error"] = "The requested user could not be found.";
                return RedirectToAction(nameof(InternalUsers));
            }

            await PopulateReferenceDataAsync(model);
            model.SelectedRoles = (await userManager.GetRolesAsync(user)).ToList();

            ValidateUserForm(model, isCreate: false);

            var existingUser = await userManager.FindByEmailAsync(model.Email ?? string.Empty);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "That email address is already in use.");
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildDetailViewModelAsync(user.Id, model);
                ViewData["Title"] = "Manage User";
                ViewData["Subtitle"] = $"Review and update {user.Name} {user.LastName}";
                return View(nameof(ViewUser), invalidModel);
            }

            user.Name = model.Name?.Trim();
            user.LastName = model.LastName?.Trim();
            user.Email = model.Email?.Trim();
            user.UserEmail = model.Email?.Trim();
            user.UserName = model.Email?.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.UserPhoneNumber = model.PhoneNumber?.Trim();
            user.PhysicalAddress = model.PhysicalAddress?.Trim();
            user.NatID = model.NatID?.Trim();
            user.DOB = model.DOB?.ToString("yyyy-MM-dd") ?? user.DOB;
            user.Nationality = model.Nationality?.Trim();
            user.CountryOfResidence = model.CountryOfResidence?.Trim();
            user.Gender = model.Gender?.Trim();
            user.Province = model.Province?.Trim();

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(updateResult);
                var invalidModel = await BuildDetailViewModelAsync(user.Id, model);
                ViewData["Title"] = "Manage User";
                ViewData["Subtitle"] = $"Review and update {user.Name} {user.LastName}";
                return View(nameof(ViewUser), invalidModel);
            }

            TempData["success"] = "User profile updated successfully.";
            return RedirectToAction(nameof(ViewUser), new { id = user.Id });
        }

        [HttpPost("UpdateRoles")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoles(string id, List<string> selectedRoles)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error"] = "The requested user could not be found.";
                return RedirectToAction(nameof(InternalUsers));
            }

            var normalizedRoles = await NormalizeSelectedRolesAsync(selectedRoles);
            if (normalizedRoles.Count == 0)
            {
                TempData["error"] = "Select at least one role for the user.";
                return RedirectToAction(nameof(ViewUser), new { id });
            }

            if (IsCurrentUser(user.Id) && !normalizedRoles.Any(IsManagementRole))
            {
                TempData["error"] = "You cannot remove all management roles from your own account.";
                return RedirectToAction(nameof(ViewUser), new { id });
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(normalizedRoles, StringComparer.OrdinalIgnoreCase).ToList();
            var rolesToAdd = normalizedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    TempData["error"] = string.Join(" ", removeResult.Errors.Select(error => error.Description));
                    return RedirectToAction(nameof(ViewUser), new { id });
                }
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    TempData["error"] = string.Join(" ", addResult.Errors.Select(error => error.Description));
                    return RedirectToAction(nameof(ViewUser), new { id });
                }
            }

            TempData["success"] = "User roles updated successfully.";
            return RedirectToAction(nameof(ViewUser), new { id });
        }

        [HttpPost("ChangeRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string id, string oldrole, string newrole)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error"] = "The requested user could not be found.";
                return RedirectToAction(nameof(InternalUsers));
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            var updatedRoles = currentRoles
                .Where(roleName => !string.Equals(roleName, oldrole, StringComparison.OrdinalIgnoreCase))
                .Append(newrole)
                .ToList();

            return await UpdateRoles(id, updatedRoles);
        }

        [HttpPost("UpdateLeaveStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLeaveStatus(string id, string? leaveStatus)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error"] = "The requested user could not be found.";
                return RedirectToAction(nameof(InternalUsers));
            }

            user.LeaveStatus = NormalizeLeaveStatus(leaveStatus);
            var result = await userManager.UpdateAsync(user);

            TempData[result.Succeeded ? "success" : "error"] = result.Succeeded
                ? "Leave status updated successfully."
                : string.Join(" ", result.Errors.Select(error => error.Description));

            return RedirectToAction(nameof(ViewUser), new { id });
        }

        [HttpPost("UpdateActivation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateActivation(string id, bool activate)
        {
            var result = await ToggleActivationAsync(id, activate);
            TempData[result.Success ? "success" : "error"] = result.Message;
            return RedirectToAction(nameof(ViewUser), new { id });
        }

        [HttpGet("Block")]
        public async Task<IActionResult> Block(
            string userId,
            string status,
            string? role,
            string? searchTerm,
            string? statusFilter,
            string? scope)
        {
            var activate = !string.Equals(status, "block", StringComparison.OrdinalIgnoreCase);
            var result = await ToggleActivationAsync(userId, activate);
            TempData[result.Success ? "success" : "error"] = result.Message;

            return RedirectToAction(nameof(InternalUsers), new
            {
                role,
                searchTerm,
                status = statusFilter,
                scope
            });
        }

        [HttpPost("UnlockUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error"] = "The requested user could not be found.";
                return RedirectToAction(nameof(InternalUsers));
            }

            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.ResetAccessFailedCountAsync(user);

            TempData["success"] = "The user account has been unlocked.";
            return RedirectToAction(nameof(ViewUser), new { id });
        }

        [HttpGet("ResetPassword")]
        public async Task<IActionResult> ResetPasswordAsync(
            string userid,
            string? email,
            string? role,
            string? searchTerm,
            string? status,
            string? scope)
        {
            var result = await ResetPasswordInternalAsync(userid, null);
            TempData[result.Success ? "success" : "error"] = result.Message;
            if (result.Success && !string.IsNullOrWhiteSpace(result.GeneratedPassword))
            {
                TempData["generatedPassword"] = result.GeneratedPassword;
            }

            if (!string.IsNullOrWhiteSpace(role) || !string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction(nameof(InternalUsers), new
                {
                    role,
                    searchTerm,
                    status,
                    scope
                });
            }

            return RedirectToAction(nameof(ViewUser), new { id = userid });
        }

        [HttpPost("ResetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string? newPassword)
        {
            var result = await ResetPasswordInternalAsync(userId, newPassword);
            TempData[result.Success ? "success" : "error"] = result.Message;
            if (result.Success && !string.IsNullOrWhiteSpace(result.GeneratedPassword))
            {
                TempData["generatedPassword"] = result.GeneratedPassword;
            }

            return RedirectToAction(nameof(ViewUser), new { id = userId });
        }

        private async Task<AdminUserDirectoryViewModel> BuildDirectoryViewModelAsync(
            string? searchTerm,
            string? roleFilter,
            string? statusFilter,
            string? scopeFilter)
        {
            var allUsers = await userManager.Users
                .OrderBy(user => user.Name)
                .ThenBy(user => user.LastName)
                .ToListAsync();

            var roleLookup = await BuildRoleLookupAsync();
            var items = allUsers.Select(user =>
            {
                var roles = roleLookup.TryGetValue(user.Id, out var values)
                    ? values
                    : new List<string>();

                var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
                return new AdminUserListItemViewModel
                {
                    Id = user.Id,
                    FullName = BuildFullName(user.Name, user.LastName),
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? user.UserPhoneNumber ?? string.Empty,
                    Roles = roles,
                    PrimaryRole = GetPrimaryRole(roles),
                    Scope = IsInternalUser(roles) ? "Internal" : "Client",
                    IsActive = user.IsActive,
                    IsLockedOut = isLockedOut,
                    LeaveStatus = NormalizeLeaveStatus(user.LeaveStatus),
                    DateOfApplication = user.DateOfApplication == default ? null : user.DateOfApplication
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(scopeFilter) && !string.Equals(scopeFilter, "all", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(item =>
                        string.Equals(item.Scope, scopeFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                items = items.Where(item =>
                        item.Roles.Any(role => string.Equals(role, roleFilter, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "all", StringComparison.OrdinalIgnoreCase))
            {
                items = statusFilter.ToLowerInvariant() switch
                {
                    "active" => items.Where(item => item.IsActive).ToList(),
                    "inactive" => items.Where(item => !item.IsActive).ToList(),
                    "locked" => items.Where(item => item.IsLockedOut).ToList(),
                    "onleave" => items.Where(item =>
                            !string.Equals(item.LeaveStatus, "Available", StringComparison.OrdinalIgnoreCase))
                        .ToList(),
                    _ => items
                };
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                items = items.Where(item =>
                        item.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        item.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        item.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        item.PrimaryRole.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return new AdminUserDirectoryViewModel
            {
                SearchTerm = searchTerm,
                RoleFilter = roleFilter,
                StatusFilter = string.IsNullOrWhiteSpace(statusFilter) ? "all" : statusFilter,
                ScopeFilter = string.IsNullOrWhiteSpace(scopeFilter) ? "all" : scopeFilter,
                TotalUsers = items.Count,
                ActiveUsers = items.Count(item => item.IsActive),
                InactiveUsers = items.Count(item => !item.IsActive),
                LockedUsers = items.Count(item => item.IsLockedOut),
                AvailableRoles = await GetAllRoleNamesAsync(),
                Users = items
            };
        }

        private async Task<AdminUserDetailViewModel?> BuildDetailViewModelAsync(
            string userId,
            AdminUserUpsertViewModel? postedModel = null)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var roles = (await userManager.GetRolesAsync(user)).ToList();
            var model = new AdminUserDetailViewModel
            {
                Id = user.Id,
                Name = postedModel?.Name ?? user.Name,
                LastName = postedModel?.LastName ?? user.LastName,
                Email = postedModel?.Email ?? user.Email,
                PhysicalAddress = postedModel?.PhysicalAddress ?? user.PhysicalAddress,
                PhoneNumber = postedModel?.PhoneNumber ?? user.PhoneNumber ?? user.UserPhoneNumber,
                NatID = postedModel?.NatID ?? user.NatID,
                DOB = postedModel?.DOB ?? ParseDob(user.DOB),
                Nationality = postedModel?.Nationality ?? user.Nationality,
                CountryOfResidence = postedModel?.CountryOfResidence ?? user.CountryOfResidence,
                Gender = postedModel?.Gender ?? user.Gender,
                Province = postedModel?.Province ?? user.Province,
                LeaveStatus = postedModel?.LeaveStatus ?? NormalizeLeaveStatus(user.LeaveStatus),
                IsActive = user.IsActive,
                SelectedRoles = postedModel?.SelectedRoles?.Count > 0
                    ? postedModel.SelectedRoles
                    : roles,
                AvailableRoles = await GetAllRoleNamesAsync(),
                LeaveStatusOptions = LeaveStatuses.ToList(),
                UserName = user.UserName,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                AccessFailedCount = user.AccessFailedCount,
                LockoutEnd = user.LockoutEnd,
                DateOfApplication = user.DateOfApplication == default ? null : user.DateOfApplication,
                ApplicationBy = user.ApplicationBy
            };

            return model;
        }

        private async Task<Dictionary<string, List<string>>> BuildRoleLookupAsync()
        {
            var roleEntries = await (
                from userRole in db.UserRoles
                join role in db.Roles on userRole.RoleId equals role.Id
                select new { userRole.UserId, role.Name }
            ).ToListAsync();

            return roleEntries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                .GroupBy(entry => entry.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(entry => entry.Name!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(entry => entry)
                        .ToList());
        }

        private async Task<List<string>> GetAllRoleNamesAsync()
        {
            return await roleManager.Roles
                .Select(role => role.Name!)
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .OrderBy(roleName => roleName)
                .ToListAsync();
        }

        private async Task PopulateReferenceDataAsync(AdminUserUpsertViewModel model)
        {
            model.AvailableRoles = await GetAllRoleNamesAsync();
            model.LeaveStatusOptions = LeaveStatuses.ToList();
            model.LeaveStatus = NormalizeLeaveStatus(model.LeaveStatus);
        }

        private void ValidateUserForm(AdminUserUpsertViewModel model, bool isCreate)
        {
            if (!model.DOB.HasValue)
            {
                ModelState.AddModelError(nameof(model.DOB), "Date of birth is required.");
            }

            if (isCreate)
            {
                if (!string.IsNullOrWhiteSpace(model.Password) &&
                    !string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
                {
                    ModelState.AddModelError(nameof(model.ConfirmPassword), "Password confirmation does not match.");
                }
            }
        }

        private async Task<List<string>> NormalizeSelectedRolesAsync(IEnumerable<string>? selectedRoles)
        {
            var availableRoles = await GetAllRoleNamesAsync();
            var normalizedRoles = selectedRoles?
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(role => availableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                .ToList() ?? new List<string>();

            if (normalizedRoles.Count == 0)
            {
                return normalizedRoles;
            }

            var onlyExternalRoles = normalizedRoles.All(role => ExternalRoles.Contains(role));
            if (!onlyExternalRoles && availableRoles.Contains("internal", StringComparer.OrdinalIgnoreCase))
            {
                if (!normalizedRoles.Contains("internal", StringComparer.OrdinalIgnoreCase))
                {
                    normalizedRoles.Add("internal");
                }
            }
            else
            {
                normalizedRoles.RemoveAll(role => string.Equals(role, "internal", StringComparison.OrdinalIgnoreCase));
            }

            return normalizedRoles
                .OrderBy(role => role)
                .ToList();
        }

        private async Task<(bool Success, string Message)> ToggleActivationAsync(string userId, bool activate)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "The requested user could not be found.");
            }

            if (!activate && IsCurrentUser(user.Id))
            {
                return (false, "You cannot deactivate your own account.");
            }

            user.IsActive = activate;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return (false, string.Join(" ", updateResult.Errors.Select(error => error.Description)));
            }

            await userManager.SetLockoutEndDateAsync(
                user,
                activate ? null : DateTimeOffset.UtcNow.AddYears(100));

            if (activate)
            {
                await userManager.ResetAccessFailedCountAsync(user);
            }

            return (true, activate
                ? "User activated successfully."
                : "User deactivated successfully.");
        }

        private async Task<(bool Success, string Message, string? GeneratedPassword)> ResetPasswordInternalAsync(
            string userId,
            string? requestedPassword)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, "The requested user could not be found.", null);
            }

            var password = string.IsNullOrWhiteSpace(requestedPassword)
                ? PasswordHelper.GenerateStrongPassword()
                : requestedPassword.Trim();

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded)
            {
                return (false, string.Join(" ", result.Errors.Select(error => error.Description)), null);
            }

            await userManager.ResetAccessFailedCountAsync(user);

            return (true, "Password reset successfully. Share the temporary password securely.", password);
        }

        private static string BuildFullName(string? firstName, string? lastName)
        {
            var parts = new[] { firstName, lastName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim());

            var fullName = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(fullName) ? "Unnamed User" : fullName;
        }

        private static string GetPrimaryRole(IEnumerable<string> roles)
        {
            var roleList = roles.ToList();
            return roleList
                .FirstOrDefault(role => !string.Equals(role, "internal", StringComparison.OrdinalIgnoreCase))
                ?? roleList.FirstOrDefault()
                ?? "Unassigned";
        }

        private static bool IsInternalUser(IEnumerable<string> roles)
        {
            var roleList = roles.ToList();
            return roleList.Contains("internal", StringComparer.OrdinalIgnoreCase) ||
                   roleList.Any(role => !ExternalRoles.Contains(role));
        }

        private static bool IsManagementRole(string role)
        {
            return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "super user", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCurrentUser(string userId)
        {
            return string.Equals(userManager.GetUserId(User), userId, StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime? ParseDob(string? dob)
        {
            if (string.IsNullOrWhiteSpace(dob))
            {
                return null;
            }

            return DateTime.TryParse(dob, out var parsed) ? parsed : null;
        }

        private static string NormalizeLeaveStatus(string? leaveStatus)
        {
            return string.IsNullOrWhiteSpace(leaveStatus)
                ? "Available"
                : leaveStatus.Trim();
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
