using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using LLB.Data;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using LLB.Helpers;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Webdev.Payments;
using System;
using System.Runtime.Intrinsics.Arm;
using System.Linq;

namespace LLB.Controllers
{


    [Route("Postprocess")]
    public class PostprocessController : Controller
    {


        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public PostprocessController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        [HttpGet("AddFee")]
        public IActionResult AddFee()
        {

            var Fees = _db.PostFormationFees.ToList();
            ViewBag.Fees = Fees;
            return View();
        }

        [HttpPost("AddFee")]
        public async Task<IActionResult> AddFee(string Description, string Code, string ProcessName, double Fee)
        {
            PostFormationFees ppfee = new PostFormationFees();
            ppfee.Id = Guid.NewGuid().ToString();
            var user = await userManager.GetUserAsync(User); // Get the current user
            var userId = user?.Id; // Get the User ID
            ppfee.UserId = userId;
            ppfee.ProcessName = ProcessName;
            ppfee.Description = Description;
            ppfee.Code = Code;
            ppfee.Status = "active";
            ppfee.Fee = Fee;

            ppfee.DateAdded = DateTime.Now;


            _db.Add(ppfee);
            _db.SaveChanges();


            var Fees = _db.PostFormationFees.ToList();
            ViewBag.Fees = Fees;
            return View();
            // RedirectToAction("", "");
        }

        [HttpPost("UpdateFee")]
        public async Task<IActionResult> AddFee(string Id, string Description, string Code, string ProcessName, double Fee)
        { // Check if the fee exists in the database
            var existingFee = await _db.PostFormationFees.FindAsync(Id);

            if (existingFee == null)
            {
                // If the fee does not exist, return a not found result
                return NotFound();
            }

            // Update the properties with the new values from the form
            existingFee.ProcessName = ProcessName;
            existingFee.Description = Description;
            existingFee.Code = Code;
            existingFee.Fee = Fee;
            //existingFee.Status = Status; // Optional: You can decide whether to allow editing Status
            existingFee.DateUpdated = DateTime.Now; // You may not want to update this, depending on your requirements

            // Save the changes to the database
            _db.Update(existingFee);
            _db.SaveChanges();
            // return View();
            return RedirectToAction("AddFee", "Postprocess");
        }


