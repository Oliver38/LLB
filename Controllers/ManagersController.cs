using System.Globalization;
using LLB.Data;
using LLB.Helpers;
using LLB.Models;
using LLB.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webdev.Payments;

namespace LLB.Controllers
{
    [Authorize]
    [Route("Managers")]
    public class ManagersController : Controller
    {
        private static readonly string[] DraftStatusChanges = { "pending-resigned", "pending-deceased" };

        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public ManagersController(
            TaskAllocationHelper taskAllocationHelper,
            AppDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            this.userManager = userManager;
            _taskAllocationHelper = taskAllocationHelper;
        }

        [HttpGet("ManagerChange")]
        public async Task<IActionResult> ManagerChange(string id, string process = "APM", string? changeId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["error"] = "The application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var application = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (application == null)
            {
                TempData["error"] = "The application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var currentUserId = userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var selectedChangeApplication = await ResolveSelectedManagerChangeApplicationAsync(
                id,
                changeId,
                cancellationToken);

            if (selectedChangeApplication == null)
            {
                selectedChangeApplication = await CreateManagerChangeDraftAsync(id, currentUserId, cancellationToken);
            }

            var pageModel = await BuildManagerChangePageViewModelAsync(
                application,
                selectedChangeApplication,
                process,
                cancellationToken);

            ViewData["Title"] = "Manager Change";
            ViewData["Subtitle"] = "Create, pay for, and submit manager change applications";
            return View(pageModel);
        }

