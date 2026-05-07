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
    [Route("TemporaryRemoval")]
    public class TemporaryRemovalController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public TemporaryRemovalController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            TaskAllocationHelper taskAllocationHelper)
        {
            _db = db;
            _userManager = userManager;
            _taskAllocationHelper = taskAllocationHelper;
        }

        [HttpGet("Apply")]
        public async Task<IActionResult> ApplyAsync(string? applicationId, string? recordId, string? id)
        {
            applicationId ??= id;

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ApplicationInfo? sourceApplication;
            ApplicationInfo? removalApplication;

            if (!string.IsNullOrWhiteSpace(recordId))
            {
                removalApplication = await _db.ApplicationInfo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.Id == recordId
                        && item.UserID == currentUser.Id
                        && item.ExaminationStatus == TemporaryRemovalHelper.ServiceName);

                if (removalApplication == null)
                {
                    TempData["error"] = "The temporary removal application could not be found.";
                    return RedirectToAction("Dashboard", "Home");
                }

                sourceApplication = await GetSourceApplicationAsync(removalApplication.CompanyNumber);
            }
            else
            {
                sourceApplication = await GetSourceApplicationAsync(applicationId, currentUser.Id, requireApproved: true);
                if (sourceApplication == null)
                {
                    TempData["error"] = "The selected licence could not be found or is not approved for temporary removal.";
                    return RedirectToAction("Dashboard", "Home");
                }

                removalApplication = await GetOpenTemporaryRemovalApplicationAsync(currentUser.Id, sourceApplication.Id);
                if (removalApplication == null)
                {
                    removalApplication = await CreateDraftTemporaryRemovalAsync(currentUser, sourceApplication);
                    return RedirectToAction("Apply", new { recordId = removalApplication.Id });
                }
            }

            if (sourceApplication == null || removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be prepared.";
                return RedirectToAction("Dashboard", "Home");
            }

            await PopulateApplyViewAsync(currentUser, sourceApplication, removalApplication);
            return View();
        }

        [HttpPost("SaveOutlet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveOutletAsync(
            string recordId,
            [FromForm(Name = "Address")] string address,
            string province,
            string city,
            string council,
            [FromForm(Name = "LicenseTypeID")] string licenseTypeId,
            [FromForm(Name = "ApplicationType")] string applicationType,
            string? latitude,
            string? longitude)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var removalApplication = await GetOwnedTemporaryRemovalApplicationAsync(recordId, currentUser.Id);
            if (removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!CanEditTemporaryRemoval(removalApplication))
            {
                TempData["error"] = "The new outlet details can only be edited before the temporary removal application is submitted.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            if (string.IsNullOrWhiteSpace(address)
                || string.IsNullOrWhiteSpace(province)
                || string.IsNullOrWhiteSpace(city)
                || string.IsNullOrWhiteSpace(council)
                || string.IsNullOrWhiteSpace(licenseTypeId)
                || string.IsNullOrWhiteSpace(applicationType))
            {
                TempData["error"] = "Complete the licence type, location of premises, address, province, district, and council before saving the new outlet information.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var sourceApplication = await GetSourceApplicationAsync(removalApplication.CompanyNumber);
            if (sourceApplication == null)
            {
                TempData["error"] = "The source licence could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var proposedOutlet = await GetProposedOutletAsync(removalApplication.Id);
            var originalOutlet = await GetPreferredOutletAsync(sourceApplication.Id, asNoTracking: true);
            var now = DateTime.Now;
            var lockedTradingName = proposedOutlet?.TradingName;

            if (string.IsNullOrWhiteSpace(lockedTradingName))
            {
                lockedTradingName = originalOutlet?.TradingName
                    ?? sourceApplication.BusinessName
                    ?? removalApplication.BusinessName;
            }

            if (string.IsNullOrWhiteSpace(lockedTradingName))
            {
                TempData["error"] = "The trading name could not be resolved for this temporary removal application.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            if (proposedOutlet == null)
            {
                proposedOutlet = new OutletInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = removalApplication.Id,
                    UserId = currentUser.Id,
                    Status = "draft",
                    LicenseTypeID = licenseTypeId.Trim(),
                    ApplicationType = applicationType.Trim(),
                    DateAdded = now
                };

                _db.Add(proposedOutlet);
            }

            proposedOutlet.TradingName = lockedTradingName.Trim();
            proposedOutlet.Address = address.Trim();
            proposedOutlet.Province = province.Trim();
            proposedOutlet.City = city.Trim();
            proposedOutlet.Council = council.Trim();
            proposedOutlet.Latitude = latitude?.Trim() ?? string.Empty;
            proposedOutlet.Longitude = longitude?.Trim() ?? string.Empty;
            proposedOutlet.LicenseTypeID = licenseTypeId.Trim();
            proposedOutlet.ApplicationType = applicationType.Trim();
            proposedOutlet.DateUpdated = now;

            removalApplication.BusinessName = proposedOutlet.TradingName;
            removalApplication.OperationAddress = proposedOutlet.Address;
            removalApplication.LLBNum = sourceApplication.LLBNum;
            removalApplication.ApplicantType = sourceApplication.ApplicantType;
            removalApplication.LicenseTypeID = proposedOutlet.LicenseTypeID;
            removalApplication.ApplicationType = proposedOutlet.ApplicationType;
            removalApplication.PaymentFee = await GetTemporaryRemovalFeeAsync(removalApplication);
            removalApplication.DateUpdated = now;

            _db.Update(removalApplication);
            await _db.SaveChangesAsync();

            TempData["success"] = "The proposed new outlet information was saved successfully.";
            return RedirectToAction("Apply", new { recordId = removalApplication.Id });
        }

        [HttpPost("UploadAttachment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachmentAsync(string recordId, string attachmentId, IFormFile file)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var removalApplication = await GetOwnedTemporaryRemovalApplicationAsync(recordId, currentUser.Id);
            if (removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!CanEditTemporaryRemoval(removalApplication))
            {
                TempData["error"] = "Attachments can only be updated before the temporary removal application is submitted.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var attachment = await _db.AttachmentInfo.FirstOrDefaultAsync(item =>
                item.Id == attachmentId
                && item.ApplicationId == removalApplication.Id);

            if (attachment == null)
            {
                TempData["error"] = "The selected attachment could not be found.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            if (file == null || file.Length <= 0)
            {
                TempData["error"] = "Select a file to upload.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            attachment.DocumentLocation = await SaveApplicationAttachmentAsync(file);
            attachment.Status = "uploaded";
            attachment.DateUpdated = DateTime.Now;

            removalApplication.DateUpdated = DateTime.Now;
            _db.Update(removalApplication);
            await _db.SaveChangesAsync();

            TempData["success"] = $"{attachment.DocumentTitle} uploaded successfully.";
            return RedirectToAction("Apply", new { recordId = removalApplication.Id });
        }

        [HttpPost("RemoveAttachment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAttachmentAsync(string recordId, string attachmentId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var removalApplication = await GetOwnedTemporaryRemovalApplicationAsync(recordId, currentUser.Id);
            if (removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!CanEditTemporaryRemoval(removalApplication))
            {
                TempData["error"] = "Attachments can only be changed before the temporary removal application is submitted.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var attachment = await _db.AttachmentInfo.FirstOrDefaultAsync(item =>
                item.Id == attachmentId
                && item.ApplicationId == removalApplication.Id);

            if (attachment == null)
            {
                TempData["error"] = "The selected attachment could not be found.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            attachment.DocumentLocation = string.Empty;
            attachment.Status = "empty";
            attachment.DateUpdated = DateTime.Now;

            removalApplication.DateUpdated = DateTime.Now;
            _db.Update(removalApplication);
            await _db.SaveChangesAsync();

            TempData["success"] = $"{attachment.DocumentTitle} removed successfully.";
            return RedirectToAction("Apply", new { recordId = removalApplication.Id });
        }

        [HttpPost("Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAsync(string recordId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var removalApplication = await GetOwnedTemporaryRemovalApplicationAsync(recordId, currentUser.Id);
            if (removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!CanEditTemporaryRemoval(removalApplication))
            {
                TempData["error"] = "Only draft temporary removal applications can be submitted.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var proposedOutlet = await GetProposedOutletAsync(removalApplication.Id, asNoTracking: true);
            if (!IsOutletComplete(proposedOutlet))
            {
                TempData["error"] = "Complete the proposed new outlet information before submitting the temporary removal application.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var attachments = await EnsureRequiredTemporaryRemovalAttachmentsAsync(removalApplication.Id, currentUser.Id);
            if (!AreAllRequiredDocumentsUploaded(attachments))
            {
                TempData["error"] = "Upload all required temporary removal attachments before submitting the application.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            removalApplication.Status = "submitted";
            removalApplication.DateUpdated = DateTime.Now;
            _db.Update(removalApplication);
            await _db.SaveChangesAsync();

            TempData["success"] = "Temporary removal application submitted. Payment is now the next step.";
            return RedirectToAction("Apply", new { recordId = removalApplication.Id });
        }

        [HttpGet("Payment")]
        public async Task<IActionResult> PaymentAsync(string recordId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var removalApplication = await GetOwnedTemporaryRemovalApplicationAsync(recordId, currentUser.Id);
            if (removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!string.Equals(removalApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Submit the temporary removal application before making payment.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var sourceApplication = await GetSourceApplicationAsync(removalApplication.CompanyNumber);
            if (sourceApplication == null)
            {
                TempData["error"] = "The source licence could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var proposedOutlet = await GetProposedOutletAsync(removalApplication.Id, asNoTracking: true);
            if (!IsOutletComplete(proposedOutlet))
            {
                TempData["error"] = "Complete the proposed new outlet information before making payment.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var attachments = await EnsureRequiredTemporaryRemovalAttachmentsAsync(removalApplication.Id, currentUser.Id);
            if (!AreAllRequiredDocumentsUploaded(attachments))
            {
                TempData["error"] = "Upload all required temporary removal attachments before making payment.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var fee = await GetTemporaryRemovalFeeAsync(removalApplication);
            if (!fee.HasValue)
            {
                TempData["error"] = "The temporary removal fee has not been configured.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var existingTransaction = await GetLatestTemporaryRemovalPaymentAsync(removalApplication.Id);
            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                removalApplication.PaymentStatus = existingTransaction.PaymentStatus ?? existingTransaction.Status ?? "Paid";
                removalApplication.PaymentFee = fee.Value;
                removalApplication.DateUpdated = DateTime.Now;
                _db.Update(removalApplication);
                await _db.SaveChangesAsync();

                TempData["success"] = "This temporary removal application has already been paid for.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = "Complete the current payment before starting another one.";
                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
            var callbackUrl = Url.Action("Apply", "TemporaryRemoval", new { recordId = removalApplication.Id }, Request.Scheme);
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            var paymentReference = !string.IsNullOrWhiteSpace(removalApplication.RefNum)
                ? removalApplication.RefNum
                : removalApplication.Id;

            var payment = paynow.CreatePayment(paymentReference ?? Guid.NewGuid().ToString("N"));

            var licenseType = string.IsNullOrWhiteSpace(sourceApplication.LicenseTypeID)
                ? null
                : await _db.LicenseTypes.FirstOrDefaultAsync(item => item.Id == sourceApplication.LicenseTypeID);

            payment.Add(licenseType?.LicenseName ?? TemporaryRemovalHelper.ServiceName, fee.Value);

            var response = paynow.Send(payment);
            if (!response.Success())
            {
                TempData["error"] = "The payment request could not be created. Try again.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var transaction = new Payments
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUser.Id,
                Amount = payment.Total,
                ApplicationId = removalApplication.Id,
                Service = TemporaryRemovalHelper.ServiceName,
                PollUrl = response.PollUrl(),
                PopDoc = string.Empty,
                Status = "not paid",
                SystemRef = response.RedirectLink(),
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };

            var status = paynow.PollTransaction(transaction.PollUrl);
            var statusData = status.GetData();
            transaction.PaynowRef = statusData["paynowreference"];
            transaction.PaymentStatus = statusData["status"];

            _db.Add(transaction);

            removalApplication.PaymentFee = fee.Value;
            removalApplication.PaymentStatus = transaction.PaymentStatus ?? transaction.Status ?? "Not Paid";
            removalApplication.DateUpdated = DateTime.Now;
            _db.Update(removalApplication);
            await _db.SaveChangesAsync();

            return Redirect(response.RedirectLink());
        }

        [HttpPost("Continue")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContinueAsync(string recordId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var removalApplication = await GetOwnedTemporaryRemovalApplicationAsync(recordId, currentUser.Id);
            if (removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!string.Equals(removalApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This temporary removal application is not ready to continue.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var payment = await GetLatestTemporaryRemovalPaymentAsync(removalApplication.Id);
            if (payment == null || !HasPaymentStatus(payment, "Paid"))
            {
                TempData["error"] = "Payment must be completed before the application can be sent to the secretary.";
                return RedirectToAction("Apply", new { recordId = removalApplication.Id });
            }

            var existingTask = await _db.Tasks.FirstOrDefaultAsync(task =>
                task.ApplicationId == removalApplication.Id
                && task.Status == "assigned"
                && task.Service == TemporaryRemovalHelper.ServiceName);

            if (existingTask == null)
            {
                var secretaryId = await _taskAllocationHelper.GetSecretary(_db, _userManager);
                if (string.IsNullOrWhiteSpace(secretaryId))
                {
                    TempData["error"] = "No secretary is currently available to receive the temporary removal application.";
                    return RedirectToAction("Apply", new { recordId = removalApplication.Id });
                }

                existingTask = new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = removalApplication.Id,
                    ApproverId = secretaryId,
                    AssignerId = "system",
                    Service = TemporaryRemovalHelper.ServiceName,
                    ExaminationStatus = "approval",
                    Status = "assigned",
                    DateAdded = DateTime.Now,
                    DateUpdated = DateTime.Now
                };

                _db.Add(existingTask);
                removalApplication.Secretary = secretaryId;
            }

            removalApplication.Status = "awaiting approval";
            removalApplication.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? "Paid";
            removalApplication.DateUpdated = DateTime.Now;
            _db.Update(removalApplication);
            await _db.SaveChangesAsync();

            TempData["success"] = "Temporary removal application sent to the secretary for approval.";
            return RedirectToAction("Apply", new { recordId = removalApplication.Id });
        }

        [HttpGet("ViewApplications")]
        public async Task<IActionResult> ViewApplicationsAsync(string? recordId, string? id)
        {
            recordId ??= id;

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var assignedTask = await _db.Tasks
                .AsNoTracking()
                .Where(task =>
                    task.ApplicationId == recordId
                    && task.Service == TemporaryRemovalHelper.ServiceName)
                .OrderByDescending(task => task.DateUpdated)
                .ThenByDescending(task => task.DateAdded)
                .FirstOrDefaultAsync();

            if (assignedTask == null || !string.Equals(assignedTask.ApproverId, currentUser.Id, StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "You are not assigned to this temporary removal application.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var removalApplication = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == recordId
                    && item.ExaminationStatus == TemporaryRemovalHelper.ServiceName);

            if (removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var sourceApplication = await GetSourceApplicationAsync(removalApplication.CompanyNumber);
            if (sourceApplication == null)
            {
                TempData["error"] = "The source licence could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            await PopulateReviewViewAsync(sourceApplication, removalApplication, assignedTask);
            return View();
        }

        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAsync(string recordId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var assignedTask = await _db.Tasks.FirstOrDefaultAsync(task =>
                task.ApplicationId == recordId
                && task.Service == TemporaryRemovalHelper.ServiceName
                && task.Status == "assigned");

            if (assignedTask == null || !string.Equals(assignedTask.ApproverId, currentUser.Id, StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "You are not assigned to this temporary removal application.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var removalApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == recordId
                && item.ExaminationStatus == TemporaryRemovalHelper.ServiceName);

            if (removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            if (!string.Equals(removalApplication.Status, "awaiting approval", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only temporary removal applications awaiting approval can be approved.";
                return RedirectToAction("ViewApplications", new { recordId });
            }

            var payment = await GetLatestTemporaryRemovalPaymentAsync(removalApplication.Id);
            if (payment == null || !HasPaymentStatus(payment, "Paid"))
            {
                TempData["error"] = "This temporary removal application cannot be approved until payment is confirmed.";
                return RedirectToAction("ViewApplications", new { recordId });
            }

            removalApplication.Status = "Approved";
            removalApplication.Secretary = currentUser.Id;
            removalApplication.ApprovedDate = DateTime.Now;
            removalApplication.DateUpdated = DateTime.Now;

            assignedTask.Status = "completed";
            assignedTask.ApprovedDate = DateTime.Now;
            assignedTask.DateUpdated = DateTime.Now;

            _db.Update(removalApplication);
            _db.Update(assignedTask);
            await _db.SaveChangesAsync();

            TempData["success"] = "Temporary removal application approved successfully.";
            return RedirectToAction("ViewApplications", new { recordId });
        }

        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAsync(string recordId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var assignedTask = await _db.Tasks.FirstOrDefaultAsync(task =>
                task.ApplicationId == recordId
                && task.Service == TemporaryRemovalHelper.ServiceName
                && task.Status == "assigned");

            if (assignedTask == null || !string.Equals(assignedTask.ApproverId, currentUser.Id, StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "You are not assigned to this temporary removal application.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var removalApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == recordId
                && item.ExaminationStatus == TemporaryRemovalHelper.ServiceName);

            if (removalApplication == null)
            {
                TempData["error"] = "The temporary removal application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            if (!string.Equals(removalApplication.Status, "awaiting approval", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only temporary removal applications awaiting approval can be rejected.";
                return RedirectToAction("ViewApplications", new { recordId });
            }

            removalApplication.Status = "Rejected";
            removalApplication.Secretary = currentUser.Id;
            removalApplication.DateUpdated = DateTime.Now;
            removalApplication.RejectionReason ??= string.Empty;

            assignedTask.Status = "completed";
            assignedTask.ApprovedDate = DateTime.Now;
            assignedTask.DateUpdated = DateTime.Now;

            _db.Update(removalApplication);
            _db.Update(assignedTask);
            await _db.SaveChangesAsync();

            TempData["success"] = "Temporary removal application rejected.";
            return RedirectToAction("ViewApplications", new { recordId });
        }

        private async Task PopulateApplyViewAsync(
            ApplicationUser applicantUser,
            ApplicationInfo sourceApplication,
            ApplicationInfo removalApplication)
        {
            var originalOutlet = await GetPreferredOutletAsync(sourceApplication.Id, asNoTracking: true);
            var proposedOutlet = await GetProposedOutletAsync(removalApplication.Id, asNoTracking: true);
            var attachments = await EnsureRequiredTemporaryRemovalAttachmentsAsync(removalApplication.Id, applicantUser.Id);
            var payment = await RefreshTemporaryRemovalPaymentStatusAsync(removalApplication.Id);
            var fee = await GetTemporaryRemovalFeeAsync(removalApplication);
            var proposedLicenseId = proposedOutlet?.LicenseTypeID ?? removalApplication.LicenseTypeID;
            var proposedRegionId = proposedOutlet?.ApplicationType ?? removalApplication.ApplicationType;

            var paymentReceived = payment != null && HasPaymentStatus(payment, "Paid");
            var hasActivePayment = payment != null && IsActivePaymentTransaction(payment);
            var outletComplete = IsOutletComplete(proposedOutlet);
            var attachmentsComplete = AreAllRequiredDocumentsUploaded(attachments);
            var canEditOutlet = CanEditTemporaryRemoval(removalApplication);
            var canSubmit = canEditOutlet && outletComplete && attachmentsComplete;
            var canMakePayment = string.Equals(removalApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase)
                && !paymentReceived
                && !hasActivePayment;
            var canContinue = string.Equals(removalApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase)
                && paymentReceived;
            var awaitingSecretary = string.Equals(removalApplication.Status, "awaiting approval", StringComparison.OrdinalIgnoreCase);

            ViewData["Title"] = "Temporary Removal";
            ViewData["Subtitle"] = "Move the bar to a new outlet, upload the required approvals, submit, pay, and continue to the secretary.";
            ViewBag.SourceApplication = sourceApplication;
            ViewBag.RemovalApplication = removalApplication;
            ViewBag.OriginalOutlet = originalOutlet;
            ViewBag.ProposedOutlet = proposedOutlet;
            ViewBag.ApplicantUser = applicantUser;
            ViewBag.License = string.IsNullOrWhiteSpace(sourceApplication.LicenseTypeID)
                ? null
                : await _db.LicenseTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.LicenseTypeID);
            ViewBag.Region = string.IsNullOrWhiteSpace(sourceApplication.ApplicationType)
                ? null
                : await _db.LicenseRegions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.ApplicationType);
            ViewBag.ProposedLicense = string.IsNullOrWhiteSpace(proposedLicenseId)
                ? null
                : await _db.LicenseTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == proposedLicenseId);
            ViewBag.ProposedRegion = string.IsNullOrWhiteSpace(proposedRegionId)
                ? null
                : await _db.LicenseRegions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == proposedRegionId);
            ViewBag.LicenseTypes = await _db.LicenseTypes
                .AsNoTracking()
                .OrderBy(item => item.LicenseName)
                .ToListAsync();
            ViewBag.Regions = await _db.LicenseRegions
                .AsNoTracking()
                .OrderBy(item => item.RegionName)
                .ToListAsync();
            ViewBag.Provinces = await _db.Province
                .AsNoTracking()
                .OrderBy(item => item.Name)
                .ToListAsync();
            ViewBag.Attachments = attachments;
            ViewBag.Payment = payment;
            ViewBag.Fee = fee;
            ViewBag.TotalFee = fee ?? removalApplication.PaymentFee;
            ViewBag.OutletComplete = outletComplete;
            ViewBag.AttachmentsComplete = attachmentsComplete;
            ViewBag.CanEditOutlet = canEditOutlet;
            ViewBag.CanUploadAttachments = canEditOutlet;
            ViewBag.CanSubmit = canSubmit;
            ViewBag.CanMakePayment = canMakePayment;
            ViewBag.CanContinue = canContinue;
            ViewBag.AwaitingSecretary = awaitingSecretary;
            ViewBag.PaymentReceived = paymentReceived;
            ViewBag.HasActivePayment = hasActivePayment;
        }

        private async Task PopulateReviewViewAsync(
            ApplicationInfo sourceApplication,
            ApplicationInfo removalApplication,
            Tasks assignedTask)
        {
            var originalOutlet = await GetPreferredOutletAsync(sourceApplication.Id, asNoTracking: true);
            var proposedOutlet = await GetProposedOutletAsync(removalApplication.Id, asNoTracking: true);
            var attachments = await GetTemporaryRemovalAttachmentsAsync(removalApplication.Id);
            var payment = await RefreshTemporaryRemovalPaymentStatusAsync(removalApplication.Id);
            var proposedLicenseId = proposedOutlet?.LicenseTypeID ?? removalApplication.LicenseTypeID;
            var proposedRegionId = proposedOutlet?.ApplicationType ?? removalApplication.ApplicationType;

            ViewData["Title"] = "Temporary Removal Review";
            ViewData["Subtitle"] = "Secretary examination and approval.";
            ViewBag.SourceApplication = sourceApplication;
            ViewBag.RemovalApplication = removalApplication;
            ViewBag.OriginalOutlet = originalOutlet;
            ViewBag.ProposedOutlet = proposedOutlet;
            ViewBag.License = string.IsNullOrWhiteSpace(sourceApplication.LicenseTypeID)
                ? null
                : await _db.LicenseTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.LicenseTypeID);
            ViewBag.Region = string.IsNullOrWhiteSpace(sourceApplication.ApplicationType)
                ? null
                : await _db.LicenseRegions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.ApplicationType);
            ViewBag.ProposedLicense = string.IsNullOrWhiteSpace(proposedLicenseId)
                ? null
                : await _db.LicenseTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == proposedLicenseId);
            ViewBag.ProposedRegion = string.IsNullOrWhiteSpace(proposedRegionId)
                ? null
                : await _db.LicenseRegions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == proposedRegionId);
            ViewBag.Attachments = OrderRequiredTemporaryRemovalAttachments(attachments);
            ViewBag.Payment = payment;
            ViewBag.TotalFee = removalApplication.PaymentFee;
            ViewBag.Task = assignedTask;
            ViewBag.CanAct = string.Equals(removalApplication.Status, "awaiting approval", StringComparison.OrdinalIgnoreCase)
                && payment != null
                && HasPaymentStatus(payment, "Paid");
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return null;
            }

            return await _userManager.Users.FirstOrDefaultAsync(item => item.Id == currentUserId);
        }

        private async Task<ApplicationInfo?> GetSourceApplicationAsync(
            string? sourceApplicationId,
            string? userId = null,
            bool requireApproved = false)
        {
            if (string.IsNullOrWhiteSpace(sourceApplicationId))
            {
                return null;
            }

            var query = _db.ApplicationInfo
                .AsNoTracking()
                .Where(item =>
                    item.Id == sourceApplicationId
                    && (item.ExaminationStatus == null || item.ExaminationStatus != TemporaryRemovalHelper.ServiceName));

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(item => item.UserID == userId);
            }

            if (requireApproved)
            {
                query = query.Where(item => item.Status == "approved" || item.Status == "Approved");
            }

            return await query.FirstOrDefaultAsync();
        }

        private async Task<ApplicationInfo?> GetOpenTemporaryRemovalApplicationAsync(string applicantUserId, string? sourceApplicationId)
        {
            if (string.IsNullOrWhiteSpace(applicantUserId) || string.IsNullOrWhiteSpace(sourceApplicationId))
            {
                return null;
            }

            return await _db.ApplicationInfo
                .AsNoTracking()
                .Where(item =>
                    item.UserID == applicantUserId
                    && item.CompanyNumber == sourceApplicationId
                    && item.ExaminationStatus == TemporaryRemovalHelper.ServiceName
                    && (item.Status == null
                        || (item.Status != "Approved" && item.Status != "Rejected")))
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.ApplicationDate)
                .FirstOrDefaultAsync();
        }

        private async Task<ApplicationInfo?> GetOwnedTemporaryRemovalApplicationAsync(string? recordId, string userId)
        {
            if (string.IsNullOrWhiteSpace(recordId) || string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == recordId
                && item.UserID == userId
                && item.ExaminationStatus == TemporaryRemovalHelper.ServiceName);
        }

        private async Task<ApplicationInfo> CreateDraftTemporaryRemovalAsync(ApplicationUser applicantUser, ApplicationInfo sourceApplication)
        {
            var fee = await GetTemporaryRemovalFeeAsync(sourceApplication);
            var originalOutlet = await GetPreferredOutletAsync(sourceApplication.Id);
            var now = DateTime.Now;

            var draft = new ApplicationInfo
            {
                Id = Guid.NewGuid().ToString(),
                UserID = applicantUser.Id,
                ApplicationType = sourceApplication.ApplicationType,
                LicenseTypeID = sourceApplication.LicenseTypeID,
                PaymentStatus = "Not Paid",
                PaymentId = string.Empty,
                RefNum = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, TemporaryRemovalHelper.ServiceCode),
                ExaminationStatus = TemporaryRemovalHelper.ServiceName,
                PaymentFee = fee,
                OperationAddress = originalOutlet?.Address ?? sourceApplication.OperationAddress ?? string.Empty,
                LLBNum = sourceApplication.LLBNum,
                ApplicantType = sourceApplication.ApplicantType,
                BusinessName = originalOutlet?.TradingName ?? sourceApplication.BusinessName ?? string.Empty,
                IdPass = applicantUser.NatID ?? string.Empty,
                Status = "inprogress",
                ApplicationDate = now,
                DateUpdated = now,
                InspectorID = string.Empty,
                Secretary = string.Empty,
                RejectionReason = string.Empty,
                CompanyNumber = sourceApplication.Id
            };

            var proposedOutlet = new OutletInfo
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationId = draft.Id,
                UserId = applicantUser.Id,
                TradingName = originalOutlet?.TradingName ?? sourceApplication.BusinessName ?? string.Empty,
                Province = originalOutlet?.Province ?? string.Empty,
                Address = originalOutlet?.Address ?? sourceApplication.OperationAddress ?? string.Empty,
                City = originalOutlet?.City ?? string.Empty,
                Status = "draft",
                ApplicationType = sourceApplication.ApplicationType,
                Latitude = originalOutlet?.Latitude ?? string.Empty,
                Longitude = originalOutlet?.Longitude ?? string.Empty,
                LicenseTypeID = sourceApplication.LicenseTypeID,
                Council = originalOutlet?.Council ?? string.Empty,
                DateAdded = now,
                DateUpdated = now
            };

            _db.Add(draft);
            _db.Add(proposedOutlet);
            await _db.SaveChangesAsync();

            await EnsureRequiredTemporaryRemovalAttachmentsAsync(draft.Id, applicantUser.Id);
            return draft;
        }

        private async Task<OutletInfo?> GetPreferredOutletAsync(string? applicationId, bool asNoTracking = false)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return null;
            }

            IQueryable<OutletInfo> query = _db.OutletInfo;
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query
                .Where(item => item.ApplicationId == applicationId)
                .OrderByDescending(item => item.Status != null && item.Status.ToLower() == "active")
                .ThenByDescending(item => item.DateUpdated)
                .FirstOrDefaultAsync();
        }

        private async Task<OutletInfo?> GetProposedOutletAsync(string? recordId, bool asNoTracking = false)
        {
            if (string.IsNullOrWhiteSpace(recordId))
            {
                return null;
            }

            IQueryable<OutletInfo> query = _db.OutletInfo;
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query
                .Where(item => item.ApplicationId == recordId)
                .OrderByDescending(item => item.DateUpdated)
                .FirstOrDefaultAsync();
        }

        private async Task<List<AttachmentInfo>> EnsureRequiredTemporaryRemovalAttachmentsAsync(string applicationRecordId, string userId)
        {
            var attachments = await _db.AttachmentInfo
                .Where(item => item.ApplicationId == applicationRecordId)
                .ToListAsync();

            var existingTitles = attachments
                .Where(item => !string.IsNullOrWhiteSpace(item.DocumentTitle))
                .Select(item => item.DocumentTitle!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var now = DateTime.Now;
            foreach (var title in TemporaryRemovalHelper.RequiredDocumentTitles)
            {
                if (existingTitles.Contains(title))
                {
                    continue;
                }

                var attachment = new AttachmentInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ApplicationId = applicationRecordId,
                    DocumentTitle = title,
                    DocumentLocation = string.Empty,
                    Status = "empty",
                    DateAdded = now,
                    DateUpdated = now
                };

                _db.Add(attachment);
                attachments.Add(attachment);
            }

            await _db.SaveChangesAsync();
            return OrderRequiredTemporaryRemovalAttachments(attachments);
        }

        private async Task<List<AttachmentInfo>> GetTemporaryRemovalAttachmentsAsync(string? applicationRecordId)
        {
            if (string.IsNullOrWhiteSpace(applicationRecordId))
            {
                return new List<AttachmentInfo>();
            }

            var attachments = await _db.AttachmentInfo
                .AsNoTracking()
                .Where(item => item.ApplicationId == applicationRecordId)
                .ToListAsync();

            return OrderRequiredTemporaryRemovalAttachments(attachments);
        }

        private static List<AttachmentInfo> OrderRequiredTemporaryRemovalAttachments(IEnumerable<AttachmentInfo> attachments)
        {
            return attachments
                .Where(item => TemporaryRemovalHelper.RequiredDocumentTitles.Any(
                    title => string.Equals(title, item.DocumentTitle, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(item =>
                {
                    var index = Array.FindIndex(
                        TemporaryRemovalHelper.RequiredDocumentTitles,
                        title => string.Equals(title, item.DocumentTitle, StringComparison.OrdinalIgnoreCase));

                    return index < 0 ? int.MaxValue : index;
                })
                .ThenBy(item => item.DocumentTitle)
                .ToList();
        }

        private static bool AreAllRequiredDocumentsUploaded(IEnumerable<AttachmentInfo> attachments)
        {
            var uploadedTitles = attachments
                .Where(item => !string.IsNullOrWhiteSpace(item.DocumentLocation))
                .Select(item => item.DocumentTitle ?? string.Empty)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return TemporaryRemovalHelper.RequiredDocumentTitles.All(uploadedTitles.Contains);
        }

        private static bool IsOutletComplete(OutletInfo? outlet)
        {
            return outlet != null
                && !string.IsNullOrWhiteSpace(outlet.TradingName)
                && !string.IsNullOrWhiteSpace(outlet.Address)
                && !string.IsNullOrWhiteSpace(outlet.LicenseTypeID)
                && !string.IsNullOrWhiteSpace(outlet.ApplicationType)
                && !string.IsNullOrWhiteSpace(outlet.Province)
                && !string.IsNullOrWhiteSpace(outlet.City)
                && !string.IsNullOrWhiteSpace(outlet.Council);
        }

        private static bool CanEditTemporaryRemoval(ApplicationInfo? removalApplication)
        {
            return string.Equals(removalApplication?.Status, "inprogress", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<Payments?> GetLatestTemporaryRemovalPaymentAsync(string? recordId)
        {
            if (string.IsNullOrWhiteSpace(recordId))
            {
                return null;
            }

            return await _db.Payments
                .Where(item =>
                    item.ApplicationId == recordId
                    && item.Service == TemporaryRemovalHelper.ServiceName)
                .OrderByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();
        }

        private async Task<Payments?> RefreshTemporaryRemovalPaymentStatusAsync(string? recordId)
        {
            var payment = await GetLatestTemporaryRemovalPaymentAsync(recordId);
            if (payment == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(payment.PollUrl)
                && !string.Equals(payment.PollUrl, "transfer", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(payment.PollUrl, "manual", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
                    var status = paynow.PollTransaction(payment.PollUrl);
                    var statusData = status.GetData();
                    payment.PaynowRef = statusData["paynowreference"];
                    payment.PaymentStatus = statusData["status"];
                    payment.Status = statusData["status"];
                    payment.DateUpdated = DateTime.Now;
                    _db.Update(payment);

                    var removalApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item => item.Id == recordId);
                    if (removalApplication != null)
                    {
                        removalApplication.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? removalApplication.PaymentStatus;
                        removalApplication.DateUpdated = DateTime.Now;
                        _db.Update(removalApplication);
                    }

                    await _db.SaveChangesAsync();
                }
                catch
                {
                    return payment;
                }
            }

            return payment;
        }

        private async Task<decimal?> GetTemporaryRemovalFeeAsync(ApplicationInfo? application)
        {
            if (application == null)
            {
                return null;
            }

            var licenseType = string.IsNullOrWhiteSpace(application.LicenseTypeID)
                ? null
                : await _db.LicenseTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == application.LicenseTypeID);

            var region = string.IsNullOrWhiteSpace(application.ApplicationType)
                ? null
                : await _db.LicenseRegions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == application.ApplicationType);

            if (!string.IsNullOrWhiteSpace(licenseType?.LicenseCode))
            {
                var removalType = await _db.RemovalTypes
                    .AsNoTracking()
                    .Where(item => item.LicenseCode == licenseType.LicenseCode)
                    .OrderByDescending(item => item.Status == "active")
                    .ThenByDescending(item => item.DateUpdated)
                    .FirstOrDefaultAsync();

                if (removalType != null)
                {
                    return region?.RegionName switch
                    {
                        "Town" => Convert.ToDecimal(removalType.TownFee),
                        "City" => Convert.ToDecimal(removalType.CityFee),
                        "Municipality" => Convert.ToDecimal(removalType.MunicipaltyFee),
                        "RDC" => Convert.ToDecimal(removalType.RDCFee),
                        _ => Convert.ToDecimal(removalType.CityFee)
                    };
                }
            }

            return await _db.PostFormationFees
                .AsNoTracking()
                .Where(item =>
                    item.Code == TemporaryRemovalHelper.ServiceCode
                    || item.ProcessName == TemporaryRemovalHelper.ServiceName)
                .Select(item => (decimal?)item.Fee)
                .FirstOrDefaultAsync();
        }

        private async Task<string> SaveApplicationAttachmentAsync(IFormFile file)
        {
            Directory.CreateDirectory(Path.Combine("wwwroot", "ApplicationAttchments"));
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var relativePath = Path.Combine("ApplicationAttchments", fileName).Replace("\\", "/");
            var absolutePath = Path.Combine("wwwroot", "ApplicationAttchments", fileName);

            await using var fileStream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            return relativePath;
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
                && !HasPaymentStatus(payment, "Rejected")
                && !HasPaymentStatus(payment, "Expired");
        }
    }
}