        [HttpGet("Renewal")]
        public IActionResult Renewal(string id, string process)
        {
            //  var serv = _db.PostFormationFees.Where(a => a.Code == process).FirstOrDefault();

            var appinfo = _db.ApplicationInfo.Where(b => b.Id == id).FirstOrDefault();
            var mainlicense = _db.LicenseTypes.Where(z => z.Id == appinfo.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(d => d.Id == appinfo.ApplicationType).FirstOrDefault();
            var RenewalFees = _db.RenewalTypes.Where(a => a.LicenseCode == mainlicense.LicenseCode).FirstOrDefault();
            var outletinfo = _db.OutletInfo.Where(c => c.ApplicationId == id && c.Status == "active").FirstOrDefault();

            var today = DateTime.Now;
            // var penalty = DateTime.Now.Month - appinfo.ExpiryDate.Month ;
            int time;
            int totalMonths = ((today.Year - appinfo.ExpiryDate.Year) * 12) + today.Month - appinfo.ExpiryDate.Month;
            if (totalMonths <= 0)
            {
                time = 0;
            }
            else
            {
                time = totalMonths;
            }

            double getFee = 0;

            if (licenseRegion.RegionName == "RDC")
            {
                getFee = RenewalFees.RDCFee;
            }
            else if (licenseRegion.RegionName == "Town")
            {
                getFee = RenewalFees.TownFee;
            }
            else if (licenseRegion.RegionName == "City")
            {
                getFee = RenewalFees.CityFee;
            }
            else if (licenseRegion.RegionName == "Municipality")
            {
                getFee = RenewalFees.MunicipaltyFee;
            }
            else
            {
                getFee = 0;
            }

            // var Fees = _db.PostFormationFees.Where(n => n.Code == process).FirstOrDefault();
            var PenaltyFees = _db.PostFormationFees.Where(n => n.Code == "PNL").FirstOrDefault();
            var penalty = time * PenaltyFees.Fee;
            var totalfee = penalty + getFee;

            Payments payment = null;
            var paymentTrans = _db.Payments.Where(s => s.ApplicationId == id && s.Service == "renewal").OrderByDescending(x => x.DateAdded).FirstOrDefault();
            if (paymentTrans == null)
            {

            }
            else
            {
                var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");

                var status = paynow.PollTransaction(paymentTrans.PollUrl);

                var statusdata = status.GetData();
                paymentTrans.PaynowRef = statusdata["paynowreference"];
                paymentTrans.PaymentStatus = statusdata["status"];
                paymentTrans.Status = statusdata["status"];
                paymentTrans.DateUpdated = DateTime.Now;

                _db.Update(paymentTrans);
                _db.SaveChanges();
                payment = paymentTrans;
            }
            var renewaldata = _db.Renewals.Where(x => x.ApplicationId == id).OrderByDescending(s => s.DateApplied).FirstOrDefault();
            ViewBag.Payment = payment;
            ViewBag.Process = process;
            ViewBag.Fee = getFee;
            ViewBag.Penalty = penalty;
            ViewBag.TotalFee = totalfee;
            ViewBag.Months = time;
            ViewBag.Outletinfo = outletinfo;
            ViewBag.Appinfo = appinfo;
            ViewBag.ServeFee = getFee;
            ViewBag.Renewaldata = renewaldata;
            return View();
        }

        [HttpPost("PostRenenwals")]
        public async Task<IActionResult> PostRenDocsAsync(Renewals renewal, IFormFile prevcert, IFormFile healthcert)
        {
            if(renewal.Id == null) { 
            renewal.Id = Guid.NewGuid().ToString();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            renewal.UserId = id;
           

            //spublic DateTime DateCreated
            if (prevcert != null)
            {
                string picb = System.IO.Path.GetFileName(prevcert.FileName);
                string dicb = System.IO.Path.GetExtension(prevcert.FileName);
                string newname = "PreviousCertificate_" + renewal.Id;
                string path = System.IO.Path.Combine($"Renewals", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/Renewals", newname + dicb);
                renewal.CertifiedLicense = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await prevcert.CopyToAsync(fileStream);
                }
            }
            else
            {
                renewal.CertifiedLicense = "";
            }


            //spublic DateTime DateCreated
            if (prevcert != null)
            {
                string picb = System.IO.Path.GetFileName(healthcert.FileName);
                string dicb = System.IO.Path.GetExtension(healthcert.FileName);
                string newname = "PreviousCertificate_" + renewal.Id;
                string path = System.IO.Path.Combine($"Renewals", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/Renewals", newname + dicb);
                renewal.HealthCert = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await healthcert.CopyToAsync(fileStream);
                }
            }
            else
            {
                renewal.HealthCert = "";
            }

                _db.Add(renewal);
                _db.SaveChanges();

            return RedirectToAction("", "");
            }
            else
            {


                return RedirectToAction("", "");
            }
        }

        //[HttpPost("Renewal")]
        //public IActionResult Renewal(Renewals renewal, IFormFile HealthCert, IFormFile CertifiedLicense)
        //{
        //    renewal.Id = Guid.NewGuid().ToString();
        //    renewal.DateApplied = DateTime.Now;
        //    _db.Add(renewal);
        //    _db.SaveChanges();

        //    var serv = _db.PostFormationFees.Where(a => a.Code == process).FirstOrDefault();
        //    var appinfo = _db.ApplicationInfo.Where(b => b.Id == renewal.ApplicationId).FirstOrDefault();
        //    var outletinfo = _db.OutletInfo.Where(c => c.ApplicationId == renewal.ApplicationId && c.Status == "active").FirstOrDefault();

        //    var today = DateTime.Now;
        //    // var penalty = DateTime.Now.Month - appinfo.ExpiryDate.Month ;
        //    int time;
        //    int totalMonths = ((today.Year - appinfo.ExpiryDate.Year) * 12) + today.Month - appinfo.ExpiryDate.Month;
        //    if (totalMonths <= 0)
        //    {
        //        time = 0;
        //    }
        //    else
        //    {
        //        time = totalMonths;

        //    }
        //    var Fees = _db.PostFormationFees.Where(n => n.Code == process).FirstOrDefault();
        //    var PenaltyFees = _db.PostFormationFees.Where(n => n.Code == "PNL").FirstOrDefault();
        //    var penalty = time * PenaltyFees.Fee;
        //    var totalfee = penalty + Fees.Fee;

        //    Payments payment = null;
        //    var paymentTrans = _db.Payments.Where(s => s.ApplicationId == id && s.Service == "renewal").OrderByDescending(x => x.DateAdded).FirstOrDefault();
        //    if (paymentTrans == null)
        //    {

        //    }
        //    else
        //    {
        //        var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");

        //        var status = paynow.PollTransaction(paymentTrans.PollUrl);

        //        var statusdata = status.GetData();
        //        paymentTrans.PaynowRef = statusdata["paynowreference"];
        //        paymentTrans.PaymentStatus = statusdata["status"];
        //        paymentTrans.Status = statusdata["status"];
        //        paymentTrans.DateUpdated = DateTime.Now;

        //        _db.Update(paymentTrans);
        //        _db.SaveChanges();
        //        payment = paymentTrans;
        //    }
        //    //var appl = _db.ApplicationInfo.Where(w => w.Id == renewal.ApplicationId).FirstOrDefault();
        //    //var afteryear = DateTime.Now.AddYears(1);
        //    //appl.ExpiryDate = afteryear;
        //    // _db.Update(appl);
        //    // _db.SaveChanges();
        //    // return RedirectToAction("Dashboard", "Home");
        //    ViewBag.Payment = payment;
        //    ViewBag.Process = process;
        //    ViewBag.Fee = Fees;
        //    ViewBag.Penalty = penalty;
        //    ViewBag.TotalFee = totalfee;
        //    ViewBag.Months = time;
        //    ViewBag.Outletinfo = outletinfo;
        //    ViewBag.Appinfo = appinfo;
        //    ViewBag.ServeFee = serv;
        //    return View();
        //  //  return View();
        //}

        //                 if(process==  "RNW")
        //            {
        //                return RedirectToAction("Renewal", "Postprocess", new { id = id, process = id
        //    });

        //            }else if (process == "APM")
        //{
        //    return RedirectToAction("Managerchange", "Postprocess", new { param1 = id, param2 = id });

        //}
        //else if (process == "GDP")
        //{
        //    return RedirectToAction("Governmentpermit", "Postprocess", new { param1 = id, param2 = id });

        //}
        //else if (process == "INP")
        //{
        //    return RedirectToAction("Inspection", "Postprocess", new { param1 = id, param2 = id });

        //}
        //else if (process == "DPL")
        //{
        //    return RedirectToAction("Duplication", "Postprocess", new { param1 = id, param2 = id });

        //}
        //else if (process == "TRM")
        //{
        //    return RedirectToAction("Tempremoval", "Postprocess", new { param1 = id, param2 = id });

        //}
        //else if (process == "TTR")
        //{
        //    return RedirectToAction("Temptranfer", "Postprocess", new { param1 = id, param2 = id });

        //}
        //else if (process == "EXH")
        //{
        //    return RedirectToAction("Extendhours", "Postprocess", new { param1 = id, param2 = id });

        //}
        //else if (process == "TRL")
        //{
        //    return RedirectToAction("Temporalretail", "Postprocess", new { param1 = id, param2 = id });

        //}
        //else if (process == "ECF")
        //{
        //    return RedirectToAction("Extracounter", "Postprocess", new { param1 = id, param2 = id });

        //}

    }
}