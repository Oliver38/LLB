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
using LLB.Helpers;

namespace LLB.Controllers
{
    
    [Route("")]
    [Route("Extracounter")]
    public class ExtracounterController : Controller
    {
        

        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public ExtracounterController(TaskAllocationHelper taskAllocationHelper, AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
            _taskAllocationHelper = taskAllocationHelper;
        }

        [HttpGet("Extracounter")]
        public IActionResult Extracounter(string id, string process)
        {


            //set tempdata here

            var appinfo = _db.ApplicationInfo.Where(b => b.Id == id).FirstOrDefault();
            var mainlicense = _db.LicenseTypes.Where(z => z.Id == appinfo.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(d => d.Id == appinfo.ApplicationType).FirstOrDefault();
            var ExtracounterFees = _db.PostFormationFees.Where(a => a.ProcessName == "Extra Counter").FirstOrDefault();
            var outletinfo = _db.OutletInfo.Where(c => c.ApplicationId == id && c.Status == "active").FirstOrDefault();

            var extendedhoursdata = _db.ExtendedHours.Where(x => x.ApplicationId == id && x.Status == "Applied").OrderByDescending(s => s.DateAdded).FirstOrDefault();

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

            double getFee = ExtracounterFees.Fee;

            var totalfee = getFee;
            Payments payment = null;
            if (extendedhoursdata == null)
            {

            }
            else
            {

                //var paymentTrans = _db.Payments.Where(s => s.ApplicationId == extendedhoursdata.Id && s.Service == "extended hours").OrderByDescending(x => x.DateAdded).FirstOrDefault();
                var paymentTrans = _db.Payments.Where(s => s.ApplicationId == extendedhoursdata.Id && s.Service == "extra counter").OrderByDescending(x => x.DateAdded).FirstOrDefault();
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
            }
            //check status
            // var inspectiondatastate = 
            //submitted check status
            ViewBag.License = mainlicense;
            ViewBag.Region = licenseRegion;
            ViewBag.Payment = payment;
            ViewBag.Process = process;
            ViewBag.Fee = getFee;
            // ViewBag.Penalty = penalty;
            ViewBag.TotalFee = totalfee;
            ViewBag.Months = time;
            ViewBag.Outletinfo = outletinfo;
            ViewBag.Appinfo = appinfo;
            ViewBag.ServeFee = getFee;
            ViewBag.Inspectiondata = extendedhoursdata;


            return View();
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
        public async Task<IActionResult> ExtendedHoursPaymentAsync(string id, double amount, string service, string process)
        {

            // The return url can be set at later stages. You might want to do this if you want to pass data to the return url (like the reference of the transaction)
            var userId = userManager.GetUserId(User);
            ExtendedHours extendedHours = new ExtendedHours();

            extendedHours.Id = Guid.NewGuid().ToString();
            extendedHours.UserId = userId;
            extendedHours.Status = "Applied";
            //ReferenceHelper.GenerateReferenceNumber(_db);
            var refnum = ReferenceHelper.GenerateReferenceNumber(_db);
            extendedHours.Reference = refnum;

            extendedHours.ApplicationId = id;


            extendedHours.PaidFee = amount;
            extendedHours.PaymentStatus = "Not Paid";
            extendedHours.ReasonForExtention = "";
            extendedHours.HoursOfExtension = "";
            extendedHours.ExtendedHoursDate = DateTime.Now;

            extendedHours.DateAdded = DateTime.Now;
            extendedHours.DateUpdated = DateTime.Now;
            _db.Add(extendedHours);
            _db.SaveChanges();


            //Id = "84aecb8d-4ec2-4ad5-86e8-971070a66b00";
            //amount = 55.7;
            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");

            paynow.ResultUrl = "https://llb.pfms.gov.zw/Postprocess/" + service + "?id=" + id + "&process=" + process;
            paynow.ReturnUrl = "https://llb.pfms.gov.zw/Postprocess/" + service + "?id=" + id + "&process=" + process;
            //paynow.ResultUrl = "https://localhost:41018/Postprocess/" + service + "?id=" + id + "&process=" + process;
            //paynow.ReturnUrl = "https://localhost:41018/Postprocess/" + service + "?id=" + id + "&process=" + process;

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

                //string id = userId.Id;
                transaction.UserId = userId;
                transaction.Amount = payment.Total;
                transaction.ApplicationId = extendedHours.Id;
                transaction.Service = "extra counter";
                //   transaction.PaynowRef = payment.Reference;
                transaction.PollUrl = response.PollUrl();
                transaction.PopDoc = "";
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


                var link = response.RedirectLink();


                // Get the poll url of the transaction

                // var instructions = response.
                return Redirect(link);
            }


            return View();
        }




    }
}