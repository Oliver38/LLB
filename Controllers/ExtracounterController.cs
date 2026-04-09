using LLB.Data;
using LLB.Helpers;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Webdev.Payments;

namespace LLB.Controllers
{
    [Route("")]
    [Route("Extracounter")]
    public class ExtracounterController : Controller
    {
        private const string PermissionToAlterService = "Permission to Alter";
        private const string LegacyExtraCounterService = "Extra Counter";

        private static readonly string[] RequiredPermissionToAlterDocuments =
        {
            "Local Authority Letter of approval",
            "Tie Affidavit",
            "A3 Plan approved by local Environmental Health",
            "Lease/Deed documents"
        };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _db;
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public ExtracounterController(
            TaskAllocationHelper taskAllocationHelper,
            AppDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
            _taskAllocationHelper = taskAllocationHelper;
        }

        [HttpGet("Extracounter")]
        public IActionResult Extracounter(string id, string process, string? ecId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var application = _db.ApplicationInfo.FirstOrDefault(item => item.Id == id);
            if (application == null)
            {
                TempData["error"] = "Application information could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var fee = GetPermissionToAlterFee();
            if (fee == null)
            {
                TempData["error"] = "The permission to alter fee has not been configured.";
                return RedirectToAction("Dashboard", "Home");
            }

            var permissionApplications = GetPermissionToAlterApplications(id, currentUserId);
            var selectedApplication = !string.IsNullOrWhiteSpace(ecId)
                ? permissionApplications.FirstOrDefault(item => item.Id == ecId)
                : GetLatestOpenPermissionToAlterApplication(id, currentUserId);

            if (selectedApplication == null)
            {
                selectedApplication = CreatePermissionToAlterDraft(id, currentUserId, fee.Value);
                return RedirectToAction("Extracounter", new { id, process = "ECF", ecId = selectedApplication.Id });
            }

            var attachments = EnsureRequiredPermissionToAlterAttachments(selectedApplication.Id, currentUserId);
            var payment = RefreshPermissionToAlterPaymentStatus(selectedApplication);
            var paymentReceived = payment != null && HasPaymentStatus(payment, "Paid");
            var attachmentsComplete = AreAllRequiredDocumentsUploaded(attachments);
            var canUploadAttachments = string.Equals(selectedApplication.Status, "inprogress", StringComparison.OrdinalIgnoreCase);
            var canSubmit = canUploadAttachments && attachmentsComplete;
            var canMakePayment = string.Equals(selectedApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase) && !paymentReceived;
            var canContinue = string.Equals(selectedApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase) && paymentReceived;
            var awaitingSecretary = string.Equals(selectedApplication.Status, "awaiting approval", StringComparison.OrdinalIgnoreCase);

            selectedApplication.PaidFee = fee.Value;
            selectedApplication.DateUpdated = DateTime.Now;
            _db.Update(selectedApplication);
            _db.SaveChanges();

            ViewBag.Appinfo = application;
            ViewBag.Process = process;
            ViewBag.License = _db.LicenseTypes.FirstOrDefault(item => item.Id == application.LicenseTypeID);
            ViewBag.Region = _db.LicenseRegions.FirstOrDefault(item => item.Id == application.ApplicationType);
            ViewBag.Outletinfo = _db.OutletInfo.FirstOrDefault(item => item.ApplicationId == id && item.Status == "active");
            ViewBag.ApplicationRecord = selectedApplication;
            ViewBag.Applications = permissionApplications.OrderByDescending(item => item.DateUpdated).ToList();
            ViewBag.Attachments = attachments;
            ViewBag.Payment = payment;
            ViewBag.Fee = fee.Value;
            ViewBag.TotalFee = fee.Value;
            ViewBag.AttachmentsComplete = attachmentsComplete;
            ViewBag.CanUploadAttachments = canUploadAttachments;
            ViewBag.CanSubmit = canSubmit;
            ViewBag.CanMakePayment = canMakePayment;
            ViewBag.CanContinue = canContinue;
            ViewBag.AwaitingSecretary = awaitingSecretary;
            ViewBag.PaymentReceived = paymentReceived;

            return View();
        }

        [HttpPost("UploadPermissionToAlterAttachment")]
        public async Task<IActionResult> UploadPermissionToAlterAttachmentAsync(string attachmentId, string applicationId, string ecId, IFormFile file)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var permissionApplication = _db.ExtraCounter.FirstOrDefault(item =>
                item.Id == ecId
                && item.ApplicationId == applicationId
                && item.UserId == currentUserId);

            if (permissionApplication == null)
            {
                TempData["error"] = "The permission to alter application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!string.Equals(permissionApplication.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Attachments can only be updated while the permission to alter application is still in progress.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = "ECF", ecId });
            }

            var attachment = _db.AttachmentInfo.FirstOrDefault(item => item.Id == attachmentId && item.ApplicationId == ecId);
            if (attachment == null)
            {
                TempData["error"] = "The selected attachment could not be found.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = "ECF", ecId });
            }

            if (file == null || file.Length <= 0)
            {
                TempData["error"] = "Select a file to upload.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = "ECF", ecId });
            }

            var extension = Path.GetExtension(file.FileName);
            var newFileName = $"{Guid.NewGuid()}{extension}";
            var relativePath = Path.Combine("ApplicationAttchments", newFileName).Replace("\\", "/");
            var uploadDirectory = Path.Combine("wwwroot", "ApplicationAttchments");
            Directory.CreateDirectory(uploadDirectory);

            var absolutePath = Path.Combine(uploadDirectory, newFileName);
            await using (var fileStream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            attachment.DocumentLocation = relativePath;
            attachment.Status = "uploaded";
            attachment.DateUpdated = DateTime.Now;
            _db.Update(attachment);

            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);
            _db.SaveChanges();

            TempData["success"] = $"{attachment.DocumentTitle} uploaded successfully.";
            return RedirectToAction("Extracounter", new { id = applicationId, process = "ECF", ecId });
        }

