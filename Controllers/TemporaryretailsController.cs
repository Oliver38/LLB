using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using LLB.Data;
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

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;



            TemporaryRetails getinfo = new  TemporaryRetails();

                var applications = _db.TemporaryRetails.Where(a => a.Id == Id).FirstOrDefault();

                getinfo = applications;
            

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
            return View();
        }


        [HttpGet("Approve")]
        public async Task<IActionResult> Approve(string Id)
        {

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;



            TemporaryRetails getinfo = new TemporaryRetails();

            var applications = _db.TemporaryRetails.Where(a => a.Id == Id).FirstOrDefault();
            applications.ApproverId = id;
            applications.DateOfApproval = DateTime.Now;
            applications.Status = "Approved";

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

            return RedirectToAction("ViewApplications", "TemporaryRetails" , new {Id= Id });
            return View();
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


    }
}