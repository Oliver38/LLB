using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using LLB.Data;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Identity;
using LLB.Models.ViewModel;
using LLB.Helpers;

namespace LLB.Controllers
{
    [Route("")]
    [Route("Home")]
    public class HomeController : Controller
    {
        

        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public HomeController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("")]
        [HttpGet("LandingPage")]
        [AllowAnonymous]
        public IActionResult LandingPage()
        {
            return View();
        }

      
        [HttpGet("FAQ")]
        [AllowAnonymous]
        public IActionResult FAQ()
        {
            return View();
        }



        [HttpPost("PostFormation")]
       
        public IActionResult PostFormation(string id, string process)
        {
                     if(process==  "RNW")
            {
                var renewalApplication = _db.ApplicationInfo.Where(a => a.Id == id).FirstOrDefault();
                if (renewalApplication == null)
                {
                    TempData["error"] = "Application information could not be found.";
                    return RedirectToAction("Dashboard");
                }

                var renewalEligibility = RenewalEligibilityHelper.Evaluate(renewalApplication.ExpiryDate, DateTime.Now);
                if (!renewalEligibility.IsEligible)
                {
                    TempData["error"] = renewalEligibility.WarningMessage;
                    return RedirectToAction("Dashboard");
                }

                return RedirectToAction("Renewal", "Postprocess", new { id = id, process = process });

            }else if (process == "APM")
            {
                return RedirectToAction("Managerchange", "Managers", new { id = id, process = process });

            }
            else if (process == "GDP")
            {
                return RedirectToAction("Governmentpermit", "Postprocess", new { param1 = id, param2 = id });

            }
            else if (process == "INP")
            {
                return RedirectToAction("Inspection", "Postprocess", new { id = id, process = id });

            }
            else if (process == "DPL")
            {
                return RedirectToAction("Duplication", "Postprocess", new { param1 = id, param2 = id });

            }
            else if (process == "TRM")
            {
                return RedirectToAction("Apply", "TemporaryRemoval", new { applicationId = id });

            }
            else if (process == "TTR")
            {
                var transferSource = _db.ApplicationInfo.FirstOrDefault(a => a.Id == id);
                return RedirectToAction(
                    "Apply",
                    "TemporaryTransfer",
                    new { llbNumber = transferSource?.LLBNum });

            }
            else if (process == "EXH")
            {
                return RedirectToAction("Extendedhours", "Postprocess", new { id = id, process = process });

            }
            else if (process == "TRL")
            {
                return RedirectToAction("TemporaryRetails", "Postprocess", new { id = id, process= process });

            }
            else if (process == "ECF")
            {
                return RedirectToAction("Extracounter", "Extracounter", new { id = id, process = process });

            }
            else { }
            //< option value = "APM" > Approval of a person as a Manager 100.00 </ option >
            //@*< option value = "GDP" > Government Department Permit 100.00 </ option > *@
            //< option value = "INP" > Inspection 150.00 </ option >
            //< option value = "DPL" > Duplication 60.00 </ option >
            //< option value = "TRM" > Temporal removal 200.00 </ option >
            //< option value = "TTR" > Temporal Transfer 200.00 </ option >
            //< option value = "EXH" > Extended hours(Occasional) liquor licence 300.00 </ option >
            //< option value = "TRL" > Temporal Retail liquor license </ option >
            var licenseInfo = _db.ApplicationInfo.Where(a => a.Id == id).FirstOrDefault();
            var outletInfo = _db.OutletInfo.Where(b => b.ApplicationId == id).FirstOrDefault();
            var userId = userManager.GetUserId(User);
            var renewals = _db.Renewals.Where(n => n.UserId == userId).ToList();



            List<RenewalViewModel> renewaltasks = new List<RenewalViewModel>();
          //  var rentasks = _db.Tasks.Where(f => f.VerifierId == id && f.Service == "renewal" && f.Status == "assigned").ToList();
            foreach (var rentask in renewals)
            {
                RenewalViewModel getreninfo = new RenewalViewModel();

                var renapps = _db.Renewals.Where(a => a.Id == rentask.ApplicationId).FirstOrDefault();
                var renappinfo = _db.ApplicationInfo.Where(s => s.Id == renapps.ApplicationId).FirstOrDefault();
                var reaoutletinfo = _db.OutletInfo.Where(q => q.ApplicationId == renapps.ApplicationId).FirstOrDefault();
                var licensetype = _db.LicenseTypes.Where(w => w.Id == renappinfo.LicenseTypeID).FirstOrDefault();
                var licenseReg = _db.LicenseRegions.Where(e => e.Id == renappinfo.ApplicationType).FirstOrDefault();
                getreninfo.ApplicationId = renapps.ApplicationId;
                getreninfo.Id = renapps.Id;
                getreninfo.Reference = renapps.Reference;
                getreninfo.LLBNumber = renapps.LLBNumber;
                getreninfo.PreviousExpiry = renapps.PreviousExpiry;
                getreninfo.TradingName = reaoutletinfo.TradingName;
                getreninfo.Licensetype = licensetype.LicenseName;
                getreninfo.LicenseRegion = licenseReg.RegionName;
                getreninfo.Status = renapps.Status;

                renewaltasks.Add(getreninfo);
            }

           
            ViewBag.Renewaltasks = renewaltasks;
            ViewBag.Renewals= renewals;
            ViewBag.LicenseInfo = licenseInfo;
            ViewBag.OutletInfo = outletInfo;
            ViewBag.Id = id;
            return View();
        }





