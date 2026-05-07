using LLB.Data;
using LLB.Helpers;
using LLB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LLB.Controllers
{
    [Authorize]
    [Route("AgentLicense")]
    public class AgentLicenseController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public AgentLicenseController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            TaskAllocationHelper taskAllocationHelper)
        {
            _db = db;
            _userManager = userManager;
            _taskAllocationHelper = taskAllocationHelper;
        }

        [HttpGet("Apply")]
        public async Task<IActionResult> ApplyAsync(string? llbNumber, string? id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ApplicationInfo? agentApplication = null;
            ApplicationInfo? wholesaleApplication = null;

            if (!string.IsNullOrWhiteSpace(id))
            {
                agentApplication = await _db.ApplicationInfo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.Id == id
                        && item.UserID == currentUser.Id
                        && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

                if (agentApplication == null)
                {
                    TempData["error"] = "The agent license application could not be found.";
                    return RedirectToAction("Apply");
                }

                wholesaleApplication = await GetWholesaleSourceApplicationAsync(agentApplication.CompanyNumber);
            }
            else if (!string.IsNullOrWhiteSpace(llbNumber))
            {
                wholesaleApplication = await FindApprovedWholesaleApplicationByLlbNumberAsync(llbNumber);
                if (wholesaleApplication == null)
                {
                    TempData["error"] = "The LLB number entered is not an approved wholesale licence. You cannot proceed with an agent license application under this licence.";
                }
            }

            await PopulateApplyViewAsync(currentUser, wholesaleApplication, agentApplication, llbNumber);
            return View();
        }

        [HttpPost("Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAsync(
            string llbNumber,
            string name,
            string surname,
            string dob,
            string gender,
            string nationality,
            string idPass,
            string applicantType,
            string businessName,
            IFormFile publicationProof,
            IFormFile tieAffidavit,
            IFormFile fingerprints,
            IFormFile form55,
            IFormFile idCopy,
            IFormFile wholesaleLicense)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var wholesaleApplication = await FindApprovedWholesaleApplicationByLlbNumberAsync(llbNumber);
            if (wholesaleApplication == null)
            {
                TempData["error"] = "The LLB number entered is not an approved wholesale licence. You cannot proceed with an agent license application under this licence.";
                return RedirectToAction("Apply", new { llbNumber });
            }

            name = (name ?? string.Empty).Trim();
            surname = (surname ?? string.Empty).Trim();
            dob = (dob ?? string.Empty).Trim();
            gender = (gender ?? string.Empty).Trim();
            nationality = (nationality ?? string.Empty).Trim();
            idPass = (idPass ?? string.Empty).Trim();
            applicantType = string.IsNullOrWhiteSpace(applicantType) ? "Individual" : applicantType.Trim();
            businessName = $"{name} {surname}".Trim();

            if (string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(surname)
                || string.IsNullOrWhiteSpace(dob)
                || string.IsNullOrWhiteSpace(gender)
                || string.IsNullOrWhiteSpace(nationality)
                || string.IsNullOrWhiteSpace(idPass)
                || string.IsNullOrWhiteSpace(businessName))
            {
                TempData["error"] = "Complete all agent information fields before submitting.";
                return RedirectToAction("Apply", new { llbNumber });
            }

            if (publicationProof == null || publicationProof.Length <= 0
                || tieAffidavit == null || tieAffidavit.Length <= 0
                || fingerprints == null || fingerprints.Length <= 0
                || form55 == null || form55.Length <= 0
                || idCopy == null || idCopy.Length <= 0
                || wholesaleLicense == null || wholesaleLicense.Length <= 0)
            {
                TempData["error"] = "Upload all required agent license attachments before submitting.";
                return RedirectToAction("Apply", new { llbNumber });
            }

            var secretaryId = await _taskAllocationHelper.GetSecretary(_db, _userManager);
            if (string.IsNullOrWhiteSpace(secretaryId))
            {
                TempData["error"] = "No secretary is currently available to receive this agent license application.";
                return RedirectToAction("Apply", new { llbNumber });
            }

            var licenseType = await GetAgentLicenseTypeAsync(wholesaleApplication);
            var wholesaleOutlet = await GetPreferredOutletAsync(wholesaleApplication.Id);
            var now = DateTime.Now;
            var applicationId = Guid.NewGuid().ToString();

            var agentApplication = new ApplicationInfo
            {
                Id = applicationId,
                UserID = currentUser.Id,
                ApplicationType = wholesaleApplication.ApplicationType,
                LicenseTypeID = licenseType?.Id ?? wholesaleApplication.LicenseTypeID,
                PaymentStatus = "Not Paid",
                PaymentId = string.Empty,
                RefNum = ReferenceHelper.GenerateReferenceNumber(_db),
                ExaminationStatus = AgentLicenseHelper.ServiceName,
                PaymentFee = 0,
                PlaceOfBirth = name,
                PlaceOfEntry = surname,
                DateofEntryIntoZimbabwe = dob,
                OperationAddress = nationality,
                ApplicantType = applicantType,
                BusinessName = businessName,
                IdPass = idPass,
                Status = "awaiting approval",
                ApplicationDate = now,
                DateUpdated = now,
                InspectorID = string.Empty,
                Secretary = string.Empty,
                RejectionReason = string.Empty,
                RenewalStatus = gender,
                CompanyNumber = wholesaleApplication.Id
            };

            agentApplication.IdCopy = await SaveApplicantFileAsync(idCopy, "IDCopy", applicationId);
            agentApplication.Fingerprints = await SaveApplicantFileAsync(fingerprints, "Fingerprints", applicationId);
            agentApplication.FormFF = await SaveApplicantFileAsync(form55, "FormFF", applicationId);

            _db.Add(agentApplication);

            _db.Add(BuildAgentAttachment(currentUser.Id, applicationId, AgentLicenseHelper.PublicationDocumentTitle, await SaveApplicationAttachmentAsync(publicationProof), now));
            _db.Add(BuildAgentAttachment(currentUser.Id, applicationId, AgentLicenseHelper.TieAffidavitDocumentTitle, await SaveApplicationAttachmentAsync(tieAffidavit), now));
            _db.Add(BuildAgentAttachment(currentUser.Id, applicationId, AgentLicenseHelper.WholesaleLicenseDocumentTitle, await SaveApplicationAttachmentAsync(wholesaleLicense), now));

            _db.Add(new OutletInfo
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationId = applicationId,
                UserId = currentUser.Id,
                TradingName = businessName,
                Province = wholesaleOutlet?.Province ?? string.Empty,
                Address = wholesaleOutlet?.Address ?? "Agent license under wholesale licence " + (wholesaleApplication.LLBNum ?? string.Empty),
                City = wholesaleOutlet?.City ?? string.Empty,
                DirectorNames = $"{name} {surname}".Trim(),
                Status = "inactive",
                ApplicationType = wholesaleOutlet?.ApplicationType ?? wholesaleApplication.ApplicationType,
                Latitude = wholesaleOutlet?.Latitude ?? string.Empty,
                Longitude = wholesaleOutlet?.Longitude ?? string.Empty,
                LicenseTypeID = licenseType?.Id ?? wholesaleApplication.LicenseTypeID,
                Council = wholesaleOutlet?.Council ?? string.Empty,
                DateAdded = now,
                DateUpdated = now
            });

            _db.Add(new Tasks
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationId = applicationId,
                ApproverId = secretaryId,
                AssignerId = "system",
                Service = AgentLicenseHelper.ServiceName,
                ExaminationStatus = "approval",
                Status = "assigned",
                DateAdded = now,
                DateUpdated = now
            });

            _db.SaveChanges();

            TempData["success"] = "Agent license application submitted to the secretary for examination.";
            return RedirectToAction("Apply", new { id = applicationId });
        }

        [Authorize(Roles = "secretary,admin,super user")]
        [HttpGet("ViewApplications")]
        public async Task<IActionResult> ViewApplicationsAsync(string id)
        {
            var agentApplication = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var task = await GetReviewTaskAsync(agentApplication.Id, includeCompleted: true);
            if (!CanBypassReviewAssignment() && task == null)
            {
                TempData["error"] = "This agent license application is not assigned to you.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var wholesaleApplication = await GetWholesaleSourceApplicationAsync(agentApplication.CompanyNumber);
            await PopulateReviewViewAsync(agentApplication, wholesaleApplication, task);
            return View();
        }

        [Authorize(Roles = "secretary,admin,super user")]
        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAsync(string id)
        {
            var agentApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var task = await GetReviewTaskAsync(agentApplication.Id, includeCompleted: false);
            if (!CanBypassReviewAssignment() && task == null)
            {
                TempData["error"] = "This agent license application is not assigned to you.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var wholesaleApplication = await GetWholesaleSourceApplicationAsync(agentApplication.CompanyNumber);
            if (wholesaleApplication == null)
            {
                TempData["error"] = "The linked wholesale licence could not be found.";
                return RedirectToAction("ViewApplications", new { id = agentApplication.Id });
            }

            var outlet = await _db.OutletInfo.FirstOrDefaultAsync(item => item.ApplicationId == agentApplication.Id);
            var wholesaleOutlet = await GetPreferredOutletAsync(wholesaleApplication.Id);
            var district = await GetDistrictCodeAsync(outlet?.City ?? wholesaleOutlet?.City);
            var licenseType = await GetAgentLicenseTypeAsync(wholesaleApplication);
            var now = DateTime.Now;

            agentApplication.LicenseTypeID = licenseType?.Id ?? agentApplication.LicenseTypeID;
            agentApplication.LLBNum = BuildAgentLlbNumber(district, agentApplication.RefNum, licenseType);
            agentApplication.Status = "approved";
            agentApplication.ExaminationStatus = AgentLicenseHelper.ServiceName;
            agentApplication.PaymentStatus = "Paid";
            agentApplication.ApprovedDate = now;
            agentApplication.ExpiryDate = now.AddYears(1);
            agentApplication.Secretary = _userManager.GetUserId(User) ?? string.Empty;
            agentApplication.DateUpdated = now;
            _db.Update(agentApplication);

            if (outlet != null)
            {
                outlet.Status = "active";
                outlet.DateUpdated = now;
                _db.Update(outlet);
            }

            if (task != null)
            {
                task.Status = "completed";
                task.ApprovedDate = now;
                task.DateUpdated = now;
                _db.Update(task);
            }

            _db.SaveChanges();
            DownloadStatusHelper.OpenLicenseDownload(_db, agentApplication, agentApplication.UserID);

            TempData["success"] = "Agent license application approved. The agent licence is now available for download.";
            return RedirectToAction("ViewApplications", new { id = agentApplication.Id });
        }

        [Authorize(Roles = "secretary,admin,super user")]
        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAsync(string id)
        {
            var agentApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var task = await GetReviewTaskAsync(agentApplication.Id, includeCompleted: false);
            if (!CanBypassReviewAssignment() && task == null)
            {
                TempData["error"] = "This agent license application is not assigned to you.";
                return RedirectToAction("Dashboard", "Approval");
            }

            agentApplication.Status = "rejected";
            agentApplication.ExaminationStatus = AgentLicenseHelper.ServiceName;
            agentApplication.DateUpdated = DateTime.Now;
            agentApplication.Secretary = _userManager.GetUserId(User) ?? string.Empty;
            _db.Update(agentApplication);

            if (task != null)
            {
                task.Status = "completed";
                task.ApprovedDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Update(task);
            }

            _db.SaveChanges();
            TempData["success"] = "Agent license application rejected.";
            return RedirectToAction("Dashboard", "Approval");
        }

        private async Task PopulateApplyViewAsync(
            ApplicationUser currentUser,
            ApplicationInfo? wholesaleApplication,
            ApplicationInfo? agentApplication,
            string? llbNumber)
        {
            ViewData["Title"] = "Agent License";
            ViewBag.User = currentUser;
            ViewBag.SearchValue = llbNumber ?? wholesaleApplication?.LLBNum ?? string.Empty;
            ViewBag.WholesaleApplication = wholesaleApplication;
            ViewBag.AgentApplication = agentApplication;
            ViewBag.WholesaleLicense = await GetLicenseTypeAsync(wholesaleApplication?.LicenseTypeID);
            ViewBag.AgentLicense = await GetLicenseTypeAsync(agentApplication?.LicenseTypeID);
            ViewBag.WholesaleOutlet = await GetPreferredOutletAsync(wholesaleApplication?.Id);
            ViewBag.PublicationProof = await GetAttachmentAsync(agentApplication?.Id, AgentLicenseHelper.PublicationDocumentTitle);
            ViewBag.TieAffidavit = await GetAttachmentAsync(agentApplication?.Id, AgentLicenseHelper.TieAffidavitDocumentTitle);
            ViewBag.WholesaleLicenseAttachment = await GetAttachmentAsync(agentApplication?.Id, AgentLicenseHelper.WholesaleLicenseDocumentTitle);
        }

        private async Task PopulateReviewViewAsync(ApplicationInfo agentApplication, ApplicationInfo? wholesaleApplication, Tasks? task)
        {
            ViewBag.AgentApplication = agentApplication;
            ViewBag.WholesaleApplication = wholesaleApplication;
            ViewBag.AgentLicense = await GetLicenseTypeAsync(agentApplication.LicenseTypeID);
            ViewBag.WholesaleLicense = await GetLicenseTypeAsync(wholesaleApplication?.LicenseTypeID);
            ViewBag.WholesaleOutlet = await GetPreferredOutletAsync(wholesaleApplication?.Id);
            ViewBag.PublicationProof = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.PublicationDocumentTitle);
            ViewBag.TieAffidavit = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.TieAffidavitDocumentTitle);
            ViewBag.WholesaleLicenseAttachment = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.WholesaleLicenseDocumentTitle);
            ViewBag.Task = task;
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            return string.IsNullOrWhiteSpace(currentUserId)
                ? null
                : await _userManager.Users.FirstOrDefaultAsync(item => item.Id == currentUserId);
        }

        private async Task<ApplicationInfo?> FindApprovedWholesaleApplicationByLlbNumberAsync(string? llbNumber)
        {
            if (string.IsNullOrWhiteSpace(llbNumber))
            {
                return null;
            }

            var application = await _db.ApplicationInfo
                .AsNoTracking()
                .Where(item =>
                    item.LLBNum == llbNumber.Trim()
                    && (item.Status == "approved" || item.Status == "Approved"))
                .OrderByDescending(item => item.ApprovedDate)
                .ThenByDescending(item => item.DateUpdated)
                .FirstOrDefaultAsync();

            var license = await GetLicenseTypeAsync(application?.LicenseTypeID);
            return AgentLicenseHelper.IsWholesaleLicense(license) ? application : null;
        }

        private async Task<ApplicationInfo?> GetWholesaleSourceApplicationAsync(string? sourceApplicationId)
        {
            if (string.IsNullOrWhiteSpace(sourceApplicationId))
            {
                return null;
            }

            return await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == sourceApplicationId);
        }

        private async Task<LicenseTypes?> GetLicenseTypeAsync(string? licenseTypeId)
        {
            if (string.IsNullOrWhiteSpace(licenseTypeId))
            {
                return null;
            }

            return await _db.LicenseTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == licenseTypeId);
        }

        private async Task<LicenseTypes?> GetAgentLicenseTypeAsync(ApplicationInfo? wholesaleApplication)
        {
            var agentLicense = await _db.LicenseTypes
                .Where(item => item.Status != null
                    && item.Status.ToLower() == "active"
                    && item.LicenseName != null
                    && item.LicenseName.ToLower().Contains("agent"))
                .OrderBy(item => item.LicenseName)
                .FirstOrDefaultAsync();

            if (agentLicense != null)
            {
                return agentLicense;
            }

            agentLicense = new LicenseTypes
            {
                Id = Guid.NewGuid().ToString(),
                LicenseName = "Agent License",
                Description = "Agent License",
                Status = "active",
                UserId = _userManager.GetUserId(User) ?? string.Empty,
                LicenseCode = "AG",
                CityFee = 0,
                MunicipaltyFee = 0,
                TownFee = 0,
                RDCFee = 0,
                ConditionList = string.Empty,
                LicenseInstructions = string.Empty,
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };

            _db.Add(agentLicense);
            await _db.SaveChangesAsync();
            return agentLicense;
        }

        private async Task<OutletInfo?> GetPreferredOutletAsync(string? applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return null;
            }

            return await _db.OutletInfo
                .AsNoTracking()
                .Where(item => item.ApplicationId == applicationId)
                .OrderByDescending(item => item.Status != null && item.Status.ToLower() == "active")
                .ThenByDescending(item => item.DateUpdated)
                .FirstOrDefaultAsync();
        }

        private async Task<DistrictCodes?> GetDistrictCodeAsync(string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            return await _db.DistrictCodes.AsNoTracking().FirstOrDefaultAsync(item => item.District == city);
        }

        private async Task<AttachmentInfo?> GetAttachmentAsync(string? applicationId, string documentTitle)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return null;
            }

            return await _db.AttachmentInfo
                .AsNoTracking()
                .Where(item => item.ApplicationId == applicationId && item.DocumentTitle == documentTitle)
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();
        }

        private async Task<Tasks?> GetReviewTaskAsync(string? applicationId, bool includeCompleted)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return null;
            }

            var query = _db.Tasks.Where(item =>
                item.ApplicationId == applicationId
                && item.Service == AgentLicenseHelper.ServiceName);

            if (!includeCompleted)
            {
                query = query.Where(item => item.Status == "assigned");
            }

            if (!CanBypassReviewAssignment())
            {
                var currentUserId = _userManager.GetUserId(User);
                query = query.Where(item => item.ApproverId == currentUserId);
            }

            return await query
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();
        }

        private bool CanBypassReviewAssignment()
        {
            return User.IsInRole("admin") || User.IsInRole("super user");
        }

        private static AttachmentInfo BuildAgentAttachment(string userId, string applicationId, string title, string location, DateTime now)
        {
            return new AttachmentInfo
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                ApplicationId = applicationId,
                DocumentTitle = title,
                DocumentLocation = location,
                Status = "uploaded",
                DateAdded = now,
                DateUpdated = now
            };
        }

        private async Task<string> SaveApplicantFileAsync(IFormFile file, string prefix, string applicationId)
        {
            var directory = Path.Combine("wwwroot", "ApplicantInfo");
            Directory.CreateDirectory(directory);
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{prefix}_{applicationId}{extension}";
            var relativePath = Path.Combine("ApplicantInfo", fileName).Replace("\\", "/");
            var absolutePath = Path.Combine(directory, fileName);

            await using var fileStream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(fileStream);
            return relativePath;
        }

        private async Task<string> SaveApplicationAttachmentAsync(IFormFile file)
        {
            var directory = Path.Combine("wwwroot", "ApplicationAttchments");
            Directory.CreateDirectory(directory);
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var relativePath = Path.Combine("ApplicationAttchments", fileName).Replace("\\", "/");
            var absolutePath = Path.Combine(directory, fileName);

            await using var fileStream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(fileStream);
            return relativePath;
        }

        private static string BuildAgentLlbNumber(DistrictCodes? district, string? reference, LicenseTypes? license)
        {
            var districtCode = string.IsNullOrWhiteSpace(district?.DistrictCode) ? "00" : district!.DistrictCode;
            var refNumber = string.IsNullOrWhiteSpace(reference) ? ReferenceFallback() : reference;
            var licenseCode = string.IsNullOrWhiteSpace(license?.LicenseCode) ? "AG" : license!.LicenseCode;
            return $"{districtCode}{DateTime.Now:yy}{refNumber}{licenseCode}";
        }

        private static string ReferenceFallback()
        {
            return DateTime.Now.ToString("MMddHHmmss");
        }
    }
}
