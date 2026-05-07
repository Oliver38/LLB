using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using LLB.Data;
using LLB.Helpers;
using LLB.Models.ViewModel;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Identity;
using Webdev.Payments;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LLB.Controllers
{
    
    [Route("")]
    [Route("Temporaryretails")]
    public class TemporaryretailsController : Controller
    {
        

        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public TemporaryretailsController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }
        [HttpGet("AllApplications")]
        public async Task<IActionResult> AllApplications()
        {

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;


            List<TemporaryRetails> appinfo = new List<TemporaryRetails>();
            var tasks = _db.Tasks.Where(s => s.ApproverId == id && s.Status == "assigned" && s.Service == "Temporary Retails").ToList();

            foreach(var task in tasks)
            {
                TemporaryRetails getinfo = new TemporaryRetails();

                //var applications = _db.ApplicationInfo.Where(a => a.Id == task.ApplicationId).FirstOrDefault();
                var applications = _db.TemporaryRetails.Where(a => a.Id == task.ApplicationId).FirstOrDefault();

                getinfo = applications;
                appinfo.Add(getinfo);
            }

            //var applications = _db.ApplicationInfo.Where(a => a.UserID == id).ToList();
            var outletinfo = _db.OutletInfo.ToList();
            var license = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var user = await userManager.FindByEmailAsync(User.Identity.Name);

            ViewBag.User = user;
            ViewBag.OutletInfo = outletinfo;
            ViewBag.Regions = regions;
            ViewBag.License = license;
            ViewBag.Applications = appinfo;
            return View();
        }


        [HttpGet("ViewApplications")]
        public async Task<IActionResult> ViewApplications(string Id)
        {
            var model = BuildTemporaryRetailReviewModel(Id);
            if (model == null)
            {
                TempData["error"] = "The temporary retail application could not be found.";
                return RedirectToAction("AllApplications");
            }

            var user = await userManager.FindByEmailAsync(User.Identity?.Name);
            ViewBag.User = user;
            return View(model);
        }


        [HttpGet("Approve")]
        public async Task<IActionResult> Approve(string Id)
        {
            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;

            var applications = _db.TemporaryRetails.Where(a => a.Id == Id).FirstOrDefault();
            if (applications == null)
            {
                TempData["error"] = "The temporary retail application could not be found.";
                return RedirectToAction("Dashboard", "Approval");
            }

            var outlet = _db.OutletInfo.Where(a => a.ApplicationId == applications.ApplicationId).FirstOrDefault();
            var tradingName = outlet?.TradingName ?? "the selected outlet";

            applications.ApproverId = id;
            applications.DateOfApproval = DateTime.Now;
            applications.Status = "Approved";
            applications.DateUpdated = DateTime.Now;
            _db.Update(applications);
            _db.SaveChanges();

            var rootApplication = _db.ApplicationInfo.Where(a => a.Id == applications.ApplicationId).FirstOrDefault();
            DownloadStatusHelper.OpenLicenseDownload(_db, rootApplication, rootApplication?.UserID);

            //complete task
            var task = _db.Tasks.Where(a => a.ApplicationId == Id && a.Service == "Temporary Retails").FirstOrDefault();
            if (task != null)
            {
                task.Status = "completed";
                task.ApprovedDate = DateTime.Now;
                _db.Update(task);
            }

            _db.SaveChanges();

            TempData["success"] = $"Temporary retail application approved successfully for {tradingName}.";
            return RedirectToAction("Dashboard", "Approval");
        }




        [HttpGet("Reject")]
        public async Task<IActionResult> Reject(string Id)
        {

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;



            TemporaryRetails getinfo = new TemporaryRetails();

            var applications = _db.TemporaryRetails.Where(a => a.Id == Id).FirstOrDefault();
            applications.ApproverId = id;
            applications.DateOfApproval = DateTime.Now;
            applications.Status = "Rejected";

            getinfo = applications;

            //complete task
            var task = _db.Tasks.Where(a => a.ApplicationId == Id && a.Service == "Temporary Retails").FirstOrDefault();
            task.Status = "completed";
            task.ApprovedDate = DateTime.Now;
            _db.Update(task);
            _db.SaveChanges();

            //var applications = _db.ApplicationInfo.Where(a => a.UserID == id).ToList();
            var outletinfo = _db.OutletInfo.ToList();
            var license = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var user = await userManager.FindByEmailAsync(User.Identity.Name);

            ViewBag.User = user;
            ViewBag.OutletInfo = outletinfo;
            ViewBag.Regions = regions;
            ViewBag.License = license;
            ViewBag.Application = getinfo;

            return RedirectToAction("ViewApplications", "TemporaryRetails", new { Id = Id });
            return View();
        }


        [HttpGet("MyApprovedTR")]
        public async Task<IActionResult> MyApprovedTR()
        {

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;


          
                List<TemporaryRetails> getinfo = new List<TemporaryRetails>();

                //var applications = _db.ApplicationInfo.Where(a => a.Id == task.ApplicationId).FirstOrDefault();
                var applications = _db.TemporaryRetails.Where(a => a.UserId == id && a.Status == "Approved").ToList();
           
                getinfo = applications;
                //  appinfo.Add(getinfo);
            



            //var applications = _db.ApplicationInfo.Where(a => a.UserID == id).ToList();
            var outletinfo = _db.OutletInfo.ToList();
            var license = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var user = await userManager.FindByEmailAsync(User.Identity.Name);

            ViewBag.User = user;
            ViewBag.OutletInfo = outletinfo;
            ViewBag.Regions = regions;
            ViewBag.License = license;
            ViewBag.Applications = applications;
            return View();
        }
        //Submitted
        //Rejected
        //statuses for the submitted and rejected applications

        private TemporaryRetailReviewViewModel? BuildTemporaryRetailReviewModel(string temporaryRetailId)
        {
            var temporaryRetail = _db.TemporaryRetails.Where(application => application.Id == temporaryRetailId).FirstOrDefault();
            if (temporaryRetail == null || string.IsNullOrWhiteSpace(temporaryRetail.ApplicationId))
            {
                return null;
            }

            var application = _db.ApplicationInfo.Where(record => record.Id == temporaryRetail.ApplicationId).FirstOrDefault();
            if (application == null)
            {
                return null;
            }

            var outlet = _db.OutletInfo.Where(record => record.ApplicationId == application.Id).FirstOrDefault();
            var licenseType = _db.LicenseTypes.Where(record => record.Id == application.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(record => record.Id == application.ApplicationType).FirstOrDefault();
            var payment = _db.Payments
                .Where(record => record.ApplicationId == temporaryRetail.Id
                    && (record.Service == "temporary retails"
                        || record.Service == "Temporary Retails"))
                .OrderByDescending(record => record.DateAdded)
                .FirstOrDefault();

            return new TemporaryRetailReviewViewModel
            {
                Id = temporaryRetail.Id ?? string.Empty,
                ApplicationId = application.Id ?? string.Empty,
                Reference = temporaryRetail.Reference ?? string.Empty,
                TradingName = outlet?.TradingName,
                Address = outlet?.Address ?? application.OperationAddress,
                Province = outlet?.Province,
                Council = outlet?.Council,
                LLBNumber = application.LLBNum,
                LicenseType = licenseType?.LicenseName,
                LicenseRegion = licenseRegion?.RegionName,
                Status = temporaryRetail.Status,
                PaymentStatus = payment?.PaymentStatus ?? payment?.Status ?? temporaryRetail.PaymentStatus ?? "Not Paid",
                PaynowReference = payment?.PaynowRef,
                ReasonForExtention = temporaryRetail.ReasonForExtention,
                LocationAddress = temporaryRetail.LocationAddress,
                PaidFee = temporaryRetail.PaidFee,
                TemporaryRetailDate = temporaryRetail.TemporaryRetailsDate,
                RequestedOn = temporaryRetail.DateAdded
            };
        }

    }
}
