using LLB.Data;
using LLB.Helpers;
using LLB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webdev.Payments;

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
        public async Task<IActionResult> ApplyAsync(string? llbNumber, string? id, bool fromPostFormation = false)
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
                var payment = RefreshAgentLicensePaymentStatus(agentApplication);
                await TrySubmitAgentForVerificationAsync(agentApplication, payment, showMessages: false);
            }
            else if (!string.IsNullOrWhiteSpace(llbNumber))
            {
                wholesaleApplication = await FindApprovedWholesaleApplicationByLlbNumberAsync(llbNumber);
                if (wholesaleApplication == null)
                {
                    TempData["error"] = "The LLB number entered is not an approved wholesale licence. You cannot proceed with an agent license application under this licence.";
                }
            }

            await PopulateApplyViewAsync(currentUser, wholesaleApplication, agentApplication, llbNumber, fromPostFormation);
            return View();
        }

        [HttpPost("Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAsync(
            string llbNumber,
            string title,
            string name,
            string surname,
            string dob,
            string gender,
            string nationality,
            string idPass,
            string applicantType,
            string businessName,
            bool fromPostFormation)
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

            title = (title ?? string.Empty).Trim();
            name = (name ?? string.Empty).Trim();
            surname = (surname ?? string.Empty).Trim();
            dob = (dob ?? string.Empty).Trim();
            gender = (gender ?? string.Empty).Trim();
            nationality = (nationality ?? string.Empty).Trim();
            idPass = (idPass ?? string.Empty).Trim();
            applicantType = string.IsNullOrWhiteSpace(applicantType) ? "Individual" : applicantType.Trim();
            businessName = $"{name} {surname}".Trim();

            if (string.IsNullOrWhiteSpace(title)
                || string.IsNullOrWhiteSpace(name)
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

            var licenseType = await GetAgentLicenseTypeAsync(wholesaleApplication);
            var wholesaleOutlet = await GetPreferredOutletAsync(wholesaleApplication.Id);
            var licenseRegion = await GetLicenseRegionAsync(wholesaleApplication.ApplicationType);
            var applicationFee = GetLicenseFee(licenseType, licenseRegion);
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
                PaymentFee = applicationFee,
                Title = title,
                PlaceOfBirth = name,
                PlaceOfEntry = surname,
                DateofEntryIntoZimbabwe = dob,
                OperationAddress = nationality,
                ApplicantType = applicantType,
                BusinessName = businessName,
                IdPass = idPass,
                Status = applicationFee > 0 ? "awaiting payment" : "payment not required",
                ApplicationDate = now,
                DateUpdated = now,
                InspectorID = string.Empty,
                Secretary = string.Empty,
                RejectionReason = string.Empty,
                RenewalStatus = gender,
                CompanyNumber = wholesaleApplication.Id
            };

            _db.Add(agentApplication);

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

            _db.SaveChanges();

            TempData["success"] = "Agent license application saved. Upload each required document before payment or verification.";
            return RedirectToAction("Apply", new { id = applicationId, fromPostFormation });
        }

        [HttpPost("UploadAttachment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachmentAsync(string id, string documentType, IFormFile file)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var agentApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUser.Id
                && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToAction("AgentLicenseListings", "Home");
            }

            var payment = GetLatestAgentLicensePayment(agentApplication);
            if (!CanUploadAgentAttachments(agentApplication, payment))
            {
                TempData["error"] = "Agent license documents can only be updated before payment starts.";
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            if (file == null || file.Length <= 0)
            {
                TempData["error"] = "Choose a file to upload.";
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            var now = DateTime.Now;
            var normalizedDocumentType = NormalizeDocumentType(documentType);
            var documentLabel = GetAgentDocumentLabel(documentType);

            switch (normalizedDocumentType)
            {
                case "tieaffidavit":
                    await SaveAgentAttachmentAsync(
                        agentApplication.Id,
                        currentUser.Id,
                        AgentLicenseHelper.TieAffidavitDocumentTitle,
                        file,
                        now);
                    break;

                case "fingerprints":
                case "fingerprint":
                    agentApplication.Fingerprints = await SaveApplicantFileAsync(file, "Fingerprints", agentApplication.Id!);
                    break;

                case "form55":
                case "formff":
                    agentApplication.FormFF = await SaveApplicantFileAsync(file, "FormFF", agentApplication.Id!);
                    break;

                case "idcopy":
                case "id":
                    agentApplication.IdCopy = await SaveApplicantFileAsync(file, "IDCopy", agentApplication.Id!);
                    break;

                case "wholesalelicense":
                    await SaveAgentAttachmentAsync(
                        agentApplication.Id,
                        currentUser.Id,
                        AgentLicenseHelper.WholesaleLicenseDocumentTitle,
                        file,
                        now);
                    break;

                default:
                    TempData["error"] = "The selected attachment type is not supported.";
                    return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            agentApplication.DateUpdated = now;
            _db.Update(agentApplication);
            await _db.SaveChangesAsync();

            TempData["success"] = $"{documentLabel} uploaded successfully.";
            return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
        }

        [HttpGet("Payment")]
        public async Task<IActionResult> PaymentAsync(string id, string? currency = null)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var agentApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUser.Id
                && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToAction("AgentLicenseListings", "Home");
            }

            if (!IsAgentPaymentStage(agentApplication))
            {
                TempData["error"] = "This agent license application is not open for payment.";
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            var fee = agentApplication.PaymentFee ?? 0;
            var tieAffidavit = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.TieAffidavitDocumentTitle);
            var wholesaleLicenseAttachment = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.WholesaleLicenseDocumentTitle);

            if (!HasRequiredAgentAttachments(agentApplication, tieAffidavit, wholesaleLicenseAttachment))
            {
                TempData["error"] = "Upload all required agent license documents before making payment.";
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            if (fee <= 0)
            {
                agentApplication.PaymentStatus = "Not Required";
                agentApplication.Status = "payment not required";
                agentApplication.DateUpdated = DateTime.Now;
                _db.Update(agentApplication);
                await _db.SaveChangesAsync();

                var submitted = await TrySubmitAgentForVerificationAsync(agentApplication, null, showMessages: true);
                if (!submitted && TempData["error"] == null)
                {
                    TempData["success"] = "No payment is required for this agent license application.";
                }

                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            var existingTransaction = RefreshAgentLicensePaymentStatus(agentApplication);
            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                agentApplication.PaymentId = existingTransaction.Id;
                agentApplication.PaymentStatus = existingTransaction.PaymentStatus ?? existingTransaction.Status ?? "Paid";
                agentApplication.Status = "paid";
                agentApplication.DateUpdated = DateTime.Now;
                _db.Update(agentApplication);
                await _db.SaveChangesAsync();

                var submitted = await TrySubmitAgentForVerificationAsync(agentApplication, existingTransaction, showMessages: true);
                if (!submitted && TempData["error"] == null)
                {
                    TempData["success"] = "This agent license application has already been paid for.";
                }

                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = "Complete the current agent license payment before starting another one.";

                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            PaynowCurrencyContext paymentCurrency;
            try
            {
                paymentCurrency = PaynowCurrencyHelper.BuildPaymentContext(_db, fee, currency);
            }
            catch (InvalidOperationException ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            var paynow = PaynowCurrencyHelper.CreatePaynow(paymentCurrency);
            var callbackUrl = PaynowCurrencyHelper.BuildReturnUrl("/AgentLicense/Apply?id=" + agentApplication.Id + "&fromPostFormation=True", paymentCurrency.PaymentMode);
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            var payment = paynow.CreatePayment("12345");
            payment.Add(AgentLicenseHelper.ServiceName, paymentCurrency.PaynowAmount);

            var response = paynow.Send(payment);
            if (!response.Success())
            {
                TempData["error"] = "The payment request could not be created. Try again.";
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            var transaction = new Payments
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUser.Id,
                ApplicationId = agentApplication.Id,
                Service = AgentLicenseHelper.ServiceName,
                PollUrl = response.PollUrl(),
                PopDoc = string.Empty,
                SystemRef = response.RedirectLink(),
                Status = "not paid",
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };
            PaynowCurrencyHelper.ApplyCurrency(transaction, paymentCurrency);

            var status = paynow.PollTransaction(transaction.PollUrl);
            var statusData = status.GetData();
            transaction.PaynowRef = statusData["paynowreference"];
            transaction.PaymentStatus = statusData["status"];

            _db.Add(transaction);

            agentApplication.PaymentId = transaction.Id;
            agentApplication.PaymentStatus = transaction.PaymentStatus ?? transaction.Status ?? "Not Paid";
            agentApplication.DateUpdated = DateTime.Now;
            _db.Update(agentApplication);
            await _db.SaveChangesAsync();

            return Redirect(response.RedirectLink());
        }

        [HttpPost("Continue")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContinueAsync(string id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var agentApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUser.Id
                && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToAction("AgentLicenseListings", "Home");
            }

            if (!IsAgentPaymentStage(agentApplication)
                && !string.Equals(agentApplication.Status, "paid", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(agentApplication.Status, "payment not required", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This agent license application has already been submitted for verification.";
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            var payment = RefreshAgentLicensePaymentStatus(agentApplication);
            var paymentRequired = (agentApplication.PaymentFee ?? 0) > 0;
            var paymentComplete = !paymentRequired
                || (payment != null && HasPaymentStatus(payment, "Paid"))
                || string.Equals(agentApplication.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase);

            if (!paymentComplete)
            {
                TempData["error"] = "Complete payment before submitting the agent license application for verification.";
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            if (!await TrySubmitAgentForVerificationAsync(agentApplication, payment, showMessages: true))
            {
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            TempData["success"] ??= "Agent license application submitted to a verifier for examination.";
            return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
        }

        [HttpPost("UpdateApplicantDetails")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicantDetailsAsync(
            string id,
            string title,
            string name,
            string surname,
            string dob,
            string gender,
            string nationality,
            string idPass,
            string applicantType)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var agentApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUser.Id
                && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToAction("AgentLicenseListings", "Home");
            }

            if (string.Equals(agentApplication.Status, "approved", StringComparison.OrdinalIgnoreCase)
                || string.Equals(agentApplication.Status, "rejected", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Applicant details cannot be edited after the agent license application has been finalised.";
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            title = (title ?? string.Empty).Trim();
            name = (name ?? string.Empty).Trim();
            surname = (surname ?? string.Empty).Trim();
            dob = (dob ?? string.Empty).Trim();
            gender = (gender ?? string.Empty).Trim();
            nationality = (nationality ?? string.Empty).Trim();
            idPass = (idPass ?? string.Empty).Trim();
            applicantType = string.IsNullOrWhiteSpace(applicantType) ? "Individual" : applicantType.Trim();
            var businessName = $"{name} {surname}".Trim();

            if (string.IsNullOrWhiteSpace(title)
                || string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(surname)
                || string.IsNullOrWhiteSpace(dob)
                || string.IsNullOrWhiteSpace(gender)
                || string.IsNullOrWhiteSpace(nationality)
                || string.IsNullOrWhiteSpace(idPass)
                || string.IsNullOrWhiteSpace(businessName))
            {
                TempData["error"] = "Complete all agent information fields before saving.";
                return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
            }

            agentApplication.Title = title;
            agentApplication.PlaceOfBirth = name;
            agentApplication.PlaceOfEntry = surname;
            agentApplication.DateofEntryIntoZimbabwe = dob;
            agentApplication.RenewalStatus = gender;
            agentApplication.OperationAddress = nationality;
            agentApplication.IdPass = idPass;
            agentApplication.ApplicantType = applicantType;
            agentApplication.BusinessName = businessName;
            agentApplication.DateUpdated = DateTime.Now;
            _db.Update(agentApplication);

            var outlet = await _db.OutletInfo.FirstOrDefaultAsync(item => item.ApplicationId == agentApplication.Id);
            if (outlet != null)
            {
                outlet.TradingName = businessName;
                outlet.DirectorNames = businessName;
                outlet.DateUpdated = DateTime.Now;
                _db.Update(outlet);
            }

            await _db.SaveChangesAsync();

            TempData["success"] = "Agent license applicant details updated.";
            return RedirectToAction("Apply", new { id = agentApplication.Id, fromPostFormation = true });
        }

        [Authorize(Roles = "verifier,recommender,secretary,admin,super user")]
        [HttpGet("ViewApplications")]
        public async Task<IActionResult> ViewApplicationsAsync(string id)
        {
            var agentApplication = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToReviewDashboard();
            }

            var task = await GetReviewTaskAsync(agentApplication.Id, includeCompleted: true);
            if (!CanBypassReviewAssignment() && task == null)
            {
                TempData["error"] = "This agent license application is not assigned to you.";
                return RedirectToReviewDashboard();
            }

            var wholesaleApplication = await GetWholesaleSourceApplicationAsync(agentApplication.CompanyNumber);
            await PopulateReviewViewAsync(agentApplication, wholesaleApplication, task);
            return View();
        }

        [Authorize(Roles = "verifier,recommender,secretary,admin,super user")]
        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAsync(string id)
        {
            var agentApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToReviewDashboard();
            }

            var task = await GetReviewTaskAsync(agentApplication.Id, includeCompleted: false);
            if (!CanBypassReviewAssignment() && task == null)
            {
                TempData["error"] = "This agent license application is not assigned to you.";
                return RedirectToReviewDashboard();
            }

            var reviewStage = GetReviewStage(task);
            var now = DateTime.Now;

            if (reviewStage == "verification")
            {
                var recommenderId = await GetAvailableRecommenderIdAsync();
                if (string.IsNullOrWhiteSpace(recommenderId))
                {
                    TempData["error"] = "No recommender is currently available to receive this agent license application.";
                    return RedirectToAction("ViewApplications", new { id = agentApplication.Id });
                }

                agentApplication.Status = "verified";
                agentApplication.DateUpdated = now;
                _db.Update(agentApplication);

                CompleteTask(task, now);

                _db.Add(new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = agentApplication.Id,
                    RecommenderId = recommenderId,
                    AssignerId = "system",
                    Service = AgentLicenseHelper.ServiceName,
                    ExaminationStatus = "recommendation",
                    Status = "assigned",
                    DateAdded = now,
                    DateUpdated = now
                });

                await _db.SaveChangesAsync();

                TempData["success"] = "Agent license application verified and sent to a recommender.";
                return RedirectToAction("ViewApplications", new { id = agentApplication.Id });
            }

            if (reviewStage == "recommendation")
            {
                var secretaryId = await GetAvailableSecretaryIdAsync();
                if (string.IsNullOrWhiteSpace(secretaryId))
                {
                    TempData["error"] = "No secretary is currently available to receive this agent license application.";
                    return RedirectToAction("ViewApplications", new { id = agentApplication.Id });
                }

                agentApplication.Status = "recommended";
                agentApplication.DateUpdated = now;
                _db.Update(agentApplication);

                CompleteTask(task, now);

                _db.Add(new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = agentApplication.Id,
                    ApproverId = secretaryId,
                    AssignerId = "system",
                    Service = AgentLicenseHelper.ServiceName,
                    ExaminationStatus = "approval",
                    Status = "assigned",
                    DateAdded = now,
                    DateUpdated = now
                });

                await _db.SaveChangesAsync();

                TempData["success"] = "Agent license application recommended and sent to the secretary for approval.";
                return RedirectToAction("ViewApplications", new { id = agentApplication.Id });
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
                CompleteTask(task, now);
            }

            await _db.SaveChangesAsync();
            DownloadStatusHelper.OpenLicenseDownload(_db, agentApplication, agentApplication.UserID);

            TempData["success"] = "Agent license application approved. The agent licence is now available for download.";
            return RedirectToAction("ViewApplications", new { id = agentApplication.Id });
        }

        [Authorize(Roles = "verifier,recommender,secretary,admin,super user")]
        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAsync(string id)
        {
            var agentApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id && item.ExaminationStatus == AgentLicenseHelper.ServiceName);

            if (agentApplication == null)
            {
                TempData["error"] = "The agent license application could not be found.";
                return RedirectToReviewDashboard();
            }

            var task = await GetReviewTaskAsync(agentApplication.Id, includeCompleted: false);
            if (!CanBypassReviewAssignment() && task == null)
            {
                TempData["error"] = "This agent license application is not assigned to you.";
                return RedirectToReviewDashboard();
            }

            agentApplication.Status = "rejected";
            agentApplication.ExaminationStatus = AgentLicenseHelper.ServiceName;
            agentApplication.DateUpdated = DateTime.Now;
            agentApplication.Secretary = _userManager.GetUserId(User) ?? string.Empty;
            _db.Update(agentApplication);

            if (task != null)
            {
                CompleteTask(task, DateTime.Now);
            }

            _db.SaveChanges();
            TempData["success"] = "Agent license application rejected.";
            return RedirectToReviewDashboard();
        }

        private async Task PopulateApplyViewAsync(
            ApplicationUser currentUser,
            ApplicationInfo? wholesaleApplication,
            ApplicationInfo? agentApplication,
            string? llbNumber,
            bool fromPostFormation)
        {
            ViewData["Title"] = "Agent License";
            ViewBag.User = currentUser;
            ViewBag.FromPostFormation = fromPostFormation;
            ViewBag.SearchValue = llbNumber ?? wholesaleApplication?.LLBNum ?? string.Empty;
            ViewBag.WholesaleApplication = wholesaleApplication;
            ViewBag.AgentApplication = agentApplication;
            ViewBag.WholesaleLicense = await GetLicenseTypeAsync(wholesaleApplication?.LicenseTypeID);
            ViewBag.AgentLicense = await GetLicenseTypeAsync(agentApplication?.LicenseTypeID);
            ViewBag.WholesaleOutlet = await GetPreferredOutletAsync(wholesaleApplication?.Id);
            ViewBag.PublicationProof = await GetAttachmentAsync(agentApplication?.Id, AgentLicenseHelper.PublicationDocumentTitle);
            ViewBag.TieAffidavit = await GetAttachmentAsync(agentApplication?.Id, AgentLicenseHelper.TieAffidavitDocumentTitle);
            ViewBag.WholesaleLicenseAttachment = await GetAttachmentAsync(agentApplication?.Id, AgentLicenseHelper.WholesaleLicenseDocumentTitle);
            ViewBag.Payment = RefreshAgentLicensePaymentStatus(agentApplication);
            ViewBag.LatestZwgExchangeRate = await _db.ExchangeRate
                .Where(rate => rate.ZWGrate.HasValue && rate.ZWGrate.Value > 0)
                .OrderByDescending(rate => rate.DateUpdated)
                .ThenByDescending(rate => rate.DateAdded)
                .Select(rate => rate.ZWGrate)
                .FirstOrDefaultAsync();
        }

        private async Task PopulateReviewViewAsync(ApplicationInfo agentApplication, ApplicationInfo? wholesaleApplication, Tasks? task)
        {
            var reviewStage = GetReviewStage(task);
            ViewBag.AgentApplication = agentApplication;
            ViewBag.WholesaleApplication = wholesaleApplication;
            ViewBag.AgentLicense = await GetLicenseTypeAsync(agentApplication.LicenseTypeID);
            ViewBag.WholesaleLicense = await GetLicenseTypeAsync(wholesaleApplication?.LicenseTypeID);
            ViewBag.WholesaleOutlet = await GetPreferredOutletAsync(wholesaleApplication?.Id);
            ViewBag.PublicationProof = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.PublicationDocumentTitle);
            ViewBag.TieAffidavit = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.TieAffidavitDocumentTitle);
            ViewBag.WholesaleLicenseAttachment = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.WholesaleLicenseDocumentTitle);
            ViewBag.Task = task;
            ViewBag.CanReviewAction = task != null
                && string.Equals(task.Status, "assigned", StringComparison.OrdinalIgnoreCase);
            ViewBag.ReviewStageLabel = reviewStage switch
            {
                "verification" => "Verifier examination",
                "recommendation" => "Recommender examination",
                _ => "Secretary approval"
            };
            ViewBag.ApproveButtonLabel = reviewStage switch
            {
                "verification" => "Verify",
                "recommendation" => "Recommend",
                _ => "Approve"
            };
            ViewBag.RejectButtonLabel = reviewStage switch
            {
                "verification" => "Reject",
                "recommendation" => "Reject",
                _ => "Reject"
            };
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

        private async Task<LicenseRegion?> GetLicenseRegionAsync(string? licenseRegionId)
        {
            if (string.IsNullOrWhiteSpace(licenseRegionId))
            {
                return null;
            }

            return await _db.LicenseRegions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == licenseRegionId);
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

        private Payments? GetLatestAgentLicensePayment(ApplicationInfo? agentApplication)
        {
            if (agentApplication == null || string.IsNullOrWhiteSpace(agentApplication.Id))
            {
                return null;
            }

            return _db.Payments
                .Where(item => item.ApplicationId == agentApplication.Id
                    && (item.Service == AgentLicenseHelper.ServiceName || item.Service == "agent license"))
                .OrderByDescending(item => item.DateAdded)
                .FirstOrDefault();
        }

        private Payments? RefreshAgentLicensePaymentStatus(ApplicationInfo? agentApplication)
        {
            var payment = GetLatestAgentLicensePayment(agentApplication);
            if (payment == null || string.IsNullOrWhiteSpace(payment.PollUrl))
            {
                return payment;
            }

            var paynow = PaynowCurrencyHelper.CreatePaynow(payment);
            var status = paynow.PollTransaction(payment.PollUrl);
            var statusData = status.GetData();
            payment.PaynowRef = statusData["paynowreference"];
            payment.PaymentStatus = statusData["status"];
            payment.DateUpdated = DateTime.Now;
            _db.Update(payment);

            if (agentApplication != null)
            {
                agentApplication.PaymentId = payment.Id;
                agentApplication.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? agentApplication.PaymentStatus;
                if (HasPaymentStatus(payment, "Paid") && IsAgentPaymentStage(agentApplication))
                {
                    agentApplication.Status = "paid";
                }

                agentApplication.DateUpdated = DateTime.Now;
                _db.Update(agentApplication);
            }

            _db.SaveChanges();
            return payment;
        }

        private static bool HasPaymentStatus(Payments payment, string expectedStatus)
        {
            return string.Equals(payment.Status, expectedStatus, StringComparison.OrdinalIgnoreCase)
                || string.Equals(payment.PaymentStatus, expectedStatus, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsActivePaymentTransaction(Payments payment)
        {
            return !HasPaymentStatus(payment, "Paid")
                && !HasPaymentStatus(payment, "Cancelled")
                && !HasPaymentStatus(payment, "Canceled")
                && !HasPaymentStatus(payment, "Rejected")
                && !HasPaymentStatus(payment, "Expired")
                && !HasPaymentStatus(payment, "Created")
                && !HasPaymentStatus(payment, "Awaiting Delivery");
        }

        private static bool IsAgentPaymentStage(ApplicationInfo? agentApplication)
        {
            var status = agentApplication?.Status?.Trim().ToLowerInvariant();
            return status == "awaiting payment"
                || status == "payment pending"
                || status == "paid"
                || status == "payment not required";
        }

        private static decimal GetLicenseFee(LicenseTypes? licenseType, LicenseRegion? licenseRegion)
        {
            var regionName = licenseRegion?.RegionName?.Trim().ToLowerInvariant();
            double fee = regionName switch
            {
                "city" => licenseType?.CityFee ?? 0,
                "municipality" => licenseType?.MunicipaltyFee ?? 0,
                "town" => licenseType?.TownFee ?? 0,
                "rdc" => licenseType?.RDCFee ?? 0,
                _ => licenseType?.CityFee
                    ?? licenseType?.MunicipaltyFee
                    ?? licenseType?.TownFee
                    ?? licenseType?.RDCFee
                    ?? 0
            };

            return (decimal)fee;
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
                if (User.IsInRole("verifier"))
                {
                    query = query.Where(item => item.VerifierId == currentUserId);
                }
                else if (User.IsInRole("recommender"))
                {
                    query = query.Where(item => item.RecommenderId == currentUserId);
                }
                else
                {
                    query = query.Where(item => item.ApproverId == currentUserId);
                }
            }

            return await query
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();
        }

        private async Task<string?> GetAvailableVerifierIdAsync()
        {
            var users = await _userManager.GetUsersInRoleAsync("verifier");
            return users.Count == 0 ? null : await _taskAllocationHelper.GetVerifier(_db, _userManager);
        }

        private async Task<string?> GetAvailableRecommenderIdAsync()
        {
            var users = await _userManager.GetUsersInRoleAsync("recommender");
            return users.Count == 0 ? null : await _taskAllocationHelper.GetRecommender(_db, _userManager);
        }

        private async Task<string?> GetAvailableSecretaryIdAsync()
        {
            var users = await _userManager.GetUsersInRoleAsync("secretary");
            return users.Count == 0 ? null : await _taskAllocationHelper.GetSecretary(_db, _userManager);
        }

        private bool CanBypassReviewAssignment()
        {
            return User.IsInRole("admin") || User.IsInRole("super user");
        }

        private IActionResult RedirectToReviewDashboard()
        {
            if (User.IsInRole("verifier"))
            {
                return RedirectToAction("Dashboard", "Verify");
            }

            if (User.IsInRole("recommender"))
            {
                return RedirectToAction("Dashboard", "Recommend");
            }

            return RedirectToAction("Dashboard", "Approval");
        }

        private static string GetReviewStage(Tasks? task)
        {
            var stage = task?.ExaminationStatus?.Trim().ToLowerInvariant();
            if (stage == "verification" || stage == "recommendation" || stage == "approval")
            {
                return stage;
            }

            if (!string.IsNullOrWhiteSpace(task?.VerifierId))
            {
                return "verification";
            }

            if (!string.IsNullOrWhiteSpace(task?.RecommenderId))
            {
                return "recommendation";
            }

            return "approval";
        }

        private static void CompleteTask(Tasks task, DateTime completedAt)
        {
            task.Status = "completed";
            task.DateUpdated = completedAt;

            var stage = GetReviewStage(task);
            if (stage == "verification")
            {
                task.VerificationDate = completedAt;
            }
            else if (stage == "recommendation")
            {
                task.RecommendationDate = completedAt;
            }
            else
            {
                task.ApprovedDate = completedAt;
            }
        }

        private async Task SaveAgentAttachmentAsync(
            string? applicationId,
            string userId,
            string documentTitle,
            IFormFile file,
            DateTime now)
        {
            var attachment = await _db.AttachmentInfo
                .Where(item => item.ApplicationId == applicationId && item.DocumentTitle == documentTitle)
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();

            if (attachment == null)
            {
                attachment = new AttachmentInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ApplicationId = applicationId,
                    DocumentTitle = documentTitle,
                    Status = "uploaded",
                    DateAdded = now
                };

                _db.Add(attachment);
            }

            attachment.DocumentLocation = await SaveApplicationAttachmentAsync(file);
            attachment.Status = "uploaded";
            attachment.DateUpdated = now;
        }

        private async Task<bool> TrySubmitAgentForVerificationAsync(
            ApplicationInfo? agentApplication,
            Payments? payment,
            bool showMessages)
        {
            if (agentApplication == null || !IsAgentPaymentStage(agentApplication))
            {
                return false;
            }

            var paymentRequired = (agentApplication.PaymentFee ?? 0) > 0;
            var paymentComplete = !paymentRequired
                || (payment != null && HasPaymentStatus(payment, "Paid"))
                || string.Equals(agentApplication.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(agentApplication.PaymentStatus, "Not Required", StringComparison.OrdinalIgnoreCase);

            if (!paymentComplete)
            {
                return false;
            }

            var tieAffidavit = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.TieAffidavitDocumentTitle);
            var wholesaleLicenseAttachment = await GetAttachmentAsync(agentApplication.Id, AgentLicenseHelper.WholesaleLicenseDocumentTitle);
            if (!HasRequiredAgentAttachments(agentApplication, tieAffidavit, wholesaleLicenseAttachment))
            {
                if (showMessages)
                {
                    TempData["error"] = "Upload all required agent license documents before submitting for verification.";
                }

                return false;
            }

            var existingTask = await _db.Tasks.FirstOrDefaultAsync(task =>
                task.ApplicationId == agentApplication.Id
                && task.Service == AgentLicenseHelper.ServiceName
                && task.Status == "assigned");

            if (existingTask == null)
            {
                var verifierId = await GetAvailableVerifierIdAsync();
                if (string.IsNullOrWhiteSpace(verifierId))
                {
                    if (showMessages)
                    {
                        TempData["error"] = "No verifier is currently available to receive this agent license application.";
                    }

                    return false;
                }

                var now = DateTime.Now;
                _db.Add(new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = agentApplication.Id,
                    VerifierId = verifierId,
                    AssignerId = "system",
                    Service = AgentLicenseHelper.ServiceName,
                    ExaminationStatus = "verification",
                    Status = "assigned",
                    DateAdded = now,
                    DateUpdated = now
                });
            }

            agentApplication.Status = "awaiting verification";
            agentApplication.PaymentStatus = payment?.PaymentStatus
                ?? payment?.Status
                ?? agentApplication.PaymentStatus
                ?? (paymentRequired ? "Paid" : "Not Required");
            agentApplication.DateUpdated = DateTime.Now;
            _db.Update(agentApplication);
            await _db.SaveChangesAsync();

            if (showMessages)
            {
                TempData["success"] = "Agent license application submitted to a verifier for examination.";
            }

            return true;
        }

        private static bool HasRequiredAgentAttachments(
            ApplicationInfo? agentApplication,
            AttachmentInfo? tieAffidavit,
            AttachmentInfo? wholesaleLicenseAttachment)
        {
            return agentApplication != null
                && !string.IsNullOrWhiteSpace(agentApplication.IdCopy)
                && !string.IsNullOrWhiteSpace(agentApplication.Fingerprints)
                && !string.IsNullOrWhiteSpace(agentApplication.FormFF)
                && tieAffidavit != null
                && !string.IsNullOrWhiteSpace(tieAffidavit.DocumentLocation)
                && wholesaleLicenseAttachment != null
                && !string.IsNullOrWhiteSpace(wholesaleLicenseAttachment.DocumentLocation);
        }

        private static bool CanUploadAgentAttachments(ApplicationInfo? agentApplication, Payments? payment)
        {
            if (agentApplication == null || !IsAgentPaymentStage(agentApplication))
            {
                return false;
            }

            if (payment == null)
            {
                return true;
            }

            return !IsActivePaymentTransaction(payment) && !HasPaymentStatus(payment, "Paid");
        }

        private static string NormalizeDocumentType(string? documentType)
        {
            return new string((documentType ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static string GetAgentDocumentLabel(string? documentType)
        {
            return NormalizeDocumentType(documentType) switch
            {
                "tieaffidavit" => AgentLicenseHelper.TieAffidavitDocumentTitle,
                "fingerprint" or "fingerprints" => "Fingerprint",
                "form55" or "formff" => "Form 55",
                "id" or "idcopy" => "ID",
                "wholesalelicense" => AgentLicenseHelper.WholesaleLicenseDocumentTitle,
                _ => "Attachment"
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