        [HttpGet(("SignUp"))]
        [AllowAnonymous]
        public IActionResult SignUp()
        {
            return View();
        }

       
        [HttpGet("SignIn")]
        [AllowAnonymous]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpGet("AccessDenied")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("Dashboard")]
      

        public async Task<IActionResult> DashboardAsync(string? tab)
        {
            var redirect = RedirectNonClientUser();
            if (redirect != null)
            {
                return redirect;
            }

            var currentUserId = await GetCurrentClientUserIdAsync();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            await LoadClientDashboardViewDataAsync(currentUserId, tab);
            return View();
        }

        [HttpGet("RenewalListings")]
        public async Task<IActionResult> RenewalListings()
        {
            return await RenderClientListingViewAsync("RenewalListings");
        }

        [HttpGet("InspectionListings")]
        public async Task<IActionResult> InspectionListings()
        {
            return await RenderClientListingViewAsync("InspectionListings");
        }

        [HttpGet("ManagerChangeListings")]
        public async Task<IActionResult> ManagerChangeListings()
        {
            return await RenderClientListingViewAsync("ManagerChangeListings");
        }

        [HttpGet("ExtendedHoursListings")]
        public async Task<IActionResult> ExtendedHoursListings()
        {
            return await RenderClientListingViewAsync("ExtendedHoursListings");
        }

        [HttpGet("TemporaryRetailListings")]
        public async Task<IActionResult> TemporaryRetailListings()
        {
            return await RenderClientListingViewAsync("TemporaryRetailListings");
        }

        [HttpGet("ExtraCounterListings")]
        public async Task<IActionResult> ExtraCounterListings()
        {
            return await RenderClientListingViewAsync("ExtraCounterListings");
        }

        private void GetUserId()
        {
            throw new NotImplementedException();
        }

        private IActionResult? RedirectNonClientUser()
        {
            if (User.IsInRole("admin"))
            {
                return RedirectToAction("AdminDashboard", "Tasks");
            }

            if (User.IsInRole("verifier"))
            {
                return RedirectToAction("Dashboard", "Verify");
            }

            if (User.IsInRole("recommender"))
            {
                return RedirectToAction("Dashboard", "Recommend");
            }

            if (User.IsInRole("secretary"))
            {
                return RedirectToAction("Dashboard", "Approval");
            }

            if (User.IsInRole("inspector"))
            {
                return RedirectToAction("Dashboard", "Examination");
            }

            if (User.IsInRole("accountant"))
            {
                return RedirectToAction("Dashboard", "Accountant");
            }

            return null;
        }

        private async Task<string?> GetCurrentClientUserIdAsync()
        {
            var currentUser = await userManager.FindByEmailAsync(User.Identity?.Name ?? string.Empty);
            return currentUser?.Id;
        }

        private async Task<IActionResult> RenderClientListingViewAsync(string viewName)
        {
            var redirect = RedirectNonClientUser();
            if (redirect != null)
            {
                return redirect;
            }

            var currentUserId = await GetCurrentClientUserIdAsync();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            await LoadClientDashboardViewDataAsync(currentUserId, "services-pane");
            return View(viewName);
        }

        private async Task LoadClientDashboardViewDataAsync(string userId, string? tab)
        {
            var renewals = _db.Renewals.Where(q => q.UserId == userId).ToList();
            var allApplications = _db.ApplicationInfo.Where(a => a.UserID == userId).ToList();
            var clientLicenseApplications = allApplications
                .Where(a => !TemporaryTransferHelper.IsTemporaryTransferApplication(a))
                .ToList();
            var applications = clientLicenseApplications.Where(a => a.Status != "approved").ToList();
            var approvedapplications = clientLicenseApplications.Where(a => a.Status == "approved").ToList();
            var applicationIds = clientLicenseApplications
                .Where(a => !string.IsNullOrWhiteSpace(a.Id))
                .Select(a => a.Id!)
                .ToHashSet();
            var outletinfo = _db.OutletInfo
                .Where(a => a.ApplicationId != null && applicationIds.Contains(a.ApplicationId))
                .ToList();
            var license = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var user = await userManager.FindByIdAsync(userId);
            var applicationLookup = clientLicenseApplications
                .Where(a => !string.IsNullOrWhiteSpace(a.Id))
                .GroupBy(a => a.Id!)
                .ToDictionary(group => group.Key, group => group.First());
            var outletLookup = outletinfo
                .Where(a => !string.IsNullOrWhiteSpace(a.ApplicationId))
                .GroupBy(a => a.ApplicationId!)
                .ToDictionary(group => group.Key, group => group.First());
            var licenseLookup = license
                .Where(a => !string.IsNullOrWhiteSpace(a.Id))
                .GroupBy(a => a.Id!)
                .ToDictionary(group => group.Key, group => group.First());
            var regionLookup = regions
                .Where(a => !string.IsNullOrWhiteSpace(a.Id))
                .GroupBy(a => a.Id!)
                .ToDictionary(group => group.Key, group => group.First());

            List<RenewalViewModel> renewaltasks = new List<RenewalViewModel>();

            foreach (var rentask in renewals)
            {
                RenewalViewModel getreninfo = new RenewalViewModel();

                if (string.IsNullOrWhiteSpace(rentask.ApplicationId)
                    || !applicationLookup.TryGetValue(rentask.ApplicationId, out var renappinfo))
                {
                    continue;
                }

                outletLookup.TryGetValue(rentask.ApplicationId, out var reaoutletinfo);
                LicenseTypes? licensetype = null;
                if (!string.IsNullOrWhiteSpace(renappinfo.LicenseTypeID))
                {
                    licenseLookup.TryGetValue(renappinfo.LicenseTypeID, out licensetype);
                }

                LicenseRegion? licenseReg = null;
                if (!string.IsNullOrWhiteSpace(renappinfo.ApplicationType))
                {
                    regionLookup.TryGetValue(renappinfo.ApplicationType, out licenseReg);
                }

                getreninfo.ApplicationId = rentask.ApplicationId;
                getreninfo.Id = rentask.Id;
                getreninfo.Reference = rentask.Reference;
                getreninfo.LLBNumber = rentask.LLBNumber;
                getreninfo.PreviousExpiry = rentask.PreviousExpiry;
                getreninfo.TradingName = reaoutletinfo?.TradingName ?? "N/A";
                getreninfo.Licensetype = licensetype?.LicenseName ?? "N/A";
                getreninfo.LicenseRegion = licenseReg?.RegionName ?? "N/A";
                getreninfo.Status = rentask.Status;

                renewaltasks.Add(getreninfo);
            }

            var inspections = _db.Inspection.Where(z => z.UserId == userId && z.Status == "Inspected").ToList();
            List<InspectionViewModel> renewalinspectiontasks = new List<InspectionViewModel>();
            foreach (var insptask in inspections)
            {
                var applId = insptask.ApplicationId;
                var appinfoq = _db.ApplicationInfo.Where(i => i.Id == applId).FirstOrDefault();
                var outletinfoq = _db.OutletInfo.Where(i => i.ApplicationId == applId).FirstOrDefault();
                var licensetype = _db.LicenseTypes.Where(a => a.Id == appinfoq.LicenseTypeID).FirstOrDefault();
                var licenseregion = _db.LicenseRegions.Where(a => a.Id == appinfoq.ApplicationType).FirstOrDefault();
                var inspecy = _db.Inspection.Where(s => s.ApplicationId == applId).OrderByDescending(z => z.DateApplied).FirstOrDefault();
                InspectionViewModel renewalinspectiontask = new InspectionViewModel();

                renewalinspectiontask.TradingName = outletinfoq.TradingName;
                renewalinspectiontask.LLBNumber = appinfoq.LLBNum;
                renewalinspectiontask.ApplicationId = applId;
                renewalinspectiontask.DateApplied = inspecy.DateApplied;
                renewalinspectiontask.Id = inspecy.Id;
                renewalinspectiontask.Reference = inspecy.Reference;
                renewalinspectiontask.Status = inspecy.Status;
                renewalinspectiontask.Service = inspecy.Service;
                renewalinspectiontask.LicenseType = licensetype.LicenseName;
                renewalinspectiontask.LicenseRegion = licenseregion.RegionName;
                renewalinspectiontask.TaskId = insptask.Id;
                renewalinspectiontask.InspectionDate = insptask.InspectionDate;

                renewalinspectiontasks.Add(renewalinspectiontask);
            }

            var managerChangeListings = _db.ChangeManaager
                .Where(a => a.UserId == userId)
                .ToList()
                .Select(a => BuildClientPostFormationListing(
                    a.ApplicationId,
                    a.Id,
                    a.Reference,
                    a.Status,
                    ParseClientListingDate(a.DateUpdated) ?? ParseClientListingDate(a.DateApplied),
                    null,
                    $"/Managers/ManagerChange?id={a.ApplicationId}&process=APM&changeId={a.Id}",
                    "View Manager Change",
                    applicationLookup,
                    outletLookup,
                    licenseLookup,
                    regionLookup))
                .Where(a => a != null)
                .Cast<ClientPostFormationListingViewModel>()
                .OrderByDescending(a => a.SubmittedDate ?? DateTime.MinValue)
                .ToList();

            var extendedHoursRecords = _db.ExtendedHours
                .Where(a => a.UserId == userId)
                .ToList();

            var extendedHourListings = extendedHoursRecords
                .Where(a => !IsExtraCounterReference(a.Reference))
                .Select(a => BuildClientPostFormationListing(
                    a.ApplicationId,
                    a.Id,
                    a.Reference,
                    a.Status,
                    a.DateUpdated,
                    a.ExtendedHoursDate,
                    string.Equals(a.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                        ? $"/Documents/ExtendedHoursLicense?searchref={a.Id}"
                        : $"/Postprocess/ExtendedHours?id={a.ApplicationId}&process=EXH&extId={a.Id}",
                    string.Equals(a.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                        ? "Display Extended Hours License"
                        : "Open Application",
                    applicationLookup,
                    outletLookup,
                    licenseLookup,
                    regionLookup))
                .Where(a => a != null)
                .Cast<ClientPostFormationListingViewModel>()
                .OrderByDescending(a => a.SubmittedDate ?? DateTime.MinValue)
                .ToList();

            var temporaryRetailListings = _db.TemporaryRetails
                .Where(a => a.UserId == userId)
                .ToList()
                .Select(a => BuildClientPostFormationListing(
                    a.ApplicationId,
                    a.Id,
                    a.Reference,
                    a.Status,
                    a.DateUpdated,
                    a.TemporaryRetailsDate,
                    string.Equals(a.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                        ? $"/Documents/TemporaryRetailLicense?searchref={a.Id}"
                        : $"/Postprocess/TemporaryRetails?id={a.ApplicationId}&process=TRL",
                    string.Equals(a.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                        ? "Display Temporary Retail License"
                        : "Open Application",
                    applicationLookup,
                    outletLookup,
                    licenseLookup,
                    regionLookup))
                .Where(a => a != null)
                .Cast<ClientPostFormationListingViewModel>()
                .OrderByDescending(a => a.SubmittedDate ?? DateTime.MinValue)
                .ToList();

            var extraCounterListings = _db.ExtraCounter
                .Where(a => a.UserId == userId)
                .ToList()
                .Select(a => BuildClientPostFormationListing(
                    a.ApplicationId,
                    a.Id,
                    a.Reference,
                    a.Status,
                    a.DateUpdated,
                    null,
                    $"/Extracounter/Extracounter?id={a.ApplicationId}&process=ECF&ecId={a.Id}",
                    "Open Permission To Alter",
                    applicationLookup,
                    outletLookup,
                    licenseLookup,
                    regionLookup))
                .Where(a => a != null)
                .Cast<ClientPostFormationListingViewModel>()
                .OrderByDescending(a => a.SubmittedDate ?? DateTime.MinValue)
                .ToList();

            var legacyExtraCounterListings = extendedHoursRecords
                .Where(a => IsExtraCounterReference(a.Reference))
                .Select(a => BuildClientPostFormationListing(
                    a.ApplicationId,
                    a.Id,
                    a.Reference,
                    a.Status,
                    a.DateUpdated,
                    null,
                    $"/Extracounter/Extracounter?id={a.ApplicationId}&process=ECF",
                    "Open Permission To Alter",
                    applicationLookup,
                    outletLookup,
                    licenseLookup,
                    regionLookup))
                .Where(a => a != null)
                .Cast<ClientPostFormationListingViewModel>()
                .OrderByDescending(a => a.SubmittedDate ?? DateTime.MinValue)
                .ToList();

            foreach (var legacyListing in legacyExtraCounterListings)
            {
                if (extraCounterListings.All(a => a.RecordId != legacyListing.RecordId))
                {
                    extraCounterListings.Add(legacyListing);
                }
            }

            extraCounterListings = extraCounterListings
                .OrderByDescending(a => a.SubmittedDate ?? DateTime.MinValue)
                .ToList();

            ViewBag.Inspections = renewalinspectiontasks;
            ViewBag.ActiveDashboardTab = NormalizeDashboardTab(tab);
            ViewBag.RenewalTasks = renewaltasks;
            ViewBag.ManagerChangeListings = managerChangeListings;
            ViewBag.ExtendedHourListings = extendedHourListings;
            ViewBag.TemporaryRetailListings = temporaryRetailListings;
            ViewBag.ExtraCounterListings = extraCounterListings;
            ViewBag.Renewals = renewals;
            ViewBag.User = user;
            ViewBag.OutletInfo = outletinfo;
            ViewBag.Regions = regions;
            ViewBag.License = license;
            ViewBag.Applications = applications;
            ViewBag.ApprovedApplications = approvedapplications;
        }

        private static string NormalizeDashboardTab(string? tab)
        {
            return tab?.Trim().ToLowerInvariant() switch
            {
                "licences-pane" => "licences-pane",
                "services-pane" => "services-pane",
                _ => "in-progress-pane"
            };
        }

        private static DateTime? ParseClientListingDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParse(value, out var parsedValue))
            {
                return parsedValue;
            }

            return null;
        }

        private static bool IsExtraCounterReference(string? reference)
        {
            return !string.IsNullOrWhiteSpace(reference)
                && reference.StartsWith("PF-ECF-", StringComparison.OrdinalIgnoreCase);
        }

        private static ClientPostFormationListingViewModel? BuildClientPostFormationListing(
            string? applicationId,
            string? recordId,
            string? reference,
            string? status,
            DateTime? submittedDate,
            DateTime? eventDate,
            string actionUrl,
            string actionLabel,
            IReadOnlyDictionary<string, ApplicationInfo> applicationLookup,
            IReadOnlyDictionary<string, OutletInfo> outletLookup,
            IReadOnlyDictionary<string, LicenseTypes> licenseLookup,
            IReadOnlyDictionary<string, LicenseRegion> regionLookup)
        {
            if (string.IsNullOrWhiteSpace(applicationId)
                || !applicationLookup.TryGetValue(applicationId, out var application))
            {
                return null;
            }

            outletLookup.TryGetValue(applicationId, out var outlet);

            LicenseTypes? licenseType = null;
            if (!string.IsNullOrWhiteSpace(application.LicenseTypeID))
            {
                licenseLookup.TryGetValue(application.LicenseTypeID, out licenseType);
            }

            LicenseRegion? region = null;
            if (!string.IsNullOrWhiteSpace(application.ApplicationType))
            {
                regionLookup.TryGetValue(application.ApplicationType, out region);
            }

            return new ClientPostFormationListingViewModel
            {
                RecordId = recordId ?? string.Empty,
                Reference = reference ?? application.RefNum ?? string.Empty,
                ApplicationId = applicationId,
                TradingName = outlet?.TradingName ?? application.BusinessName ?? "N/A",
                LLBNumber = application.LLBNum ?? "N/A",
                LicenseName = licenseType?.LicenseName ?? "N/A",
                RegionName = region?.RegionName ?? "N/A",
                Status = status ?? "Unknown",
                SubmittedDate = submittedDate,
                EventDate = eventDate,
                ActionUrl = actionUrl,
                ActionLabel = actionLabel
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
