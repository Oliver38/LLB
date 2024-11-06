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
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace LLB.Controllers
{
    
    [Route("")]
    [Route("Recommend")]
    public class RecommendController : Controller
    {
        

        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public RecommendController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }
        [HttpGet("Dashboard")]
        public async Task<IActionResult> DashboardAsync()
        {

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;


            List<ApplicationInfo> appinfo = new List<ApplicationInfo>();
            var tasks = _db.Tasks.Where(f => f.RecommenderId == id && f.Status == "assigned").ToList();
            foreach(var task in tasks)
            {
                ApplicationInfo getinfo = new ApplicationInfo();

                var applications = _db.ApplicationInfo.Where(a => a.Id == task.ApplicationId).FirstOrDefault();

                getinfo = applications;
                appinfo.Add(getinfo);
            }

            List<ApplicationInfo> completeappinfo = new List<ApplicationInfo>();

            var completetasks = _db.Tasks.Where(f => f.RecommenderId == id && f.Status == "completed").ToList();
            foreach (var completetask in completetasks)
            {
                ApplicationInfo getcomplinfo = new ApplicationInfo();

                var applications = _db.ApplicationInfo.Where(a => a.Id == completetask.ApplicationId).FirstOrDefault();

                getcomplinfo = applications;
                completeappinfo.Add(getcomplinfo);
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
            ViewBag.Completed = completeappinfo;
            return View();
        }


        [HttpGet(("Apply"))]
        public async Task<IActionResult> ApplyAsync(string Id, string error)
        {
            //using Microsoft.AspNetCore.Identity;
            var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var user = await userManager.FindByIdAsync(application.UserID);
            var licenses = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            TempData["result"] = error;
            ViewBag.ApplicationInfo = application;
            ViewBag.User = user;
            ViewBag.Regions = regions;
            ViewBag.License = licenses;
            return View();
        }



        

        [HttpGet(("OutletInfo"))]
        public IActionResult OutletInfo(string Id, string error)
        {
            // 00826805 - 0853 - 45c5 - 9fe3 - bce855854091
            var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var outletInfo = _db.OutletInfo.Where(b => b.ApplicationId == Id).FirstOrDefault();
            var directorsInfo = _db.DirectorDetails.Where(b => b.ApplicationId == Id).ToList();
            // var application = await _db.ApplicationInfo.FindAsync(dd.Id);
            TempData["result"] = error;
            ViewBag.Application = application;
            ViewBag.OutletInfo = outletInfo;
            ViewBag.Directors = directorsInfo;
            ViewBag.DirectorsCount = directorsInfo.Count();
            //ViewBag.User = user;

            return View();
        }

        

        


        // ManagersInfo

        [HttpGet(("ManagersInfo"))]
        public IActionResult ManagersInfo(string Id)
        {

            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var managersInfo = _db.ManagersParticulars.Where(b => b.ApplicationId == Id).ToList();


            ViewBag.ApplicationInfo = applicationInfo;
            ViewBag.ManagersInfo = managersInfo;


            return View();
        }



       

        [HttpGet(("Attachments"))]
        public async Task<IActionResult> AttachmentsAsync(string Id)
        {
            var attachments = _db.AttachmentInfo.Where(b => b.ApplicationId == Id).ToList();

            if (attachments.Count() <= 0)
            {
                string[] documents = { "Vetted fingerprints",
"Police report",
"Form 55",
"Affidavit by transferee",
"Lease documents",
"Advert",
"Manager Applicant Fingerprints",};
                foreach (var document in documents)
                {
                    AttachmentInfo documentInfo = new AttachmentInfo();
                    documentInfo.Id = Guid.NewGuid().ToString();
                    documentInfo.DocumentTitle = document.ToString();

                    var userId = await userManager.FindByEmailAsync(User.Identity.Name);
                    string id = userId.Id;
                    documentInfo.UserId = id;
                    documentInfo.DateAdded = DateTime.Now;
                    documentInfo.DateUpdated = DateTime.Now;
                    documentInfo.Status = "empty";
                    documentInfo.DocumentLocation = "";
                    documentInfo.ApplicationId = Id;
                    _db.Add(documentInfo);
                    _db.SaveChanges();
                }

            }
            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var attachmentDocs = _db.AttachmentInfo.Where(b => b.ApplicationId == Id).ToList();


            ViewBag.ApplicationInfo = applicationInfo;
            ViewBag.Attachments = attachmentDocs;


            return View();
        }



        [HttpGet("Finalising")]
        public async Task<IActionResult> FinalisingAsync(string Id, string error, string gateway)
        {
            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();

            if (gateway == "paynow")
            {
                var paymentTrans = _db.Payments.Where(s => s.ApplicationId == Id).OrderByDescending(x => x.DateAdded).FirstOrDefault();
                var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");

                var status = paynow.PollTransaction(paymentTrans.PollUrl);

                var statusdata = status.GetData();
                paymentTrans.PaynowRef = statusdata["paynowreference"];
                paymentTrans.PaymentStatus = statusdata["status"];
                paymentTrans.Status = statusdata["status"];
                paymentTrans.DateUpdated = DateTime.Now;

                _db.Update(paymentTrans);
                _db.SaveChanges();
                // applicationInfo.PaymentFee = paymentTrans.Amount;
                applicationInfo.PaymentId = paymentTrans.Id;
                applicationInfo.PaymentStatus = statusdata["status"];
                _db.Update(applicationInfo);
                _db.SaveChanges();
            }


            var paymentTransb = _db.Payments.Where(s => s.ApplicationId == Id).OrderByDescending(x => x.DateAdded).FirstOrDefault();

            if (paymentTransb == null)
            {

            }
            else
            {
                if (paymentTransb.PollUrl == "transfer")
                {

                }
                else
                {
                    var paynowb = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");

                    var statusb = paynowb.PollTransaction(paymentTransb.PollUrl);

                    var statusdatab = statusb.GetData();
                    paymentTransb.PaynowRef = statusdatab["paynowreference"];
                    paymentTransb.PaymentStatus = statusdatab["status"];
                    paymentTransb.Status = statusdatab["status"];
                    paymentTransb.DateUpdated = DateTime.Now;

                    _db.Update(paymentTransb);
                    _db.SaveChanges();
                    // applicationInfo.PaymentFee = paymentTrans.Amount;
                    applicationInfo.PaymentId = paymentTransb.Id;
                    applicationInfo.PaymentStatus = statusdatab["status"];
                    _db.Update(applicationInfo);
                    _db.SaveChanges();
                }
            }
            Finalising finaldata = new Finalising();
            /*public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? UserId { get; set; }
        public string? ManagersInfo { get; set; }
        public string? DirectorsInfo { get; set; }
        public string? OutletInfo { get; set; }
        public string? DocumentInfo { get; set; }
        public string? ManagersPrice { get; set; }
        public string? LicencePrice { get; set; }
        public string? Status { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }*/
            finaldata.ApplicationId = Id;

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            finaldata.UserId = id;
            finaldata.ManagersInfo = "correct";
            finaldata.OutletInfo = "correct";
            finaldata.DocumentInfo = "correct";
            // finaldata.DocumentInfo = "correct";

            var regiondata = _db.LicenseRegions.Where(s => s.Id == applicationInfo.ApplicationType).FirstOrDefault();
            var licensefees = _db.LicenseTypes.Where(a => a.Id == applicationInfo.LicenseTypeID).FirstOrDefault();
            var managerfees = _db.LicenseTypes.Where(a => a.Id == "080146d5-6427-4db4-a851-3adb95ee208a").FirstOrDefault();
            var managers = _db.ManagersParticulars.Where(a => a.ApplicationId == Id).ToList();
            int managerscount = managers.Count();
            finaldata.ManagersCount = managerscount;

            var payment = _db.Payments.Where(s => s.ApplicationId == Id).OrderByDescending(x => x.DateAdded).FirstOrDefault();

            if (regiondata.RegionName == "Town")
            {
                finaldata.LicencePrice = licensefees.TownFee;
                finaldata.ManagersPrice = managerfees.TownFee;

                var managertotal = managerfees.TownFee * managerscount;
                finaldata.ManagersTotal = managertotal;
                finaldata.Total = managertotal + licensefees.TownFee;
                var totalfee = finaldata.Total;

                applicationInfo.PaymentFee = (decimal)totalfee;
                _db.Update(applicationInfo);
                _db.SaveChanges();





            }
            else if (regiondata.RegionName == "City")
            {
                finaldata.LicencePrice = licensefees.CityFee;
                finaldata.ManagersPrice = managerfees.CityFee;

                var managertotal = managerfees.CityFee * managerscount;
                finaldata.ManagersTotal = managertotal;
                finaldata.Total = managertotal + licensefees.CityFee;
                var totalfee = finaldata.Total;

                applicationInfo.PaymentFee = (decimal)totalfee;
                _db.Update(applicationInfo);
                _db.SaveChanges();
            }
            else if (regiondata.RegionName == "Municipality")
            {
                finaldata.LicencePrice = licensefees.MunicipaltyFee;
                finaldata.ManagersPrice = managerfees.MunicipaltyFee;

                var managertotal = managerfees.MunicipaltyFee * managerscount;
                finaldata.ManagersTotal = managertotal;
                finaldata.Total = managertotal + licensefees.MunicipaltyFee;
                var totalfee = finaldata.Total;

                applicationInfo.PaymentFee = (decimal)totalfee;
                _db.Update(applicationInfo);
                _db.SaveChanges();
            }
            else if (regiondata.RegionName == "RDC")
            {
                finaldata.LicencePrice = licensefees.RDCFee;
                finaldata.ManagersPrice = managerfees.RDCFee;

                var managertotal = managerfees.RDCFee * managerscount;
                finaldata.ManagersTotal = managertotal;
                finaldata.Total = managertotal + licensefees.RDCFee;
                var totalfee = finaldata.Total;

                applicationInfo.PaymentFee = (decimal)totalfee;
                _db.Update(applicationInfo);
                _db.SaveChanges();
            }
            TempData["result"] = error;
            var applicationInfob = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var hasquery = _db.Queries.Where(a => a.ApplicationId == Id && a.Status == "Has Query").ToList();
            var task = _db.Tasks.Where(q => q.ApplicationId == Id && q.Status == "assigned").OrderByDescending(x => x.DateAdded).FirstOrDefault();

            ViewBag.Task = task;

            ViewBag.HasQuery = hasquery;
            ViewBag.ApplicationInfo = applicationInfob;
            ViewBag.FinalData = finaldata;
            ViewBag.Payment = payment;


            return View();
        }


        [HttpGet("PaynowPayment")]
        public async Task<IActionResult> PaynowPaymentAsync(string Id, double amount)
        {
            //Id = "84aecb8d-4ec2-4ad5-86e8-971070a66b00";
            //amount = 55.7;
            var paynow = new Paynow("7175", "62d86b2a-9f71-40e2-8b52-b9f1cd327cf0");

            paynow.ResultUrl = "https://localhost:41018/License/Submit?gateway=paynow";
            paynow.ReturnUrl = "https://localhost:41018/License/Finalising?Id=" + Id + "&gateway=paynow";
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




       

        [HttpGet("Submit")]
        public IActionResult Submit(string Id)
        {

            var payment = _db.Payments.Where(s => s.ApplicationId == Id && s.Status == "Paid").FirstOrDefault();
            if (payment == null || payment.PaymentStatus == "not paid")
            {
                string error = "Please make payment to submit application";
                return RedirectToAction("Finalising", new { Id = Id, error = error });

                // var applicationInfo = 
            }
            else
            {
                var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
                application.Status = "submitted";
                _db.Update(application);
                _db.SaveChanges();
                return RedirectToAction("Dashboard", "Home");
            }



        }

        [HttpPost("Query")]
        public IActionResult Query(Queries queries)
        {
           
            /*  [Key]
        // Liquor Outlet Info
        public string? Id { get; set; }
        public string? ApplicationId { get; set; }
        public string? InspectorId { get; set; }
        public string? Stage { get; set; }
        public string? Query { get; set; }
        public string? Status { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }

*/
            queries.Id = Guid.NewGuid().ToString();
            queries.InspectorId = userManager.GetUserId(User);
            queries.DateAdded = DateTime.Now;
            queries.Status = "Has Query";
            _db.Add(queries);
            _db.SaveChanges();
            string error = "Query has been raised successfully";
            if (queries.Stage == "Recommend Application")
            {
                return RedirectToAction("Apply", new { Id = queries.ApplicationId, error = error });

            }else if (queries.Stage == "Recommend Outlet")
            {
                return RedirectToAction("OutletInfo", new { Id = queries.ApplicationId, error = error });

            }
            else if (queries.Stage == "Recommend Managers")
            {
                return RedirectToAction("ManagersInfo", new { Id = queries.ApplicationId, error = error });

            }
            else if (queries.Stage == "Recommend Attachments")
            {
                return RedirectToAction("Attachments", new { Id = queries.ApplicationId, error = error });

            }
            else
            {
               
            }
            return RedirectToAction("Apply", new { Id = queries.ApplicationId, error = error });

        }

        [HttpGet("HasQuery")]
        public IActionResult HasQuery(string Id, string taskid)
        {
            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            applicationInfo.Status = "Has Query";
            _db.Update(applicationInfo);
            _db.SaveChanges();
            var task = _db.Tasks.Where(f => f.Id == taskid).FirstOrDefault();
            task.Status = "completed";
            _db.Update(task);
            _db.SaveChanges();
            return RedirectToAction("Dashboard", "Recommend");
        }


            [HttpGet("Approve")]
        public async Task<IActionResult> ApproveAsync(string Id, string taskid)
        {
            var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            application.Status = "recommended";
            application.ExaminationStatus= "recommendation";
            _db.Update(application);
            _db.SaveChanges();

            var task = _db.Tasks.Where(f => f.Id == taskid).FirstOrDefault();
            task.Status = "completed";
            task.RecommendationDate = DateTime.Now;
            _db.Update(task);
            _db.SaveChanges();

            Tasks tasks = new Tasks();
            tasks.Id = Guid.NewGuid().ToString();
            tasks.ApplicationId = application.Id;
            //auto allocation to replace
            var userId = await userManager.FindByEmailAsync("secretary@secretary.com");
            tasks.ApproverId= userId.Id;
            tasks.AssignerId = "system";
            tasks.Status = "assigned";
            tasks.DateAdded = DateTime.Now;
            tasks.DateUpdated = DateTime.Now;
            _db.Add(tasks);
            _db.SaveChanges();
            return RedirectToAction("Dashboard", "Recommend");
        }
    }
}