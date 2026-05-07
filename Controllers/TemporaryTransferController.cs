using System.Text.Json;
using System.Net;
using System.Net.Mail;
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
    [Route("TemporaryTransfer")]
    public class TemporaryTransferController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public TemporaryTransferController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            TaskAllocationHelper taskAllocationHelper)
        {
            _db = db;
            _userManager = userManager;
            _taskAllocationHelper = taskAllocationHelper;
        }

        [HttpGet("List")]
        public async Task<IActionResult> ListAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var transferApplications = await _db.ApplicationInfo
                .AsNoTracking()
                .Where(item => item.UserID == currentUserId && item.ExaminationStatus == TemporaryTransferHelper.ServiceName)
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.ApplicationDate)
                .ToListAsync();

            var rootApplicationIds = transferApplications
                .Select(item => item.CompanyNumber)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rootApplications = rootApplicationIds.Count == 0
                ? new List<ApplicationInfo>()
                : await _db.ApplicationInfo
                    .AsNoTracking()
                    .Where(item => item.Id != null && rootApplicationIds.Contains(item.Id))
                    .ToListAsync();

            var rootApplicationLookup = rootApplications
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(item => item.Id!, item => item, StringComparer.OrdinalIgnoreCase);

            var outletLookup = await _db.OutletInfo
                .AsNoTracking()
                .Where(item => item.ApplicationId != null && rootApplicationIds.Contains(item.ApplicationId))
                .GroupBy(item => item.ApplicationId!)
                .ToDictionaryAsync(
                    group => group.Key,
                    group => group
                        .OrderByDescending(item => item.Status != null && item.Status.ToLower() == "active")
                        .ThenByDescending(item => item.DateUpdated)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);

            var licenseIds = rootApplications
                .Select(item => item.LicenseTypeID)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var regionIds = rootApplications
                .Select(item => item.ApplicationType)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var licenseLookup = licenseIds.Count == 0
                ? new Dictionary<string, LicenseTypes>(StringComparer.OrdinalIgnoreCase)
                : await _db.LicenseTypes
                    .AsNoTracking()
                    .Where(item => item.Id != null && licenseIds.Contains(item.Id))
                    .ToDictionaryAsync(item => item.Id!, item => item, StringComparer.OrdinalIgnoreCase);

            var regionLookup = regionIds.Count == 0
                ? new Dictionary<string, LicenseRegion>(StringComparer.OrdinalIgnoreCase)
                : await _db.LicenseRegions
                    .AsNoTracking()
                    .Where(item => item.Id != null && regionIds.Contains(item.Id))
                    .ToDictionaryAsync(item => item.Id!, item => item, StringComparer.OrdinalIgnoreCase);

            var rows = new List<ClientPostFormationListingViewModel>();
            foreach (var transferApplication in transferApplications)
            {
                if (string.IsNullOrWhiteSpace(transferApplication.CompanyNumber)
                    || !rootApplicationLookup.TryGetValue(transferApplication.CompanyNumber, out var rootApplication))
                {
                    continue;
                }

                outletLookup.TryGetValue(transferApplication.CompanyNumber, out var outlet);

                var licenseName = "N/A";
                if (!string.IsNullOrWhiteSpace(rootApplication.LicenseTypeID)
                    && licenseLookup.TryGetValue(rootApplication.LicenseTypeID, out var license))
                {
                    licenseName = license.LicenseName ?? "N/A";
                }

                var regionName = "N/A";
                if (!string.IsNullOrWhiteSpace(rootApplication.ApplicationType)
                    && regionLookup.TryGetValue(rootApplication.ApplicationType, out var region))
                {
                    regionName = region.RegionName ?? "N/A";
                }

                rows.Add(new ClientPostFormationListingViewModel
                {
                    RecordId = transferApplication.Id ?? string.Empty,
                    Reference = transferApplication.RefNum ?? "Pending",
                    ApplicationId = rootApplication.Id ?? string.Empty,
                    TradingName = outlet?.TradingName ?? rootApplication.BusinessName ?? "N/A",
                    LLBNumber = rootApplication.LLBNum ?? "N/A",
                    LicenseName = licenseName,
                    RegionName = regionName,
                    Status = transferApplication.Status ?? "Unknown",
                    SubmittedDate = transferApplication.ApplicationDate,
                    ServiceName = TemporaryTransferHelper.GetTransferType(transferApplication),
                    ActionUrl = $"/TemporaryTransfer/Apply?id={transferApplication.Id}",
                    ActionLabel = "Open Transfer"
                });
            }

            ViewBag.Listings = rows;
            return View();
        }

        [HttpGet("Apply")]
        public async Task<IActionResult> ApplyAsync(string? id, string? llbNumber, string? transferType, bool edit = false)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ApplicationInfo? transferApplication = null;
            ApplicationInfo? sourceApplication = null;

            if (!string.IsNullOrWhiteSpace(id))
            {
                transferApplication = await _db.ApplicationInfo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.Id == id
                        && item.UserID == currentUser.Id
                        && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

                if (transferApplication == null)
                {
                    TempData["error"] = "The transfer application could not be found.";
                    return RedirectToAction("List");
                }

                sourceApplication = await GetSourceApplicationAsync(transferApplication.CompanyNumber);
                if (sourceApplication == null)
                {
                    TempData["error"] = "The source liquor licence linked to this transfer could not be found.";
                    return RedirectToAction("List");
                }
            }
            else if (!string.IsNullOrWhiteSpace(llbNumber))
            {
                sourceApplication = await FindApprovedSourceApplicationByLlbNumberAsync(llbNumber);
                if (sourceApplication == null)
                {
                    TempData["error"] = $"No approved liquor licence was found for LLB number {llbNumber}.";
                    await PopulateApplyViewAsync(currentUser, null, null, llbNumber, transferType);
                    return View();
                }

                transferApplication = await GetOpenTemporaryTransferApplicationAsync(currentUser.Id, sourceApplication.Id, transferType);
            }

            await PopulateApplyViewAsync(currentUser, sourceApplication, transferApplication, llbNumber, transferType);
            ViewBag.EditMode = edit;
            return View();
        }

        [HttpPost("Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAsync(
            string llbNumber,
            string transferType,
            string applicantType,
            string businessName,
            string operationAddress,
            string? placeOfEntry,
            string? dateofEntryIntoZimbabwe,
            string? managerDraftJson)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var sourceApplication = await FindApprovedSourceApplicationByLlbNumberAsync(llbNumber);
            if (sourceApplication == null)
            {
                TempData["error"] = $"No approved liquor licence was found for LLB number {llbNumber}.";
                return RedirectToAction("Apply");
            }

            var normalizedTransferType = TemporaryTransferHelper.NormalizeTransferType(transferType);
            if (string.IsNullOrWhiteSpace(normalizedTransferType))
            {
                TempData["error"] = "Select whether this is a permanent transfer or a temporary transfer.";
                return RedirectToAction("Apply", new { llbNumber = sourceApplication.LLBNum });
            }

            var existingOpenApplication = await GetOpenTemporaryTransferApplicationAsync(currentUser.Id, sourceApplication.Id, normalizedTransferType);
            if (existingOpenApplication != null)
            {
                TempData["error"] = $"A {normalizedTransferType.ToLowerInvariant()} application for this liquor licence is already in progress.";
                return RedirectToAction("Apply", new { id = existingOpenApplication.Id });
            }

            if (string.IsNullOrWhiteSpace(applicantType)
                || string.IsNullOrWhiteSpace(businessName)
                || string.IsNullOrWhiteSpace(operationAddress))
            {
                TempData["error"] = "Complete all applicant information fields before submitting the transfer application.";
                return RedirectToAction("Apply", new { llbNumber = sourceApplication.LLBNum, transferType = normalizedTransferType });
            }

            var fee = await GetTemporaryTransferFeeAsync(sourceApplication);
            if (fee == null)
            {
                TempData["error"] = "The transfer fee has not been configured.";
                return RedirectToAction("Apply", new { llbNumber = sourceApplication.LLBNum, transferType = normalizedTransferType });
            }

            var draftManagers = ParseTemporaryTransferDraftManagers(managerDraftJson);
            if (draftManagers == null)
            {
                TempData["error"] = "The transfer managers list could not be processed. Add the managers again and retry.";
                return RedirectToAction("Apply", new { llbNumber = sourceApplication.LLBNum, transferType = normalizedTransferType });
            }

            var managerValidationError = ValidateTemporaryTransferManagers(draftManagers);
            if (!string.IsNullOrWhiteSpace(managerValidationError))
            {
                TempData["error"] = managerValidationError;
                return RedirectToAction("Apply", new { llbNumber = sourceApplication.LLBNum, transferType = normalizedTransferType });
            }

            var draftManagerPathValidationError = ValidateTemporaryTransferDraftManagerFiles(draftManagers, currentUser.Id);
            if (!string.IsNullOrWhiteSpace(draftManagerPathValidationError))
            {
                TempData["error"] = draftManagerPathValidationError;
                return RedirectToAction("Apply", new { llbNumber = sourceApplication.LLBNum, transferType = normalizedTransferType });
            }

            var pricing = await BuildTemporaryTransferPricingAsync(sourceApplication, draftManagers.Count);
            if (!pricing.BaseFee.HasValue)
            {
                TempData["error"] = "The transfer fee has not been configured.";
                return RedirectToAction("Apply", new { llbNumber = sourceApplication.LLBNum, transferType = normalizedTransferType });
            }

            if (pricing.ChargeableManagerCount > 0 && !pricing.ManagerFee.HasValue)
            {
                TempData["error"] = "The additional manager fee has not been configured.";
                return RedirectToAction("Apply", new { llbNumber = sourceApplication.LLBNum, transferType = normalizedTransferType });
            }

            var transferApplication = new ApplicationInfo
            {
                Id = Guid.NewGuid().ToString(),
                UserID = currentUser.Id,
                ApplicationType = sourceApplication.ApplicationType,
                LicenseTypeID = sourceApplication.LicenseTypeID,
                PaymentStatus = "Not Paid",
                PaymentId = string.Empty,
                RefNum = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, TemporaryTransferHelper.ServiceCode),
                ExaminationStatus = TemporaryTransferHelper.ServiceName,
                PaymentFee = pricing.TotalFee ?? fee.Value,
                PlaceOfBirth = normalizedTransferType,
                DateofEntryIntoZimbabwe = IsZimbabweanApplicant(currentUser) ? string.Empty : dateofEntryIntoZimbabwe ?? string.Empty,
                PlaceOfEntry = IsZimbabweanApplicant(currentUser) ? string.Empty : placeOfEntry ?? string.Empty,
                OperationAddress = operationAddress,
                LLBNum = sourceApplication.LLBNum,
                ApplicantType = applicantType,
                BusinessName = businessName,
                IdPass = currentUser.NatID ?? string.Empty,
                Status = "submitted",
                ApplicationDate = DateTime.Now,
                DateUpdated = DateTime.Now,
                InspectorID = string.Empty,
                Secretary = string.Empty,
                RejectionReason = string.Empty,
                CompanyNumber = sourceApplication.Id
            };

            _db.Add(transferApplication);
            _db.SaveChanges();

            if (draftManagers.Count > 0)
            {
                foreach (var draftManager in draftManagers)
                {
                    var managerId = Guid.NewGuid().ToString();
                    _db.Add(new ManagersParticulars
                    {
                        Id = managerId,
                        UserId = currentUser.Id,
                        Name = draftManager.Name,
                        Surname = draftManager.Surname,
                        NationalId = draftManager.NationalId,
                        Address = draftManager.Address,
                        Attachment = PromoteTemporaryTransferDraftManagerFile(draftManager.Attachment, currentUser.Id, "NatId", managerId),
                        Fingerprints = PromoteTemporaryTransferDraftManagerFile(draftManager.Fingerprints, currentUser.Id, "Fingerprints", managerId),
                        Form55 = PromoteTemporaryTransferDraftManagerFile(draftManager.Form55, currentUser.Id, "Form55", managerId),
                        ApplicationId = transferApplication.Id,
                        Status = "submitted",
                        DateAdded = DateTime.Now,
                        DateUpdated = DateTime.Now
                    });
                }

                _db.SaveChanges();
            }

            var ownerNotified = await TryNotifyLicenseOwnerAsync(sourceApplication, transferApplication, currentUser);
            TempData["success"] = ownerNotified
                ? $"{normalizedTransferType} application submitted and the licence owner has been notified. Add the required attachments, then complete payment."
                : $"{normalizedTransferType} application submitted. Add the required attachments, then complete payment.";

            return RedirectToAction("Apply", new { id = transferApplication.Id });
        }

        [HttpPost("AddDraftManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDraftManagerAsync(
            string llbNumber,
            string name,
            string surname,
            string nationalId,
            string address,
            IFormFile file,
            IFormFile fileb,
            IFormFile form55)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "You must be signed in to add a manager." });
            }

            var sourceApplication = await FindApprovedSourceApplicationByLlbNumberAsync(llbNumber);
            if (sourceApplication == null)
            {
                return BadRequest(new { success = false, message = "The source liquor licence could not be found." });
            }

            var manager = NormalizeTemporaryTransferManager(name, surname, nationalId, address);
            var validationError = ValidateTemporaryTransferManager(manager, requireAttachments: false);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return BadRequest(new { success = false, message = validationError });
            }

            var fileValidationError = ValidateTemporaryTransferManagerFileInputs(file, fileb, form55);
            if (!string.IsNullOrWhiteSpace(fileValidationError))
            {
                return BadRequest(new { success = false, message = fileValidationError });
            }

            var draftKey = $"TempTransferDraft_{GetTemporaryTransferDraftOwnerToken(currentUser.Id)}_{Guid.NewGuid():N}";
            var attachmentPath = await SaveTemporaryTransferManagerFileAsync(file, "NatId", draftKey);
            var fingerprintsPath = await SaveTemporaryTransferManagerFileAsync(fileb, "Fingerprints", draftKey);
            var form55Path = await SaveTemporaryTransferManagerFileAsync(form55, "Form55", draftKey);

            return Json(new
            {
                success = true,
                manager = new
                {
                    name = manager.Name,
                    surname = manager.Surname,
                    nationalId = manager.NationalId,
                    address = manager.Address,
                    attachment = attachmentPath,
                    fingerprints = fingerprintsPath,
                    form55 = form55Path
                }
            });
        }

        [HttpPost("AddManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddManagerAsync(
            string id,
            string name,
            string surname,
            string nationalId,
            string address,
            IFormFile file,
            IFormFile fileb,
            IFormFile form55)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var transferApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUser.Id
                && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

            if (transferApplication == null)
            {
                TempData["error"] = "The transfer application could not be found.";
                return RedirectToAction("List");
            }

            var payment = await GetLatestTemporaryTransferPaymentAsync(transferApplication.Id);
            if (!CanModifyBeforePayment(transferApplication, payment))
            {
                TempData["error"] = "Managers can only be changed before payment starts.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var normalizedManager = NormalizeTemporaryTransferManager(name, surname, nationalId, address);
            var validationError = ValidateTemporaryTransferManager(normalizedManager, requireAttachments: false);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                TempData["error"] = validationError;
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var fileValidationError = ValidateTemporaryTransferManagerFileInputs(file, fileb, form55);
            if (!string.IsNullOrWhiteSpace(fileValidationError))
            {
                TempData["error"] = fileValidationError;
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var existingManagers = await GetTemporaryTransferManagersAsync(transferApplication.Id);
            if (existingManagers.Any(item => string.Equals(item.NationalId, normalizedManager.NationalId, StringComparison.OrdinalIgnoreCase)))
            {
                TempData["error"] = "A manager with the same national ID has already been added to this transfer application.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var sourceApplication = await GetSourceApplicationAsync(transferApplication.CompanyNumber);
            if (sourceApplication == null)
            {
                TempData["error"] = "The source liquor licence could not be found.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var pricing = await BuildTemporaryTransferPricingAsync(sourceApplication, existingManagers.Count + 1);
            if (pricing.ChargeableManagerCount > 0 && !pricing.ManagerFee.HasValue)
            {
                TempData["error"] = "The additional manager fee has not been configured.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var managerId = Guid.NewGuid().ToString();
            _db.Add(new ManagersParticulars
            {
                Id = managerId,
                UserId = currentUser.Id,
                Name = normalizedManager.Name,
                Surname = normalizedManager.Surname,
                NationalId = normalizedManager.NationalId,
                Address = normalizedManager.Address,
                Attachment = await SaveTemporaryTransferManagerFileAsync(file, "NatId", managerId),
                Fingerprints = await SaveTemporaryTransferManagerFileAsync(fileb, "Fingerprints", managerId),
                Form55 = await SaveTemporaryTransferManagerFileAsync(form55, "Form55", managerId),
                ApplicationId = transferApplication.Id,
                Status = "submitted",
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            });

            transferApplication.PaymentFee = pricing.TotalFee ?? transferApplication.PaymentFee;
            transferApplication.DateUpdated = DateTime.Now;
            _db.Update(transferApplication);
            _db.SaveChanges();

            TempData["success"] = "Manager added to the transfer application.";
            return RedirectToAction("Apply", new { id = transferApplication.Id });
        }

        [HttpPost("DeleteManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteManagerAsync(string id, string managerId)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var transferApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUser.Id
                && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

            if (transferApplication == null)
            {
                TempData["error"] = "The transfer application could not be found.";
                return RedirectToAction("List");
            }

            var payment = await GetLatestTemporaryTransferPaymentAsync(transferApplication.Id);
            if (!CanModifyBeforePayment(transferApplication, payment))
            {
                TempData["error"] = "Managers can only be changed before payment starts.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var manager = await _db.ManagersParticulars.FirstOrDefaultAsync(item =>
                item.Id == managerId
                && item.ApplicationId == transferApplication.Id);

            if (manager == null)
            {
                TempData["error"] = "The manager could not be found.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            _db.ManagersParticulars.Remove(manager);

            var sourceApplication = await GetSourceApplicationAsync(transferApplication.CompanyNumber);
            if (sourceApplication != null)
            {
                var remainingManagersCount = Math.Max(await _db.ManagersParticulars.CountAsync(item => item.ApplicationId == transferApplication.Id) - 1, 0);
                var pricing = await BuildTemporaryTransferPricingAsync(sourceApplication, remainingManagersCount);
                transferApplication.PaymentFee = pricing.TotalFee ?? transferApplication.PaymentFee;
            }

            transferApplication.DateUpdated = DateTime.Now;
            _db.Update(transferApplication);
            _db.SaveChanges();

            TempData["success"] = "Manager removed from the transfer application.";
            return RedirectToAction("Apply", new { id = transferApplication.Id });
        }

        [HttpPost("UpdateDetails")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetailsAsync(
            string id,
            string transferType,
            string applicantType,
            string businessName,
            string operationAddress,
            string? placeOfEntry,
            string? dateofEntryIntoZimbabwe)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var transferApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUser.Id
                && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

            if (transferApplication == null)
            {
                TempData["error"] = "The transfer application could not be found.";
                return RedirectToAction("List");
            }

            var payment = await GetLatestTemporaryTransferPaymentAsync(transferApplication.Id);
            if (!CanModifyBeforePayment(transferApplication, payment))
            {
                TempData["error"] = "Transfer details can only be edited before payment starts.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var normalizedTransferType = TemporaryTransferHelper.NormalizeTransferType(transferType);
            if (string.IsNullOrWhiteSpace(normalizedTransferType))
            {
                TempData["error"] = "Select whether this is a permanent transfer or a temporary transfer.";
                return RedirectToAction("Apply", new { id = transferApplication.Id, edit = true });
            }

            var existingOpenApplication = await GetOpenTemporaryTransferApplicationAsync(
                currentUser.Id,
                transferApplication.CompanyNumber,
                normalizedTransferType);
            if (existingOpenApplication != null
                && !string.Equals(existingOpenApplication.Id, transferApplication.Id, StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = $"A {normalizedTransferType.ToLowerInvariant()} application for this liquor licence is already in progress.";
                return RedirectToAction("Apply", new { id = transferApplication.Id, edit = true });
            }

            if (string.IsNullOrWhiteSpace(applicantType)
                || string.IsNullOrWhiteSpace(businessName)
                || string.IsNullOrWhiteSpace(operationAddress))
            {
                TempData["error"] = "Complete all applicant information fields before saving the changes.";
                return RedirectToAction("Apply", new { id = transferApplication.Id, edit = true });
            }

            transferApplication.PlaceOfBirth = normalizedTransferType;
            transferApplication.ApplicantType = applicantType;
            transferApplication.BusinessName = businessName;
            transferApplication.OperationAddress = operationAddress;
            transferApplication.PlaceOfEntry = IsZimbabweanApplicant(currentUser) ? string.Empty : placeOfEntry ?? string.Empty;
            transferApplication.DateofEntryIntoZimbabwe = IsZimbabweanApplicant(currentUser) ? string.Empty : dateofEntryIntoZimbabwe ?? string.Empty;
            transferApplication.IdPass = currentUser.NatID ?? transferApplication.IdPass ?? string.Empty;
            transferApplication.DateUpdated = DateTime.Now;

            _db.Update(transferApplication);
            _db.SaveChanges();

            TempData["success"] = "Transfer details updated.";
            return RedirectToAction("Apply", new { id = transferApplication.Id });
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

            var transferApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUser.Id
                && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

            if (transferApplication == null)
            {
                TempData["error"] = "The transfer application could not be found.";
                return RedirectToAction("List");
            }

            var payment = await GetLatestTemporaryTransferPaymentAsync(transferApplication.Id);
            if (!CanModifyBeforePayment(transferApplication, payment))
            {
                TempData["error"] = "Attachments can only be updated before payment starts.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            if (file == null || file.Length <= 0)
            {
                TempData["error"] = "Choose a file to upload.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var now = DateTime.Now;
            var normalizedDocumentType = NormalizeDocumentType(documentType);
            var documentLabel = GetDocumentLabel(documentType);

            switch (normalizedDocumentType)
            {
                case "idcopy":
                    transferApplication.IdCopy = await SaveApplicantFileAsync(file, "IDCopy", transferApplication.Id!);
                    break;

                case "fingerprints":
                    transferApplication.Fingerprints = await SaveApplicantFileAsync(file, "Fingerprints", transferApplication.Id!);
                    break;

                case "form55":
                case "formff":
                    transferApplication.FormFF = await SaveApplicantFileAsync(file, "FormFF", transferApplication.Id!);
                    break;

                case "proofofpublication":
                case "publicationproof":
                case "lg2":
                    await SaveTemporaryTransferAttachmentAsync(
                        transferApplication.Id,
                        currentUser.Id,
                        TemporaryTransferHelper.ProofOfPublicationDocumentTitle,
                        file,
                        now);
                    break;

                case "tieaffidavit":
                    await SaveTemporaryTransferAttachmentAsync(
                        transferApplication.Id,
                        currentUser.Id,
                        TemporaryTransferHelper.TieAffidavitDocumentTitle,
                        file,
                        now);
                    break;

                case "transferaffidavit":
                    await SaveTemporaryTransferAttachmentAsync(
                        transferApplication.Id,
                        currentUser.Id,
                        TemporaryTransferHelper.TransferAffidavitDocumentTitle,
                        file,
                        now);
                    break;

                case "leasedocuments":
                case "rightofoccupation":
                case "titledeeds":
                    await SaveTemporaryTransferAttachmentAsync(
                        transferApplication.Id,
                        currentUser.Id,
                        TemporaryTransferHelper.LeaseDocumentsDocumentTitle,
                        file,
                        now);
                    break;

                default:
                    TempData["error"] = "The selected attachment type is not supported.";
                    return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            transferApplication.DateUpdated = now;
            _db.Update(transferApplication);
            _db.SaveChanges();

            TempData["success"] = $"{documentLabel} uploaded successfully.";
            return RedirectToAction("Apply", new { id = transferApplication.Id });
        }

        [HttpGet("Payment")]
        public async Task<IActionResult> PaymentAsync(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var transferApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUserId
                && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

            if (transferApplication == null)
            {
                TempData["error"] = "The transfer application could not be found.";
                return RedirectToAction("List");
            }

            if (!string.Equals(transferApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only submitted transfer applications can proceed to payment.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var sourceApplication = await GetSourceApplicationAsync(transferApplication.CompanyNumber);
            if (sourceApplication == null)
            {
                TempData["error"] = "The source liquor licence could not be found.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var pricing = await BuildTemporaryTransferPricingAsync(sourceApplication, transferApplication.Id);
            if (!pricing.TotalFee.HasValue)
            {
                TempData["error"] = "The transfer fee has not been configured.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            if (pricing.ChargeableManagerCount > 0 && !pricing.ManagerFee.HasValue)
            {
                TempData["error"] = "The additional manager fee has not been configured.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var existingTransaction = await GetLatestTemporaryTransferPaymentAsync(transferApplication.Id);
            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                transferApplication.PaymentStatus = existingTransaction.PaymentStatus ?? existingTransaction.Status ?? "Paid";
                transferApplication.DateUpdated = DateTime.Now;
                _db.Update(transferApplication);
                _db.SaveChanges();

                TempData["success"] = "This transfer application has already been paid for.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = "Complete the current payment before starting another one.";
                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var proofOfPublication = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.ProofOfPublicationDocumentTitle,
                asNoTracking: true);
            var tieAffidavit = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.TieAffidavitDocumentTitle,
                asNoTracking: true);
            var transferAffidavit = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.TransferAffidavitDocumentTitle,
                asNoTracking: true);
            var leaseDocuments = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.LeaseDocumentsDocumentTitle,
                asNoTracking: true);

            if (!HasRequiredAttachments(transferApplication, proofOfPublication, tieAffidavit, transferAffidavit, leaseDocuments))
            {
                TempData["error"] = "Upload proof of publication, tie affidavit, transfer affidavit, certified ID/passport copies, Form 55 against fingerprints, and lease/title deed/right of occupation documents before making payment.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
            var callbackUrl = Url.Action("Apply", "TemporaryTransfer", new { id = transferApplication.Id }, Request.Scheme);
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            var paymentReference = !string.IsNullOrWhiteSpace(transferApplication.RefNum)
                ? transferApplication.RefNum
                : transferApplication.Id;
            var payment = paynow.CreatePayment(paymentReference ?? Guid.NewGuid().ToString("N"));
            var licenseType = string.IsNullOrWhiteSpace(sourceApplication.LicenseTypeID)
                ? null
                : await _db.LicenseTypes.FirstOrDefaultAsync(item => item.Id == sourceApplication.LicenseTypeID);

            payment.Add(licenseType?.LicenseName ?? TemporaryTransferHelper.ServiceName, pricing.TotalFee.Value);

            var response = paynow.Send(payment);
            if (!response.Success())
            {
                TempData["error"] = "The payment request could not be created. Try again.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var transaction = new Payments
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUserId,
                Amount = payment.Total,
                ApplicationId = transferApplication.Id,
                Service = TemporaryTransferHelper.ServiceName,
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

            transferApplication.PaymentFee = pricing.TotalFee.Value;
            transferApplication.PaymentStatus = transaction.PaymentStatus ?? "Not Paid";
            transferApplication.DateUpdated = DateTime.Now;
            _db.Update(transferApplication);
            _db.SaveChanges();

            return Redirect(response.RedirectLink());
        }

        [HttpPost("Continue")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContinueAsync(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var transferApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id
                && item.UserID == currentUserId
                && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

            if (transferApplication == null)
            {
                TempData["error"] = "The transfer application could not be found.";
                return RedirectToAction("List");
            }

            if (!string.Equals(transferApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This transfer application is not ready to continue.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var payment = await GetLatestTemporaryTransferPaymentAsync(transferApplication.Id);
            if (payment == null || !HasPaymentStatus(payment, "Paid"))
            {
                TempData["error"] = "Payment must be completed before the application can be sent to the secretary.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var proofOfPublication = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.ProofOfPublicationDocumentTitle,
                asNoTracking: true);
            var tieAffidavit = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.TieAffidavitDocumentTitle,
                asNoTracking: true);
            var transferAffidavit = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.TransferAffidavitDocumentTitle,
                asNoTracking: true);
            var leaseDocuments = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.LeaseDocumentsDocumentTitle,
                asNoTracking: true);
            if (!HasRequiredAttachments(transferApplication, proofOfPublication, tieAffidavit, transferAffidavit, leaseDocuments))
            {
                TempData["error"] = "Upload all required attachments before sending the application to the secretary.";
                return RedirectToAction("Apply", new { id = transferApplication.Id });
            }

            var existingTask = await _db.Tasks.FirstOrDefaultAsync(item =>
                item.ApplicationId == transferApplication.Id
                && item.Status == "assigned"
                && item.Service == TemporaryTransferHelper.ServiceName);

            if (existingTask == null)
            {
                var secretaryId = await _taskAllocationHelper.GetSecretary(_db, _userManager);
                if (string.IsNullOrWhiteSpace(secretaryId))
                {
                    TempData["error"] = "No secretary is currently available to receive this transfer application.";
                    return RedirectToAction("Apply", new { id = transferApplication.Id });
                }

                _db.Add(new Tasks
                {
                    Id = Guid.NewGuid().ToString(),
                    ApplicationId = transferApplication.Id,
                    ApproverId = secretaryId,
                    AssignerId = "system",
                    Service = TemporaryTransferHelper.ServiceName,
                    ExaminationStatus = "approval",
                    Status = "assigned",
                    DateAdded = DateTime.Now,
                    DateUpdated = DateTime.Now
                });
            }

            transferApplication.Status = "awaiting approval";
            transferApplication.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? "Paid";
            transferApplication.DateUpdated = DateTime.Now;
            _db.Update(transferApplication);
            _db.SaveChanges();

            TempData["success"] = "Transfer application sent to the secretary for examination and approval.";
            return RedirectToAction("Apply", new { id = transferApplication.Id });
        }

        [Authorize(Roles = "secretary,admin,super user")]
        [HttpGet("ViewApplications")]
        public async Task<IActionResult> ViewApplicationsAsync(string id)
        {
            var transferApplication = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

            if (transferApplication == null)
            {
                TempData["error"] = "The transfer application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var sourceApplication = await GetSourceApplicationAsync(transferApplication.CompanyNumber);
            if (sourceApplication == null)
            {
                TempData["error"] = "The source liquor licence could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var reviewTask = await GetTemporaryTransferReviewTaskAsync(transferApplication.Id, includeCompleted: true);
            if (!CanBypassReviewAssignment() && reviewTask == null)
            {
                TempData["error"] = "This transfer application is not assigned to you.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var applicant = string.IsNullOrWhiteSpace(transferApplication.UserID)
                ? null
                : await _userManager.Users.FirstOrDefaultAsync(item => item.Id == transferApplication.UserID);

            var owner = string.IsNullOrWhiteSpace(sourceApplication.UserID)
                ? null
                : await _userManager.Users.FirstOrDefaultAsync(item => item.Id == sourceApplication.UserID);

            var outlet = await GetPreferredOutletAsync(sourceApplication.Id);

            var license = string.IsNullOrWhiteSpace(sourceApplication.LicenseTypeID)
                ? null
                : await _db.LicenseTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.LicenseTypeID);

            var region = string.IsNullOrWhiteSpace(sourceApplication.ApplicationType)
                ? null
                : await _db.LicenseRegions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.ApplicationType);

            var proofOfPublication = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.ProofOfPublicationDocumentTitle,
                asNoTracking: true);
            var tieAffidavit = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.TieAffidavitDocumentTitle,
                asNoTracking: true);
            var transferAffidavit = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.TransferAffidavitDocumentTitle,
                asNoTracking: true);
            var leaseDocuments = await GetLatestTemporaryTransferAttachmentAsync(
                transferApplication.Id,
                TemporaryTransferHelper.LeaseDocumentsDocumentTitle,
                asNoTracking: true);
            var managers = await GetTemporaryTransferManagersAsync(transferApplication.Id, asNoTracking: true);
            var pricing = await BuildTemporaryTransferPricingAsync(sourceApplication, managers.Count);

            ViewBag.TransferApplication = transferApplication;
            ViewBag.SourceApplication = sourceApplication;
            ViewBag.ApplicantUser = applicant;
            ViewBag.OwnerUser = owner;
            ViewBag.Outlet = outlet;
            ViewBag.License = license;
            ViewBag.Region = region;
            ViewBag.ProofOfPublication = proofOfPublication;
            ViewBag.TieAffidavit = tieAffidavit;
            ViewBag.TransferAffidavit = transferAffidavit;
            ViewBag.LeaseDocuments = leaseDocuments;
            ViewBag.TransferManagers = managers;
            ViewBag.BaseFee = pricing.BaseFee;
            ViewBag.ManagerFee = pricing.ManagerFee;
            ViewBag.ManagerCount = pricing.ManagerCount;
            ViewBag.ChargeableManagerCount = pricing.ChargeableManagerCount;
            ViewBag.AdditionalManagerFee = pricing.AdditionalManagerFee;
            ViewBag.TotalFee = pricing.TotalFee;
            ViewBag.Task = reviewTask;
            return View();
        }

        [Authorize(Roles = "secretary,admin,super user")]
        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAsync(string id)
        {
            var transferApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

            if (transferApplication == null)
            {
                TempData["error"] = "The transfer application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var reviewTask = await GetTemporaryTransferReviewTaskAsync(transferApplication.Id, includeCompleted: false);
            if (!CanBypassReviewAssignment() && reviewTask == null)
            {
                TempData["error"] = "This transfer application is not assigned to you.";
                return RedirectToAction("Dashboard", "Approval");
            }

            if (!string.Equals(transferApplication.Status, "awaiting approval", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only transfer applications awaiting approval can be approved.";
                return RedirectToAction("ViewApplications", new { id = transferApplication.Id });
            }

            var payment = await RefreshTemporaryTransferPaymentStatusAsync(transferApplication.Id);
            if (payment == null || !HasPaymentStatus(payment, "Paid"))
            {
                TempData["error"] = "Payment has not been confirmed for this transfer application.";
                return RedirectToAction("ViewApplications", new { id = transferApplication.Id });
            }

            transferApplication.Status = "Approved";
            transferApplication.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? transferApplication.PaymentStatus ?? "Paid";
            transferApplication.DateUpdated = DateTime.Now;
            transferApplication.ApprovedDate = DateTime.Now;
            transferApplication.Secretary = _userManager.GetUserId(User) ?? string.Empty;
            _db.Update(transferApplication);

            if (reviewTask != null)
            {
                reviewTask.Status = "completed";
                reviewTask.ApprovedDate = DateTime.Now;
                reviewTask.DateUpdated = DateTime.Now;
                _db.Update(reviewTask);
            }

            _db.SaveChanges();
            TempData["success"] = "Transfer application approved.";
            return RedirectToAction("ViewApplications", new { id = transferApplication.Id });
        }

        [Authorize(Roles = "secretary,admin,super user")]
        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAsync(string id)
        {
            var transferApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item =>
                item.Id == id && item.ExaminationStatus == TemporaryTransferHelper.ServiceName);

            if (transferApplication == null)
            {
                TempData["error"] = "The transfer application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var reviewTask = await GetTemporaryTransferReviewTaskAsync(transferApplication.Id, includeCompleted: false);
            if (!CanBypassReviewAssignment() && reviewTask == null)
            {
                TempData["error"] = "This transfer application is not assigned to you.";
                return RedirectToAction("Dashboard", "Approval");
            }

            if (!string.Equals(transferApplication.Status, "awaiting approval", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Only transfer applications awaiting approval can be rejected.";
                return RedirectToAction("ViewApplications", new { id = transferApplication.Id });
            }

            transferApplication.Status = "Rejected";
            transferApplication.DateUpdated = DateTime.Now;
            transferApplication.ApprovedDate = DateTime.Now;
            transferApplication.Secretary = _userManager.GetUserId(User) ?? string.Empty;
            _db.Update(transferApplication);

            if (reviewTask != null)
            {
                reviewTask.Status = "completed";
                reviewTask.ApprovedDate = DateTime.Now;
                reviewTask.DateUpdated = DateTime.Now;
                _db.Update(reviewTask);
            }

            _db.SaveChanges();
            TempData["success"] = "Transfer application rejected.";
            return RedirectToAction("ViewApplications", new { id = transferApplication.Id });
        }

        private async Task PopulateApplyViewAsync(
            ApplicationUser applicantUser,
            ApplicationInfo? sourceApplication,
            ApplicationInfo? transferApplication,
            string? llbNumber,
            string? selectedTransferType = null)
        {
            ApplicationUser? ownerUser = null;
            OutletInfo? outlet = null;
            LicenseTypes? license = null;
            LicenseRegion? region = null;
            AttachmentInfo? proofOfPublication = null;
            AttachmentInfo? tieAffidavit = null;
            AttachmentInfo? transferAffidavit = null;
            AttachmentInfo? leaseDocuments = null;
            Payments? payment = null;
            decimal? fee = null;
            decimal? baseFee = null;
            decimal? managerFee = null;
            decimal? additionalManagerFee = null;
            decimal? totalFee = null;
            var managers = new List<ManagersParticulars>();
            var managerCount = 0;
            var chargeableManagerCount = 0;

            if (sourceApplication != null)
            {
                ownerUser = string.IsNullOrWhiteSpace(sourceApplication.UserID)
                    ? null
                    : await _userManager.Users.FirstOrDefaultAsync(item => item.Id == sourceApplication.UserID);

                outlet = await GetPreferredOutletAsync(sourceApplication.Id);

                license = string.IsNullOrWhiteSpace(sourceApplication.LicenseTypeID)
                    ? null
                    : await _db.LicenseTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.LicenseTypeID);

                region = string.IsNullOrWhiteSpace(sourceApplication.ApplicationType)
                    ? null
                    : await _db.LicenseRegions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.ApplicationType);

                var pricing = await BuildTemporaryTransferPricingAsync(sourceApplication);
                fee = pricing.TotalFee;
                baseFee = pricing.BaseFee;
                managerFee = pricing.ManagerFee;
                additionalManagerFee = pricing.AdditionalManagerFee;
                totalFee = pricing.TotalFee;
                managerCount = pricing.ManagerCount;
                chargeableManagerCount = pricing.ChargeableManagerCount;
            }

            if (transferApplication != null)
            {
                proofOfPublication = await GetLatestTemporaryTransferAttachmentAsync(
                    transferApplication.Id,
                    TemporaryTransferHelper.ProofOfPublicationDocumentTitle,
                    asNoTracking: true);

                tieAffidavit = await GetLatestTemporaryTransferAttachmentAsync(
                    transferApplication.Id,
                    TemporaryTransferHelper.TieAffidavitDocumentTitle,
                    asNoTracking: true);

                transferAffidavit = await GetLatestTemporaryTransferAttachmentAsync(
                    transferApplication.Id,
                    TemporaryTransferHelper.TransferAffidavitDocumentTitle,
                    asNoTracking: true);

                leaseDocuments = await GetLatestTemporaryTransferAttachmentAsync(
                    transferApplication.Id,
                    TemporaryTransferHelper.LeaseDocumentsDocumentTitle,
                    asNoTracking: true);

                managers = await GetTemporaryTransferManagersAsync(transferApplication.Id, asNoTracking: true);

                payment = await RefreshTemporaryTransferPaymentStatusAsync(transferApplication.Id);

                if (sourceApplication != null)
                {
                    var pricing = await BuildTemporaryTransferPricingAsync(sourceApplication, managers.Count);
                    fee = pricing.TotalFee ?? transferApplication.PaymentFee ?? fee;
                    baseFee = pricing.BaseFee;
                    managerFee = pricing.ManagerFee;
                    additionalManagerFee = pricing.AdditionalManagerFee;
                    totalFee = pricing.TotalFee ?? transferApplication.PaymentFee;
                    managerCount = pricing.ManagerCount;
                    chargeableManagerCount = pricing.ChargeableManagerCount;
                }
            }

            var paymentReceived = payment != null && HasPaymentStatus(payment, "Paid");
            var hasActivePayment = payment != null && IsActivePaymentTransaction(payment);
            var attachmentsComplete = HasRequiredAttachments(transferApplication, proofOfPublication, tieAffidavit, transferAffidavit, leaseDocuments);
            var canEditDetails = CanModifyBeforePayment(transferApplication, payment);
            var canMakePayment = transferApplication != null
                && string.Equals(transferApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase)
                && attachmentsComplete
                && !paymentReceived;
            var canContinue = transferApplication != null
                && string.Equals(transferApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase)
                && paymentReceived;
            var awaitingSecretary = transferApplication != null
                && string.Equals(transferApplication.Status, "awaiting approval", StringComparison.OrdinalIgnoreCase);

            ViewData["Title"] = TemporaryTransferHelper.DisplayServiceName;
            ViewBag.SearchValue = llbNumber ?? sourceApplication?.LLBNum ?? string.Empty;
            ViewBag.SelectedTransferType = transferApplication != null
                ? TemporaryTransferHelper.GetTransferType(transferApplication)
                : TemporaryTransferHelper.NormalizeTransferType(selectedTransferType);
            ViewBag.SourceApplication = sourceApplication;
            ViewBag.TransferApplication = transferApplication;
            ViewBag.ApplicantUser = applicantUser;
            ViewBag.OwnerUser = ownerUser;
            ViewBag.Outlet = outlet;
            ViewBag.License = license;
            ViewBag.Region = region;
            ViewBag.ProofOfPublication = proofOfPublication;
            ViewBag.TieAffidavit = tieAffidavit;
            ViewBag.TransferAffidavit = transferAffidavit;
            ViewBag.LeaseDocuments = leaseDocuments;
            ViewBag.TransferManagers = managers;
            ViewBag.BaseFee = baseFee;
            ViewBag.ManagerFee = managerFee;
            ViewBag.ManagerCount = managerCount;
            ViewBag.ChargeableManagerCount = chargeableManagerCount;
            ViewBag.AdditionalManagerFee = additionalManagerFee;
            ViewBag.TotalFee = totalFee ?? fee;
            ViewBag.Payment = payment;
            ViewBag.Fee = fee;
            ViewBag.PaymentReceived = paymentReceived;
            ViewBag.HasActivePayment = hasActivePayment;
            ViewBag.AttachmentsComplete = attachmentsComplete;
            ViewBag.CanEditDetails = canEditDetails;
            ViewBag.CanUploadAttachments = canEditDetails;
            ViewBag.CanMakePayment = canMakePayment && !hasActivePayment;
            ViewBag.CanContinue = canContinue;
            ViewBag.AwaitingSecretary = awaitingSecretary;
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

        private async Task<ApplicationInfo?> FindApprovedSourceApplicationByLlbNumberAsync(string? llbNumber)
        {
            if (string.IsNullOrWhiteSpace(llbNumber))
            {
                return null;
            }

            var normalized = llbNumber.Trim();

            return await _db.ApplicationInfo
                .AsNoTracking()
                .Where(item =>
                    item.LLBNum != null
                    && item.LLBNum == normalized
                    && (item.ExaminationStatus == null || item.ExaminationStatus != TemporaryTransferHelper.ServiceName)
                    && (item.Status == "approved" || item.Status == "Approved"))
                .OrderByDescending(item => item.ApprovedDate)
                .ThenByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.ApplicationDate)
                .FirstOrDefaultAsync();
        }

        private async Task<ApplicationInfo?> GetSourceApplicationAsync(string? sourceApplicationId)
        {
            if (string.IsNullOrWhiteSpace(sourceApplicationId))
            {
                return null;
            }

            return await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == sourceApplicationId
                    && (item.ExaminationStatus == null || item.ExaminationStatus != TemporaryTransferHelper.ServiceName));
        }

        private async Task<ApplicationInfo?> GetOpenTemporaryTransferApplicationAsync(string applicantUserId, string? sourceApplicationId, string? transferType = null)
        {
            if (string.IsNullOrWhiteSpace(applicantUserId) || string.IsNullOrWhiteSpace(sourceApplicationId))
            {
                return null;
            }

            var normalizedTransferType = TemporaryTransferHelper.NormalizeTransferType(transferType);

            return await _db.ApplicationInfo
                .AsNoTracking()
                .Where(item =>
                    item.UserID == applicantUserId
                    && item.CompanyNumber == sourceApplicationId
                    && item.ExaminationStatus == TemporaryTransferHelper.ServiceName
                    && (string.IsNullOrWhiteSpace(normalizedTransferType)
                        || item.PlaceOfBirth == normalizedTransferType
                        || (normalizedTransferType == TemporaryTransferHelper.TemporaryTransferType
                            && (item.PlaceOfBirth == null || item.PlaceOfBirth == string.Empty)))
                    && (item.Status == null
                        || (item.Status != "Approved"
                            && item.Status != "Rejected")))
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.ApplicationDate)
                .FirstOrDefaultAsync();
        }

        private async Task<Payments?> GetLatestTemporaryTransferPaymentAsync(string? transferApplicationId)
        {
            if (string.IsNullOrWhiteSpace(transferApplicationId))
            {
                return null;
            }

            return await _db.Payments
                .Where(item =>
                    item.ApplicationId == transferApplicationId
                    && item.Service == TemporaryTransferHelper.ServiceName)
                .OrderByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();
        }

        private async Task<Payments?> RefreshTemporaryTransferPaymentStatusAsync(string? transferApplicationId)
        {
            var payment = await _db.Payments
                .Where(item =>
                    item.ApplicationId == transferApplicationId
                    && item.Service == TemporaryTransferHelper.ServiceName)
                .OrderByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();

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

                    var transferApplication = await _db.ApplicationInfo.FirstOrDefaultAsync(item => item.Id == transferApplicationId);
                    if (transferApplication != null)
                    {
                        transferApplication.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? transferApplication.PaymentStatus;
                        transferApplication.DateUpdated = DateTime.Now;
                        _db.Update(transferApplication);
                    }

                    _db.SaveChanges();
                }
                catch
                {
                    return payment;
                }
            }

            return payment;
        }

        private async Task<List<ManagersParticulars>> GetTemporaryTransferManagersAsync(string? applicationId, bool asNoTracking = false)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return new List<ManagersParticulars>();
            }

            IQueryable<ManagersParticulars> query = _db.ManagersParticulars;
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query
                .Where(item => item.ApplicationId == applicationId)
                .OrderBy(item => item.Name)
                .ThenBy(item => item.Surname)
                .ThenBy(item => item.NationalId)
                .ToListAsync();
        }

        private async Task<TemporaryTransferPricingSummary> BuildTemporaryTransferPricingAsync(
            ApplicationInfo? sourceApplication,
            string? transferApplicationId = null)
        {
            var managerCount = string.IsNullOrWhiteSpace(transferApplicationId)
                ? 0
                : await _db.ManagersParticulars.CountAsync(item => item.ApplicationId == transferApplicationId);

            return await BuildTemporaryTransferPricingAsync(sourceApplication, managerCount);
        }

        private async Task<TemporaryTransferPricingSummary> BuildTemporaryTransferPricingAsync(
            ApplicationInfo? sourceApplication,
            int managerCount)
        {
            var baseFee = await GetTemporaryTransferFeeAsync(sourceApplication);
            var managerFee = await GetTemporaryTransferManagerFeeAsync();
            var chargeableManagerCount = Math.Max(managerCount - 1, 0);
            var additionalManagerFee = managerFee.HasValue
                ? managerFee.Value * chargeableManagerCount
                : (decimal?)null;

            return new TemporaryTransferPricingSummary
            {
                BaseFee = baseFee,
                ManagerFee = managerFee,
                ManagerCount = managerCount,
                ChargeableManagerCount = chargeableManagerCount,
                AdditionalManagerFee = additionalManagerFee ?? 0m,
                TotalFee = baseFee.HasValue
                    ? baseFee.Value + (additionalManagerFee ?? 0m)
                    : null
            };
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

        private async Task<AttachmentInfo?> GetLatestTemporaryTransferAttachmentAsync(
            string? applicationId,
            string documentTitle,
            bool asNoTracking = false)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return null;
            }

            IQueryable<AttachmentInfo> query = _db.AttachmentInfo;
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query
                .Where(item => item.ApplicationId == applicationId && item.DocumentTitle == documentTitle)
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();
        }

        private async Task SaveTemporaryTransferAttachmentAsync(
            string? applicationId,
            string userId,
            string documentTitle,
            IFormFile file,
            DateTime now)
        {
            var attachment = await GetLatestTemporaryTransferAttachmentAsync(applicationId, documentTitle);

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

        private static bool HasRequiredAttachments(
            ApplicationInfo? transferApplication,
            AttachmentInfo? proofOfPublication,
            AttachmentInfo? tieAffidavit,
            AttachmentInfo? transferAffidavit,
            AttachmentInfo? leaseDocuments)
        {
            return transferApplication != null
                && !string.IsNullOrWhiteSpace(transferApplication.IdCopy)
                && !string.IsNullOrWhiteSpace(transferApplication.FormFF)
                && proofOfPublication != null
                && !string.IsNullOrWhiteSpace(proofOfPublication.DocumentLocation)
                && tieAffidavit != null
                && !string.IsNullOrWhiteSpace(tieAffidavit.DocumentLocation)
                && transferAffidavit != null
                && !string.IsNullOrWhiteSpace(transferAffidavit.DocumentLocation)
                && leaseDocuments != null
                && !string.IsNullOrWhiteSpace(leaseDocuments.DocumentLocation);
        }

        private static bool CanModifyBeforePayment(ApplicationInfo? transferApplication, Payments? payment)
        {
            if (transferApplication == null
                || !string.Equals(transferApplication.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (payment == null)
            {
                return true;
            }

            if (HasPaymentStatus(payment, "Paid"))
            {
                return false;
            }

            return !IsActivePaymentTransaction(payment);
        }

        private bool CanBypassReviewAssignment()
        {
            return User.IsInRole("admin") || User.IsInRole("super user");
        }

        private async Task<Tasks?> GetTemporaryTransferReviewTaskAsync(string? transferApplicationId, bool includeCompleted)
        {
            if (string.IsNullOrWhiteSpace(transferApplicationId))
            {
                return null;
            }

            var query = _db.Tasks.Where(item =>
                item.ApplicationId == transferApplicationId
                && item.Service == TemporaryTransferHelper.ServiceName);

            if (!includeCompleted)
            {
                query = query.Where(item => item.Status == "assigned");
            }

            if (!CanBypassReviewAssignment())
            {
                var currentUserId = _userManager.GetUserId(User);
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return null;
                }

                query = query.Where(item => item.ApproverId == currentUserId);
            }

            return await query
                .OrderByDescending(item => item.DateUpdated)
                .ThenByDescending(item => item.DateAdded)
                .FirstOrDefaultAsync();
        }

        private async Task<decimal?> GetTemporaryTransferFeeAsync(ApplicationInfo? sourceApplication)
        {
            if (sourceApplication == null)
            {
                return null;
            }

            var region = string.IsNullOrWhiteSpace(sourceApplication.ApplicationType)
                ? null
                : await _db.LicenseRegions.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.ApplicationType);

            var license = string.IsNullOrWhiteSpace(sourceApplication.LicenseTypeID)
                ? null
                : await _db.LicenseTypes.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceApplication.LicenseTypeID);

            var transferTypes = await _db.TransferTypes
                .AsNoTracking()
                .Where(item => item.Status == "active")
                .ToListAsync();

            TransferTypes? matchingTransferType = transferTypes
                .Where(item =>
                    string.IsNullOrWhiteSpace(item.LicenseCode)
                    || string.Equals(item.LicenseCode, sourceApplication.LicenseTypeID, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.LicenseCode, license?.LicenseCode, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.LicenseCode, license?.LicenseName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => string.Equals(item.TransferName, TemporaryTransferHelper.ServiceName, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(item => ContainsIgnoreCase(item.TransferName, "temporary transfer"))
                .ThenByDescending(item => ContainsIgnoreCase(item.TransferName, "transfer"))
                .FirstOrDefault();

            matchingTransferType ??= transferTypes
                .OrderByDescending(item => string.Equals(item.TransferName, TemporaryTransferHelper.ServiceName, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(item => ContainsIgnoreCase(item.TransferName, "temporary transfer"))
                .ThenByDescending(item => ContainsIgnoreCase(item.TransferName, "transfer"))
                .FirstOrDefault();

            if (matchingTransferType != null && region != null)
            {
                return region.RegionName switch
                {
                    "Town" => Convert.ToDecimal(matchingTransferType.TownFee),
                    "City" => Convert.ToDecimal(matchingTransferType.CityFee),
                    "Municipality" => Convert.ToDecimal(matchingTransferType.MunicipaltyFee),
                    "RDC" => Convert.ToDecimal(matchingTransferType.RDCFee),
                    _ => Convert.ToDecimal(matchingTransferType.CityFee)
                };
            }

            var postFormationFee = await _db.PostFormationFees
                .AsNoTracking()
                .Where(item =>
                    item.Code == TemporaryTransferHelper.ServiceCode
                    || item.ProcessName == TemporaryTransferHelper.ServiceName)
                .Select(item => (decimal?)item.Fee)
                .FirstOrDefaultAsync();

            return postFormationFee;
        }

        private async Task<decimal?> GetTemporaryTransferManagerFeeAsync()
        {
            return await _db.PostFormationFees
                .AsNoTracking()
                .Where(fee => fee.Code == "APM" || fee.ProcessName.Contains("Manager"))
                .OrderByDescending(fee => fee.Code == "APM")
                .Select(fee => (decimal?)fee.Fee)
                .FirstOrDefaultAsync();
        }

        private string? ValidateTemporaryTransferDraftManagerFiles(
            IEnumerable<TemporaryTransferManagerDraftItem> managers,
            string currentUserId)
        {
            foreach (var manager in managers)
            {
                if (!IsValidTemporaryTransferDraftManagerFile(manager.Attachment, currentUserId, "NatId")
                    || !IsValidTemporaryTransferDraftManagerFile(manager.Fingerprints, currentUserId, "Fingerprints")
                    || !IsValidTemporaryTransferDraftManagerFile(manager.Form55, currentUserId, "Form55"))
                {
                    return "One or more manager attachments could not be verified. Add the managers again and retry.";
                }
            }

            return null;
        }

        private static string? ValidateTemporaryTransferManagerFileInputs(
            IFormFile? attachment,
            IFormFile? fingerprints,
            IFormFile? form55)
        {
            if (attachment == null || attachment.Length == 0)
            {
                return "Upload the manager national ID copy before saving.";
            }

            if (fingerprints == null || fingerprints.Length == 0)
            {
                return "Upload the manager fingerprints copy before saving.";
            }

            if (form55 == null || form55.Length == 0)
            {
                return "Upload the manager Form 55 before saving.";
            }

            return null;
        }

        private static bool ContainsIgnoreCase(string? value, string match)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static List<TemporaryTransferManagerDraftItem>? ParseTemporaryTransferDraftManagers(string? managerDraftJson)
        {
            if (string.IsNullOrWhiteSpace(managerDraftJson))
            {
                return new List<TemporaryTransferManagerDraftItem>();
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<List<TemporaryTransferManagerDraftItem>>(
                    managerDraftJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return parsed?
                    .Select(item => NormalizeTemporaryTransferManager(
                        item.Name,
                        item.Surname,
                        item.NationalId,
                        item.Address,
                        item.Attachment,
                        item.Fingerprints,
                        item.Form55))
                    .ToList() ?? new List<TemporaryTransferManagerDraftItem>();
            }
            catch
            {
                return null;
            }
        }

        private static string? ValidateTemporaryTransferManagers(IEnumerable<TemporaryTransferManagerDraftItem> managers)
        {
            var seenNationalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var manager in managers)
            {
                var error = ValidateTemporaryTransferManager(manager);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    return error;
                }

                if (!seenNationalIds.Add(manager.NationalId))
                {
                    return "Each temporary transfer manager must have a unique national ID.";
                }
            }

            return null;
        }

        private static string? ValidateTemporaryTransferManager(
            TemporaryTransferManagerDraftItem manager,
            bool requireAttachments = true)
        {
            if (string.IsNullOrWhiteSpace(manager.Name)
                || string.IsNullOrWhiteSpace(manager.Surname)
                || string.IsNullOrWhiteSpace(manager.NationalId)
                || string.IsNullOrWhiteSpace(manager.Address))
            {
                return "Complete the manager name, surname, national ID, and address before saving.";
            }

            if (requireAttachments
                && (string.IsNullOrWhiteSpace(manager.Attachment)
                    || string.IsNullOrWhiteSpace(manager.Fingerprints)
                    || string.IsNullOrWhiteSpace(manager.Form55)))
            {
                return "Upload the manager national ID copy, fingerprints, and Form 55 before saving.";
            }

            return null;
        }

        private static TemporaryTransferManagerDraftItem NormalizeTemporaryTransferManager(
            string? name,
            string? surname,
            string? nationalId,
            string? address,
            string? attachment = null,
            string? fingerprints = null,
            string? form55 = null)
        {
            return new TemporaryTransferManagerDraftItem
            {
                Name = name?.Trim() ?? string.Empty,
                Surname = surname?.Trim() ?? string.Empty,
                NationalId = nationalId?.Trim() ?? string.Empty,
                Address = address?.Trim() ?? string.Empty,
                Attachment = NormalizeRelativeWebPath(attachment),
                Fingerprints = NormalizeRelativeWebPath(fingerprints),
                Form55 = NormalizeRelativeWebPath(form55)
            };
        }

        private static string NormalizeRelativeWebPath(string? relativePath)
        {
            return (relativePath ?? string.Empty)
                .Replace('\\', '/')
                .Trim()
                .TrimStart('/');
        }

        private static string GetTemporaryTransferDraftOwnerToken(string? userId)
        {
            var token = new string((userId ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
            return string.IsNullOrWhiteSpace(token) ? "user" : token;
        }

        private bool IsValidTemporaryTransferDraftManagerFile(
            string? relativePath,
            string currentUserId,
            string expectedPrefix)
        {
            var normalizedPath = NormalizeRelativeWebPath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return false;
            }

            var expectedPathPrefix = $"ManagerFingerprints/{expectedPrefix}_";
            if (!normalizedPath.StartsWith(expectedPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var draftMarker = $"TempTransferDraft_{GetTemporaryTransferDraftOwnerToken(currentUserId)}_";
            if (!normalizedPath.Contains(draftMarker, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var absolutePath = Path.Combine("wwwroot", normalizedPath.Replace('/', Path.DirectorySeparatorChar));
            return System.IO.File.Exists(absolutePath);
        }

        private string PromoteTemporaryTransferDraftManagerFile(
            string? draftRelativePath,
            string currentUserId,
            string prefix,
            string managerId)
        {
            if (!IsValidTemporaryTransferDraftManagerFile(draftRelativePath, currentUserId, prefix))
            {
                throw new InvalidOperationException("The manager attachment could not be verified.");
            }

            var normalizedPath = NormalizeRelativeWebPath(draftRelativePath);
            var sourcePath = Path.Combine("wwwroot", normalizedPath.Replace('/', Path.DirectorySeparatorChar));
            var extension = Path.GetExtension(sourcePath);
            var fileName = $"{prefix}_{managerId}{extension}";
            var directory = Path.Combine("wwwroot", "ManagerFingerprints");
            Directory.CreateDirectory(directory);

            var destinationPath = Path.Combine(directory, fileName);
            if (System.IO.File.Exists(destinationPath))
            {
                System.IO.File.Delete(destinationPath);
            }

            System.IO.File.Move(sourcePath, destinationPath);
            return Path.Combine("ManagerFingerprints", fileName).Replace("\\", "/");
        }

        private static bool IsZimbabweanApplicant(ApplicationUser? applicantUser)
        {
            return string.Equals(applicantUser?.Nationality, "Zimbabwe", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDocumentType(string? documentType)
        {
            return (documentType ?? string.Empty)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Trim()
                .ToLowerInvariant();
        }

        private static string GetDocumentLabel(string? documentType)
        {
            return NormalizeDocumentType(documentType) switch
            {
                "idcopy" => "Certified copies of ID or passport of applicant",
                "fingerprints" => "Fingerprints",
                "form55" or "formff" => "Form 55 against fingerprints",
                "proofofpublication" or "publicationproof" or "lg2" => "Proof of publication in the government gazette and local paper.{L.G 2}",
                "tieaffidavit" => "Tie affidavit",
                "transferaffidavit" => "Transfer affidavit",
                "leasedocuments" or "rightofoccupation" or "titledeeds" => "Lease documents, title deeds or evidence of right of occupation",
                _ => "Attachment"
            };
        }

        private sealed class TemporaryTransferManagerDraftItem
        {
            public string Name { get; set; } = string.Empty;
            public string Surname { get; set; } = string.Empty;
            public string NationalId { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Attachment { get; set; } = string.Empty;
            public string Fingerprints { get; set; } = string.Empty;
            public string Form55 { get; set; } = string.Empty;
        }

        private sealed class TemporaryTransferPricingSummary
        {
            public decimal? BaseFee { get; set; }
            public decimal? ManagerFee { get; set; }
            public int ManagerCount { get; set; }
            public int ChargeableManagerCount { get; set; }
            public decimal? AdditionalManagerFee { get; set; }
            public decimal? TotalFee { get; set; }
        }

        private async Task<string> SaveApplicantFileAsync(IFormFile file, string prefix, string applicationId)
        {
            Directory.CreateDirectory(Path.Combine("wwwroot", "ApplicantInfo"));
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{prefix}_{applicationId}{extension}";
            var relativePath = Path.Combine("ApplicantInfo", fileName).Replace("\\", "/");
            var absolutePath = Path.Combine("wwwroot", "ApplicantInfo", fileName);

            await using var fileStream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            return relativePath;
        }

        private async Task<string> SaveTemporaryTransferManagerFileAsync(IFormFile file, string prefix, string fileKey)
        {
            Directory.CreateDirectory(Path.Combine("wwwroot", "ManagerFingerprints"));
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{prefix}_{fileKey}{extension}";
            var relativePath = Path.Combine("ManagerFingerprints", fileName).Replace("\\", "/");
            var absolutePath = Path.Combine("wwwroot", "ManagerFingerprints", fileName);

            await using var fileStream = new FileStream(absolutePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            return relativePath;
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

        private async Task<bool> TryNotifyLicenseOwnerAsync(
            ApplicationInfo sourceApplication,
            ApplicationInfo transferApplication,
            ApplicationUser applicantUser)
        {
            if (string.IsNullOrWhiteSpace(sourceApplication.UserID)
                || string.Equals(sourceApplication.UserID, applicantUser.Id, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var owner = await _userManager.Users.FirstOrDefaultAsync(item => item.Id == sourceApplication.UserID);
            if (owner == null || string.IsNullOrWhiteSpace(owner.Email))
            {
                return false;
            }

            var applicantName = BuildDisplayName(applicantUser);
            var ownerName = BuildDisplayName(owner);
            var transferType = TemporaryTransferHelper.GetTransferType(transferApplication);

            try
            {
                var client = new SmtpClient("smtp.gmail.com", 465)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential("ftagwirei24@gmail.com", "kwjxjbsrahhtqfwd")
                };

                var message = new MailMessage
                {
                    From = new MailAddress("ftagwirei24@gmail.com"),
                    Subject = $"{transferType} Application Notification",
                    IsBodyHtml = true,
                    Body =
                        "<html><body style=\"font-family:Arial,Helvetica,sans-serif;\">" +
                        $"<p>Dear {WebUtility.HtmlEncode(ownerName)},</p>" +
                        $"<p>A {WebUtility.HtmlEncode(transferType.ToLowerInvariant())} application has been submitted against liquor licence <strong>{WebUtility.HtmlEncode(sourceApplication.LLBNum ?? "N/A")}</strong>.</p>" +
                        $"<p>Applicant: <strong>{WebUtility.HtmlEncode(applicantName)}</strong></p>" +
                        $"<p>Reference: <strong>{WebUtility.HtmlEncode(transferApplication.RefNum ?? "Pending")}</strong></p>" +
                        "<p>This notification is for your information while the application proceeds through the approval workflow.</p>" +
                        "<p>Liquor Licensing Board</p>" +
                        "</body></html>"
                };

                message.To.Add(owner.Email);
                await client.SendMailAsync(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string BuildDisplayName(ApplicationUser? user)
        {
            if (user == null)
            {
                return "N/A";
            }

            var parts = new[] { user.Name, user.LastName }
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();

            if (parts.Count > 0)
            {
                return string.Join(" ", parts);
            }

            return user.UserName ?? user.Email ?? "N/A";
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
