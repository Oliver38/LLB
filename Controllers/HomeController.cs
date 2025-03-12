using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using LLB.Data;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Identity;
using LLB.Models.ViewModel;

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
                return RedirectToAction("Renewal", "Postprocess", new { id = id, process = process });

            }else if (process == "APM")
            {
                return RedirectToAction("Managerchange", "Postprocess", new { param1 = id, param2 = id });

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
                return RedirectToAction("Tempremoval", "Postprocess", new { param1 = id, param2 = id });

            }
            else if (process == "TTR")
            {
                return RedirectToAction("Temptranfer", "Postprocess", new { param1 = id, param2 = id });

            }
            else if (process == "EXH")
            {
                return RedirectToAction("Extendhours", "Postprocess", new { param1 = id, param2 = id });

            }
            else if (process == "TRL")
            {
                return RedirectToAction("Temporalretail", "Postprocess", new { param1 = id, param2 = id });

            }
            else if (process == "ECF")
            {
                return RedirectToAction("Extracounter", "Postprocess", new { param1 = id, param2 = id });

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
      

        public async Task<IActionResult> DashboardAsync()
        {

            if (User.IsInRole("admin"))
            {
                return RedirectToAction("AdminDashboard", "Tasks");
            }
           
            else if (User.IsInRole("verifier"))
            {
                return RedirectToAction("Dashboard", "Verify");
            }

            else if (User.IsInRole("recommender"))
            {
                return RedirectToAction("Dashboard", "Recommend");
            }
            else if (User.IsInRole("secretary"))
            {
                return RedirectToAction("Dashboard", "Approval");
            }
            else if (User.IsInRole("inspector"))
            {
                return RedirectToAction("Dashboard", "Examination");
            }
            else if (User.IsInRole("accountant"))
            {
                return RedirectToAction("Dashboard", "Accountant");
            }

           
            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            //var Id = await userManager.GetUserId(User.Identity.Name);
            string id = userId.Id;
            var renewals = _db.Renewals.Where(q => q.UserId == id).ToList();

            var applications = _db.ApplicationInfo.Where(a => a.UserID == id && a.Status != "approved").ToList();
            var approvedapplications = _db.ApplicationInfo.Where(a => a.UserID == id && a.Status == "approved").ToList();
            var outletinfo  = _db.OutletInfo.ToList();
            var license = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var user = await userManager.FindByEmailAsync(User.Identity.Name);

            List<RenewalViewModel> renewaltasks = new List<RenewalViewModel>();

            foreach (var rentask in renewals)
            {
                RenewalViewModel getreninfo = new RenewalViewModel();

               // var renapps = _db.Renewals.Where(a => a.Id == rentask.ApplicationId).FirstOrDefault();
                var renappinfo = _db.ApplicationInfo.Where(s => s.Id == rentask.ApplicationId).FirstOrDefault();
                var reaoutletinfo = _db.OutletInfo.Where(q => q.ApplicationId == rentask.ApplicationId).FirstOrDefault();
                var licensetype = _db.LicenseTypes.Where(w => w.Id == renappinfo.LicenseTypeID).FirstOrDefault();
                var licenseReg = _db.LicenseRegions.Where(e => e.Id == renappinfo.ApplicationType).FirstOrDefault();
                getreninfo.ApplicationId = rentask.ApplicationId;
                getreninfo.Id = rentask.Id;
                getreninfo.LLBNumber = rentask.LLBNumber;
                getreninfo.PreviousExpiry = rentask.PreviousExpiry;
                getreninfo.TradingName = reaoutletinfo.TradingName;
                getreninfo.Licensetype = licensetype.LicenseName;
                getreninfo.LicenseRegion = licenseReg.RegionName;
                getreninfo.Status = rentask.Status;

                renewaltasks.Add(getreninfo);
            }


            var inspections = _db.Inspection.Where(z => z.UserId == id && z.Status == "Inspected").ToList();
            List<InspectionViewModel> renewalinspectiontasks = new List<InspectionViewModel>();
            // var reninsptasks = _db.Tasks.Where(f => f.VerifierId == id && f.Service == "renewal inspection" && f.Status == "assigned").ToList();
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
                renewalinspectiontask.Status = inspecy.Status;
                renewalinspectiontask.Service = inspecy.Service;
                renewalinspectiontask.LicenseType = licensetype.LicenseName;
                renewalinspectiontask.LicenseRegion = licenseregion.RegionName;
                renewalinspectiontask.TaskId = insptask.Id;
                renewalinspectiontask.InspectionDate = insptask.InspectionDate;

                renewalinspectiontasks.Add(renewalinspectiontask);


            }



            ViewBag.Inspections = renewalinspectiontasks;

            ViewBag.RenewalTasks = renewaltasks;
            ViewBag.Renewals = renewals;
            ViewBag.User = user;
            ViewBag.OutletInfo = outletinfo;
            ViewBag.Regions = regions;
            ViewBag.License= license;
            ViewBag.Applications = applications;
            ViewBag.ApprovedApplications = approvedapplications;
            return View();
        }

        private void GetUserId()
        {
            throw new NotImplementedException();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}