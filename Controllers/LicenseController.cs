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
using LLB.Helpers;
using System.Threading.Tasks;


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
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public LicenseController(TaskAllocationHelper taskAllocationHelper, AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
            _taskAllocationHelper = taskAllocationHelper;
        }


        [HttpGet(("Apply"))]
        public async Task<IActionResult> ApplyAsync(string Id)
        {

            var userId = userManager.GetUserId(User);
            var incompleteapplications = _db.ApplicationInfo.Where(a => a.UserID == userId && a.Status == "inprogress").ToList();
            if(incompleteapplications.Count() > 2)
            {
                //condition will be set
            }
            //using Microsoft.AspNetCore.Identity;
            var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var user = await userManager.FindByEmailAsync(User.Identity.Name);
            var licenses = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var queries = _db.Queries.Where(x => x.ApplicationId == Id && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
            ViewBag.ApplicationInfo = application;
            ViewBag.User = user;
            ViewBag.Regions = regions;
            ViewBag.License = licenses;
            return View();
        }



        [HttpPost(("Apply"))]
        public async Task<IActionResult> ApplyAsync(ApplicationInfo info)
        {



            if (info.Id == null)
            {
                info.Id = Guid.NewGuid().ToString();
                var dbref = 2;
                /* for (int i = 1; i < dbref; i++)
                 {
                     i.ToString().PadLeft(4, '0');
                 }
                string jobRef = new Random().Next(1000, 9999).ToString();
                 */
                // * ApplicationID /Id

                var userId = await userManager.FindByEmailAsync(User.Identity.Name);
                string id = userId.Id;
                info.UserID = id;

               

                info.PaymentId = "";
                info.PaymentStatus = "";
                info.RefNum = "";
                info.PaymentFee = 0;

                info.PlaceOfBirth = "";
                //info.DateofEntryIntoZimbabwe = "";
                //info.PlaceOfEntry = "";
                info.RejectionReason = "";


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
                var queries = _db.Queries.Where(x => x.ApplicationId == info.Id && x.Status == "Has Query").ToList();
                ViewBag.Queries = queries;
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
                //updateinfo.DateofEntryIntoZimbabwe = "";
                //updateinfo.PlaceOfEntry = "";

                updateinfo.Status = "inprogress";
                updateinfo.ApplicationDate = DateTime.Now;
                updateinfo.InspectorID = "";

                //public DateTime InspectionDate 
                updateinfo.Secretary = "";
                updateinfo.DateUpdated = DateTime.Now;
                // public DateTime ApprovedDate 
                ///////////// info.RejectionReason = "";
                //spublic DateTime DateCreated 
                var o = _db.Update(updateinfo);
                var p = _db.SaveChanges();
                // ViewBag.License = licenses;
                var application = _db.ApplicationInfo.Where(a => a.Id == info.Id).FirstOrDefault();
                var user = await userManager.FindByEmailAsync(User.Identity.Name);
                var licenses = _db.LicenseTypes.ToList();
                var regions = _db.LicenseRegions.ToList();
                var queries = _db.Queries.Where(x => x.ApplicationId == info.Id && x.Status == "Has Query").ToList();
                ViewBag.Queries = queries;
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
        public IActionResult OutletInfo(string Id)
        {
            // 00826805 - 0853 - 45c5 - 9fe3 - bce855854091
            var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var outletInfo = _db.OutletInfo.Where(b => b.ApplicationId == Id).FirstOrDefault();
            var directorsInfo = _db.DirectorDetails.Where(b => b.ApplicationId == Id).ToList();
            // var application = await _db.ApplicationInfo.FindAsync(dd.Id);
            var queries = _db.Queries.Where(x => x.ApplicationId == Id && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
            ViewBag.Application = application;
            ViewBag.OutletInfo = outletInfo;
            ViewBag.Directors = directorsInfo;
            ViewBag.DirectorsCount = directorsInfo.Count();
            //ViewBag.User = user;

            return View();
        }

        [HttpPost(("OutletInfo"))]
        public async Task<IActionResult> OutletInfoAsync(OutletInfo outletInfo)
        {
            /*    public string? Id { get; set; }
          public string? ApplicationId { get; set; }
          public string? UserId { get; set; }
          public string? TradingName { get; set; }
          public string? Province { get; set; }
          public string? Address { get; set; }
          public string? City { get; set; }
          public string? DirectorNames { get; set; }
          public string? Status { get; set; }
          public DateTime DateAdded { get; set; }
          public DateTime DateUpdated { get; set; }*/
            if (outletInfo.Id == null)
            {
                outletInfo.Id = Guid.NewGuid().ToString();

                var userId = await userManager.FindByEmailAsync(User.Identity.Name);
                string id = userId.Id;
                outletInfo.UserId = id;
                outletInfo.Status = "Unsubmitted";
                outletInfo.DirectorNames = "";
                outletInfo.DateAdded = DateTime.Now;
                outletInfo.DateUpdated = DateTime.Now;
                _db.Add(outletInfo);
                _db.SaveChanges();


                // 00826805 - 0853 - 45c5 - 9fe3 - bce855854091
                var application = _db.ApplicationInfo.Where(a => a.Id == outletInfo.ApplicationId).FirstOrDefault();
                var outletInfob = _db.OutletInfo.Where(b => b.ApplicationId == outletInfo.ApplicationId).FirstOrDefault();
                var directorsInfo = _db.DirectorDetails.Where(b => b.ApplicationId == outletInfo.ApplicationId).ToList();
                // var application = await _db.ApplicationInfo.FindAsync(dd.Id);
                var queries = _db.Queries.Where(x => x.ApplicationId == outletInfo.ApplicationId && x.Status == "Has Query").ToList();
                ViewBag.Queries = queries;
                ViewBag.Application = application;
                ViewBag.OutletInfo = outletInfob;
                ViewBag.Directors = directorsInfo;
                ViewBag.DirectorsCount = directorsInfo.Count();
                TempData["result"] = "Applicant details successfully added";
                return View();
            }
            else
            {
                // outletInfo.Id = Guid.NewGuid().ToString();
                //outletInfo.UserId = userManager.GetUserId(User);
                //  outletInfo.Status = "Unsubmitted";
                var updateOutletInfo = _db.OutletInfo.Where(b => b.ApplicationId == outletInfo.ApplicationId).FirstOrDefault();

                //outletInfo.DirectorNames = "";
                //outletInfo.DateAdded = DateTime.Now;
                updateOutletInfo.TradingName = outletInfo.TradingName;
                updateOutletInfo.Address = outletInfo.Address;
                updateOutletInfo.Province = outletInfo.Province;
                updateOutletInfo.City = outletInfo.City;

                updateOutletInfo.DateUpdated = DateTime.Now;
                _db.Update(updateOutletInfo);
                _db.SaveChanges();


                // 00826805 - 0853 - 45c5 - 9fe3 - bce855854091
                var application = _db.ApplicationInfo.Where(a => a.Id == outletInfo.ApplicationId).FirstOrDefault();
                var outletInfob = _db.OutletInfo.Where(b => b.ApplicationId == outletInfo.ApplicationId).FirstOrDefault();
                var directorsInfo = _db.DirectorDetails.Where(b => b.ApplicationId == outletInfo.ApplicationId).ToList();
                // var application = await _db.ApplicationInfo.FindAsync(dd.Id);
                var queries = _db.Queries.Where(x => x.ApplicationId == outletInfo.ApplicationId && x.Status == "Has Query").ToList();
                ViewBag.Queries = queries;
                ViewBag.Application = application;
                ViewBag.OutletInfo = outletInfob;
                ViewBag.Directors = directorsInfo;
                ViewBag.DirectorsCount = directorsInfo.Count();
                TempData["result"] = "Applicant details successfully Updated";
                return View();

            }
            //ViewBag.User = user;


        }

        [HttpPost(("Director"))]
        public async Task<IActionResult> DirectorAsync(DirectorDetails directorDetails)
        {

            /*     public string? Id { get; set; }
           public string? UserId { get; set; }
           public string? Name { get; set; }
           public string? ApplicationId { get; set; }
           public string? Surname { get; set; }
           public string? NationalId { get; set; }
           public string? Address { get; set; }
           public string? Status { get; set; }
           public DateTime DateAdded { get; set; }
           public DateTime DateUpdated { get; set; }*/
            directorDetails.Id = Guid.NewGuid().ToString();
            directorDetails.Status = "active";

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            directorDetails.UserId = id;
            directorDetails.DateAdded = DateTime.Now;
            directorDetails.DateUpdated = DateTime.Now;
            _db.Add(directorDetails);
            _db.SaveChanges();

            return RedirectToAction("OutletInfo", new { Id = directorDetails.ApplicationId });
        }


        // ManagersInfo

        [HttpGet(("ManagersInfo"))]
        public IActionResult ManagersInfo(string Id)
        {

            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var managersInfo = _db.ManagersParticulars.Where(b => b.ApplicationId == Id).ToList();

            var queries = _db.Queries.Where(x => x.ApplicationId == Id && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
            ViewBag.ApplicationInfo = applicationInfo;
            ViewBag.ManagersInfo = managersInfo;


            return View();
        }



        [HttpPost("ManagersInfo")]
        public async Task<IActionResult> ManagersInfoAsync(ManagersParticulars manager, IFormFile file, IFormFile fileb)
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
            if (file != null)
            {
                string pic = System.IO.Path.GetFileName(file.FileName);
                string dic = System.IO.Path.GetExtension(file.FileName);
                string newname = manager.ApplicationId;
                string path = System.IO.Path.Combine($"ManagerIds", newname + dic);
                string docpath = System.IO.Path.Combine($"wwwroot/ManagerIds", newname + dic);
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
                string newname = manager.ApplicationId;
                string path = System.IO.Path.Combine($"ManagerFingerprints", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/ManagerFingerprints", newname + dicb);
                manager.Fingerprints = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            else
            {
                manager.Fingerprints = "";
            }





            manager.Id = Guid.NewGuid().ToString();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            manager.UserId = id;
            manager.Status = "UnSubmitted";
            manager.DateAdded = DateTime.Now;
            manager.DateUpdated = DateTime.Now;
            _db.Add(manager);
            _db.SaveChanges();

            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == manager.ApplicationId).FirstOrDefault();
            var managersInfo = _db.ManagersParticulars.Where(b => b.ApplicationId == manager.ApplicationId).ToList();

            var queries = _db.Queries.Where(x => x.ApplicationId == manager.ApplicationId && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
            ViewBag.ApplicationInfo = applicationInfo;
            ViewBag.ManagersInfo = managersInfo;
            TempData["result"] = "Manager details successfully added";

            return View();
        }


        [HttpGet(("Attachments"))]
        public async Task<IActionResult> AttachmentsAsync(string Id)
        {
            var attachments = _db.AttachmentInfo.Where(b => b.ApplicationId == Id).ToList();

            if (attachments.Count() <= 0)
            {
                var userm = await userManager.FindByEmailAsync(User.Identity.Name);
                //string[] documents = null;
                if (userm.Nationality == "Zimbabwean")
                {

                    string[] documents = {
                        "",
"Affidavit by transferee",
"Lease documents",
"Advert",
"Manager Applicant Fingerprints",

                };



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
                else
                {


                    string[] documents = { "Vetted fingerprints",
"Police report",
"Form 55",
"Affidavit by transferee",
"Lease documents",
"Advert",
"Manager Applicant Fingerprints",
"Letter From the Minister"

                };



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

            }
            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var attachmentDocs = _db.AttachmentInfo.Where(b => b.ApplicationId == Id).ToList();
            var user = await userManager.FindByEmailAsync(User.Identity.Name);
                 ViewBag.User = user;
            var queries = _db.Queries.Where(x => x.ApplicationId == Id && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
            ViewBag.ApplicationInfo = applicationInfo;
            ViewBag.Attachments = attachmentDocs;


            return View();
        }


        [HttpPost("Attachments")]
        public async Task<IActionResult> AttachmentsAsync(AttachmentInfo attachment, IFormFile file)
        {
            var attachmentUpdate = _db.AttachmentInfo.Where(a => a.Id == attachment.Id).FirstOrDefault();
            attachmentUpdate.DateUpdated = DateTime.Now;
            attachmentUpdate.Status = "posted";
            if (file != null)
            {
                string pic = System.IO.Path.GetFileName(file.FileName);
                string dic = System.IO.Path.GetExtension(file.FileName);
                string newname = attachmentUpdate.Id;
                string path = System.IO.Path.Combine($"ApplicationAttchments", newname + dic);
                string docpath = System.IO.Path.Combine($"wwwroot/ApplicationAttchments", newname + dic);
                attachmentUpdate.DocumentLocation = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            else
            {
                attachmentUpdate.DocumentLocation = "";
            }
            _db.Update(attachmentUpdate);
            _db.SaveChanges();


            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == attachment.ApplicationId).FirstOrDefault();
            var attachmentDocs = _db.AttachmentInfo.Where(b => b.ApplicationId == attachment.ApplicationId).ToList();

            var user = await userManager.FindByEmailAsync(User.Identity.Name);
            var queries = _db.Queries.Where(x => x.ApplicationId == attachment.ApplicationId && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
            ViewBag.User = user;
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
                else { 
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
                

                


            } else if (regiondata.RegionName == "City") {
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
            else if (regiondata.RegionName == "Municipality") {
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
            else if (regiondata.RegionName == "RDC") {
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
            var queries = _db.Queries.Where(x => x.ApplicationId == Id && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
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
            paynow.ReturnUrl = "https://localhost:41018/License/Finalising?Id=" + Id+"&gateway=paynow";
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
                transaction.Service = "new application";
                //   transaction.PaynowRef = payment.Reference;
                transaction.PollUrl = response.PollUrl();
                transaction.PopDoc = "";
                transaction.Status = "not paid";
                transaction.DateAdded = DateTime.Now;
                transaction.DateUpdated= DateTime.Now;

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

        [HttpPost("PaymentPOP")]
        public async Task<IActionResult> PaynowPOP(string Id, double amount, IFormFile file)
        {
            Payments transaction = new Payments();
            transaction.Id = Guid.NewGuid().ToString();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            transaction.UserId = id;
            transaction.Amount = (decimal)amount;
            transaction.ApplicationId = Id;
            //   transaction.PaynowRef = payment.Reference;
            // transaction.PollUrl = response.PollUrl();
            transaction.Service = "new application";

            transaction.PopDoc = "";
            transaction.Status = "awaiting verification";
            transaction.DateAdded = DateTime.Now;
            transaction.DateUpdated = DateTime.Now;
          transaction.PollUrl = "transfer";

            transaction.PaynowRef = "";
            transaction.PaymentStatus = "payment verification";
            if (file != null)
            {
                string pic = System.IO.Path.GetFileName(file.FileName);
                string dic = System.IO.Path.GetExtension(file.FileName);
                string newname = Id;
                string path = System.IO.Path.Combine($"POPS", newname + dic);
                string docpath = System.IO.Path.Combine($"wwwroot/POPS", newname + dic);
                transaction.PopDoc = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            else
            {
                transaction.PopDoc = "";
            }
            _db.Add(transaction);
            _db.SaveChanges();

            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            applicationInfo.PaymentId = transaction.Id;
            applicationInfo.PaymentFee =(decimal)amount;
            applicationInfo.PaymentStatus = "payment verification";
            _db.Update(applicationInfo);
            _db.SaveChanges();

            string error = "POP Uploaded";
            string gateway = "transfer";

            return RedirectToAction("Finalising",  new { Id = Id, error = error, gateway = gateway });
            return View();
        }


        //RemovePop
        [HttpGet("RemovePop")]
        public async Task<IActionResult> RemovePop(string Id)
        {
            return View();
        }

            [HttpGet("Submit")]
        public async Task<IActionResult> SubmitAsync(string Id)
        {
           
            var payment = _db.Payments.Where(s => s.ApplicationId == Id ).OrderByDescending(x => x.DateAdded).FirstOrDefault();
            //var paymentvet = _db.Payments.Where(c => c.ApplicationId == Id && c.Status == "POP").FirstOrDefault();

            if (payment == null || payment.PaymentStatus == "not paid" || payment.PaymentStatus == "Cancelled")
            {
                string error = "Please make payment to submit application";
                 return RedirectToAction("Finalising", new { Id = Id, error = error });

                // var applicationInfo = 
            }else if ( payment.PaymentStatus == "Awaiting Delivery" || payment.PaymentStatus == "Cancelled")
            {
                string error = "Please make contact Paynow for Payment Clarification";
                return RedirectToAction("Finalising", new { Id = Id, error = error });

                // var applicationInfo = 
            }

            else if (payment.Status == "awaiting verification" )

            {
                var newReferenceNumber = ReferenceHelper.GenerateReferenceNumber(_db);

                var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
                application.Status = "payment verification";
                application.RefNum = newReferenceNumber;
                _db.Update(application);
                _db.SaveChanges();

                //giving refnum for trace ability
               
                return RedirectToAction("Dashboard", "Home");
            }
            else
            {
                var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
                var newReferenceNumber = ReferenceHelper.GenerateReferenceNumber(_db);

                application.RefNum = newReferenceNumber;
                application.Status = "submitted";
                application.ExaminationStatus = "verification";
                _db.Update(application);
                _db.SaveChanges();

                var managers = _db.ManagersParticulars.Where(a => a.ApplicationId == Id).ToList();
                foreach (var manager in managers)
                {
                    manager.Status = "submitted";
                    manager.EffectiveDate = DateTime.Now;
                    _db.Update(manager);
                    _db.SaveChanges();
                }

               
                //var verifierId = await TaskAllocator()
                Tasks tasks = new Tasks();
                tasks.Id = Guid.NewGuid().ToString();
                tasks.ApplicationId = application.Id;
                //tasks.AssignerId

                //auto allocation to replace
                // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
                // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");

                var verifierWithLeastTasks = await _taskAllocationHelper.GetVerifier(_db, userManager);
                //   tasks.VerifierId = selectedUser.Id;
                tasks.Service = "new application";
                tasks.VerifierId = verifierWithLeastTasks;
                tasks.AssignerId = "system";
                tasks.Status = "assigned";
                tasks.DateAdded = DateTime.Now;
                tasks.DateUpdated = DateTime.Now;
                _db.Add(tasks);
                _db.SaveChanges();


                return RedirectToAction("Dashboard", "Home");
            }


            var paymentvet = _db.Payments.Where(c => c.ApplicationId == Id && c.Status == "POP").FirstOrDefault();



        }

        //ResolveQuery

        [HttpGet("ResolveQuery")]
        public IActionResult ResolveQuery(string Id, string stage, string applicationId)
        {
            if (stage == "apply")
            {
                var getquery = _db.Queries.Where(e => e.Id == Id).FirstOrDefault();
                getquery.Status = "Resolved";
                getquery.DateUpdated = DateTime.Now;
                _db.Update(getquery);
                _db.SaveChanges();
                return RedirectToAction("Apply", new { Id = applicationId });
            }

            return View();

        }

        [HttpGet("ResolveApplication")]
        public IActionResult ResolveApplication(  string applicationId)
        {
            var queries = _db.Queries.Where(x => x.ApplicationId == applicationId && x.Status == "Has Query").ToList();
            if (queries.Count == 0)
            {
                var application = _db.ApplicationInfo.Where(x => x.Id == applicationId).FirstOrDefault();
                application.Status = "submitted";
                application.ExaminationStatus = "verification";
                _db.Update(application);
                _db.SaveChanges();
                return RedirectToAction("Dashboard", "Home");
            }
            else
            {
                return RedirectToAction("Finalising", new { Id = applicationId,error ="Please CLick resolve query where necessary in your forms and then submit to reslove" });
            }
        }

        [HttpGet("TaskAllocator")]
        public async Task<IActionResult> TaskAllocator(string applicationId)
        {
            var verifiers = await userManager.GetUsersInRoleAsync("Verifier");

            // Get task counts for each verifier
            var taskCounts = await _db.Tasks
                .Where(t => verifiers.Select(v => v.Id).Contains(t.VerifierId)  &&
                t.DateAdded.Month == DateTime.Now.Month) 
                .GroupBy(t => t.VerifierId)
                .Select(g => new { VerifierId = g.Key, TaskCount = g.Count() })
                .ToDictionaryAsync(x => x.VerifierId, x => x.TaskCount);

            // Find the verifier with the least tasks
            IdentityUser selectedUser = null;
            int minTaskCount = int.MaxValue;

            foreach (var verifier in verifiers)
            {
                int taskCount = taskCounts.ContainsKey(verifier.Id) ? taskCounts[verifier.Id] : 0;

                if (taskCount < minTaskCount)
                {
                    minTaskCount = taskCount;
                    selectedUser = verifier;
                    
                }
            }
          
        
            return View();
        }
}

}

