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

namespace LLB.Controllers
{
    [Authorize]
    [Route("Downloads")]
    public class DownloadsController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public DownloadsController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }


        [HttpGet("CheckDownload")]
        public IActionResult CheckDownload(string LLBNUM, string DocumentType)
        { //documenttype ready for a change in document type requirement is needed
            var appinfo = _db.ApplicationInfo.Where(z => z.LLBNum == LLBNUM ).FirstOrDefault();
            var downloadstatus = _db.Downloads.Where(a => a.LLBNUM == LLBNUM && a.DocumentType == DocumentType).FirstOrDefault();
            if(downloadstatus == null)
            {
                Downloads llblicense = new Downloads();
                llblicense.Id = Guid.NewGuid().ToString();
                llblicense.LLBNUM = LLBNUM;
                var UserId = userManager.GetUserId(User);
                llblicense.UserId = UserId;
                llblicense.DocumentType = "License";
                llblicense.Status = "Closed";
                llblicense.DateUpdated = DateTime.Now;
                llblicense.DownloadCount = 1;
                _db.Add(llblicense);
                _db.SaveChanges();

                return RedirectToAction("LLBLicense", "Documents", new { searchref = appinfo.Id });

            }
            else if(downloadstatus.Status == "Open"){
                // llblicense.Id = Guid.NewGuid().ToString();
                // llblicense.LLBNUM = LLBNUM;
                //  var UserId = userManager.GetUserId(User);
                // llblicense.UserId = UserId;
                downloadstatus.Status = "Closed";
                downloadstatus.DateUpdated = DateTime.Now;
                downloadstatus.DownloadCount = downloadstatus.DownloadCount + 1;
                _db.Update(downloadstatus);
                _db.SaveChanges();
                return RedirectToAction("LLBLicense", "Documents", new { searchref = appinfo.Id} );

            }
            else { 
                //return Json(new { success = "err", msg = "Valid Download" });
                return RedirectToAction("GetDuplicate", "Downloads", new { searchref = appinfo.Id });


            }

            return View();
        }



        [HttpGet("GetDuplicate")]
        public IActionResult GetDuplicate(string searchref )
        {
            var appinfo = _db.ApplicationInfo.Where(z => z.Id==searchref).FirstOrDefault();
            var outletinfo = _db.OutletInfo.Where(z => z.ApplicationId ==searchref).FirstOrDefault();
            var downloadinfo = _db.Downloads.Where(a => a.LLBNUM == appinfo.LLBNum).FirstOrDefault();
            var paymentTrans = _db.Payments.Where(s => s.ApplicationId == downloadinfo.Id && s.Service == "Download").OrderByDescending(x => x.DateAdded).FirstOrDefault();
            //var paymentTrans = _db.Payments.Where(s => s.ApplicationId == Id).OrderByDescending(x => x.DateAdded).FirstOrDefault();
            var fee = _db.PostFormationFees.Where(a => a.Code == "DPL").FirstOrDefault();
            if (paymentTrans != null )
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
                downloadinfo.PaymentRef = statusdata["paynowreference"];
                downloadinfo.PaymentStatus = statusdata["status"];
                if(statusdata["status"] == "Paid")
                {
                    downloadinfo.Status = "Open";
                }
                
                _db.Update(downloadinfo);
                _db.SaveChanges();
            }

            ViewBag.OutletInfo = outletinfo;
            ViewBag.AppInfo = appinfo;
            ViewBag.Download = downloadinfo;
            ViewBag.Fee = fee;
            return View();
        }


        [HttpGet("DownloadPayment")]
        public IActionResult DownloadPayment(string downloadId, double fee, string applicationId)
        {
            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");

            paynow.ResultUrl = "https://localhost:41018/Downloads/GetDuplicate?searchref=" + applicationId;
            paynow.ReturnUrl = "https://localhost:41018/Downloads/GetDuplicate?searchref=" + applicationId;
            // The return url can be set at later stages. You might want to do this if you want to pass data to the return url (like the reference of the transaction)


            // Create a new payment 
            var payment = paynow.CreatePayment("12345");

            //payment.AuthEmail = "chimukaoliver@gmail.com";
           // var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
          //  var licenseType = _db.LicenseTypes.Where(s => s.Id == applicationInfo.LicenseTypeID).FirstOrDefault();

            // Add items to the payment
            payment.Add("Duplicate", (decimal)fee);

            // Send payment to paynow
            var response = paynow.Send(payment);

            // Check if payment was sent without error
            if (response.Success())
            {
                // Get the url to redirect the user to so they can make payment
                Payments transaction = new Payments();
                transaction.Id = Guid.NewGuid().ToString();

                var userId = userManager.GetUserId(User);
                string id = userId;
                transaction.UserId = id;
                transaction.Amount = payment.Total;
                transaction.ApplicationId = downloadId;
                transaction.Service = "Download";
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

                //  return RedirectToAction("", "", new { searchref = searchref });
            }
            return View();
        }

        }
}
