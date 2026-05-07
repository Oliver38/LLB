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
        private const string ExtraCounterService = "Extra Counter";
        private const string PermissionToAlterProcessCode = "ECF";
        private const string ExtraCounterProcessCode = "EXC";

        private static readonly string[] RequiredPermissionToAlterDocuments =
        {
            "Local Authority Letter of approval",
            "Tie Affidavit",
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
            var processCode = NormalizeProcessCode(process);
            var serviceName = GetServiceNameForProcess(processCode);
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
            permissionApplications = permissionApplications
                .Where(item => IsRecordForProcess(item, processCode))
                .ToList();
            var selectedApplication = !string.IsNullOrWhiteSpace(ecId)
                ? permissionApplications.FirstOrDefault(item => item.Id == ecId)
                : GetLatestOpenPermissionToAlterApplication(id, currentUserId, processCode);

            if (selectedApplication == null)
            {
                selectedApplication = CreatePermissionToAlterDraft(id, currentUserId, fee.Value, processCode);
                return RedirectToAction("Extracounter", new { id, process = processCode, ecId = selectedApplication.Id });
            }

            var requiresAlterationDetails = !IsExtraCounterRecord(selectedApplication);
            var attachments = requiresAlterationDetails
                ? new List<AttachmentInfo>()
                : EnsureRequiredPermissionToAlterAttachments(selectedApplication.Id, currentUserId);
            var payment = RefreshPermissionToAlterPaymentStatus(selectedApplication);
            var paymentReceived = payment != null && HasPaymentStatus(payment, "Paid");
            var attachmentsComplete = requiresAlterationDetails || AreAllRequiredDocumentsUploaded(attachments);
            var alterationDetailsComplete = !requiresAlterationDetails || IsAlterationDetailsComplete(selectedApplication);
            var canUploadAttachments = string.Equals(selectedApplication.Status, "inprogress", StringComparison.OrdinalIgnoreCase);
            var canSubmit = canUploadAttachments && attachmentsComplete && alterationDetailsComplete;
            var canMakePayment = string.Equals(selectedApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase) && !paymentReceived;
            var canContinue = string.Equals(selectedApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase) && paymentReceived;
            var awaitingSecretary = string.Equals(selectedApplication.Status, "awaiting approval", StringComparison.OrdinalIgnoreCase);

            selectedApplication.PaidFee = fee.Value;
            selectedApplication.DateUpdated = DateTime.Now;
            _db.Update(selectedApplication);
            _db.SaveChanges();

            ViewBag.Appinfo = application;
            ViewBag.Process = processCode;
            ViewBag.License = _db.LicenseTypes.FirstOrDefault(item => item.Id == application.LicenseTypeID);
            ViewBag.Region = _db.LicenseRegions.FirstOrDefault(item => item.Id == application.ApplicationType);
            ViewBag.Outletinfo = _db.OutletInfo.FirstOrDefault(item => item.ApplicationId == id && item.Status == "active");
            ViewBag.ApplicationRecord = selectedApplication;
            ViewBag.Applications = permissionApplications.OrderByDescending(item => item.DateUpdated).ToList();
            ViewBag.Attachments = attachments;
            ViewBag.Payment = payment;
            ViewBag.Fee = fee.Value;
            ViewBag.TotalFee = fee.Value;
            ViewBag.ServiceName = serviceName;
            ViewBag.ProcessCode = processCode;
            ViewBag.AttachmentsComplete = attachmentsComplete;
            ViewBag.RequiresAlterationDetails = requiresAlterationDetails;
            ViewBag.AlterationDetailsComplete = alterationDetailsComplete;
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

            var processCode = GetProcessCodeForRecord(permissionApplication);
            var serviceName = GetServiceNameForRecord(permissionApplication);
            var serviceLower = serviceName.ToLowerInvariant();

            if (!string.Equals(permissionApplication.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = $"Attachments can only be updated while the {serviceLower} application is still in progress.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
            }

            var attachment = _db.AttachmentInfo.FirstOrDefault(item => item.Id == attachmentId && item.ApplicationId == ecId);
            if (attachment == null)
            {
                TempData["error"] = "The selected attachment could not be found.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
            }

            if (file == null || file.Length <= 0)
            {
                TempData["error"] = "Select a file to upload.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
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
            return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
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

            var processCode = GetProcessCodeForRecord(permissionApplication);
            var serviceName = GetServiceNameForRecord(permissionApplication);
            var serviceLower = serviceName.ToLowerInvariant();

            if (!string.Equals(permissionApplication.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = $"Attachments can only be removed while the {serviceLower} application is still in progress.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
            }

            var attachment = _db.AttachmentInfo.FirstOrDefault(item => item.Id == attachmentId && item.ApplicationId == ecId);
            if (attachment == null)
            {
                TempData["error"] = "The selected attachment could not be found.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
            }

            attachment.DocumentLocation = string.Empty;
            attachment.Status = "empty";
            attachment.DateUpdated = DateTime.Now;
            _db.Update(attachment);

            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);
            _db.SaveChanges();

            TempData["success"] = $"{attachment.DocumentTitle} removed successfully.";
            return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
        }

        [HttpPost("SavePermissionToAlterDetails")]
        public async Task<IActionResult> SavePermissionToAlterDetailsAsync(string applicationId, string ecId, string alterationReason, IFormFile? planFile)
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

            var processCode = GetProcessCodeForRecord(permissionApplication);
            if (IsExtraCounterRecord(permissionApplication))
            {
                TempData["error"] = "Alteration details are only required for permission to alter applications.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
            }

            if (!string.Equals(permissionApplication.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Alteration details can only be updated while the permission to alter application is still in progress.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
            }

            if (string.IsNullOrWhiteSpace(alterationReason))
            {
                TempData["error"] = "Enter the reason for the alterations.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
            }

            if ((planFile == null || planFile.Length <= 0) && string.IsNullOrWhiteSpace(permissionApplication.NewPlanPath))
            {
                TempData["error"] = "Attach the alteration plan.";
                return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
            }

            if (planFile != null && planFile.Length > 0)
            {
                permissionApplication.NewPlanPath = await SaveApplicationAttachmentAsync(planFile);
            }

            permissionApplication.ExtracounterReason = alterationReason.Trim();
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);
            _db.SaveChanges();

            TempData["success"] = "Alteration details saved successfully.";
            return RedirectToAction("Extracounter", new { id = applicationId, process = processCode, ecId });
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

            var processCode = GetProcessCodeForRecord(permissionApplication);
            var serviceName = GetServiceNameForRecord(permissionApplication);
            var serviceLower = serviceName.ToLowerInvariant();
            var actionPast = GetSubmissionActionPastForRecord(permissionApplication);
            var actionGerund = GetSubmissionActionGerundForRecord(permissionApplication);

            if (!string.Equals(permissionApplication.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = $"Only in-progress {serviceLower} applications can be {actionPast}.";
                return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = processCode, ecId = permissionApplication.Id });
            }

            if (IsExtraCounterRecord(permissionApplication)
                && !AreAllRequiredDocumentsUploaded(EnsureRequiredPermissionToAlterAttachments(permissionApplication.Id, currentUserId)))
            {
                TempData["error"] = $"Upload all required attachments before {actionGerund} the {serviceLower} application.";
                return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = processCode, ecId = permissionApplication.Id });
            }

            if (!IsExtraCounterRecord(permissionApplication) && !IsAlterationDetailsComplete(permissionApplication))
            {
                TempData["error"] = "Attach the alteration plan and enter the reason for the alterations before submitting the permission to alter application.";
                return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = processCode, ecId = permissionApplication.Id });
            }

            permissionApplication.Status = "submitted";
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);
            _db.SaveChanges();

            TempData["success"] = $"{serviceName} application {actionPast}. Payment can now be completed.";
            return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = processCode, ecId = permissionApplication.Id });
        }

        [HttpGet("PermissionToAlterPayment")]
        public IActionResult PermissionToAlterPayment(string id, string process, string ecId)
        {
            var processCode = NormalizeProcessCode(process);
            var serviceName = GetServiceNameForProcess(processCode);
            var serviceLower = serviceName.ToLowerInvariant();
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var fee = GetPermissionToAlterFee();
            if (fee == null)
            {
                TempData["error"] = "The permission to alter fee has not been configured.";
                return RedirectToAction("Extracounter", new { id, process = processCode, ecId });
            }

            var permissionApplication = _db.ExtraCounter.FirstOrDefault(item =>
                item.Id == ecId
                && item.ApplicationId == id
                && item.UserId == currentUserId);

            if (permissionApplication == null)
            {
                TempData["error"] = $"The {serviceLower} application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            processCode = GetProcessCodeForRecord(permissionApplication);
            serviceName = GetServiceNameForRecord(permissionApplication);
            serviceLower = serviceName.ToLowerInvariant();
            var actionTitle = GetSubmissionActionTitleForRecord(permissionApplication);

            if (!string.Equals(permissionApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = $"{actionTitle} the {serviceLower} application before proceeding to payment.";
                return RedirectToAction("Extracounter", new { id, process = processCode, ecId });
            }

            if (IsExtraCounterRecord(permissionApplication)
                && !AreAllRequiredDocumentsUploaded(EnsureRequiredPermissionToAlterAttachments(permissionApplication.Id, currentUserId)))
            {
                TempData["error"] = "Upload all required attachments before proceeding to payment.";
                return RedirectToAction("Extracounter", new { id, process = processCode, ecId });
            }

            var existingTransaction = GetLatestPermissionToAlterPaymentForRecord(permissionApplication);
            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                permissionApplication.PaymentStatus = existingTransaction.PaymentStatus ?? existingTransaction.Status ?? "Paid";
                permissionApplication.DateUpdated = DateTime.Now;
                _db.Update(permissionApplication);
                _db.SaveChanges();

                TempData["success"] = $"This {serviceLower} application has already been paid for.";
                return RedirectToAction("Extracounter", new { id, process = processCode, ecId });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = $"Complete the current {serviceLower} payment before starting another one.";

                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("Extracounter", new { id, process = processCode, ecId });
            }

            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
            var callbackUrl = Url.Action("Extracounter", "Extracounter", new { id, process = processCode, ecId }, Request.Scheme);
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            var payment = paynow.CreatePayment("12345");
            payment.Add(serviceName, (decimal)fee.Value);

            var response = paynow.Send(payment);
            if (!response.Success())
            {
                TempData["error"] = "The payment request could not be created. Try again.";
                return RedirectToAction("Extracounter", new { id, process = processCode, ecId });
            }

            var transaction = new Payments
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUserId,
                Amount = payment.Total,
                ApplicationId = permissionApplication.Id,
                Service = serviceName,
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

            var processCode = GetProcessCodeForRecord(permissionApplication);
            var serviceName = GetServiceNameForRecord(permissionApplication);
            var serviceLower = serviceName.ToLowerInvariant();

            if (!string.Equals(permissionApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = $"This {serviceLower} application is not ready to continue.";
                return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = processCode, ecId = permissionApplication.Id });
            }

            var payment = GetLatestPermissionToAlterPaymentForRecord(permissionApplication);
            if (payment == null || !HasPaymentStatus(payment, "Paid"))
            {
                TempData["error"] = "Payment must be completed before the application can be sent to the secretary.";
                return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = processCode, ecId = permissionApplication.Id });
            }

            var existingTask = _db.Tasks.FirstOrDefault(task =>
                task.ApplicationId == permissionApplication.Id
                && task.Status == "assigned"
                && (task.Service == PermissionToAlterService || task.Service == ExtraCounterService));

            if (existingTask == null)
            {
                var secretaryId = await _taskAllocationHelper.GetSecretary(_db, _userManager);
                if (string.IsNullOrWhiteSpace(secretaryId))
                {
                    TempData["error"] = $"No secretary is currently available to receive the {serviceLower} application.";
                    return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = processCode, ecId = permissionApplication.Id });
                }

                var task = new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = permissionApplication.Id,
                    ApproverId = secretaryId,
                    AssignerId = "system",
                    Service = serviceName,
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

            TempData["success"] = $"{serviceName} application sent to the secretary for approval.";
            return RedirectToAction("Extracounter", new { id = permissionApplication.ApplicationId, process = processCode, ecId = permissionApplication.Id });
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

            var serviceName = GetServiceNameForRecord(permissionApplication);
            var application = _db.ApplicationInfo.FirstOrDefault(item => item.Id == permissionApplication.ApplicationId);
            var attachments = IsExtraCounterRecord(permissionApplication)
                ? OrderRequiredPermissionToAlterAttachments(_db.AttachmentInfo.Where(item => item.ApplicationId == permissionApplication.Id).ToList())
                : new List<AttachmentInfo>();
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
            ViewBag.ServiceName = serviceName;

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

            var serviceName = GetServiceNameForRecord(permissionApplication);
            permissionApplication.ApproverId = _userManager.GetUserId(User);
            permissionApplication.DateOfApproval = DateTime.Now;
            permissionApplication.Status = "Approved";
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);

            var task = _db.Tasks.FirstOrDefault(item =>
                item.ApplicationId == Id
                && item.Status == "assigned"
                && (item.Service == PermissionToAlterService || item.Service == ExtraCounterService));

            if (task != null)
            {
                task.Status = "completed";
                task.ApprovedDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Update(task);
            }

            _db.SaveChanges();
            TempData["success"] = $"{serviceName} application approved successfully.";
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

            var serviceName = GetServiceNameForRecord(permissionApplication);
            permissionApplication.ApproverId = _userManager.GetUserId(User);
            permissionApplication.DateOfApproval = DateTime.Now;
            permissionApplication.Status = "Rejected";
            permissionApplication.DateUpdated = DateTime.Now;
            _db.Update(permissionApplication);

            var task = _db.Tasks.FirstOrDefault(item =>
                item.ApplicationId == Id
                && item.Status == "assigned"
                && (item.Service == PermissionToAlterService || item.Service == ExtraCounterService));

            if (task != null)
            {
                task.Status = "completed";
                task.ApprovedDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Update(task);
            }

            _db.SaveChanges();
            TempData["success"] = $"{serviceName} application rejected.";
            return RedirectToAction("ViewApplications", new { Id });
        }

        private double? GetPermissionToAlterFee()
        {
            return _db.PostFormationFees
                .Where(item => item.ProcessName == PermissionToAlterService)
                .Select(item => (double?)item.Fee)
                .FirstOrDefault()
                ?? _db.PostFormationFees
                    .Where(item => item.ProcessName == ExtraCounterService)
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

        private ExtraCounter? GetLatestOpenPermissionToAlterApplication(string applicationId, string userId, string processCode)
        {
            return _db.ExtraCounter
                .Where(item => item.ApplicationId == applicationId
                    && item.UserId == userId
                    && item.Status != null
                    && item.Status != "Approved"
                    && item.Status != "Rejected")
                .ToList()
                .Where(item => IsRecordForProcess(item, processCode))
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .FirstOrDefault();
        }

        private ExtraCounter CreatePermissionToAlterDraft(string applicationId, string userId, double fee, string processCode)
        {
            var draft = new ExtraCounter
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Status = "inprogress",
                Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, processCode),
                ApplicationId = applicationId,
                PaidFee = fee,
                PaymentStatus = "Not Paid",
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };

            _db.Add(draft);
            _db.SaveChanges();

            if (NormalizeProcessCode(processCode) == ExtraCounterProcessCode)
            {
                EnsureRequiredPermissionToAlterAttachments(draft.Id, userId);
            }

            return draft;
        }

        private static string NormalizeProcessCode(string? process)
        {
            return string.Equals(process?.Trim(), ExtraCounterProcessCode, StringComparison.OrdinalIgnoreCase)
                ? ExtraCounterProcessCode
                : PermissionToAlterProcessCode;
        }

        private static string GetServiceNameForProcess(string? process)
        {
            return string.Equals(NormalizeProcessCode(process), ExtraCounterProcessCode, StringComparison.OrdinalIgnoreCase)
                ? ExtraCounterService
                : PermissionToAlterService;
        }

        private static string GetProcessCodeForRecord(ExtraCounter? record)
        {
            return IsExtraCounterRecord(record)
                ? ExtraCounterProcessCode
                : PermissionToAlterProcessCode;
        }

        private static string GetServiceNameForRecord(ExtraCounter? record)
        {
            return IsExtraCounterRecord(record)
                ? ExtraCounterService
                : PermissionToAlterService;
        }

        private static string GetSubmissionActionTitleForRecord(ExtraCounter? record)
        {
            return IsExtraCounterRecord(record) ? "Initialise" : "Submit";
        }

        private static string GetSubmissionActionPastForRecord(ExtraCounter? record)
        {
            return IsExtraCounterRecord(record) ? "initialised" : "submitted";
        }

        private static string GetSubmissionActionGerundForRecord(ExtraCounter? record)
        {
            return IsExtraCounterRecord(record) ? "initialising" : "submitting";
        }

        private static bool IsRecordForProcess(ExtraCounter? record, string processCode)
        {
            var normalizedProcess = NormalizeProcessCode(processCode);
            return normalizedProcess == ExtraCounterProcessCode
                ? IsExtraCounterRecord(record)
                : !IsExtraCounterRecord(record);
        }

        private static bool IsExtraCounterRecord(ExtraCounter? record)
        {
            return record?.Reference?.StartsWith($"PF-{ExtraCounterProcessCode}-", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsAlterationDetailsComplete(ExtraCounter? record)
        {
            return record != null
                && !string.IsNullOrWhiteSpace(record.NewPlanPath)
                && !string.IsNullOrWhiteSpace(record.ExtracounterReason);
        }

        private static async Task<string> SaveApplicationAttachmentAsync(IFormFile file)
        {
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

            return relativePath;
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
                        || item.Service == ExtraCounterService
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
