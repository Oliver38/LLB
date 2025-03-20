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

namespace LLB.Controllers
{

    
    [Route("Postpayments")]
    public class PostprocesspaymentsController : Controller
    {


        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public PostprocesspaymentsController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }



        [HttpGet("Paynow")]
        public async Task<IActionResult> PaynowPaymentAsync(string Id, double amount,string  service ,string process)
        {
            //Id = "84aecb8d-4ec2-4ad5-86e8-971070a66b00";
            //amount = 55.7;
            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
        
            paynow.ResultUrl = "https://llb.pfms.gov.zw/Postprocess/" + service+"?id="+Id+"&process="+process;
            paynow.ReturnUrl = "https://llb.pfms.gov.zw/Postprocess/" + service + "?id=" + Id + "&process=" + process;
           // paynow.ResultUrl = "https://localhost:41018/Postprocess/" + service + "?id=" + Id + "&process=" + process;
           // paynow.ReturnUrl = "https://localhost:41018/Postprocess/" + service + "?id=" + Id + "&process=" + process;

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



        [HttpGet("InspectionPaynow")]
        public async Task<IActionResult> InspectionPaynow(string Id, double amount, string service, string process)
        {
            //Id = "84aecb8d-4ec2-4ad5-86e8-971070a66b00";
            //amount = 55.7;
            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
        
            paynow.ResultUrl = "https://llb.pfms.gov.zw/Postprocess/" + service + "?id=" + Id + "&process=" + process;
            paynow.ReturnUrl = "https://llb.pfms.gov.zw/Postprocess/" + service + "?id=" + Id + "&process=" + process;
            //paynow.ResultUrl = "https://localhost:41018/Postprocess/" + service + "?id=" + Id + "&process=" + process;
            //paynow.ReturnUrl = "https://localhost:41018/Postprocess/" + service + "?id=" + Id + "&process=" + process;

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

        //[HttpPost("UpdateFee")]
        //public async Task<IActionResult> AddFee(string Id, string Description, string Code, string ProcessName, double Fee)
        //{ // Check if the fee exists in the database
        //    var existingFee = await _db.PostFormationFees.FindAsync(Id);

        //    if (existingFee == null)
        //    {
        //        // If the fee does not exist, return a not found result
        //        return NotFound();
        //    }

        //    // Update the properties with the new values from the form
        //    existingFee.ProcessName =ProcessName;
        //    existingFee.Description = Description;
        //    existingFee.Code =Code;
        //    existingFee.Fee = Fee;
        //    //existingFee.Status = Status; // Optional: You can decide whether to allow editing Status
        //    existingFee.DateUpdated = DateTime.Now; // You may not want to update this, depending on your requirements

        //    // Save the changes to the database
        //    _db.Update(existingFee);
        //   _db.SaveChanges();
        //   // return View();
        //    return RedirectToAction( "AddFee","Postprocess");
        //}


    }
}