        [HttpGet("RemovePermissionToAlterAttachment")]
        public IActionResult RemovePermissionToAlterAttachment(string attachmentId, string applicationId, string ecId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var permissionApplication = _db.ExtraCounter.FirstOrDefault(item =>
                item.Id == ecId
                && item.ApplicationId == applicationId
                && item.UserId == currentUserId);

            if (permissionApplication == null)
            {
                TempData["error"] = "The permission to alter application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!string.Equals(permissionApplication.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Attachments can only be removed while the permission to alter application is still in progress.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = "ECF", ecId });
            }

            var attachment = _db.AttachmentInfo.FirstOrDefault(item => item.Id == attachmentId && item.ApplicationId == ecId);
            if (attachment == null)
            {
                TempData["error"] = "The selected attachment could not be found.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = "ECF", ecId });
            }

            attachment.DocumentLocation = string.Empty;
            attachment.Status = "empty";
            attachment.DateUpdated = DateTime.Now;
            _db.Update(attachment);

            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);
            _db.SaveChanges();

            TempData["success"] = $"{attachment.DocumentTitle} removed successfully.";
            return RedirectToAction("Extracounter", new { id = applicationId, process = "ECF", ecId });
        }

        [HttpPost("SubmitPermissionToAlter")]
        public IActionResult SubmitPermissionToAlter(string ecId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var permissionApplication = _db.ExtraCounter.FirstOrDefault(item => item.Id == ecId && item.UserId == currentUserId);
            if (permissionApplication == null)
            {
                TempData["error"] = "The permission to alter application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!string.Equals(permissionApplication.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only in-progress permission to alter applications can be submitted.";
                return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = "ECF", ecId = permissionApplication.Id });
            }

            var attachments = EnsureRequiredPermissionToAlterAttachments(permissionApplication.Id, currentUserId);
            if (!AreAllRequiredDocumentsUploaded(attachments))
            {
                TempData["error"] = "Upload all required attachments before submitting the permission to alter application.";
                return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = "ECF", ecId = permissionApplication.Id });
            }

            permissionApplication.Status = "submitted";
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);
            _db.SaveChanges();

            TempData["success"] = "Permission to alter application submitted. Payment can now be completed.";
            return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = "ECF", ecId = permissionApplication.Id });
        }

        [HttpGet("PermissionToAlterPayment")]
        public IActionResult PermissionToAlterPayment(string id, string process, string ecId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var fee = GetPermissionToAlterFee();
            if (fee == null)
            {
                TempData["error"] = "The permission to alter fee has not been configured.";
                return RedirectToAction("Extracounter", new { id, process = "ECF", ecId });
            }

            var permissionApplication = _db.ExtraCounter.FirstOrDefault(item =>
                item.Id == ecId
                && item.ApplicationId == id
                && item.UserId == currentUserId);

            if (permissionApplication == null)
            {
                TempData["error"] = "The permission to alter application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!string.Equals(permissionApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Submit the permission to alter application before proceeding to payment.";
                return RedirectToAction("Extracounter", new { id, process = "ECF", ecId });
            }

            var attachments = EnsureRequiredPermissionToAlterAttachments(permissionApplication.Id, currentUserId);
            if (!AreAllRequiredDocumentsUploaded(attachments))
            {
                TempData["error"] = "Upload all required attachments before proceeding to payment.";
                return RedirectToAction("Extracounter", new { id, process = "ECF", ecId });
            }

            var existingTransaction = GetLatestPermissionToAlterPaymentForRecord(permissionApplication);
            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                permissionApplication.PaymentStatus = existingTransaction.PaymentStatus ?? existingTransaction.Status ?? "Paid";
                permissionApplication.DateUpdated = DateTime.Now;
                _db.Update(permissionApplication);
                _db.SaveChanges();

                TempData["success"] = "This permission to alter application has already been paid for.";
                return RedirectToAction("Extracounter", new { id, process = "ECF", ecId });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = "Complete the current permission to alter payment before starting another one.";

                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("Extracounter", new { id, process = "ECF", ecId });
            }

            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
            var callbackUrl = Url.Action("Extracounter", "Extracounter", new { id, process = "ECF", ecId }, Request.Scheme);
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            var payment = paynow.CreatePayment("12345");
            var application = _db.ApplicationInfo.FirstOrDefault(item => item.Id == id);
            var licenseType = application == null
                ? null
                : _db.LicenseTypes.FirstOrDefault(item => item.Id == application.LicenseTypeID);

            payment.Add(licenseType?.LicenseName ?? PermissionToAlterService, (decimal)fee.Value);

            var response = paynow.Send(payment);
            if (!response.Success())
            {
                TempData["error"] = "The payment request could not be created. Try again.";
                return RedirectToAction("Extracounter", new { id, process = "ECF", ecId });
            }

            var transaction = new Payments
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUserId,
                Amount = payment.Total,
                ApplicationId = permissionApplication.Id,
                Service = PermissionToAlterService,
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

            permissionApplication.PaidFee = fee.Value;
            permissionApplication.PaymentStatus = transaction.PaymentStatus ?? "Not Paid";
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);
            _db.SaveChanges();

            return Redirect(response.RedirectLink());
        }

        [HttpPost("ContinuePermissionToAlter")]
        public async Task<IActionResult> ContinuePermissionToAlterAsync(string ecId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var permissionApplication = _db.ExtraCounter.FirstOrDefault(item => item.Id == ecId && item.UserId == currentUserId);
            if (permissionApplication == null)
            {
                TempData["error"] = "The permission to alter application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (!string.Equals(permissionApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This permission to alter application is not ready to continue.";
                return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = "ECF", ecId = permissionApplication.Id });
            }

            var payment = GetLatestPermissionToAlterPaymentForRecord(permissionApplication);
            if (payment == null || !HasPaymentStatus(payment, "Paid"))
            {
                TempData["error"] = "Payment must be completed before the application can be sent to the secretary.";
                return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = "ECF", ecId = permissionApplication.Id });
            }

            var existingTask = _db.Tasks.FirstOrDefault(task =>
                task.ApplicationId == permissionApplication.Id
                && task.Status == "assigned"
                && (task.Service == PermissionToAlterService || task.Service == LegacyExtraCounterService));

            if (existingTask == null)
            {
                var secretaryId = await _taskAllocationHelper.GetSecretary(_db, _userManager);
                if (string.IsNullOrWhiteSpace(secretaryId))
                {
                    TempData["error"] = "No secretary is currently available to receive the permission to alter application.";
                    return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = "ECF", ecId = permissionApplication.Id });
                }

                var task = new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = permissionApplication.Id,
                    ApproverId = secretaryId,
                    AssignerId = "system",
                    Service = PermissionToAlterService,
                    ExaminationStatus = "approval",
                    Status = "assigned",
                    DateAdded = DateTime.Now,
                    DateUpdated = DateTime.Now
                };

                _db.Add(task);
            }

            permissionApplication.Status = "awaiting approval";
            permissionApplication.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? "Paid";
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);
            _db.SaveChanges();

            TempData["success"] = "Permission to alter application sent to the secretary for approval.";
            return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = "ECF", ecId = permissionApplication.Id });
        }

        [HttpGet("ViewApplications")]
        public IActionResult ViewApplications(string Id)
        {
            var permissionApplication = _db.ExtraCounter.FirstOrDefault(item => item.Id == Id);
            if (permissionApplication == null)
            {
                TempData["error"] = "The permission to alter application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var application = _db.ApplicationInfo.FirstOrDefault(item => item.Id == permissionApplication.ApplicationId);
            var attachments = OrderRequiredPermissionToAlterAttachments(
                _db.AttachmentInfo.Where(item => item.ApplicationId == permissionApplication.Id).ToList());
            var task = _db.Tasks
                .Where(item => item.ApplicationId == Id && item.Status == "assigned")
                .OrderByDescending(item => item.DateAdded)
                .FirstOrDefault();

            ViewBag.Application = permissionApplication;
            ViewBag.RootApplication = application;
            ViewBag.OutletInfo = _db.OutletInfo.FirstOrDefault(item => item.ApplicationId == permissionApplication.ApplicationId && item.Status == "active");
            ViewBag.License = application == null ? null : _db.LicenseTypes.FirstOrDefault(item => item.Id == application.LicenseTypeID);
            ViewBag.Region = application == null ? null : _db.LicenseRegions.FirstOrDefault(item => item.Id == application.ApplicationType);
            ViewBag.Attachments = attachments;
            ViewBag.Task = task;

            return View();
        }

        [HttpGet("Approve")]
        public IActionResult Approve(string Id)
        {
            var permissionApplication = _db.ExtraCounter.FirstOrDefault(item => item.Id == Id);
            if (permissionApplication == null)
            {
                TempData["error"] = "The permission to alter application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            permissionApplication.ApproverId = _userManager.GetUserId(User);
            permissionApplication.DateOfApproval = DateTime.Now;
            permissionApplication.Status = "Approved";
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);

            var task = _db.Tasks.FirstOrDefault(item =>
                item.ApplicationId == Id
                && item.Status == "assigned"
                && (item.Service == PermissionToAlterService || item.Service == LegacyExtraCounterService));

            if (task != null)
            {
                task.Status = "completed";
                task.ApprovedDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Update(task);
            }

            _db.SaveChanges();
            TempData["success"] = "Permission to alter application approved successfully.";
            return RedirectToAction("ViewApplications", new { Id });
        }

        [HttpGet("Reject")]
        public IActionResult Reject(string Id)
        {
            var permissionApplication = _db.ExtraCounter.FirstOrDefault(item => item.Id == Id);
            if (permissionApplication == null)
            {
                TempData["error"] = "The permission to alter application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            permissionApplication.ApproverId = _userManager.GetUserId(User);
            permissionApplication.DateOfApproval = DateTime.Now;
            permissionApplication.Status = "Rejected";
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);

            var task = _db.Tasks.FirstOrDefault(item =>
                item.ApplicationId == Id
                && item.Status == "assigned"
                && (item.Service == PermissionToAlterService || item.Service == LegacyExtraCounterService));

            if (task != null)
            {
                task.Status = "completed";
                task.ApprovedDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Update(task);
            }

            _db.SaveChanges();
            TempData["success"] = "Permission to alter application rejected.";
            return RedirectToAction("ViewApplications", new { Id });
        }

        private double? GetPermissionToAlterFee()
        {
            return _db.PostFormationFees
                .Where(item => item.ProcessName == PermissionToAlterService)
                .Select(item => (double?)item.Fee)
                .FirstOrDefault()
                ?? _db.PostFormationFees
                    .Where(item => item.ProcessName == LegacyExtraCounterService)
                    .Select(item => (double?)item.Fee)
                    .FirstOrDefault();
        }

        private List<ExtraCounter> GetPermissionToAlterApplications(string applicationId, string userId)
        {
            return _db.ExtraCounter
                .Where(item => item.ApplicationId == applicationId && item.UserId == userId)
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .ToList();
        }

        private ExtraCounter? GetLatestOpenPermissionToAlterApplication(string applicationId, string userId)
        {
            return _db.ExtraCounter
                .Where(item => item.ApplicationId == applicationId
                    && item.UserId == userId
                    && item.Status != null
                    && item.Status != "Approved"
                    && item.Status != "Rejected")
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .FirstOrDefault();
        }

        private ExtraCounter CreatePermissionToAlterDraft(string applicationId, string userId, double fee)
        {
            var draft = new ExtraCounter
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Status = "inprogress",
                Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, "ECF"),
                ApplicationId = applicationId,
                PaidFee = fee,
                PaymentStatus = "Not Paid",
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };

            _db.Add(draft);
            _db.SaveChanges();

            EnsureRequiredPermissionToAlterAttachments(draft.Id, userId);
            return draft;
        }

        private List<AttachmentInfo> EnsureRequiredPermissionToAlterAttachments(string applicationRecordId, string userId)
        {
            var attachments = _db.AttachmentInfo
                .Where(item => item.ApplicationId == applicationRecordId)
                .ToList();

            var hasChanges = false;
            foreach (var documentTitle in RequiredPermissionToAlterDocuments)
            {
                if (attachments.Any(item => string.Equals(item.DocumentTitle, documentTitle, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var attachment = new AttachmentInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ApplicationId = applicationRecordId,
                    DocumentTitle = documentTitle,
                    DocumentLocation = string.Empty,
                    Status = "empty",
                    DateAdded = DateTime.Now,
                    DateUpdated = DateTime.Now
                };

                attachments.Add(attachment);
                _db.Add(attachment);
                hasChanges = true;
            }

            if (hasChanges)
            {
                _db.SaveChanges();
            }

            return OrderRequiredPermissionToAlterAttachments(attachments);
        }

        private static List<AttachmentInfo> OrderRequiredPermissionToAlterAttachments(IEnumerable<AttachmentInfo> attachments)
        {
            return attachments
                .OrderBy(item => Array.IndexOf(RequiredPermissionToAlterDocuments, item.DocumentTitle ?? string.Empty))
                .ThenBy(item => item.DocumentTitle)
                .ToList();
        }

        private static bool AreAllRequiredDocumentsUploaded(IEnumerable<AttachmentInfo> attachments)
        {
            var attachmentLookup = attachments
                .Where(item => !string.IsNullOrWhiteSpace(item.DocumentTitle))
                .GroupBy(item => item.DocumentTitle!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var documentTitle in RequiredPermissionToAlterDocuments)
            {
                if (!attachmentLookup.TryGetValue(documentTitle, out var attachment)
                    || string.IsNullOrWhiteSpace(attachment.DocumentLocation))
                {
                    return false;
                }
            }

            return true;
        }

        private Payments? GetLatestPermissionToAlterPaymentForRecord(ExtraCounter? permissionApplication)
        {
            if (permissionApplication == null || string.IsNullOrWhiteSpace(permissionApplication.Id))
            {
                return null;
            }

            return _db.Payments
                .Where(item => item.ApplicationId == permissionApplication.Id
                    && (item.Service == PermissionToAlterService
                        || item.Service == LegacyExtraCounterService
                        || item.Service == "extra counter"))
                .OrderByDescending(item => item.DateAdded)
                .FirstOrDefault();
        }

        private Payments? RefreshPermissionToAlterPaymentStatus(ExtraCounter? permissionApplication)
        {
            var payment = GetLatestPermissionToAlterPaymentForRecord(permissionApplication);
            if (payment == null || permissionApplication == null)
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

            permissionApplication.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? permissionApplication.PaymentStatus;
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);
            _db.SaveChanges();

            return payment;
        }

        private static bool IsFinalPermissionToAlterStatus(string? status)
        {
            return string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase);
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
