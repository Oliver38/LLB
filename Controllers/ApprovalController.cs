using DNTCaptcha.Core;
using LLB.Data;
using LLB.Models;
using LLB.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using Webdev.Payments;

namespace LLB.Controllers
{
    
    [Route("")]
    [Route("Approval")]
    public class ApprovalController : Controller
    {
        

        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public ApprovalController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }
        [HttpGet("Dashboard")]
        public async Task<IActionResult> DashboardAsync()
        {
            var user = await userManager.FindByEmailAsync(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = await BuildSecretaryDashboardViewModelAsync(user.Id);

            ViewBag.User = user;
            ViewData["Title"] = "Secretary Dashboard";
            ViewData["Subtitle"] = "Review assigned approval-stage applications and post formations from one queue";
            return View(model);
        }

        [Authorize(Roles = "secretary,admin,super user")]
        [HttpGet("Reports")]
        public async Task<IActionResult> Reports(string? province, string? council, string? region)
        {
            var model = await BuildSecretaryReportsViewModelAsync(province, council, region);

            ViewData["Title"] = "Secretary Reports";
            ViewData["Subtitle"] = "Filter approval-stage applications by province, council, and region";
            return View(model);
        }

        [Authorize(Roles = "secretary,admin,super user")]
        [HttpGet("ExportReportsCsv")]
        public async Task<FileResult> ExportReportsCsv(string? province, string? council, string? region)
        {
            var model = await BuildSecretaryReportsViewModelAsync(province, council, region);

            var csv = new StringBuilder();
            csv.AppendLine("Trading Name,Operating Address,Province,Council,Region,License,Application Date,Status,Application Id");

            foreach (var application in model.Applications)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(application.TradingName),
                    EscapeCsv(application.OperatingAddress),
                    EscapeCsv(application.Province),
                    EscapeCsv(application.Council),
                    EscapeCsv(application.Region),
                    EscapeCsv(application.LicenseName),
                    EscapeCsv(application.ApplicationDate.ToString("yyyy-MM-dd")),
                    EscapeCsv(application.Status),
                    EscapeCsv(application.ApplicationId)));
            }

            var fileName = $"secretary-reports-{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }


        [HttpGet(("Apply"))]
        public async Task<IActionResult> ApplyAsync(string Id, string error)
        {
            //using Microsoft.AspNetCore.Identity;
            var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var user = await userManager.FindByIdAsync(application.UserID);
            var licenses = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var task = _db.Tasks.Where(q => q.ApplicationId == Id && q.Status == "assigned").OrderByDescending(x => x.DateAdded).FirstOrDefault();

            ViewBag.Task = task;
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
            var task = _db.Tasks.Where(q => q.ApplicationId == Id && q.Status == "assigned").OrderByDescending(x => x.DateAdded).FirstOrDefault();

            ViewBag.Task = task;
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
        public IActionResult ManagersInfo(string Id,string error)
        {

            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var managersInfo = _db.ManagersParticulars.Where(b => b.ApplicationId == Id).ToList();
            var task = _db.Tasks.Where(q => q.ApplicationId == Id && q.Status == "assigned").OrderByDescending(x => x.DateAdded).FirstOrDefault();

            ViewBag.Task = task;
            TempData["result"] = error;
            ViewBag.ApplicationInfo = applicationInfo;
            ViewBag.ManagersInfo = managersInfo;


            return View();
        }



       

        [HttpGet(("Attachments"))]
        public async Task<IActionResult> AttachmentsAsync(string Id,string error)
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
            var task = _db.Tasks.Where(q => q.ApplicationId == Id && q.Status == "assigned").OrderByDescending(x => x.DateAdded).FirstOrDefault();

            ViewBag.Task = task;
            TempData["result"] = error;
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
            int managerscount = 0;
            int thecount = managers.Count();

            if (thecount > 1)
            {
                managerscount = thecount - 1;
            }
            else
            {
                managerscount = 0;
            }
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

            ViewBag.Task= task;
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
        
            paynow.ResultUrl = "https://llb.pfms.gov.zw/License/Submit?gateway=paynow";
            paynow.ReturnUrl = "https://llb.pfms.gov.zw/License/Finalising?Id=" + Id + "&gateway=paynow";
            //paynow.ResultUrl = "https://localhost:41018/License/Submit?gateway=paynow";
            //paynow.ReturnUrl = "https://localhost:41018/License/Finalising?Id=" + Id + "&gateway=paynow";

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
            if (queries.Stage == "Approve Application")
            {
                return RedirectToAction("Apply", new { Id = queries.ApplicationId, error = error });

            }else if (queries.Stage == "Approve Outlet")
            {
                return RedirectToAction("OutletInfo", new { Id = queries.ApplicationId, error = error });

            }
            else if (queries.Stage == "Approve Managers")
            {
                return RedirectToAction("ManagersInfo", new { Id = queries.ApplicationId, error = error });

            }
           
            else if (queries.Stage == "Approve Attachments")
            {
                return RedirectToAction("Attachments", new { Id = queries.ApplicationId, error = error });

            }
            else
            {
               
            }
            return RedirectToAction("Apply", new { Id = queries.ApplicationId, error = error });

        }

        //ask if it returns to same verifier or not
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
            return RedirectToAction("Dashboard", "Verify");
        }


            [HttpGet("Approve")]
        public async Task<IActionResult> ApproveAsync(string Id, string taskid)
        {
            var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            //LLB Numberl;l
            var outlet = _db.OutletInfo.Where(n => n.ApplicationId == Id).FirstOrDefault();
            outlet.Status = "active";
            outlet.DateUpdated = DateTime.Now;
            _db.Update(outlet);
            _db.SaveChanges();

            //district code
            var districtinfo = _db.DistrictCodes.Where(c => c.District == outlet.City).FirstOrDefault();
 
            DateTime now = DateTime.Now;

            // Extract last two digits of the year
            string lastTwoDigits = now.ToString("yy");

            var licensecode = _db.LicenseTypes.Where(a => a.Id == application.LicenseTypeID).FirstOrDefault();

            string llbnumber = districtinfo.DistrictCode.ToString() + lastTwoDigits + application.RefNum + licensecode.LicenseCode;

            application.LLBNum = llbnumber;
            application.Status = "approved";
            application.ExaminationStatus= "Approved";
            application.ApprovedDate = DateTime.Now;
            application.ExpiryDate = application.ApprovedDate.AddYears(1);
            _db.Update(application);
            _db.SaveChanges();

            var managers = _db.ManagersParticulars.Where(a => a.ApplicationId == Id).ToList();
            foreach(var manager in managers)
            {
                manager.Status = "active";
                manager.EffectiveDate = DateTime.Now;
                _db.Update(manager);
                _db.SaveChanges();
            }

            var task = _db.Tasks.Where(f => f.Id == taskid).FirstOrDefault();
            task.Status = "completed";
            _db.Update(task);
            _db.SaveChanges();
        
            return RedirectToAction("Dashboard", "Approval");
        }





        [HttpGet("Rejected")]
        public async Task<IActionResult> Rejected(string Id, string taskid)
        {
            var application = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            //LLB Numberl;l
            var outlet = _db.OutletInfo.Where(n => n.ApplicationId == Id).FirstOrDefault();
            outlet.Status = "inactive";
            outlet.DateUpdated = DateTime.Now;
            _db.Update(outlet);
            _db.SaveChanges();

            //district code
            var districtinfo = _db.DistrictCodes.Where(c => c.District == outlet.City).FirstOrDefault();

            DateTime now = DateTime.Now;

            // Extract last two digits of the year
            string lastTwoDigits = now.ToString("yy");

            var licensecode = _db.LicenseTypes.Where(a => a.Id == application.LicenseTypeID).FirstOrDefault();

            string llbnumber = districtinfo.DistrictCode.ToString() + lastTwoDigits + application.RefNum + licensecode.LicenseCode;

            application.LLBNum = llbnumber;
            application.Status = "rejected";
            application.ExaminationStatus = "Rejected";
            application.ApprovedDate = DateTime.Now;
            application.ExpiryDate = application.ApprovedDate.AddYears(1);
            _db.Update(application);
            _db.SaveChanges();

            var managers = _db.ManagersParticulars.Where(a => a.ApplicationId == Id).ToList();
            foreach (var manager in managers)
            {
                manager.Status = "inactive";
                manager.EffectiveDate = DateTime.Now;
                _db.Update(manager);
                _db.SaveChanges();
            }

            var task = _db.Tasks.Where(f => f.Id == taskid).FirstOrDefault();
            task.Status = "completed";
            _db.Update(task);
            _db.SaveChanges();

            return RedirectToAction("Dashboard", "Approval");
        }

        private async Task<SecretaryReportsViewModel> BuildSecretaryReportsViewModelAsync(
            string? province,
            string? council,
            string? region)
        {
            var model = new SecretaryReportsViewModel
            {
                ProvinceFilter = province?.Trim(),
                CouncilFilter = council?.Trim(),
                RegionFilter = region?.Trim()
            };

            var userEmail = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return model;
            }

            var currentUser = await userManager.FindByEmailAsync(userEmail);
            if (currentUser == null)
            {
                return model;
            }

            var secretaryTasks = await _db.Tasks
                .Where(task => task.ApproverId == currentUser.Id && task.Service == "new application")
                .OrderByDescending(task => task.DateUpdated)
                .ThenByDescending(task => task.DateAdded)
                .ToListAsync();

            var latestTaskByApplication = secretaryTasks
                .Where(task => !string.IsNullOrWhiteSpace(task.ApplicationId))
                .GroupBy(task => task.ApplicationId!, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToDictionary(task => task.ApplicationId!, StringComparer.OrdinalIgnoreCase);

            if (latestTaskByApplication.Count == 0)
            {
                return model;
            }

            var applicationIds = latestTaskByApplication.Keys.ToList();

            var applications = await _db.ApplicationInfo
                .Where(application => application.Id != null && applicationIds.Contains(application.Id))
                .ToListAsync();

            var outlets = await _db.OutletInfo
                .Where(outlet => outlet.ApplicationId != null && applicationIds.Contains(outlet.ApplicationId))
                .ToListAsync();

            var licenseLookup = await _db.LicenseTypes
                .Where(license => license.Id != null)
                .ToDictionaryAsync(license => license.Id!, license => license.LicenseName ?? "N/A");

            var regionLookup = await _db.LicenseRegions
                .Where(regionEntry => regionEntry.Id != null)
                .ToDictionaryAsync(regionEntry => regionEntry.Id!, regionEntry => regionEntry.RegionName ?? "Unspecified");

            var outletLookup = outlets
                .Where(outlet => !string.IsNullOrWhiteSpace(outlet.ApplicationId))
                .GroupBy(outlet => outlet.ApplicationId!, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(outlet => outlet.DateUpdated).ThenByDescending(outlet => outlet.DateAdded).First())
                .ToDictionary(outlet => outlet.ApplicationId!, StringComparer.OrdinalIgnoreCase);

            var allRows = applications
                .Where(application => !string.IsNullOrWhiteSpace(application.Id))
                .Select(application =>
                {
                    outletLookup.TryGetValue(application.Id!, out var outlet);
                    latestTaskByApplication.TryGetValue(application.Id!, out var task);

                    return new SecretaryReportRowViewModel
                    {
                        ApplicationId = application.Id!,
                        TradingName = NormalizeDimension(outlet?.TradingName, "N/A"),
                        OperatingAddress = NormalizeDimension(outlet?.Address ?? application.OperationAddress, "N/A"),
                        Province = NormalizeDimension(outlet?.Province),
                        Council = NormalizeDimension(outlet?.Council),
                        Region = application.ApplicationType != null && regionLookup.TryGetValue(application.ApplicationType, out var regionName)
                            ? NormalizeDimension(regionName)
                            : "Unspecified",
                        LicenseName = application.LicenseTypeID != null && licenseLookup.TryGetValue(application.LicenseTypeID, out var licenseName)
                            ? NormalizeDimension(licenseName, "N/A")
                            : "N/A",
                        Status = NormalizeDimension(application.Status ?? task?.Status, "Unknown"),
                        ApplicationDate = application.ApplicationDate
                    };
                })
                .OrderByDescending(application => application.ApplicationDate)
                .ToList();

            model.TotalApplications = allRows.Count;
            model.ProvinceOptions = allRows
                .Select(application => application.Province)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();
            model.CouncilOptions = allRows
                .Select(application => application.Council)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();
            model.RegionOptions = allRows
                .Select(application => application.Region)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToList();

            var filteredRows = allRows
                .Where(application => MatchesFilter(application.Province, model.ProvinceFilter))
                .Where(application => MatchesFilter(application.Council, model.CouncilFilter))
                .Where(application => MatchesFilter(application.Region, model.RegionFilter))
                .ToList();

            model.FilteredApplications = filteredRows.Count;
            model.DistinctProvinces = filteredRows
                .Select(application => application.Province)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            model.DistinctCouncils = filteredRows
                .Select(application => application.Council)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            model.DistinctRegions = filteredRows
                .Select(application => application.Region)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            model.ProvinceBreakdown = BuildSummary(filteredRows.Select(application => application.Province));
            model.CouncilBreakdown = BuildSummary(filteredRows.Select(application => application.Council));
            model.RegionBreakdown = BuildSummary(filteredRows.Select(application => application.Region));
            model.Applications = filteredRows;

            return model;
        }

        private async Task<SecretaryDashboardViewModel> BuildSecretaryDashboardViewModelAsync(string approverId)
        {
            var postFormationServices = new[] { "Extended Hours", "Temporary Retails" };

            var applicationTasks = await _db.Tasks
                .Where(task => task.ApproverId == approverId
                    && task.Status == "assigned"
                    && task.Service == "new application")
                .OrderByDescending(task => task.DateAdded)
                .ToListAsync();

            var postFormationTasks = await _db.Tasks
                .Where(task => task.ApproverId == approverId
                    && task.Status == "assigned"
                    && task.Service != null
                    && postFormationServices.Contains(task.Service))
                .OrderByDescending(task => task.DateAdded)
                .ToListAsync();

            var extendedHoursIds = postFormationTasks
                .Where(task => task.Service == "Extended Hours" && !string.IsNullOrWhiteSpace(task.ApplicationId))
                .Select(task => task.ApplicationId!)
                .Distinct()
                .ToList();

            var temporaryRetailIds = postFormationTasks
                .Where(task => task.Service == "Temporary Retails" && !string.IsNullOrWhiteSpace(task.ApplicationId))
                .Select(task => task.ApplicationId!)
                .Distinct()
                .ToList();

            var extendedHoursRecords = await _db.ExtendedHours
                .Where(record => record.Id != null && extendedHoursIds.Contains(record.Id))
                .ToListAsync();

            var temporaryRetailRecords = await _db.TemporaryRetails
                .Where(record => record.Id != null && temporaryRetailIds.Contains(record.Id))
                .ToListAsync();

            var applicationIds = applicationTasks
                .Where(task => !string.IsNullOrWhiteSpace(task.ApplicationId))
                .Select(task => task.ApplicationId!)
                .Concat(extendedHoursRecords
                    .Where(record => !string.IsNullOrWhiteSpace(record.ApplicationId))
                    .Select(record => record.ApplicationId!))
                .Concat(temporaryRetailRecords
                    .Where(record => !string.IsNullOrWhiteSpace(record.ApplicationId))
                    .Select(record => record.ApplicationId!))
                .Distinct()
                .ToList();

            var applications = await _db.ApplicationInfo
                .Where(application => application.Id != null && applicationIds.Contains(application.Id))
                .ToListAsync();

            var applicationLookup = applications.ToDictionary(application => application.Id!, application => application);

            var outlets = await _db.OutletInfo
                .Where(outlet => outlet.ApplicationId != null && applicationIds.Contains(outlet.ApplicationId))
                .ToListAsync();

            var outletLookup = outlets
                .GroupBy(outlet => outlet.ApplicationId!)
                .ToDictionary(group => group.Key, group => group.First());

            var licenseIds = applications
                .Where(application => !string.IsNullOrWhiteSpace(application.LicenseTypeID))
                .Select(application => application.LicenseTypeID!)
                .Distinct()
                .ToList();

            var regionIds = applications
                .Where(application => !string.IsNullOrWhiteSpace(application.ApplicationType))
                .Select(application => application.ApplicationType!)
                .Distinct()
                .ToList();

            var licenseLookup = await _db.LicenseTypes
                .Where(license => license.Id != null && licenseIds.Contains(license.Id))
                .ToDictionaryAsync(license => license.Id!, license => license);

            var regionLookup = await _db.LicenseRegions
                .Where(regionItem => regionItem.Id != null && regionIds.Contains(regionItem.Id))
                .ToDictionaryAsync(regionItem => regionItem.Id!, regionItem => regionItem);

            var extendedHoursLookup = extendedHoursRecords
                .ToDictionary(record => record.Id!, record => record);

            var temporaryRetailLookup = temporaryRetailRecords
                .ToDictionary(record => record.Id!, record => record);

            var model = new SecretaryDashboardViewModel();

            foreach (var task in applicationTasks)
            {
                if (string.IsNullOrWhiteSpace(task.ApplicationId)
                    || !applicationLookup.TryGetValue(task.ApplicationId, out var application))
                {
                    continue;
                }

                outletLookup.TryGetValue(task.ApplicationId, out var outlet);

                model.Applications.Add(new SecretaryDashboardApplicationItemViewModel
                {
                    ApplicationId = application.Id ?? string.Empty,
                    TradingName = outlet?.TradingName ?? application.BusinessName ?? "N/A",
                    OperatingAddress = outlet?.Address ?? application.OperationAddress ?? "N/A",
                    ApplicationDate = application.ApplicationDate,
                    LicenseName = application.LicenseTypeID != null && licenseLookup.TryGetValue(application.LicenseTypeID, out var licenseItem)
                        ? licenseItem.LicenseName ?? "N/A"
                        : "N/A",
                    RegionName = application.ApplicationType != null && regionLookup.TryGetValue(application.ApplicationType, out var regionItem)
                        ? regionItem.RegionName ?? "N/A"
                        : "N/A",
                    Status = application.Status ?? "Unknown",
                    ReviewUrl = $"/Approval/Apply?Id={application.Id}"
                });
            }

            foreach (var task in postFormationTasks)
            {
                if (string.IsNullOrWhiteSpace(task.ApplicationId))
                {
                    continue;
                }

                string rootApplicationId;
                DateTime submittedDate;
                string status;
                string reviewUrl;

                if (task.Service == "Extended Hours")
                {
                    if (!extendedHoursLookup.TryGetValue(task.ApplicationId, out var extendedHours)
                        || string.IsNullOrWhiteSpace(extendedHours.ApplicationId))
                    {
                        continue;
                    }

                    rootApplicationId = extendedHours.ApplicationId;
                    submittedDate = extendedHours.DateAdded;
                    status = extendedHours.Status ?? task.Status ?? "Unknown";
                    reviewUrl = $"/Extendedhours/ViewApplications?Id={extendedHours.Id}";
                }
                else if (task.Service == "Temporary Retails")
                {
                    if (!temporaryRetailLookup.TryGetValue(task.ApplicationId, out var temporaryRetail)
                        || string.IsNullOrWhiteSpace(temporaryRetail.ApplicationId))
                    {
                        continue;
                    }

                    rootApplicationId = temporaryRetail.ApplicationId;
                    submittedDate = temporaryRetail.DateAdded;
                    status = temporaryRetail.Status ?? task.Status ?? "Unknown";
                    reviewUrl = $"/TemporaryRetails/ViewApplications?Id={temporaryRetail.Id}";
                }
                else
                {
                    continue;
                }

                if (!applicationLookup.TryGetValue(rootApplicationId, out var application))
                {
                    continue;
                }

                outletLookup.TryGetValue(rootApplicationId, out var outlet);

                model.PostFormations.Add(new SecretaryDashboardPostFormationItemViewModel
                {
                    RecordId = task.ApplicationId,
                    ApplicationId = rootApplicationId,
                    Service = task.Service ?? "Post Formation",
                    TradingName = outlet?.TradingName ?? application.BusinessName ?? "N/A",
                    OperatingAddress = outlet?.Address ?? application.OperationAddress ?? "N/A",
                    SubmittedDate = submittedDate,
                    LicenseName = application.LicenseTypeID != null && licenseLookup.TryGetValue(application.LicenseTypeID, out var licenseItem)
                        ? licenseItem.LicenseName ?? "N/A"
                        : "N/A",
                    RegionName = application.ApplicationType != null && regionLookup.TryGetValue(application.ApplicationType, out var regionItem)
                        ? regionItem.RegionName ?? "N/A"
                        : "N/A",
                    Status = status,
                    ReviewUrl = reviewUrl
                });
            }

            return model;
        }

        private static List<SecretaryReportSummaryItemViewModel> BuildSummary(IEnumerable<string> values)
        {
            return values
                .Select(value => NormalizeDimension(value))
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Select(group => new SecretaryReportSummaryItemViewModel
                {
                    Name = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Name)
                .ToList();
        }

        private static bool MatchesFilter(string value, string? filter)
        {
            return string.IsNullOrWhiteSpace(filter)
                || string.Equals(value, filter.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDimension(string? value, string fallback = "Unspecified")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string EscapeCsv(string? value)
        {
            var safeValue = value ?? string.Empty;
            return $"\"{safeValue.Replace("\"", "\"\"")}\"";
        }
    }
}
