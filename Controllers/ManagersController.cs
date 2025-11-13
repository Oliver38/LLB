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
using Webdev.Payments;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Cryptography.Xml;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto;
using SkiaSharp;
using System;
using static System.Net.Mime.MediaTypeNames;


namespace LLB.Controllers
{
    [Authorize]
    [Route("Managers")]
    public class ManagersController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public ManagersController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }




        [HttpGet("ManagerChange")]
        public async Task<IActionResult> ManagerChange(string id, double amount, string service, string process)
        {
            var userId = userManager.GetUserId(User);
            // Check if a ChangeManaager record already exists for this user/application
            var existing = _db.ChangeManaager
                .FirstOrDefault(c => c.ApplicationId == id);
            ChangeManaager managersapplication;
            if (existing != null || existing.Status == "completed")
            {
                managersapplication = existing;
            }
            else
            {
                var newChange = new ChangeManaager
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ApplicationId = id,
                    Status = "Initiated",
                    DateApplied = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    DateUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    PaidFee = "0",
                    PaymentStatus = "Unpaid",
                    NewManagersCount = "0"
                };

                _db.ChangeManaager.Add(newChange);
                _db.SaveChanges();

                managersapplication = newChange;

            }


            var cmapplication = _db.ChangeManaager.Where(a => a.ApplicationId == id).OrderByDescending(a => a.DateApplied).FirstOrDefault();
            if (cmapplication == null || cmapplication.Status == null || cmapplication.Status == "complete")
            {

            }
            else
            {
                TempData["Message"] = "Another application in progress";
               // return View();
            }

            var appinfo = _db.ApplicationInfo.Where(b => b.Id == id).FirstOrDefault();
            var mainlicense = _db.LicenseTypes.Where(z => z.Id == appinfo.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(d => d.Id == appinfo.ApplicationType).FirstOrDefault();
            var RenewalFees = _db.RenewalTypes.Where(a => a.LicenseCode == mainlicense.LicenseCode).FirstOrDefault();
            var outletinfo = _db.OutletInfo.Where(c => c.ApplicationId == id && c.Status == "active").FirstOrDefault();

            // var Fees = _db.PostFormationFees.Where(n => n.Code == process).FirstOrDefault();
            var PenaltyFees = _db.PostFormationFees.Where(n => n.Code == "PNL").FirstOrDefault();
            //var cmapplication = _db.ChangeManaager.Where(a => a.ApplicationId == id).FirstOrDefault();

            var managers = _db.ManagersParticulars.Where(s => s.ApplicationId == id).ToList();


            Payments payment = null;
            if (cmapplication != null)
            {
                var paymentTrans = _db.Payments.Where(s => s.ApplicationId == cmapplication.Id && s.Service == "changemanager").OrderByDescending(x => x.DateAdded).FirstOrDefault();


                if (paymentTrans == null)
                {

                }
                else
                {


                }
            }
            //submitted check status

            ViewBag.License = mainlicense;
            ViewBag.Payment = payment;
            ViewBag.Process = process;

            ViewBag.Outletinfo = outletinfo;
            ViewBag.Appinfo = appinfo;

            ViewBag.Outletinfo = outletinfo;
            ViewBag.Appinfo = appinfo;
            ViewBag.Managers = managers;
            ViewBag.ChangeManager = managersapplication;

            return View();
        }





        [HttpPost("ManagersInfo")]
        public async Task<IActionResult> ManagersInfoAsync(ManagersParticulars manager, IFormFile file, IFormFile fileb, IFormFile form55)
        {
            /*  public string? Id { get; set; }
          public string? UserId { get; set; }
          public string? Name { get; set; }
          public string? ApplicationId { get; set; }
          public string? Surname { get; set; }
          public string? NationalId { get; set; }
          public string? Address { get; set; }
          public string? Status { get; set; }
          public DateTime DateAdded { get; set; }
          public DateTime DateUpdated { get; set; }*/

            manager.Id = Guid.NewGuid().ToString();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            manager.UserId = id;
            manager.Status = "Pending";
            manager.DateAdded = DateTime.Now;
            manager.DateUpdated = DateTime.Now;

            if (file != null)
            {
                string pic = System.IO.Path.GetFileName(file.FileName);
                string dic = System.IO.Path.GetExtension(file.FileName);
                string newname = "NatId_" + manager.Id;
                string path = System.IO.Path.Combine($"ManagerFingerprints", newname + dic);
                string docpath = System.IO.Path.Combine($"wwwroot/ManagerFingerprints", newname + dic);
                manager.Attachment = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            else
            {
                manager.Attachment = "";
            }


            if (fileb != null)
            {
                string picb = System.IO.Path.GetFileName(fileb.FileName);
                string dicb = System.IO.Path.GetExtension(fileb.FileName);
                string newname = "Fingerprints_" + manager.Id;
                string path = System.IO.Path.Combine($"ManagerFingerprints", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/ManagerFingerprints", newname + dicb);
                manager.Fingerprints = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await fileb.CopyToAsync(fileStream);
                }
            }
            else
            {
                manager.Fingerprints = "";
            }


            if (form55 != null)
            {
                string picb = System.IO.Path.GetFileName(form55.FileName);
                string dicb = System.IO.Path.GetExtension(form55.FileName);
                string newname = "Form55_" + manager.Id;
                string path = System.IO.Path.Combine($"ManagerFingerprints", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/ManagerFingerprints", newname + dicb);
                manager.Form55 = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await form55.CopyToAsync(fileStream);
                }
            }
            else
            {
                manager.Form55 = "";
            }



            _db.Add(manager);
            _db.SaveChanges();

            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == manager.ApplicationId).FirstOrDefault();
            var managersInfo = _db.ManagersParticulars.Where(b => b.ApplicationId == manager.ApplicationId).ToList();

            var queries = _db.Queries.Where(x => x.ApplicationId == manager.ApplicationId && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
            ViewBag.ApplicationInfo = applicationInfo;
            ViewBag.ManagersInfo = managersInfo;
            TempData["result"] = "Manager details successfully added";

            return RedirectToAction();
        }


    }

}

