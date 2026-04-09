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
        public async Task<IActionResult> Continue(string Id, string renid)
        {
            var renewal = _db.Renewals.Where(i => i.Id == Id).FirstOrDefault();
            if (renewal == null)
            {
                TempData["error"] = "Renewal record could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var renewalEligibility = GetRenewalEligibility(renewal.ApplicationId);
            if (!renewalEligibility.IsEligible)
            {
                TempData["error"] = renewalEligibility.WarningMessage;
                return RedirectToAction("Renewal", "Postprocess", new { id = renewal.ApplicationId, process = "RNW", renid = renewal.Id });
            }

            if (string.IsNullOrWhiteSpace(renewal.CertifiedLicense) || string.IsNullOrWhiteSpace(renewal.HealthCert))
            {
                TempData["error"] = "Upload the required renewal documents before submitting.";
                return RedirectToAction("Renewal", "Postprocess", new { id = renewal.ApplicationId, process = "RNW", renid = renewal.Id });
            }

            var payment = GetLatestRenewalPaymentForDraft(renewal);

            var paymentCompleted = payment != null
                && HasPaymentStatus(payment, "Paid");

            if (!paymentCompleted)
            {
                TempData["error"] = "Complete payment before submitting the renewal.";
                return RedirectToAction("Renewal", "Postprocess", new { id = renewal.ApplicationId, process = "RNW", renid = renewal.Id });
            }

            if (string.Equals(renewal.Status, "submitted", StringComparison.OrdinalIgnoreCase)
                || string.Equals(renewal.Status, "verified", StringComparison.OrdinalIgnoreCase)
                || string.Equals(renewal.Status, "renewed", StringComparison.OrdinalIgnoreCase))
            {
                TempData["success"] = "This renewal has already been submitted.";
                return RedirectToAction("Dashboard", "Home");
            }

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
            tasks.ExaminationStatus = "verification";
            tasks.Service = "renewal";
            tasks.VerifierId = verifierWithLeastTasks;
            tasks.AssignerId = "system";
            tasks.Status = "assigned";
            tasks.DateAdded = DateTime.Now;
            tasks.DateUpdated = DateTime.Now;
            _db.Add(tasks);
            _db.SaveChanges();


            //renewal.Status = "renewal inspection"; to change after getting inpection requirements.
            renewal.Status = "submitted";
            renewal.PaymentStatus = payment.PaymentStatus ?? payment.Status;
            renewal.Verifier = verifierWithLeastTasks;
            renewal.DateUpdated = DateTime.Now;
            _db.Update(renewal);
            _db.SaveChanges();

            return RedirectToAction("Dashboard", "Home");

        }

            [HttpGet("Renewal")]
        public IActionResult Renewal(string id, string process, string renid)
        {
            var checksub = _db.Renewals.Where(a => a.ApplicationId == id && a.Status == "submitted").FirstOrDefault();
            //  var serv = _db.PostFormationFees.Where(a => a.Code == process).FirstOrDefault();

            var appinfo = _db.ApplicationInfo.Where(b => b.Id == id).FirstOrDefault();
            if (appinfo == null)
            {
                TempData["error"] = "Application information could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var renewalEligibility = RenewalEligibilityHelper.Evaluate(appinfo.ExpiryDate, DateTime.Now);
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
            var renewaldata = GetInProgressRenewalDraft(id);


            Payments payment = null;

            var previousinpections = _db.Renewals.Where(x => x.ApplicationId == id && x.Status == "submitted").ToList();


            var paymentTrans = renewaldata == null ? null : GetLatestRenewalPaymentForDraft(renewaldata);
            if (paymentTrans != null)
            {
                if (!string.IsNullOrWhiteSpace(paymentTrans.PollUrl))
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
                }

                payment = paymentTrans;
            }

            var inspectiondata = _db.Renewals.Where(x => x.ApplicationId == id && x.Status == "submitted").OrderByDescending(s => s.DateApplied).FirstOrDefault();

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
            ViewBag.Checksub = checksub;
            ViewBag.IsRenewalEligible = renewalEligibility.IsEligible;
            ViewBag.RenewalEligibilityWarning = renewalEligibility.WarningMessage;
            return View();
        }

        [HttpPost("PostRenenwals")]
        public async Task<IActionResult> PostRenDocsAsync(Renewals renewal, IFormFile prevcert, IFormFile healthcert)
        {
            var renewalEligibility = GetRenewalEligibility(renewal.ApplicationId);
            if (!renewalEligibility.IsEligible)
            {
                TempData["error"] = renewalEligibility.WarningMessage;
                return RedirectToAction("Renewal", "Postprocess", new { id = renewal.ApplicationId, process = "RNW", renid = renewal.Id });
            }

            if(renewal.Id == null) {
            var existingDraft = GetInProgressRenewalDraft(renewal.ApplicationId);
            if (existingDraft != null)
            {
                TempData["error"] = "A renewal transaction is already open. Complete payment on the current draft before starting another one.";
                return RedirectToAction("Renewal", "Postprocess", new { id = existingDraft.ApplicationId, process = "RNW", renid = existingDraft.Id });
            }

            renewal.Id = Guid.NewGuid().ToString();
            renewal.Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, "RNW");
                 

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            renewal.UserId = id;
            renewal.Status = "inprogress";
            renewal.PaymentStatus = "Not Paid";
            renewal.DateApplied = DateTime.Now;
            renewal.DateUpdated = DateTime.Now;
           

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

                return RedirectToAction("Renewal", "Postprocess", new { id = renewal.ApplicationId, process= "RNW", renid = renewal.Id });
            }
            else
            {
                var updaterenewal = _db.Renewals.Where(a => a.Id == renewal.Id).FirstOrDefault();
                if (updaterenewal == null)
                {
                    TempData["error"] = "Renewal record could not be found.";
                    return RedirectToAction("Dashboard", "Home");
                }
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

                if (string.IsNullOrWhiteSpace(updaterenewal.Reference))
                {
                    updaterenewal.Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, "RNW");
                }

                updaterenewal.DateUpdated = DateTime.Now;
                _db.Update(updaterenewal);
                    _db.SaveChanges();

                    return RedirectToAction("Renewal", "Postprocess", new { id = updaterenewal.ApplicationId , process= "RNW"});


                   // return RedirectToAction("", "");
            }
        }



        [HttpGet("PaynowRenewal")]
        public async Task<IActionResult> PaynowPaymentAsync(string Id, double amount, string service, string process, string renid )
        {
            var renewalEligibility = GetRenewalEligibility(Id);
            if (!renewalEligibility.IsEligible)
            {
                TempData["error"] = renewalEligibility.WarningMessage;
                return RedirectToAction("Renewal", "Postprocess", new { id = Id, process = "RNW", renid });
            }

            var renewalDraft = _db.Renewals
                .Where(renewal => renewal.Id == renid && renewal.ApplicationId == Id)
                .FirstOrDefault();

            if (renewalDraft == null)
            {
                TempData["error"] = "Renewal transaction could not be found.";
                return RedirectToAction("Renewal", "Postprocess", new { id = Id, process = "RNW", renid });
            }

            if (!string.Equals(renewalDraft.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This renewal transaction is no longer open for payment.";
                return RedirectToAction("Renewal", "Postprocess", new { id = Id, process = "RNW", renid });
            }

            var existingTransaction = GetLatestRenewalPaymentForDraft(renewalDraft);

            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                TempData["success"] = "This renewal transaction has already been paid for.";
                return RedirectToAction("Renewal", "Postprocess", new { id = Id, process = "RNW", renid });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = "Complete the current renewal payment before starting another one.";

                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("Renewal", "Postprocess", new { id = Id, process = "RNW", renid });
            }

            //Id = "84aecb8d-4ec2-4ad5-86e8-971070a66b00";
            //amount = 55.7;
            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");

            var callbackUrl = Url.Action("Renewal", "Postprocess", new { id = Id, process, renid }, Request.Scheme);
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            // The return url can be set at later stages. You might want to do this if you want to pass data to the return url (like the reference of the transaction)


            // Create a new payment 
            var payment = paynow.CreatePayment("12345");

            //payment.AuthEmail = "chimukaoliver@gmail.com";
            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var licenseType = _db.LicenseTypes.Where(s => s.Id == applicationInfo.LicenseTypeID).FirstOrDefault();

            // Add items to the payment
            payment.Add(licenseType.LicenseName, (decimal)amount);

            // Send payment to paynow
            var response = paynow.Send(payment);

            // Check if payment was sent without error
            if (response.Success())
            {
                // Get the url to redirect the user to so they can make payment
                Payments transaction = new Payments();
                transaction.Id = Guid.NewGuid().ToString();

                var userId = await userManager.FindByEmailAsync(User.Identity.Name);
                string id = userId.Id;
                transaction.UserId = id;
                transaction.Amount = payment.Total;
                transaction.ApplicationId = Id;
                transaction.Service = service;
                //   transaction.PaynowRef = payment.Reference;
                transaction.PollUrl = response.PollUrl();
                transaction.PopDoc = "";
                transaction.SystemRef = response.RedirectLink();
                transaction.Status = "not paid";
                transaction.DateAdded = DateTime.Now;
                transaction.DateUpdated = DateTime.Now;

                var pollUrl = response.PollUrl();
                var status = paynow.PollTransaction(pollUrl);

                var statusdata = status.GetData();
                transaction.PaynowRef = statusdata["paynowreference"];
                transaction.PaymentStatus = statusdata["status"];

                _db.Add(transaction);
                _db.SaveChanges();
                // [1]	{ [paynowreference, 17967752]}
                //transaction.PaymentStatus = payment.st


                var link = transaction.SystemRef;


                // Get the poll url of the transaction

                // var instructions = response.
                return Redirect(link);
            }


            return View();
        }

        private static bool IsActivePaymentTransaction(Payments payment)
        {
            return !HasPaymentStatus(payment, "Paid")
                && !HasPaymentStatus(payment, "Cancelled")
                && !HasPaymentStatus(payment, "Rejected")
                && !HasPaymentStatus(payment, "Expired");
        }

        private Renewals GetInProgressRenewalDraft(string applicationId)
        {
            return _db.Renewals
                .Where(renewal => renewal.ApplicationId == applicationId && renewal.Status == "inprogress")
                .OrderByDescending(renewal => renewal.DateApplied)
                .FirstOrDefault();
        }

        private RenewalEligibilityResult GetRenewalEligibility(string? applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return RenewalEligibilityHelper.Evaluate(null, DateTime.Now);
            }

            var applicationInfo = _db.ApplicationInfo
                .Where(application => application.Id == applicationId)
                .FirstOrDefault();

            return RenewalEligibilityHelper.Evaluate(applicationInfo?.ExpiryDate, DateTime.Now);
        }

        private Payments GetLatestRenewalPaymentForDraft(Renewals renewal)
        {
            var paymentCutoff = GetRenewalPaymentCutoff(renewal);

            return _db.Payments
                .Where(payment => payment.ApplicationId == renewal.ApplicationId
                    && payment.Service == "renewal"
                    && payment.DateAdded >= paymentCutoff)
                .OrderByDescending(payment => payment.DateAdded)
                .FirstOrDefault();
        }

        private static DateTime GetRenewalPaymentCutoff(Renewals renewal)
        {
            if (renewal.DateApplied > DateTime.MinValue)
            {
                return renewal.DateApplied;
            }

            if (renewal.DateUpdated > DateTime.MinValue)
            {
                return renewal.DateUpdated;
            }

            return DateTime.MinValue;
        }

        private static bool HasPaymentStatus(Payments payment, string expectedStatus)
        {
            return string.Equals(payment.Status, expectedStatus, StringComparison.OrdinalIgnoreCase)
                || string.Equals(payment.PaymentStatus, expectedStatus, StringComparison.OrdinalIgnoreCase);
        }

        private double? GetExtendedHoursFee()
        {
            return _db.PostFormationFees
                .Where(fee => fee.ProcessName == "Extended Hours")
                .Select(fee => (double?)fee.Fee)
                .FirstOrDefault();
        }

        private List<ExtendedHours> GetExtendedHoursApplications(string applicationId)
        {
            return _db.ExtendedHours
                .Where(application => application.ApplicationId == applicationId)
                .OrderByDescending(application => application.DateAdded)
                .ToList();
        }

        private ExtendedHours GetLatestActiveExtendedHoursApplication(string applicationId)
        {
            return _db.ExtendedHours
                .Where(application => application.ApplicationId == applicationId
                    && application.Status != null
                    && application.Status != "Approved"
                    && application.Status != "Rejected")
                .OrderByDescending(application => application.DateAdded)
                .FirstOrDefault();
        }

        private ExtendedHours GetLatestExtendedHoursApplication(string applicationId)
        {
            return _db.ExtendedHours
                .Where(application => application.ApplicationId == applicationId)
                .OrderByDescending(application => application.DateAdded)
                .FirstOrDefault();
        }

        private Payments GetLatestExtendedHoursPaymentForRecord(ExtendedHours extendedHours)
        {
            if (extendedHours == null || string.IsNullOrWhiteSpace(extendedHours.Id))
            {
                return null;
            }

            return _db.Payments
                .Where(payment => payment.ApplicationId == extendedHours.Id
                    && payment.Service == "extended hours")
                .OrderByDescending(payment => payment.DateAdded)
                .FirstOrDefault();
        }

        private Payments RefreshExtendedHoursPaymentStatus(ExtendedHours extendedHours)
        {
            var payment = GetLatestExtendedHoursPaymentForRecord(extendedHours);
            if (payment == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(payment.PollUrl))
            {
                var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
                var status = paynow.PollTransaction(payment.PollUrl);
                var statusdata = status.GetData();
                payment.PaynowRef = statusdata["paynowreference"];
                payment.PaymentStatus = statusdata["status"];
                payment.Status = statusdata["status"];
                payment.DateUpdated = DateTime.Now;
                _db.Update(payment);
            }

            extendedHours.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? extendedHours.PaymentStatus;
            extendedHours.DateUpdated = DateTime.Now;
            _db.Update(extendedHours);
            _db.SaveChanges();

            return payment;
        }

        private double? GetTemporaryRetailFee()
        {
            return _db.PostFormationFees
                .Where(fee => fee.ProcessName == "Temporary Retail")
                .Select(fee => (double?)fee.Fee)
                .FirstOrDefault();
        }

        private TemporaryRetails GetLatestActiveTemporaryRetailApplication(string applicationId)
        {
            return _db.TemporaryRetails
                .Where(application => application.ApplicationId == applicationId
                    && application.Status != null
                    && application.Status != "Approved"
                    && application.Status != "Rejected")
                .OrderByDescending(application => application.DateAdded)
                .FirstOrDefault();
        }

        private TemporaryRetails GetLatestTemporaryRetailApplication(string applicationId)
        {
            return _db.TemporaryRetails
                .Where(application => application.ApplicationId == applicationId)
                .OrderByDescending(application => application.DateAdded)
                .FirstOrDefault();
        }

        private Payments GetLatestTemporaryRetailPaymentForRecord(TemporaryRetails temporaryRetail)
        {
            if (temporaryRetail == null || string.IsNullOrWhiteSpace(temporaryRetail.Id))
            {
                return null;
            }

            return _db.Payments
                .Where(payment => payment.ApplicationId == temporaryRetail.Id
                    && (payment.Service == "temporary retails"
                        || payment.Service == "Temporary Retails"))
                .OrderByDescending(payment => payment.DateAdded)
                .FirstOrDefault();
        }

        private Payments RefreshTemporaryRetailPaymentStatus(TemporaryRetails temporaryRetail)
        {
            var payment = GetLatestTemporaryRetailPaymentForRecord(temporaryRetail);
            if (payment == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(payment.PollUrl))
            {
                var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
                var status = paynow.PollTransaction(payment.PollUrl);
                var statusdata = status.GetData();
                payment.PaynowRef = statusdata["paynowreference"];
                payment.PaymentStatus = statusdata["status"];
                payment.Status = statusdata["status"];
                payment.DateUpdated = DateTime.Now;
                _db.Update(payment);
            }

            temporaryRetail.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? temporaryRetail.PaymentStatus;
            temporaryRetail.DateUpdated = DateTime.Now;
            _db.Update(temporaryRetail);
            _db.SaveChanges();

            return payment;
        }


        [HttpGet("DeleteHealthCert")]
        public IActionResult DeleteHealthCert(string Id, string process)
        {
            var renewalEligibility = GetRenewalEligibility(Id);
            if (!renewalEligibility.IsEligible)
            {
                TempData["error"] = renewalEligibility.WarningMessage;
                return RedirectToAction("Renewal", "Postprocess", new { Id = Id, process = "RNW" });
            }

            var renewaldata = _db.Renewals.Where(x => x.ApplicationId == Id && x.Status == "inprogress").OrderByDescending(s => s.DateApplied).FirstOrDefault();
            if (renewaldata == null)
            {
                TempData["error"] = "No editable renewal draft was found.";
                return RedirectToAction("Renewal", "Postprocess", new { Id = Id, process = "RNW" });
            }
            renewaldata.HealthCert = "";
            renewaldata.DateUpdated = DateTime.Now;
            _db.Update(renewaldata);
            _db.SaveChanges();
            return RedirectToAction("Renewal", "Postprocess", new { Id = Id , process ="RNW"});
        }

        [HttpGet("DeleteCertifiedLisc")]
        public IActionResult DeleteCertifiedLisc(string Id, string process)
        {
            var renewalEligibility = GetRenewalEligibility(Id);
            if (!renewalEligibility.IsEligible)
            {
                TempData["error"] = renewalEligibility.WarningMessage;
                return RedirectToAction("Renewal", "Postprocess", new { Id = Id, process = "RNW" });
            }

            var renewaldata = _db.Renewals.Where(x => x.ApplicationId == Id && x.Status == "inprogress").OrderByDescending(s => s.DateApplied).FirstOrDefault();
            if (renewaldata == null)
            {
                TempData["error"] = "No editable renewal draft was found.";
                return RedirectToAction("Renewal", "Postprocess", new { Id = Id, process = "RNW" });
            }
            renewaldata.CertifiedLicense = "";
            renewaldata.DateUpdated = DateTime.Now;
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
            //payments loop sorted by inspection table status trick
            var previousinpections = _db.Inspection.Where(x => x.ApplicationId == id && x.Status == "submitted").ToList();

            //List<Inspection> inspectionsin = new List<Inspection>();

            //foreach(var previousinpectionslist in previousinpections)
            //{
            //    if(previousinpectionslist.Status == "submitted")
            //    {
            //        inspectionsin.Add(previousinpectionslist);
            //    }

            //}
            //get them into a list
            
           
                var paymentTrans = _db.Payments.Where(s => s.ApplicationId == id && s.Service == "inspection").OrderByDescending(x => x.DateAdded).FirstOrDefault();
                if (paymentTrans == null)
                {

                }
                else
                {


                if (previousinpections.Count <= 1)
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
                else
                {
                }
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
            newinspection.Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, "INP");
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
            tasks.ExaminationStatus = "verification";
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




        [HttpGet("ExtendedHours")]
        public IActionResult ExtendedHours(string id, string process, string? extId)
        {
            var appinfo = _db.ApplicationInfo.Where(b => b.Id == id).FirstOrDefault();
            if (appinfo == null)
            {
                TempData["error"] = "Application information could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }
            

            var mainlicense = _db.LicenseTypes.Where(z => z.Id == appinfo.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(d => d.Id == appinfo.ApplicationType).FirstOrDefault();
            var inspectionFees = _db.PostFormationFees.Where(a => a.ProcessName == "Extended Hours").FirstOrDefault();
            if (inspectionFees == null)
            {
                TempData["error"] = "The extended hours fee has not been configured.";
                return RedirectToAction("Dashboard", "Home");
            }

            var outletinfo = _db.OutletInfo.Where(c => c.ApplicationId == id && c.Status == "active").FirstOrDefault();
            var extendedHoursRecords = GetExtendedHoursApplications(id);
            var selectedExtendedHours = !string.IsNullOrWhiteSpace(extId)
                ? extendedHoursRecords.FirstOrDefault(record => record.Id == extId)
                : GetLatestActiveExtendedHoursApplication(id) ?? extendedHoursRecords.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(extId) && selectedExtendedHours == null)
            {
                TempData["error"] = "The selected extended hours application could not be found.";
                return RedirectToAction("ExtendedHours", new { id, process = "EXH" });
            }

            var payment = selectedExtendedHours == null
                ? null
                : RefreshExtendedHoursPaymentStatus(selectedExtendedHours);

            var isDraftPaid = payment != null && HasPaymentStatus(payment, "Paid");
            var hasSubmittedApplication = selectedExtendedHours != null
                && string.Equals(selectedExtendedHours.Status, "submitted", StringComparison.OrdinalIgnoreCase);

            var getFee = inspectionFees.Fee;
            ViewBag.License = mainlicense;
            ViewBag.Region = licenseRegion;
            ViewBag.Payment = payment;
            ViewBag.Process = process;
            ViewBag.Fee = getFee;
            ViewBag.TotalFee = getFee;
            ViewBag.Outletinfo = outletinfo;
            ViewBag.Appinfo = appinfo;
            ViewBag.ServeFee = getFee;
            ViewBag.ExtendedHoursRecord = selectedExtendedHours;
            ViewBag.ExtendedHoursRecords = extendedHoursRecords;
            ViewBag.IsExtendedHoursPaid = isDraftPaid;
            ViewBag.CanStartNewExtendedHours = true;
            ViewBag.CanSubmitExtendedHours = selectedExtendedHours != null
                && string.Equals(selectedExtendedHours.Status, "inprogress", StringComparison.OrdinalIgnoreCase)
                && isDraftPaid;
            ViewBag.IsExtendedHoursSubmitted = hasSubmittedApplication;
            ViewBag.ExtendedHoursWarning = hasSubmittedApplication
                ? "This extended hours application has already been submitted and is awaiting secretary review."
                : null;

            return View();
        }

        [HttpPost("SaveExtendedHours")]
        public IActionResult SaveExtendedHoursAsync(string? extId, string applicationId, DateTime extendedHoursDate, string reasonForExtention)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                TempData["error"] = "The application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (extendedHoursDate == default)
            {
                TempData["error"] = "Select the extended hours date before saving the application.";
                return RedirectToAction("ExtendedHours", new { id = applicationId, process = "EXH" });
            }

            if (string.IsNullOrWhiteSpace(reasonForExtention))
            {
                TempData["error"] = "Provide a justification for the extended hours application.";
                return RedirectToAction("ExtendedHours", new { id = applicationId, process = "EXH" });
            }

            var fee = GetExtendedHoursFee();
            if (fee == null)
            {
                TempData["error"] = "The extended hours fee has not been configured.";
                return RedirectToAction("ExtendedHours", new { id = applicationId, process = "EXH" });
            }

            var currentUserId = userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!string.IsNullOrWhiteSpace(extId))
            {
                TempData["error"] = "Initialize each extended hours request as a separate application so it keeps its own details and payment.";
                return RedirectToAction("ExtendedHours", new { id = applicationId, process = "EXH", extId });
            }

            ExtendedHours draft = new ExtendedHours
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUserId,
                Status = "inprogress",
                Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, "EXH"),
                ApplicationId = applicationId,
                PaidFee = fee.Value,
                PaymentStatus = "Not Paid",
                HoursOfExtension = string.Empty,
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };

            _db.Add(draft);

            var currentPayment = GetLatestExtendedHoursPaymentForRecord(draft);

            draft.UserId = currentUserId;
            draft.ExtendedHoursDate = extendedHoursDate;
            draft.ReasonForExtention = reasonForExtention.Trim();
            draft.PaidFee = fee.Value;
            draft.DateUpdated = DateTime.Now;
            draft.PaymentStatus = currentPayment != null && HasPaymentStatus(currentPayment, "Paid")
                ? (currentPayment.PaymentStatus ?? currentPayment.Status ?? "Paid")
                : "Not Paid";

            if (string.IsNullOrWhiteSpace(draft.Reference))
            {
                draft.Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, "EXH");
            }

            _db.SaveChanges();

            TempData["success"] = "Extended hours application initialized. Make payment to continue.";
            return RedirectToAction("ExtendedHours", new { id = applicationId, process = "EXH", extId = draft.Id });
        }

        [HttpPost("SubmitExtendedHours")]
        public async Task<IActionResult> SubmitExtendedHoursAsync(string extId)
        {
            var extendedHours = _db.ExtendedHours.Where(a => a.Id == extId).FirstOrDefault();
            if (extendedHours == null)
            {
                TempData["error"] = "The extended hours application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (string.Equals(extendedHours.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["success"] = "This extended hours application has already been submitted.";
                return RedirectToAction("ExtendedHours", new { id = extendedHours.ApplicationId, process = "EXH", extId = extendedHours.Id });
            }

            if (!string.Equals(extendedHours.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This extended hours application is no longer open for submission.";
                return RedirectToAction("ExtendedHours", new { id = extendedHours.ApplicationId, process = "EXH", extId = extendedHours.Id });
            }

            if (extendedHours.ExtendedHoursDate == default || string.IsNullOrWhiteSpace(extendedHours.ReasonForExtention))
            {
                TempData["error"] = "Complete the extended hours date and justification before submitting.";
                return RedirectToAction("ExtendedHours", new { id = extendedHours.ApplicationId, process = "EXH", extId = extendedHours.Id });
            }

            var payment = GetLatestExtendedHoursPaymentForRecord(extendedHours);
            if (payment == null || !HasPaymentStatus(payment, "Paid"))
            {
                TempData["error"] = "Complete payment before submitting the extended hours application.";
                return RedirectToAction("ExtendedHours", new { id = extendedHours.ApplicationId, process = "EXH", extId = extendedHours.Id });
            }

            var existingTask = _db.Tasks
                .Where(task => task.ApplicationId == extendedHours.Id
                    && task.Service == "Extended Hours"
                    && task.Status == "assigned")
                .FirstOrDefault();

            if (existingTask == null)
            {
                var secretaryId = await _taskAllocationHelper.GetSecretary(_db, userManager);
                if (string.IsNullOrWhiteSpace(secretaryId))
                {
                    TempData["error"] = "No secretary is currently available to receive the extended hours application.";
                    return RedirectToAction("ExtendedHours", new { id = extendedHours.ApplicationId, process = "EXH", extId = extendedHours.Id });
                }

                Tasks task = new Tasks();
                task.Id = Guid.NewGuid().ToString();
                task.ApplicationId = extendedHours.Id;
                task.ApproverId = secretaryId;
                task.AssignerId = "system";
                task.Service = "Extended Hours";
                task.ExaminationStatus = "approval";
                task.Status = "assigned";
                task.DateAdded = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Add(task);
            }

            extendedHours.Status = "submitted";
            extendedHours.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? "Paid";
            extendedHours.DateUpdated = DateTime.Now;
            _db.Update(extendedHours);
            _db.SaveChanges();

            TempData["success"] = "Extended hours application submitted successfully and sent to the secretary.";
            return RedirectToAction("ExtendedHours", new { id = extendedHours.ApplicationId, process = "EXH", extId = extendedHours.Id });
        }


        [HttpPost("PostExtendedHours")]
        public async Task<IActionResult> PostExtendedHoursAsync(string ExtId, DateTime ExtendedHoursDate, string ReasonForExtention)
        {

            // updating extended hours
            var extapplication = _db.ExtendedHours.Where(a => a.Id == ExtId).FirstOrDefault();
            extapplication.Status = "Submitted";
            extapplication.ExtendedHoursDate = ExtendedHoursDate;
            extapplication.ReasonForExtention = ReasonForExtention;
            _db.Update(extapplication);
            _db.SaveChanges();


            Tasks tasks = new Tasks();
            tasks.Id = Guid.NewGuid().ToString();
            tasks.ApplicationId = ExtId;
            var secretaryrWithLeastTasks = await _taskAllocationHelper.GetSecretary(_db, userManager);
            //   tasks.VerifierId = selectedUser.Id;
            tasks.Service = "Extended Hours";
            tasks.ApproverId = secretaryrWithLeastTasks;
            tasks.AssignerId = "system";
            tasks.Status = "assigned";
            tasks.DateAdded = DateTime.Now;
            tasks.DateUpdated = DateTime.Now;
            _db.Add(tasks);
            _db.SaveChanges();

            return RedirectToAction("Dashboard", "Home");
        }


        [HttpGet("ExtendedHoursPayment")]
        public async Task<IActionResult> ExtendedHoursPaymentAsync(string id, double amount, string service, string process, string? extId)
        {
            var fee = GetExtendedHoursFee();
            if (fee == null)
            {
                TempData["error"] = "The extended hours fee has not been configured.";
                return RedirectToAction("ExtendedHours", new { id, process = "EXH" });
            }

            var extendedHours = string.IsNullOrWhiteSpace(extId)
                ? GetLatestActiveExtendedHoursApplication(id)
                : _db.ExtendedHours.Where(a => a.Id == extId && a.ApplicationId == id).FirstOrDefault();

            if (extendedHours == null)
            {
                TempData["error"] = "Save the extended hours application first before making payment.";
                return RedirectToAction("ExtendedHours", new { id, process = "EXH" });
            }

            if (!string.Equals(extendedHours.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This extended hours application is no longer open for payment.";
                return RedirectToAction("ExtendedHours", new { id, process = "EXH", extId = extendedHours.Id });
            }

            if (extendedHours.ExtendedHoursDate == default || string.IsNullOrWhiteSpace(extendedHours.ReasonForExtention))
            {
                TempData["error"] = "Save the extended hours date and justification before making payment.";
                return RedirectToAction("ExtendedHours", new { id, process = "EXH", extId = extendedHours.Id });
            }

            var existingTransaction = GetLatestExtendedHoursPaymentForRecord(extendedHours);
            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                extendedHours.PaymentStatus = existingTransaction.PaymentStatus ?? existingTransaction.Status ?? "Paid";
                extendedHours.DateUpdated = DateTime.Now;
                _db.Update(extendedHours);
                _db.SaveChanges();

                TempData["success"] = "This extended hours application has already been paid for.";
                return RedirectToAction("ExtendedHours", new { id, process = "EXH", extId = extendedHours.Id });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = "Complete the current extended hours payment before starting another one.";

                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("ExtendedHours", new { id, process = "EXH", extId = extendedHours.Id });
            }

            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
            var callbackUrl = Url.Action("ExtendedHours", "Postprocess", new { id, process = "EXH", extId = extendedHours.Id }, Request.Scheme);
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            // Create a new payment 
            var payment = paynow.CreatePayment("12345");

            //payment.AuthEmail = "chimukaoliver@gmail.com";
            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == id).FirstOrDefault();
            var licenseType = _db.LicenseTypes.Where(s => s.Id == applicationInfo.LicenseTypeID).FirstOrDefault();

            // Add items to the payment
            payment.Add(licenseType.LicenseName, (decimal)fee.Value);

            // Send payment to paynow
            var response = paynow.Send(payment);

            // Check if payment was sent without error
            if (response.Success())
            {
                // Get the url to redirect the user to so they can make payment
                Payments transaction = new Payments();
                transaction.Id = Guid.NewGuid().ToString();

                transaction.UserId = userManager.GetUserId(User);
                transaction.Amount = payment.Total;
                transaction.ApplicationId = extendedHours.Id;
                transaction.Service = "extended hours";
                transaction.PollUrl = response.PollUrl();
                transaction.PopDoc = "";
                transaction.SystemRef = response.RedirectLink();
                transaction.Status = "not paid";
                transaction.DateAdded = DateTime.Now;
                transaction.DateUpdated = DateTime.Now;

                var pollUrl = response.PollUrl();
                var status = paynow.PollTransaction(pollUrl);

                var statusdata = status.GetData();
                transaction.PaynowRef = statusdata["paynowreference"];
                transaction.PaymentStatus = statusdata["status"];

                _db.Add(transaction);
                extendedHours.PaidFee = fee.Value;
                extendedHours.PaymentStatus = transaction.PaymentStatus ?? transaction.Status ?? "Not Paid";
                extendedHours.DateUpdated = DateTime.Now;
                _db.Update(extendedHours);
                _db.SaveChanges();


                return Redirect(transaction.SystemRef);
            }

            TempData["error"] = "The extended hours payment request could not be sent.";
            return RedirectToAction("ExtendedHours", new { id, process = "EXH", extId = extendedHours.Id });
        }










        [HttpGet("TemporaryRetails")]
        public IActionResult TemporaryRetails(string id, string process)
        {
            var appinfo = _db.ApplicationInfo.Where(b => b.Id == id).FirstOrDefault();
            if (appinfo == null)
            {
                TempData["error"] = "Application information could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            var mainlicense = _db.LicenseTypes.Where(z => z.Id == appinfo.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(d => d.Id == appinfo.ApplicationType).FirstOrDefault();
            var temporaryRetailFees = _db.PostFormationFees.Where(a => a.ProcessName == "Temporary Retail").FirstOrDefault();
            if (temporaryRetailFees == null)
            {
                TempData["error"] = "The temporary retail fee has not been configured.";
                return RedirectToAction("Dashboard", "Home");
            }

            var outletinfo = _db.OutletInfo.Where(c => c.ApplicationId == id && c.Status == "active").FirstOrDefault();
            var activeTemporaryRetail = GetLatestActiveTemporaryRetailApplication(id);
            var latestTemporaryRetail = activeTemporaryRetail ?? GetLatestTemporaryRetailApplication(id);
            var payment = activeTemporaryRetail == null
                ? null
                : RefreshTemporaryRetailPaymentStatus(activeTemporaryRetail);

            var isDraftPaid = payment != null && HasPaymentStatus(payment, "Paid");
            var hasSubmittedApplication = activeTemporaryRetail != null
                && string.Equals(activeTemporaryRetail.Status, "submitted", StringComparison.OrdinalIgnoreCase);

            var getFee = temporaryRetailFees.Fee;
            ViewBag.License = mainlicense;
            ViewBag.Region = licenseRegion;
            ViewBag.Payment = payment;
            ViewBag.Process = process;
            ViewBag.Fee = getFee;
            ViewBag.TotalFee = getFee;
            ViewBag.Outletinfo = outletinfo;
            ViewBag.Appinfo = appinfo;
            ViewBag.ServeFee = getFee;
            ViewBag.TemporaryRetailRecord = latestTemporaryRetail;
            ViewBag.ActiveTemporaryRetail = activeTemporaryRetail;
            ViewBag.IsTemporaryRetailPaid = isDraftPaid;
            ViewBag.CanStartNewTemporaryRetail = activeTemporaryRetail == null;
            ViewBag.CanSubmitTemporaryRetail = activeTemporaryRetail != null
                && string.Equals(activeTemporaryRetail.Status, "inprogress", StringComparison.OrdinalIgnoreCase)
                && isDraftPaid;
            ViewBag.IsTemporaryRetailSubmitted = hasSubmittedApplication;
            ViewBag.TemporaryRetailWarning = hasSubmittedApplication
                ? "This temporary retail application has already been submitted and is awaiting secretary review."
                : null;

            return View();
        }

        [HttpPost("SaveTemporaryRetails")]
        public IActionResult SaveTemporaryRetailsAsync(string applicationId, DateTime temporaryRetailsDate, string reasonForExtention, string locationAddress)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                TempData["error"] = "The application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (temporaryRetailsDate == default)
            {
                TempData["error"] = "Select the temporary retail date before initializing the application.";
                return RedirectToAction("TemporaryRetails", new { id = applicationId, process = "TRL" });
            }

            if (string.IsNullOrWhiteSpace(reasonForExtention))
            {
                TempData["error"] = "Provide a justification for the temporary retail application.";
                return RedirectToAction("TemporaryRetails", new { id = applicationId, process = "TRL" });
            }

            if (string.IsNullOrWhiteSpace(locationAddress))
            {
                TempData["error"] = "Provide the address or location for the temporary retail application.";
                return RedirectToAction("TemporaryRetails", new { id = applicationId, process = "TRL" });
            }

            var fee = GetTemporaryRetailFee();
            if (fee == null)
            {
                TempData["error"] = "The temporary retail fee has not been configured.";
                return RedirectToAction("TemporaryRetails", new { id = applicationId, process = "TRL" });
            }

            var currentUserId = userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var activeTemporaryRetail = GetLatestActiveTemporaryRetailApplication(applicationId);
            if (activeTemporaryRetail != null)
            {
                TempData["error"] = "A temporary retail application has already been initialized for this licence. Complete payment or wait for the current application to be completed before starting a new one.";
                return RedirectToAction("TemporaryRetails", new { id = applicationId, process = "TRL", temporaryRetailId = activeTemporaryRetail.Id });
            }

            TemporaryRetails temporaryRetail = new TemporaryRetails
            {
                Id = Guid.NewGuid().ToString(),
                UserId = currentUserId,
                Status = "inprogress",
                Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, "TRL"),
                ApplicationId = applicationId,
                PaidFee = fee.Value,
                PaymentStatus = "Not Paid",
                ReasonForExtention = reasonForExtention.Trim(),
                LocationAddress = locationAddress.Trim(),
                HoursOfExtension = string.Empty,
                TemporaryRetailsDate = temporaryRetailsDate,
                DateAdded = DateTime.Now,
                DateUpdated = DateTime.Now
            };

            _db.Add(temporaryRetail);
            _db.SaveChanges();

            TempData["success"] = "Temporary retail application initialized. Make payment to continue.";
            return RedirectToAction("TemporaryRetails", new { id = applicationId, process = "TRL", temporaryRetailId = temporaryRetail.Id });
        }

        [HttpPost("SubmitTemporaryRetails")]
        public async Task<IActionResult> SubmitTemporaryRetailsAsync(string temporaryRetailId)
        {
            var temporaryRetail = _db.TemporaryRetails.Where(a => a.Id == temporaryRetailId).FirstOrDefault();
            if (temporaryRetail == null)
            {
                TempData["error"] = "The temporary retail application could not be found.";
                return RedirectToAction("Dashboard", "Home");
            }

            if (string.Equals(temporaryRetail.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            {
                TempData["success"] = "This temporary retail application has already been submitted.";
                return RedirectToAction("TemporaryRetails", new { id = temporaryRetail.ApplicationId, process = "TRL", temporaryRetailId = temporaryRetail.Id });
            }

            if (!string.Equals(temporaryRetail.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This temporary retail application is no longer open for submission.";
                return RedirectToAction("TemporaryRetails", new { id = temporaryRetail.ApplicationId, process = "TRL", temporaryRetailId = temporaryRetail.Id });
            }

            if (temporaryRetail.TemporaryRetailsDate == default
                || string.IsNullOrWhiteSpace(temporaryRetail.ReasonForExtention)
                || string.IsNullOrWhiteSpace(temporaryRetail.LocationAddress))
            {
                TempData["error"] = "Complete the temporary retail date, address or location, and justification before submitting.";
                return RedirectToAction("TemporaryRetails", new { id = temporaryRetail.ApplicationId, process = "TRL", temporaryRetailId = temporaryRetail.Id });
            }

            var payment = GetLatestTemporaryRetailPaymentForRecord(temporaryRetail);
            if (payment == null || !HasPaymentStatus(payment, "Paid"))
            {
                TempData["error"] = "Complete payment before submitting the temporary retail application.";
                return RedirectToAction("TemporaryRetails", new { id = temporaryRetail.ApplicationId, process = "TRL", temporaryRetailId = temporaryRetail.Id });
            }

            var existingTask = _db.Tasks
                .Where(task => task.ApplicationId == temporaryRetail.Id
                    && task.Service == "Temporary Retails"
                    && task.Status == "assigned")
                .FirstOrDefault();

            if (existingTask == null)
            {
                var secretaryId = await _taskAllocationHelper.GetSecretary(_db, userManager);
                if (string.IsNullOrWhiteSpace(secretaryId))
                {
                    TempData["error"] = "No secretary is currently available to receive the temporary retail application.";
                    return RedirectToAction("TemporaryRetails", new { id = temporaryRetail.ApplicationId, process = "TRL", temporaryRetailId = temporaryRetail.Id });
                }

                Tasks task = new Tasks();
                task.Id = Guid.NewGuid().ToString();
                task.ApplicationId = temporaryRetail.Id;
                task.ApproverId = secretaryId;
                task.AssignerId = "system";
                task.Service = "Temporary Retails";
                task.ExaminationStatus = "approval";
                task.Status = "assigned";
                task.DateAdded = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Add(task);
            }

            temporaryRetail.Status = "submitted";
            temporaryRetail.PaymentStatus = payment.PaymentStatus ?? payment.Status ?? "Paid";
            temporaryRetail.DateUpdated = DateTime.Now;
            _db.Update(temporaryRetail);
            _db.SaveChanges();

            TempData["success"] = "Temporary retail application submitted successfully and sent to the secretary.";
            return RedirectToAction("TemporaryRetails", new { id = temporaryRetail.ApplicationId, process = "TRL", temporaryRetailId = temporaryRetail.Id });
        }

        [HttpGet("TemporaryRetailsPayment")]
        public async Task<IActionResult> TemporaryRetailsPaymentAsync(string id, double amount, string service, string process, string? temporaryRetailId)
        {
            var fee = GetTemporaryRetailFee();
            if (fee == null)
            {
                TempData["error"] = "The temporary retail fee has not been configured.";
                return RedirectToAction("TemporaryRetails", new { id, process = "TRL" });
            }

            var temporaryRetail = string.IsNullOrWhiteSpace(temporaryRetailId)
                ? GetLatestActiveTemporaryRetailApplication(id)
                : _db.TemporaryRetails.Where(a => a.Id == temporaryRetailId && a.ApplicationId == id).FirstOrDefault();

            if (temporaryRetail == null)
            {
                TempData["error"] = "Initialize the temporary retail application first before making payment.";
                return RedirectToAction("TemporaryRetails", new { id, process = "TRL" });
            }

            if (!string.Equals(temporaryRetail.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This temporary retail application is no longer open for payment.";
                return RedirectToAction("TemporaryRetails", new { id, process = "TRL", temporaryRetailId = temporaryRetail.Id });
            }

            if (temporaryRetail.TemporaryRetailsDate == default
                || string.IsNullOrWhiteSpace(temporaryRetail.ReasonForExtention)
                || string.IsNullOrWhiteSpace(temporaryRetail.LocationAddress))
            {
                TempData["error"] = "Initialize the temporary retail date, address or location, and justification before making payment.";
                return RedirectToAction("TemporaryRetails", new { id, process = "TRL", temporaryRetailId = temporaryRetail.Id });
            }

            var existingTransaction = GetLatestTemporaryRetailPaymentForRecord(temporaryRetail);
            if (existingTransaction != null && HasPaymentStatus(existingTransaction, "Paid"))
            {
                temporaryRetail.PaymentStatus = existingTransaction.PaymentStatus ?? existingTransaction.Status ?? "Paid";
                temporaryRetail.DateUpdated = DateTime.Now;
                _db.Update(temporaryRetail);
                _db.SaveChanges();

                TempData["success"] = "This temporary retail application has already been paid for.";
                return RedirectToAction("TemporaryRetails", new { id, process = "TRL", temporaryRetailId = temporaryRetail.Id });
            }

            if (existingTransaction != null && IsActivePaymentTransaction(existingTransaction))
            {
                TempData["error"] = "Complete the current temporary retail payment before starting another one.";

                if (!string.IsNullOrWhiteSpace(existingTransaction.SystemRef))
                {
                    return Redirect(existingTransaction.SystemRef);
                }

                return RedirectToAction("TemporaryRetails", new { id, process = "TRL", temporaryRetailId = temporaryRetail.Id });
            }

            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
            var callbackUrl = Url.Action("TemporaryRetails", "Postprocess", new { id, process = "TRL", temporaryRetailId = temporaryRetail.Id }, Request.Scheme);
            if (!string.IsNullOrWhiteSpace(callbackUrl))
            {
                paynow.ResultUrl = callbackUrl;
                paynow.ReturnUrl = callbackUrl;
            }

            // Create a new payment 
            var payment = paynow.CreatePayment("12345");

            //payment.AuthEmail = "chimukaoliver@gmail.com";
            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == id).FirstOrDefault();
            var licenseType = _db.LicenseTypes.Where(s => s.Id == applicationInfo.LicenseTypeID).FirstOrDefault();

            // Add items to the payment
            payment.Add(licenseType.LicenseName, (decimal)amount);

            // Send payment to paynow
            var response = paynow.Send(payment);

            // Check if payment was sent without error
            if (response.Success())
            {
                // Get the url to redirect the user to so they can make payment
                Payments transaction = new Payments();
                transaction.Id = Guid.NewGuid().ToString();

                transaction.UserId = userManager.GetUserId(User);
                transaction.Amount = payment.Total;
                transaction.ApplicationId = temporaryRetail.Id;
                transaction.Service = "temporary retails";
                transaction.PollUrl = response.PollUrl();
                transaction.PopDoc = "";
                transaction.SystemRef = response.RedirectLink();
                transaction.Status = "not paid";
                transaction.DateAdded = DateTime.Now;
                transaction.DateUpdated = DateTime.Now;

                var pollUrl = response.PollUrl();
                var status = paynow.PollTransaction(pollUrl);

                var statusdata = status.GetData();
                transaction.PaynowRef = statusdata["paynowreference"];
                transaction.PaymentStatus = statusdata["status"];

                _db.Add(transaction);
                temporaryRetail.PaidFee = fee.Value;
                temporaryRetail.PaymentStatus = transaction.PaymentStatus ?? transaction.Status ?? "Not Paid";
                temporaryRetail.DateUpdated = DateTime.Now;
                _db.Update(temporaryRetail);
                _db.SaveChanges();


                return Redirect(transaction.SystemRef);
            }

            TempData["error"] = "The temporary retail payment request could not be sent.";
            return RedirectToAction("TemporaryRetails", new { id, process = "TRL", temporaryRetailId = temporaryRetail.Id });
        }






        [HttpGet("ManagerChange")]
        public async Task<IActionResult> ManagerChange(string id, double amount, string service, string process)
        {
            var cmapplication = _db.ChangeManaager.Where(a => a.ApplicationId == id).OrderByDescending(a => a.DateApplied).FirstOrDefault();
            if(cmapplication == null || cmapplication.Status == null || cmapplication.Status == "complete")
            {

            }
            else
            {
                TempData["Message"] = "Another application in progress";
                return View();
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

            ViewBag.Payment = payment;
            ViewBag.Process = process;
          
            ViewBag.Outletinfo = outletinfo;
            ViewBag.Appinfo = appinfo;
           
            ViewBag.Outletinfo = outletinfo;
            ViewBag.Appinfo = appinfo;
            ViewBag.Managers = managers;

            return View();
        }



        }
}