        [HttpPost("AddManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddManagerAsync(
            string applicationId,
            string changeId,
            string name,
            string surname,
            string nationalId,
            string address,
            IFormFile? file,
            IFormFile? fileb,
            IFormFile? form55,
            CancellationToken cancellationToken)
        {
            var validationResult = await ValidateEditableManagerChangeDraftAsync(applicationId, changeId, cancellationToken);
            if (validationResult.redirectResult != null)
            {
                return validationResult.redirectResult;
            }

            var changeApplication = validationResult.changeApplication!;

            if (string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(surname)
                || string.IsNullOrWhiteSpace(nationalId)
                || string.IsNullOrWhiteSpace(address))
            {
                TempData["error"] = "Complete all manager details before saving.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            var duplicateManager = await _db.ManagersParticulars
                .AsNoTracking()
                .Where(manager => manager.ApplicationId == applicationId && manager.NationalId == nationalId)
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicateManager != null)
            {
                TempData["error"] = "A manager with the same national ID already exists on this licence.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            var currentUserId = userManager.GetUserId(User);
            var manager = new ManagersParticulars
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUserId,
                Name = name.Trim(),
                Surname = surname.Trim(),
                NationalId = nationalId.Trim(),
                Address = address.Trim(),
                ApplicationId = applicationId,
                Status = "UnSubmitted",
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };

            manager.Attachment = await SaveManagerFileAsync(file, "NatId", manager.Id, cancellationToken);
            manager.Fingerprints = await SaveManagerFileAsync(fileb, "Fingerprints", manager.Id, cancellationToken);
            manager.Form55 = await SaveManagerFileAsync(form55, "Form55", manager.Id, cancellationToken);

            _db.Add(manager);
            await _db.SaveChangesAsync(cancellationToken);

            await RefreshManagerChangeDraftSummaryAsync(changeApplication, cancellationToken);

            TempData["success"] = "Manager details added to the change application.";
            return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
        }

        [HttpPost("UpdateDraftManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDraftManagerAsync(
            string managerId,
            string applicationId,
            string changeId,
            string name,
            string surname,
            string nationalId,
            string address,
            IFormFile? file,
            IFormFile? fileb,
            IFormFile? form55,
            CancellationToken cancellationToken)
        {
            var validationResult = await ValidateEditableManagerChangeDraftAsync(applicationId, changeId, cancellationToken);
            if (validationResult.redirectResult != null)
            {
                return validationResult.redirectResult;
            }

            var changeApplication = validationResult.changeApplication!;
            var draftStart = ParseManagerChangeDate(changeApplication.DateApplied);

            var manager = await _db.ManagersParticulars
                .FirstOrDefaultAsync(item => item.Id == managerId && item.ApplicationId == applicationId, cancellationToken);
            if (manager == null)
            {
                TempData["error"] = "The manager could not be found.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            if (!IsDraftAddition(manager, draftStart))
            {
                TempData["error"] = "Only draft manager additions can be edited from the changes tab.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            manager.Name = name.Trim();
            manager.Surname = surname.Trim();
            manager.NationalId = nationalId.Trim();
            manager.Address = address.Trim();
            manager.Status = "UnSubmitted";
            manager.DateUpdated = DateTime.Now;

            if (file != null)
            {
                manager.Attachment = await SaveManagerFileAsync(file, "NatId", manager.Id, cancellationToken);
            }

            if (fileb != null)
            {
                manager.Fingerprints = await SaveManagerFileAsync(fileb, "Fingerprints", manager.Id, cancellationToken);
            }

            if (form55 != null)
            {
                manager.Form55 = await SaveManagerFileAsync(form55, "Form55", manager.Id, cancellationToken);
            }

            _db.Update(manager);
            await _db.SaveChangesAsync(cancellationToken);

            await RefreshManagerChangeDraftSummaryAsync(changeApplication, cancellationToken);

            TempData["success"] = "Manager details updated.";
            return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
        }

        [HttpPost("UpdateManagerStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateManagerStatusAsync(
            string managerId,
            string applicationId,
            string changeId,
            string name,
            string surname,
            string nationalId,
            string address,
            string requestedStatus,
            IFormFile? file,
            IFormFile? fileb,
            IFormFile? form55,
            CancellationToken cancellationToken)
        {
            var validationResult = await ValidateEditableManagerChangeDraftAsync(applicationId, changeId, cancellationToken);
            if (validationResult.redirectResult != null)
            {
                return validationResult.redirectResult;
            }

            var changeApplication = validationResult.changeApplication!;
            var manager = await _db.ManagersParticulars
                .FirstOrDefaultAsync(item => item.Id == managerId && item.ApplicationId == applicationId, cancellationToken);
            if (manager == null)
            {
                TempData["error"] = "The manager could not be found.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            if (string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(surname)
                || string.IsNullOrWhiteSpace(nationalId)
                || string.IsNullOrWhiteSpace(address))
            {
                TempData["error"] = "Complete all manager details before saving.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            var currentManagerStatus = NormalizeManagerStatus(manager.Status);
            if (currentManagerStatus != "active"
                && !DraftStatusChanges.Contains(currentManagerStatus, StringComparer.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only active managers or pending draft status changes can be edited from the managers list.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            var trimmedNationalId = nationalId.Trim();
            var duplicateManager = await _db.ManagersParticulars
                .AsNoTracking()
                .Where(item => item.ApplicationId == applicationId
                    && item.Id != managerId
                    && item.NationalId == trimmedNationalId)
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicateManager != null)
            {
                TempData["error"] = "A manager with the same national ID already exists on this licence.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            var normalizedStatus = NormalizeManagerStatus(requestedStatus);
            if (normalizedStatus != "active"
                && normalizedStatus != "resigned"
                && normalizedStatus != "deceased")
            {
                TempData["error"] = "Select a valid manager status.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            manager.Name = name.Trim();
            manager.Surname = surname.Trim();
            manager.NationalId = trimmedNationalId;
            manager.Address = address.Trim();
            manager.Status = normalizedStatus switch
            {
                "resigned" => "pending-resigned",
                "deceased" => "pending-deceased",
                _ => "active"
            };
            manager.DateUpdated = DateTime.Now;

            if (file != null)
            {
                manager.Attachment = await SaveManagerFileAsync(file, "NatId", manager.Id ?? managerId, cancellationToken);
            }

            if (fileb != null)
            {
                manager.Fingerprints = await SaveManagerFileAsync(fileb, "Fingerprints", manager.Id ?? managerId, cancellationToken);
            }

            if (form55 != null)
            {
                manager.Form55 = await SaveManagerFileAsync(form55, "Form55", manager.Id ?? managerId, cancellationToken);
            }

            if (string.Equals(manager.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                manager.DissmisalDate = DateTime.MinValue;
            }

            _db.Update(manager);
            await _db.SaveChangesAsync(cancellationToken);

            await RefreshManagerChangeDraftSummaryAsync(changeApplication, cancellationToken);

            TempData["success"] = "Manager status updated for this change application.";
            return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
        }

        [HttpPost("DeleteManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteManagerAsync(string managerId, string applicationId, string changeId, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateEditableManagerChangeDraftAsync(applicationId, changeId, cancellationToken);
            if (validationResult.redirectResult != null)
            {
                return validationResult.redirectResult;
            }

            var changeApplication = validationResult.changeApplication!;
            var draftStart = ParseManagerChangeDate(changeApplication.DateApplied);

            var manager = await _db.ManagersParticulars
                .FirstOrDefaultAsync(item => item.Id == managerId && item.ApplicationId == applicationId, cancellationToken);
            if (manager == null)
            {
                TempData["error"] = "The manager could not be found.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            if (!IsDraftAddition(manager, draftStart))
            {
                TempData["error"] = "Only draft manager additions can be deleted.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            _db.Remove(manager);
            await _db.SaveChangesAsync(cancellationToken);

            await RefreshManagerChangeDraftSummaryAsync(changeApplication, cancellationToken);

            TempData["success"] = "Draft manager removed from the change application.";
            return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
        }

        [HttpPost("SubmitChangeApplication")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitChangeApplicationAsync(string applicationId, string changeId, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateEditableManagerChangeDraftAsync(applicationId, changeId, cancellationToken);
            if (validationResult.redirectResult != null)
            {
                return validationResult.redirectResult;
            }

            var changeApplication = validationResult.changeApplication!;
            var feePerManager = GetManagerChangeFee();
            var managers = await _db.ManagersParticulars
                .Where(item => item.ApplicationId == applicationId)
                .ToListAsync(cancellationToken);
            var draftStart = ParseManagerChangeDate(changeApplication.DateApplied);
            var currentChanges = GetCurrentManagerChangeSet(managers, draftStart);

            if (currentChanges.Count == 0)
            {
                TempData["error"] = "Add or update at least one manager before submitting the application.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            var newManagersCount = currentChanges.Count(item => item.IsDraftAddition);
            if (newManagersCount > 0 && !feePerManager.HasValue)
            {
                TempData["error"] = "The manager change fee has not been configured.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            var totalFee = (feePerManager ?? 0d) * newManagersCount;
            changeApplication.NewManagersCount = newManagersCount.ToString(CultureInfo.InvariantCulture);
            changeApplication.PaidFee = totalFee.ToString("0.00", CultureInfo.InvariantCulture);
            changeApplication.PaymentStatus = totalFee > 0 ? "Not Paid" : "Not Required";
            changeApplication.Status = "awaiting payment";
            changeApplication.DateUpdated = FormatManagerChangeDate(DateTime.Now);

            _db.Update(changeApplication);
            await _db.SaveChangesAsync(cancellationToken);

            TempData["success"] = totalFee > 0
                ? "Manager change application submitted for payment. Complete payment to continue."
                : "Manager change application submitted. No payment is required for this draft, so you can continue immediately.";
            return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
        }

        [HttpGet("ManagerChangePayment")]
        public async Task<IActionResult> ManagerChangePaymentAsync(string id, string process = "APM", string? changeId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["error"] = "The application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var currentUserId = userManager.GetUserId(User);
            var changeApplication = await ResolveSelectedManagerChangeApplicationAsync(id, changeId, cancellationToken);
            if (changeApplication == null)
            {
                TempData["error"] = "The manager change application could not be found.";
                return RedirectToAction("ManagerChange", new { id, process });
            }

            if (!string.Equals(changeApplication.Status, "awaiting payment", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Submit the change application before starting payment.";
                return RedirectToAction("ManagerChange", new { id, process, changeId = changeApplication.Id });
            }

            var totalFee = ParseAmount(changeApplication.PaidFee);
            if (totalFee <= 0)
            {
                changeApplication.PaymentStatus = "Not Required";
                changeApplication.DateUpdated = FormatManagerChangeDate(DateTime.Now);
                _db.Update(changeApplication);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "No payment is required for this manager change application.";
                return RedirectToAction("ManagerChange", new { id, process, changeId = changeApplication.Id });
            }

            var existingTransaction = GetLatestManagerChangePaymentForRecord(changeApplication);
            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                changeApplication.PaymentStatus = existingTransaction.PaymentStatus ?? existingTransaction.Status ?? "Paid";
                changeApplication.DateUpdated = FormatManagerChangeDate(DateTime.Now);
                _db.Update(changeApplication);
                await _db.SaveChangesAsync(cancellationToken);

                TempData["success"] = "This manager change application has already been paid for.";
                return RedirectToAction("ManagerChange", new { id, process, changeId = changeApplication.Id });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = "Complete the current manager change payment before starting another one.";

                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("ManagerChange", new { id, process, changeId = changeApplication.Id });
            }

            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
            var callbackUrl = Url.Action(
                "ManagerChange",
                "Managers",
                new { id, process, changeId = changeApplication.Id },
                Request.Scheme);

            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            var applicationInfo = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            var licenseType = applicationInfo == null
                ? null
                : await _db.LicenseTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == applicationInfo.LicenseTypeID, cancellationToken);

            var payment = paynow.CreatePayment(changeApplication.Reference ?? changeApplication.Id ?? Guid.NewGuid().ToString());
            payment.Add(licenseType?.LicenseName ?? "Manager Change", Convert.ToDecimal(totalFee));

            var response = paynow.Send(payment);
            if (!response.Success())
            {
                TempData["error"] = "The manager change payment request could not be sent.";
                return RedirectToAction("ManagerChange", new { id, process, changeId = changeApplication.Id });
            }

            var transaction = new Payments
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUserId,
                Amount = payment.Total,
                ApplicationId = changeApplication.Id,
                Service = "changemanager",
                PollUrl = response.PollUrl(),
                PopDoc = string.Empty,
                SystemRef = response.RedirectLink(),
                Status = "not paid",
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };

            var status = paynow.PollTransaction(transaction.PollUrl);
            var statusData = status.GetData();
            transaction.PaynowRef = statusData["paynowreference"];
            transaction.PaymentStatus = statusData["status"];

            _db.Add(transaction);
            changeApplication.PaymentStatus = transaction.PaymentStatus ?? transaction.Status ?? "Not Paid";
            changeApplication.DateUpdated = FormatManagerChangeDate(DateTime.Now);
            _db.Update(changeApplication);
            await _db.SaveChangesAsync(cancellationToken);

            return Redirect(transaction.SystemRef ?? callbackUrl ?? "/");
        }

        [HttpPost("ContinueManagerChange")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContinueManagerChangeAsync(string applicationId, string changeId, CancellationToken cancellationToken)
        {
            var changeApplication = await _db.ChangeManaager
                .FirstOrDefaultAsync(item => item.Id == changeId && item.ApplicationId == applicationId, cancellationToken);
            if (changeApplication == null)
            {
                TempData["error"] = "The manager change application could not be found.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM" });
            }

            if (!string.Equals(changeApplication.Status, "awaiting payment", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This manager change application is not ready to continue.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            var payment = RefreshManagerChangePaymentStatus(changeApplication);
            var paymentSatisfied = IsManagerChangePaymentSatisfied(changeApplication, payment);
            if (!paymentSatisfied)
            {
                TempData["error"] = "Complete payment before sending this manager change application to the secretary.";
                return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
            }

            var existingTask = await _db.Tasks
                .Where(task => task.ApplicationId == changeApplication.Id
                    && task.Service == "Manager Change"
                    && task.Status == "assigned")
                .FirstOrDefaultAsync(cancellationToken);

            if (existingTask == null)
            {
                var secretaryId = await _taskAllocationHelper.GetSecretary(_db, userManager);
                if (string.IsNullOrWhiteSpace(secretaryId))
                {
                    TempData["error"] = "No secretary is currently available to receive the manager change application.";
                    return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
                }

                var task = new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = changeApplication.Id,
                    ApproverId = secretaryId,
                    AssignerId = "system",
                    Service = "Manager Change",
                    ExaminationStatus = "approval",
                    Status = "assigned",
                    DateAdded = DateTime.Now,
                    DateUpdated = DateTime.Now
                };

                _db.Add(task);
            }

            changeApplication.Status = "submitted";
            changeApplication.PaymentStatus = payment?.PaymentStatus
                ?? payment?.Status
                ?? changeApplication.PaymentStatus
                ?? "Paid";
            changeApplication.DateUpdated = FormatManagerChangeDate(DateTime.Now);

            _db.Update(changeApplication);
            await _db.SaveChangesAsync(cancellationToken);

            TempData["success"] = "Manager change application sent to the secretary.";
            return RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId });
        }

        [HttpGet("ViewApplications")]
        public async Task<IActionResult> ViewApplications(string Id, CancellationToken cancellationToken)
        {
            var changeApplication = await _db.ChangeManaager
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);
            if (changeApplication == null || string.IsNullOrWhiteSpace(changeApplication.ApplicationId))
            {
                TempData["error"] = "The manager change application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var application = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == changeApplication.ApplicationId, cancellationToken);
            if (application == null)
            {
                TempData["error"] = "The root application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var reviewModel = await BuildManagerChangeReviewViewModelAsync(application, changeApplication, cancellationToken);
            ViewData["Title"] = "Manager Change Review";
            ViewData["Subtitle"] = "Review manager changes before approval";
            return View(reviewModel);
        }

        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAsync(string Id, CancellationToken cancellationToken)
        {
            var changeApplication = await _db.ChangeManaager
                .FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);
            if (changeApplication == null || string.IsNullOrWhiteSpace(changeApplication.ApplicationId))
            {
                TempData["error"] = "The manager change application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            if (!string.Equals(changeApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only submitted manager change applications can be approved.";
                return RedirectToAction("ViewApplications", new { Id });
            }

            var payment = RefreshManagerChangePaymentStatus(changeApplication);
            if (!IsManagerChangePaymentSatisfied(changeApplication, payment))
            {
                TempData["error"] = "Payment has not been confirmed for this manager change application.";
                return RedirectToAction("ViewApplications", new { Id });
            }

            var draftStart = ParseManagerChangeDate(changeApplication.DateApplied);
            var managers = await _db.ManagersParticulars
                .Where(item => item.ApplicationId == changeApplication.ApplicationId)
                .ToListAsync(cancellationToken);

            foreach (var manager in managers)
            {
                var normalizedStatus = NormalizeManagerStatus(manager.Status);

                if (IsDraftAddition(manager, draftStart) && normalizedStatus == "unsubmitted")
                {
                    manager.Status = "active";
                    manager.EffectiveDate = DateTime.Now;
                    manager.DateUpdated = DateTime.Now;
                    _db.Update(manager);
                    continue;
                }

                if (normalizedStatus == "pending-resigned")
                {
                    manager.Status = "resigned";
                    manager.DissmisalDate = DateTime.Now;
                    manager.DateUpdated = DateTime.Now;
                    _db.Update(manager);
                    continue;
                }

                if (normalizedStatus == "pending-deceased")
                {
                    manager.Status = "deceased";
                    manager.DissmisalDate = DateTime.Now;
                    manager.DateUpdated = DateTime.Now;
                    _db.Update(manager);
                }
            }

            changeApplication.Status = "Approved";
            changeApplication.PaymentStatus = payment?.PaymentStatus
                ?? payment?.Status
                ?? changeApplication.PaymentStatus
                ?? "Paid";
            changeApplication.DateUpdated = FormatManagerChangeDate(DateTime.Now);
            _db.Update(changeApplication);

            var task = await _db.Tasks
                .Where(item => item.ApplicationId == Id && item.Service == "Manager Change")
                .FirstOrDefaultAsync(cancellationToken);
            if (task != null)
            {
                task.Status = "completed";
                task.ApprovedDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Update(task);
            }

            var rootApplication = await _db.ApplicationInfo
                .FirstOrDefaultAsync(item => item.Id == changeApplication.ApplicationId, cancellationToken);
            DownloadStatusHelper.OpenLicenseDownload(_db, rootApplication, rootApplication?.UserID);

            await _db.SaveChangesAsync(cancellationToken);

            TempData["success"] = "Manager change application approved successfully.";
            return RedirectToAction("ViewApplications", new { Id });
        }

        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAsync(string Id, CancellationToken cancellationToken)
        {
            var changeApplication = await _db.ChangeManaager
                .FirstOrDefaultAsync(item => item.Id == Id, cancellationToken);
            if (changeApplication == null || string.IsNullOrWhiteSpace(changeApplication.ApplicationId))
            {
                TempData["error"] = "The manager change application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            if (!string.Equals(changeApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only submitted manager change applications can be rejected.";
                return RedirectToAction("ViewApplications", new { Id });
            }

            var draftStart = ParseManagerChangeDate(changeApplication.DateApplied);
            var managers = await _db.ManagersParticulars
                .Where(item => item.ApplicationId == changeApplication.ApplicationId)
                .ToListAsync(cancellationToken);

            foreach (var manager in managers)
            {
                var normalizedStatus = NormalizeManagerStatus(manager.Status);

                if (IsDraftAddition(manager, draftStart) && normalizedStatus == "unsubmitted")
                {
                    _db.Remove(manager);
                    continue;
                }

                if (DraftStatusChanges.Contains(normalizedStatus, StringComparer.OrdinalIgnoreCase))
                {
                    manager.Status = "active";
                    manager.DissmisalDate = DateTime.MinValue;
                    manager.DateUpdated = DateTime.Now;
                    _db.Update(manager);
                }
            }

            changeApplication.Status = "Rejected";
            changeApplication.DateUpdated = FormatManagerChangeDate(DateTime.Now);
            _db.Update(changeApplication);

            var task = await _db.Tasks
                .Where(item => item.ApplicationId == Id && item.Service == "Manager Change")
                .FirstOrDefaultAsync(cancellationToken);
            if (task != null)
            {
                task.Status = "completed";
                task.ApprovedDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Update(task);
            }

            await _db.SaveChangesAsync(cancellationToken);

            TempData["success"] = "Manager change application rejected and draft changes reverted.";
            return RedirectToAction("ViewApplications", new { Id });
        }

        private async Task<ChangeManaager?> ResolveSelectedManagerChangeApplicationAsync(
            string applicationId,
            string? changeId,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(changeId))
            {
                var selectedRecord = await _db.ChangeManaager
                    .FirstOrDefaultAsync(item => item.Id == changeId && item.ApplicationId == applicationId, cancellationToken);
                if (selectedRecord != null)
                {
                    return selectedRecord;
                }
            }

            return await _db.ChangeManaager
                .Where(item => item.ApplicationId == applicationId
                    && item.Status != "Approved"
                    && item.Status != "Rejected"
                    && item.Status != "Complete")
                .OrderByDescending(item => item.DateApplied)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<ChangeManaager> CreateManagerChangeDraftAsync(string applicationId, string currentUserId, CancellationToken cancellationToken)
        {
            var draft = new ChangeManaager
            {
                Id = Guid.NewGuid().ToString(),
                Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, "MGR"),
                UserId = currentUserId,
                ApplicationId = applicationId,
                Status = "inprogress",
                DateApplied = FormatManagerChangeDate(DateTime.Now),
                DateUpdated = FormatManagerChangeDate(DateTime.Now),
                PaidFee = "0.00",
                PaymentStatus = "Not Paid",
                NewManagersCount = "0"
            };

            _db.Add(draft);
            await _db.SaveChangesAsync(cancellationToken);

            return draft;
        }

        private async Task<ManagerChangePageViewModel> BuildManagerChangePageViewModelAsync(
            ApplicationInfo application,
            ChangeManaager changeApplication,
            string process,
            CancellationToken cancellationToken)
        {
            var outlet = await _db.OutletInfo
                .AsNoTracking()
                .Where(item => item.ApplicationId == application.Id)
                .OrderByDescending(item => item.Status == "active")
                .ThenByDescending(item => item.DateUpdated)
                .FirstOrDefaultAsync(cancellationToken);

            var license = await _db.LicenseTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == application.LicenseTypeID, cancellationToken);
            var region = await _db.LicenseRegions
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == application.ApplicationType, cancellationToken);

            var managers = await _db.ManagersParticulars
                .AsNoTracking()
                .Where(item => item.ApplicationId == application.Id)
                .OrderBy(item => item.Name)
                .ThenBy(item => item.Surname)
                .ToListAsync(cancellationToken);

            var payment = RefreshManagerChangePaymentStatus(changeApplication);
            var initiatorUserName = await ResolveUserNameAsync(changeApplication.UserId, cancellationToken);
            var draftStart = ParseManagerChangeDate(changeApplication.DateApplied);
            var feePerManager = GetManagerChangeFee() ?? 0d;
            var managerItems = BuildManagerItems(managers, draftStart, CanEditDraft(changeApplication.Status));
            var changeItems = managerItems
                .Where(item => IsCurrentManagerChangeItem(item, draftStart))
                .OrderByDescending(item => item.IsDraftAddition)
                .ThenBy(item => item.Name)
                .ThenBy(item => item.Surname)
                .ToList();
            var activeManagerItems = managerItems
                .Where(item => NormalizeManagerStatus(item.Status) == "active")
                .ToList();

            var newManagersCount = ParseInteger(changeApplication.NewManagersCount);
            if (newManagersCount == 0)
            {
                newManagersCount = changeItems.Count(item => item.IsDraftAddition);
            }

            var totalFee = ParseAmount(changeApplication.PaidFee);
            if (totalFee <= 0)
            {
                totalFee = feePerManager * newManagersCount;
            }

            var isPaid = IsManagerChangePaymentSatisfied(changeApplication, payment);
            var canEditDraft = CanEditDraft(changeApplication.Status);
            var canSubmitDraft = canEditDraft && changeItems.Count > 0;
            var canMakePayment = string.Equals(changeApplication.Status, "awaiting payment", StringComparison.OrdinalIgnoreCase)
                && totalFee > 0
                && !isPaid;
            var canContinue = string.Equals(changeApplication.Status, "awaiting payment", StringComparison.OrdinalIgnoreCase)
                && (isPaid || totalFee <= 0);

            return new ManagerChangePageViewModel
            {
                Process = process,
                Application = application,
                Outlet = outlet,
                License = license,
                Region = region,
                ChangeApplication = changeApplication,
                Payment = payment,
                InitiatedByUserName = initiatorUserName,
                FeePerManager = feePerManager,
                NewManagersCount = newManagersCount,
                TotalFee = totalFee,
                HasChanges = changeItems.Count > 0,
                CanEditDraft = canEditDraft,
                CanSubmitDraft = canSubmitDraft,
                CanMakePayment = canMakePayment,
                CanContinue = canContinue,
                IsPaid = isPaid,
                Managers = activeManagerItems,
                Changes = changeItems
            };
        }

        private async Task<ManagerChangeReviewViewModel> BuildManagerChangeReviewViewModelAsync(
            ApplicationInfo application,
            ChangeManaager changeApplication,
            CancellationToken cancellationToken)
        {
            var outlet = await _db.OutletInfo
                .AsNoTracking()
                .Where(item => item.ApplicationId == application.Id)
                .OrderByDescending(item => item.Status == "active")
                .ThenByDescending(item => item.DateUpdated)
                .FirstOrDefaultAsync(cancellationToken);
            var license = await _db.LicenseTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == application.LicenseTypeID, cancellationToken);
            var region = await _db.LicenseRegions
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == application.ApplicationType, cancellationToken);
            var managers = await _db.ManagersParticulars
                .AsNoTracking()
                .Where(item => item.ApplicationId == application.Id)
                .OrderBy(item => item.Name)
                .ThenBy(item => item.Surname)
                .ToListAsync(cancellationToken);

            var payment = RefreshManagerChangePaymentStatus(changeApplication);
            var draftStart = ParseManagerChangeDate(changeApplication.DateApplied);
            var managerItems = BuildManagerItems(managers, draftStart, false);
            var changes = managerItems
                .Where(item => IsCurrentManagerChangeItem(item, draftStart))
                .OrderByDescending(item => item.IsDraftAddition)
                .ThenBy(item => item.Name)
                .ThenBy(item => item.Surname)
                .ToList();

            var newManagersCount = ParseInteger(changeApplication.NewManagersCount);
            if (newManagersCount == 0)
            {
                newManagersCount = changes.Count(item => item.IsDraftAddition);
            }

            var totalFee = ParseAmount(changeApplication.PaidFee);
            if (totalFee <= 0)
            {
                totalFee = (GetManagerChangeFee() ?? 0d) * newManagersCount;
            }

            return new ManagerChangeReviewViewModel
            {
                ChangeApplication = changeApplication,
                Application = application,
                Outlet = outlet,
                License = license,
                Region = region,
                Payment = payment,
                InitiatedByUserName = await ResolveUserNameAsync(changeApplication.UserId, cancellationToken),
                NewManagersCount = newManagersCount,
                TotalFee = totalFee,
                Changes = changes
            };
        }

        private List<ManagerChangeManagerItemViewModel> BuildManagerItems(
            IEnumerable<ManagersParticulars> managers,
            DateTime draftStart,
            bool canEditDraft)
        {
            return managers
                .Select(manager =>
                {
                    var normalizedStatus = NormalizeManagerStatus(manager.Status);
                    var isDraftAddition = IsDraftAddition(manager, draftStart);

                    return new ManagerChangeManagerItemViewModel
                    {
                        Id = manager.Id ?? string.Empty,
                        Name = manager.Name ?? string.Empty,
                        Surname = manager.Surname ?? string.Empty,
                        NationalId = manager.NationalId ?? string.Empty,
                        Address = manager.Address ?? string.Empty,
                        Status = manager.Status ?? string.Empty,
                        DisplayStatus = GetManagerStatusDisplayText(normalizedStatus),
                        Attachment = manager.Attachment ?? string.Empty,
                        Fingerprints = manager.Fingerprints ?? string.Empty,
                        Form55 = manager.Form55 ?? string.Empty,
                        EffectiveDate = manager.EffectiveDate,
                        DissmisalDate = manager.DissmisalDate,
                        DateAdded = manager.DateAdded,
                        DateUpdated = manager.DateUpdated,
                        IsDraftAddition = isDraftAddition,
                        CanEditDetails = canEditDraft && isDraftAddition,
                        CanEditStatus = canEditDraft
                            && !isDraftAddition
                            && (normalizedStatus == "active"
                                || DraftStatusChanges.Contains(normalizedStatus, StringComparer.OrdinalIgnoreCase)),
                        CanDelete = canEditDraft && isDraftAddition
                    };
                })
                .OrderByDescending(item => NormalizeManagerStatus(item.Status) == "active")
                .ThenBy(item => item.Name)
                .ThenBy(item => item.Surname)
                .ToList();
        }

        private static bool IsCurrentManagerChangeItem(ManagerChangeManagerItemViewModel manager, DateTime draftStart)
        {
            var normalizedStatus = NormalizeManagerStatus(manager.Status);
            if (manager.IsDraftAddition)
            {
                return true;
            }

            return manager.DateUpdated >= draftStart
                && (DraftStatusChanges.Contains(normalizedStatus, StringComparer.OrdinalIgnoreCase)
                    || normalizedStatus == "resigned"
                    || normalizedStatus == "deceased");
        }

        private static List<ManagerChangeManagerItemViewModel> GetCurrentManagerChangeSet(IEnumerable<ManagersParticulars> managers, DateTime draftStart)
        {
            return managers
                .Select(manager => new ManagerChangeManagerItemViewModel
                {
                    Id = manager.Id ?? string.Empty,
                    Status = manager.Status ?? string.Empty,
                    DateAdded = manager.DateAdded,
                    DateUpdated = manager.DateUpdated,
                    IsDraftAddition = IsDraftAddition(manager, draftStart)
                })
                .Where(item => IsCurrentManagerChangeItem(item, draftStart))
                .ToList();
        }

        private async Task RefreshManagerChangeDraftSummaryAsync(ChangeManaager changeApplication, CancellationToken cancellationToken)
        {
            var feePerManager = GetManagerChangeFee() ?? 0d;
            var draftStart = ParseManagerChangeDate(changeApplication.DateApplied);
            var managers = await _db.ManagersParticulars
                .Where(item => item.ApplicationId == changeApplication.ApplicationId)
                .ToListAsync(cancellationToken);

            var newManagersCount = managers.Count(manager => IsDraftAddition(manager, draftStart));
            changeApplication.NewManagersCount = newManagersCount.ToString(CultureInfo.InvariantCulture);
            changeApplication.PaidFee = (feePerManager * newManagersCount).ToString("0.00", CultureInfo.InvariantCulture);
            changeApplication.DateUpdated = FormatManagerChangeDate(DateTime.Now);

            _db.Update(changeApplication);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<(ChangeManaager? changeApplication, IActionResult? redirectResult)> ValidateEditableManagerChangeDraftAsync(
            string applicationId,
            string changeId,
            CancellationToken cancellationToken)
        {
            var changeApplication = await _db.ChangeManaager
                .FirstOrDefaultAsync(item => item.Id == changeId && item.ApplicationId == applicationId, cancellationToken);
            if (changeApplication == null)
            {
                TempData["error"] = "The manager change application could not be found.";
                return (null, RedirectToAction("ManagerChange", new { id = applicationId, process = "APM" }));
            }

            if (!CanEditDraft(changeApplication.Status))
            {
                TempData["error"] = "This manager change application can no longer be edited.";
                return (null, RedirectToAction("ManagerChange", new { id = applicationId, process = "APM", changeId }));
            }

            return (changeApplication, null);
        }

        private static bool CanEditDraft(string? status)
        {
            return string.Equals(status, "inprogress", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDraftAddition(ManagersParticulars manager, DateTime draftStart)
        {
            return manager.DateAdded >= draftStart
                && string.Equals(NormalizeManagerStatus(manager.Status), "unsubmitted", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFinalManagerChangeApplicationStatus(string? status)
        {
            return string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Complete", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeManagerStatus(string? status)
        {
            return status?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        private static string GetManagerStatusDisplayText(string normalizedStatus)
        {
            return normalizedStatus switch
            {
                "unsubmitted" => "Unsubmitted",
                "pending-resigned" => "Pending Resigned",
                "pending-deceased" => "Pending Deceased",
                "resigned" => "Resigned",
                "deceased" => "Deceased",
                "active" => "Active",
                _ => string.IsNullOrWhiteSpace(normalizedStatus)
                    ? "Unknown"
                    : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalizedStatus.Replace("-", " "))
            };
        }

        private static DateTime ParseManagerChangeDate(string? value)
        {
            if (DateTime.TryParseExact(
                    value,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedExact))
            {
                return parsedExact;
            }

            return DateTime.TryParse(value, out var parsedDate)
                ? parsedDate
                : DateTime.MinValue;
        }

        private static string FormatManagerChangeDate(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static int ParseInteger(string? value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
                ? parsedValue
                : 0;
        }

        private static double ParseAmount(string? value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue)
                ? parsedValue
                : 0d;
        }

        private double? GetManagerChangeFee()
        {
            return _db.PostFormationFees
                .Where(fee => fee.Code == "APM" || fee.ProcessName.Contains("Manager"))
                .OrderByDescending(fee => fee.Code == "APM")
                .Select(fee => (double?)fee.Fee)
                .FirstOrDefault();
        }

        private Payments? GetLatestManagerChangePaymentForRecord(ChangeManaager changeApplication)
        {
            if (changeApplication == null || string.IsNullOrWhiteSpace(changeApplication.Id))
            {
                return null;
            }

            return _db.Payments
                .Where(payment => payment.ApplicationId == changeApplication.Id
                    && (payment.Service == "changemanager" || payment.Service == "Manager Change"))
                .OrderByDescending(payment => payment.DateAdded)
                .FirstOrDefault();
        }

        private Payments? RefreshManagerChangePaymentStatus(ChangeManaager changeApplication)
        {
            var payment = GetLatestManagerChangePaymentForRecord(changeApplication);
            if (payment == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(payment.PollUrl))
            {
                var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
                var status = paynow.PollTransaction(payment.PollUrl);
                var statusData = status.GetData();
                payment.PaynowRef = statusData["paynowreference"];
                payment.PaymentStatus = statusData["status"];
                payment.Status = statusData["status"];
                payment.DateUpdated = DateTime.Now;
                _db.Update(payment);
            }

            changeApplication.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? changeApplication.PaymentStatus;
            changeApplication.DateUpdated = FormatManagerChangeDate(DateTime.Now);
            _db.Update(changeApplication);
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
                && !HasPaymentStatus(payment, "Rejected")
                && !HasPaymentStatus(payment, "Expired");
        }

        private static bool IsManagerChangePaymentSatisfied(ChangeManaager changeApplication, Payments? payment)
        {
            if (ParseAmount(changeApplication.PaidFee) <= 0)
            {
                return true;
            }

            return payment != null && HasPaymentStatus(payment, "Paid");
        }

        private async Task<string> ResolveUserNameAsync(string? userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return "N/A";
            }

            var user = await userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

            return FirstNonEmpty(
                JoinNonEmpty(" ", user?.UserName, user?.LastName),
                JoinNonEmpty(" ", user?.UserName, user?.Name),
                user?.UserName,
                user?.Email,
                "N/A");
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private static string JoinNonEmpty(string separator, params string?[] values)
        {
            return string.Join(separator, values.Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        private static async Task<string> SaveManagerFileAsync(
            IFormFile? file,
            string prefix,
            string managerId,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{prefix}_{managerId}{extension}";
            var directory = Path.Combine("wwwroot", "ManagerFingerprints");
            Directory.CreateDirectory(directory);

            var absolutePath = Path.Combine(directory, fileName);
            await using var fileStream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(fileStream, cancellationToken);

            return Path.Combine("ManagerFingerprints", fileName).Replace("\\", "/");
        }
    }
}
