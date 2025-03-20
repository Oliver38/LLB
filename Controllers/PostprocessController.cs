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
using LLB.Helpers;

namespace LLB.Controllers
{


    [Route("Postprocess")]
    public class PostprocessController : Controller
    {


        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;
        private readonly TaskAllocationHelper _taskAllocationHelper;
        

        public PostprocessController(TaskAllocationHelper taskAllocationHelper, AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
            _taskAllocationHelper = taskAllocationHelper;
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
        //for submission of renewal
        [HttpGet("Continue")]
        public async Task<IActionResult> Continue(string Id)
        {
            

            //renewal algorithm to work after inspection
            /* if(appinfo.ExpiryDate > DateTime.Now)
            {
                appinfo.ExpiryDate = DateTime.Now.AddYears(1);

            }
            else
            {
                appinfo.ExpiryDate = appinfo.ExpiryDate.AddYears(1);
            }
            */
            //var verifierId = await TaskAllocator()
            Tasks tasks = new Tasks();
            tasks.Id = Guid.NewGuid().ToString();
            tasks.ApplicationId = Id;
            //tasks.AssignerId

            //auto allocation to replace
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");

            var verifierWithLeastTasks = await _taskAllocationHelper.GetVerifier(_db, userManager);
            //   tasks.VerifierId = selectedUser.Id;
            tasks.Service = "renewal";
            tasks.VerifierId = verifierWithLeastTasks;
            tasks.AssignerId = "system";
            tasks.Status = "assigned";
            tasks.DateAdded = DateTime.Now;
            tasks.DateUpdated = DateTime.Now;
            _db.Add(tasks);
            _db.SaveChanges();


            var renewal = _db.Renewals.Where(a => a.Id == Id).FirstOrDefault();
            //renewal.Status = "renewal inspection"; to change after getting inpection requirements.
            renewal.Status = "submitted";
            renewal.Verifier = verifierWithLeastTasks;
            renewal.DateUpdated = DateTime.Now;
            _db.Update(renewal);
            _db.SaveChanges();
            var appinfo = _db.ApplicationInfo.Where(a => a.Id == renewal.ApplicationId).FirstOrDefault();

            return RedirectToAction("Dashboard", "Home");

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
            var renewaldata = _db.Renewals.Where(x => x.ApplicationId == id && x.Status == "applied").OrderByDescending(s => s.DateApplied).FirstOrDefault();
            //submitted check status
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
                renewal.Status = "applied";
           

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
            if (healthcert != null)
            {
                string picb = System.IO.Path.GetFileName(healthcert.FileName);
                string dicb = System.IO.Path.GetExtension(healthcert.FileName);
                string newname = "HealthCertificate_" + renewal.Id;
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

                return RedirectToAction("Renewal", "Postprocess", new { id = renewal.ApplicationId, process= "RNW" });
            }
            else
            {
                var updaterenewal = _db.Renewals.Where(a => a.Id == renewal.Id).FirstOrDefault();
                //renewal.Id = Guid.NewGuid().ToString();

                //  var userId = await userManager.FindByEmailAsync(User.Identity.Name);
                //string id = userId.Id;
                //  renewal.UserId = id;


                //spublic DateTime DateCreated
                //  updaterenewal = renewal;
                if (updaterenewal.CertifiedLicense != "") { }
                else
                {
                    if (prevcert != null)
                    {
                        string picb = System.IO.Path.GetFileName(prevcert.FileName);
                        string dicb = System.IO.Path.GetExtension(prevcert.FileName);
                        string newname = "PreviousCertificate_" + updaterenewal.Id;
                        string path = System.IO.Path.Combine($"Renewals", newname + dicb);
                        string docpath = System.IO.Path.Combine($"wwwroot/Renewals", newname + dicb);
                        updaterenewal.CertifiedLicense = path;
                        using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                        {
                            await prevcert.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        updaterenewal.CertifiedLicense = "";
                    }

                }

                if (updaterenewal.HealthCert != "") { } else { 
                    //spublic DateTime DateCreated
                    if (healthcert != null)
                    {
                        string picb = System.IO.Path.GetFileName(healthcert.FileName);
                        string dicb = System.IO.Path.GetExtension(healthcert.FileName);
                        string newname = "HealthCertificate_" + updaterenewal.Id;
                        string path = System.IO.Path.Combine($"Renewals", newname + dicb);
                        string docpath = System.IO.Path.Combine($"wwwroot/Renewals", newname + dicb);
                    updaterenewal.HealthCert = path;
                        using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                        {
                            await healthcert.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                    updaterenewal.HealthCert = "";
                    }
                }

                _db.Update(updaterenewal);
                    _db.SaveChanges();

                    return RedirectToAction("Renewal", "Postprocess", new { id = updaterenewal.ApplicationId , process= "RNW"});


                   // return RedirectToAction("", "");
            }
        }


        [HttpGet("DeleteHealthCert")]
        public IActionResult DeleteHealthCert(string Id, string process)
        {
            var renewaldata = _db.Renewals.Where(x => x.ApplicationId == Id && x.Status == "applied").OrderByDescending(s => s.DateApplied).FirstOrDefault();
            renewaldata.HealthCert = "";
            _db.Update(renewaldata);
            _db.SaveChanges();
            return RedirectToAction("Renewal", "Postprocess", new { Id = Id , process ="RNW"});
        }

        [HttpGet("DeleteCertifiedLisc")]
        public IActionResult DeleteCertifiedLisc(string Id, string process)
        {
            var renewaldata = _db.Renewals.Where(x => x.ApplicationId == Id && x.Status == "applied").OrderByDescending(s => s.DateApplied).FirstOrDefault();
            renewaldata.CertifiedLicense = "";
            _db.Update(renewaldata);
            _db.SaveChanges();
            return RedirectToAction("Renewal", "Postprocess", new { Id = Id, process = "RNW" });
        }


        [HttpGet("Inspection")]
        public IActionResult Inspection(string id, string process)
        {



            var appinfo = _db.ApplicationInfo.Where(b => b.Id == id).FirstOrDefault();
            var mainlicense = _db.LicenseTypes.Where(z => z.Id == appinfo.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(d => d.Id == appinfo.ApplicationType).FirstOrDefault();
            var InspectionFees = _db.PostFormationFees.Where(a => a.ProcessName == "Inspection Fee").FirstOrDefault();
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

            double getFee = InspectionFees.Fee;

            var totalfee = getFee;

            Payments payment = null;
            var paymentTrans = _db.Payments.Where(s => s.ApplicationId == id && s.Service == "inspection").OrderByDescending(x => x.DateAdded).FirstOrDefault();
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
            var inspectiondata = _db.Inspection.Where(x => x.ApplicationId == id  &&  x.Status == "submitted").OrderByDescending(s => s.DateApplied).FirstOrDefault();
            //check status
           // var inspectiondatastate = 
            //submitted check status
            ViewBag.Payment = payment;
            ViewBag.Process = process;
            ViewBag.Fee = getFee;
           // ViewBag.Penalty = penalty;
            ViewBag.TotalFee = totalfee;
            ViewBag.Months = time;
            ViewBag.Outletinfo = outletinfo;
            ViewBag.Appinfo = appinfo;
            ViewBag.ServeFee = getFee;
            ViewBag.Inspectiondata = inspectiondata;

            return View();
        }

        //PostInspection


        [HttpPost("PostInspection")]
        public async Task<IActionResult> PostInspection(Renewals renewal)
        {
            Inspection newinspection = new Inspection();
            //     [Key]
            //     public string? Id { get; set; }

            //public string? Service { get; set; }
            // public string? Application { get; set; }
            // public string? Status { get; set; }
            // public string? UserId { get; set; }
            // public string? ApplicationId { get; set; }
            // public string? InspectorId { get; set; }
            // public DateTime InspectionDate { get; set; }
            // public DateTime DateApplied { get; set; }
            // public DateTime DateUpdate { get; set; }
            var userId = userManager.GetUserId(User);

            newinspection.Id = Guid.NewGuid().ToString();
            newinspection.Service = renewal.Service;

            newinspection.Status = "submitted";
            newinspection.UserId = userId;
            newinspection.ApplicationId = renewal.ApplicationId;
            newinspection.DateApplied = DateTime.Now;
            newinspection.DateUpdate = DateTime.Now;
            _db.Add(newinspection);
            _db.SaveChanges();

            //var verifierId = await TaskAllocator()
            Tasks tasks = new Tasks();
            tasks.Id = Guid.NewGuid().ToString();
            tasks.ApplicationId = renewal.ApplicationId;
            //tasks.AssignerId

            //auto allocation to replace
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");

            var verifierWithLeastTasks = await _taskAllocationHelper.GetVerifier(_db, userManager);
            //   tasks.VerifierId = selectedUser.Id;
            tasks.Service = "inspection";
            tasks.VerifierId = verifierWithLeastTasks;
            tasks.AssignerId = "system";
            tasks.Status = "assigned";
            tasks.DateAdded = DateTime.Now;
            tasks.DateUpdated = DateTime.Now;
            _db.Add(tasks);
            _db.SaveChanges();



            return RedirectToAction("Inspection", "Postprocess", new { id = renewal.ApplicationId });

            return View();
        }
        }
}