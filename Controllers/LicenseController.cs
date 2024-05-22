using LLB.Data;
using LLB.Models;
//using LLB.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using LLB.Models.DataModel;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using static System.Net.WebRequestMethods;
using System.Net.Mail;
using System.Net;
using PasswordGenerator;
using DNTCaptcha.Core;
using LLB.Models.ViewModel;

namespace LLB.Controllers
{
    [Authorize]
    [Route("License")]
    public class LicenseController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public LicenseController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }


        [HttpGet(("Apply"))]
        public async Task<IActionResult> ApplyAsync(string Id)
        {
            //using Microsoft.AspNetCore.Identity;
            var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var user = await userManager.FindByEmailAsync(User.Identity.Name);
            var licenses = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            ViewBag.ApplicationInfo = application;
            ViewBag.User = user;
            ViewBag.Regions= regions;
            ViewBag.License = licenses;
            return View();
        }



        [HttpPost(("Apply"))]
        public async Task<IActionResult> ApplyAsync(ApplicationInfo info)
        {
            if (info.Id == null)
            {
                info.Id = Guid.NewGuid().ToString();
                // * ApplicationID /Id
                info.UserID = userManager.GetUserId(User);

                info.PlaceOfBirth = "";
                info.DateofEntryIntoZimbabwe = "";
                info.PlaceOfEntry = "";

                info.Status = "inprogress";
                info.ApplicationDate = DateTime.Now;
                info.InspectorID = "";

                //public DateTime InspectionDate 
                info.Secretary = "";
                // public DateTime ApprovedDate 
                ///////////// info.RejectionReason = "";
                //spublic DateTime DateCreated 
                _db.Add(info);
                _db.SaveChanges();
                // ViewBag.License = licenses;
                var application = _db.ApplicationInfo.Where(a => a.Id == info.Id).FirstOrDefault();
                var user = await userManager.FindByEmailAsync(User.Identity.Name);
                var licenses = _db.LicenseTypes.ToList();
                var regions = _db.LicenseRegions.ToList();
                ViewBag.ApplicationInfo = application;
                ViewBag.User = user;
                ViewBag.Regions = regions;
                ViewBag.License = licenses;
                TempData["result"] = "Applicant details successfully submited";
                return View();
            }
            else
            {
                var updateinfo = _db.ApplicationInfo.Where(x => x.Id == info.Id).FirstOrDefault();

                updateinfo.OperationAddress = info.OperationAddress;
                updateinfo.BusinessName = info.BusinessName;
                updateinfo.LicenseTypeID = info.LicenseTypeID;
                updateinfo.ApplicationType = info.ApplicationType;
                    

                updateinfo.PlaceOfBirth = "";
                updateinfo.DateofEntryIntoZimbabwe = "";
                updateinfo.PlaceOfEntry = "";

                updateinfo.Status = "inprogress";
                updateinfo.ApplicationDate = DateTime.Now;
                updateinfo.InspectorID = "";

                //public DateTime InspectionDate 
                updateinfo.Secretary = "";
                // public DateTime ApprovedDate 
                ///////////// info.RejectionReason = "";
                //spublic DateTime DateCreated 
                var o =_db.Update(updateinfo);
                var p = _db.SaveChanges();
                // ViewBag.License = licenses;
                var application = _db.ApplicationInfo.Where(a => a.Id == info.Id).FirstOrDefault();
                var user = await userManager.FindByEmailAsync(User.Identity.Name);
                var licenses = _db.LicenseTypes.ToList();
                var regions = _db.LicenseRegions.ToList();
                ViewBag.ApplicationInfo = application;
                ViewBag.User = user;
                ViewBag.Regions = regions;
                ViewBag.License = licenses;
                TempData["result"] = "Applicant details successfully updated";
                return View();
                //return View();
            }
            }

        [HttpGet(("OutletInfo"))]
        public async Task<IActionResult> OutletInfo(string Id)
            {
                // 00826805 - 0853 - 45c5 - 9fe3 - bce855854091
                var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
                var outletInfo = _db.OutletInfo.Where(b => b.ApplicationId == Id).FirstOrDefault();
                // var application = await _db.ApplicationInfo.FindAsync(dd.Id);

                ViewBag.Application = application;
                ViewBag.OutletInfo = outletInfo;
                //ViewBag.User = user;

                return View();
            }
            
        }

    }

