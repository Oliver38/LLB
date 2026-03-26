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
using LLB.Helpers;

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
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public DownloadsController(TaskAllocationHelper taskAllocationHelper, AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
            _taskAllocationHelper = taskAllocationHelper;
        }


        [HttpGet("CheckDownload")]
        public IActionResult CheckDownload(string LLBNUM, string DocumentType)
        { //documenttype ready for a change in document type requirement is needed
            var appinfo = _db.ApplicationInfo.Where(z => z.LLBNum == LLBNUM).FirstOrDefault();
            if (appinfo == null)
            {
                TempData["result"] = "The selected licence could not be found.";
                return RedirectToAction("Dashboard", "Home", new { tab = "licences-pane" });
            }

            var userId = userManager.GetUserId(User);
            var downloadstatus = DownloadStatusHelper.GetOrCreateLicenseDownload(_db, appinfo, userId);
            if (downloadstatus == null)
            {
                TempData["result"] = "The licence download record could not be created.";
                return RedirectToAction("Dashboard", "Home", new { tab = "licences-pane" });
            }

            if (string.Equals(downloadstatus.Status, DownloadStatusHelper.DownloadOpenStatus, StringComparison.OrdinalIgnoreCase))
            {
                DownloadStatusHelper.CloseLicenseDownload(_db, downloadstatus);
                return RedirectToAction("LLBLicense", "Documents", new { searchref = appinfo.Id });
            }

            TempData["result"] = "The previous download closed the duplicate status. Pay for a duplicate request to reopen this licence.";
            return RedirectToAction("GetDuplicate", "Downloads", new { searchref = appinfo.Id });
        }



        [HttpGet("GetDuplicate")]
        public async Task<IActionResult> GetDuplicate(string searchref)
        {
            var appinfo = _db.ApplicationInfo.Where(z => z.Id == searchref).FirstOrDefault();
            if (appinfo == null)
            {
                TempData["result"] = "The selected licence could not be found.";
                return RedirectToAction("Dashboard", "Home", new { tab = "licences-pane" });
            }

            var outletinfo = _db.OutletInfo.Where(z => z.ApplicationId == searchref).FirstOrDefault();
            var downloadinfo = DownloadStatusHelper.GetOrCreateLicenseDownload(_db, appinfo, userManager.GetUserId(User));
            if (downloadinfo == null)
            {
                TempData["result"] = "The duplicate request record could not be prepared.";
                return RedirectToAction("Dashboard", "Home", new { tab = "licences-pane" });
            }

            var paymentTrans = _db.Payments
                .Where(s => s.ApplicationId == downloadinfo.Id
                    && (s.Service == DownloadStatusHelper.DuplicatePaymentService
                        || s.Service == DownloadStatusHelper.LegacyDuplicatePaymentService))
                .OrderByDescending(x => x.DateAdded)
                .FirstOrDefault();
            var fee = _db.PostFormationFees.Where(a => a.Code == "DPL").FirstOrDefault();
            if (paymentTrans != null && !string.IsNullOrWhiteSpace(paymentTrans.PollUrl))
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
                downloadinfo.DateUpdated = DateTime.Now;

                var activeTask = _db.Tasks
                    .Where(task => task.ApplicationId == appinfo.Id
                        && task.Service == DownloadStatusHelper.DuplicateTaskService
                        && task.Status == "assigned")
                    .OrderByDescending(task => task.DateAdded)
                    .FirstOrDefault();

                if (string.Equals(statusdata["status"], "Paid", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(downloadinfo.PaymentStatus, DownloadStatusHelper.DuplicateAwaitingPaymentStatus, StringComparison.OrdinalIgnoreCase)
                        && activeTask == null)
                    {
                        var inspectorId = await _taskAllocationHelper.GetInspector(_db, userManager);
                        if (!string.IsNullOrWhiteSpace(inspectorId))
                        {
                            Tasks reviewTask = new Tasks();
                            reviewTask.Id = Guid.NewGuid().ToString();
                            reviewTask.ApplicationId = appinfo.Id;
                            reviewTask.ExaminationStatus = "verification";
                            reviewTask.Service = DownloadStatusHelper.DuplicateTaskService;
                            reviewTask.VerifierId = inspectorId;
                            reviewTask.AssignerId = "system";
                            reviewTask.Status = "assigned";
                            reviewTask.DateAdded = DateTime.Now;
                            reviewTask.DateUpdated = DateTime.Now;
                            _db.Add(reviewTask);
                            _db.SaveChanges();
                            activeTask = reviewTask;
                        }
                    }

                    if (activeTask != null && string.Equals(activeTask.Status, "assigned", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadinfo.PaymentStatus = DownloadStatusHelper.DuplicateUnderReviewStatus;
                    }
                }
                else if (string.Equals(downloadinfo.PaymentStatus, DownloadStatusHelper.DuplicateAwaitingPaymentStatus, StringComparison.OrdinalIgnoreCase))
                {
                    downloadinfo.Status = DownloadStatusHelper.DownloadClosedStatus;
                }

                _db.Update(downloadinfo);
                _db.SaveChanges();
            }

            var currentTask = _db.Tasks
                .Where(task => task.ApplicationId == appinfo.Id
                    && task.Service == DownloadStatusHelper.DuplicateTaskService
                    && task.Status == "assigned")
                .OrderByDescending(task => task.DateAdded)
                .FirstOrDefault();

            ViewBag.OutletInfo = outletinfo;
            ViewBag.AppInfo = appinfo;
            ViewBag.Download = downloadinfo;
            ViewBag.Fee = fee;
            ViewBag.Payment = paymentTrans;
            ViewBag.CurrentTask = currentTask;
            return View();
        }


        [HttpGet("DownloadPayment")]
        public IActionResult DownloadPayment(string downloadId, double fee, string applicationId)
        {
            var appinfo = _db.ApplicationInfo.Where(a => a.Id == applicationId).FirstOrDefault();
            var downloadinfo = _db.Downloads.Where(a => a.Id == downloadId).FirstOrDefault();
            if (appinfo == null || downloadinfo == null)
            {
                TempData["result"] = "The duplicate request could not be started.";
                return RedirectToAction("Dashboard", "Home", new { tab = "licences-pane" });
            }

            if (string.Equals(downloadinfo.Status, DownloadStatusHelper.DownloadOpenStatus, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("CheckDownload", new { LLBNUM = appinfo.LLBNum, DocumentType = DownloadStatusHelper.LicenseDocumentType });
            }

            var activeTask = _db.Tasks
                .Where(task => task.ApplicationId == applicationId
                    && task.Service == DownloadStatusHelper.DuplicateTaskService
                    && task.Status == "assigned")
                .FirstOrDefault();
            if (activeTask != null)
            {
                TempData["result"] = "This duplicate request is already under review.";
                return RedirectToAction("GetDuplicate", new { searchref = applicationId });
            }

            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");
            var duplicateUrl = $"{Request.Scheme}://{Request.Host}/Downloads/GetDuplicate?searchref={applicationId}";

            paynow.ResultUrl = duplicateUrl;
            paynow.ReturnUrl = duplicateUrl;


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
                transaction.Service = DownloadStatusHelper.DuplicatePaymentService;
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

                downloadinfo.PaymentRef = transaction.PaynowRef;
                downloadinfo.PaymentStatus = DownloadStatusHelper.DuplicateAwaitingPaymentStatus;
                downloadinfo.Status = DownloadStatusHelper.DownloadClosedStatus;
                downloadinfo.DateApplied = DateTime.Now;
                downloadinfo.DateUpdated = DateTime.Now;
                _db.Update(downloadinfo);
                _db.SaveChanges();
                // [1]	{ [paynowreference, 17967752]}
                //transaction.PaymentStatus = payment.st


                var link = response.RedirectLink();


                // Get the poll url of the transaction

                // var instructions = response.
                return Redirect(link);

                //  return RedirectToAction("", "", new { searchref = searchref });
            }

            TempData["result"] = "The duplicate payment request could not be sent.";
            return RedirectToAction("GetDuplicate", new { searchref = applicationId });
        }

        }
}
