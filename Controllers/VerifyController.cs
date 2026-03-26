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
using Microsoft.EntityFrameworkCore;
using LLB.Helpers;
using LLB.Models.ViewModel;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;

namespace LLB.Controllers
{
    
    [Route("")]
    [Route("Verify")]
    public class VerifyController : Controller
    {
        

        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public VerifyController(TaskAllocationHelper taskAllocationHelper,AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
            _taskAllocationHelper = taskAllocationHelper;
        }
        [HttpGet("Dashboard")]
        public async Task<IActionResult> DashboardAsync()
        {

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;


            List<ApplicationInfo> appinfo = new List<ApplicationInfo>();
            //var tasks = _db.Tasks.Where(f => f.VerifierId == id  && f.Service == "new application" ).Where(x => x.Status == "assigned" || x.Status == "reassigned").ToList();
            var tasks = _db.Tasks.Where(f => f.VerifierId == id  && f.Service == "new application" ).Where(x => x.Status == "assigned" ).ToList();
            foreach(var task in tasks)
            {
                ApplicationInfo getinfo = new ApplicationInfo();

                var applications = _db.ApplicationInfo.Where(a => a.Id == task.ApplicationId).FirstOrDefault();

                getinfo = applications;
                appinfo.Add(getinfo);
            }


            List<RenewalViewModel> renewaltasks = new List<RenewalViewModel>();
            var rentasks = _db.Tasks.Where(f => f.VerifierId == id && f.Service == "renewal" && f.Status == "assigned").ToList();
            foreach (var rentask in rentasks)
            {
                RenewalViewModel getreninfo = new RenewalViewModel();

                var renapps = _db.Renewals.Where(a => a.ApplicationId == rentask.ApplicationId).FirstOrDefault();
                var renappinfo = _db.ApplicationInfo.Where(s => s.Id == renapps.ApplicationId).FirstOrDefault();
                var reaoutletinfo = _db.OutletInfo.Where(q => q.ApplicationId == renapps.ApplicationId).FirstOrDefault();
                var licensetype = _db.LicenseTypes.Where(w => w.Id == renappinfo.LicenseTypeID).FirstOrDefault();
                var licenseReg = _db.LicenseRegions.Where(e => e.Id == renappinfo.ApplicationType).FirstOrDefault();
                getreninfo.ApplicationId = renapps.ApplicationId;
                getreninfo.Id = renapps.Id;
                getreninfo.Reference = renapps.Reference;
                getreninfo.LLBNumber = renapps.LLBNumber;
                getreninfo.PreviousExpiry = renapps.PreviousExpiry;
                getreninfo.TradingName = reaoutletinfo.TradingName;
                getreninfo.Licensetype = licensetype.LicenseName;
                getreninfo.LicenseRegion = licenseReg.RegionName;
                getreninfo.Status = renapps.Status;
                getreninfo.TaskId = rentask.Id;

                renewaltasks.Add(getreninfo);
            }

            List<InspectionViewModel> renewalinspectiontasks = new List<InspectionViewModel>();
            var reninsptasks = _db.Tasks.Where(f => f.VerifierId == id && f.Service == "renewal inspection" && f.Status == "assigned").ToList();
            foreach(var insptask in reninsptasks)
            {
                var applId = insptask.ApplicationId;
                var appinfoq = _db.ApplicationInfo.Where(i => i.Id == applId).FirstOrDefault();
                var outletinfoq = _db.OutletInfo.Where(i => i.ApplicationId == applId).FirstOrDefault();
                var licensetype = _db.LicenseTypes.Where(a => a.Id == appinfoq.LicenseTypeID).FirstOrDefault();
                var licenseregion = _db.LicenseRegions.Where(a => a.Id == appinfoq.ApplicationType).FirstOrDefault();
                var inspecy = _db.Inspection.Where(s => s.ApplicationId == applId).OrderByDescending(z => z.DateApplied).FirstOrDefault();
                InspectionViewModel renewalinspectiontask = new InspectionViewModel();

                renewalinspectiontask.TradingName = outletinfoq.TradingName;
                renewalinspectiontask.LLBNumber = appinfoq.LLBNum;
                renewalinspectiontask.ApplicationId = applId;
                renewalinspectiontask.DateApplied = inspecy.DateApplied;
                renewalinspectiontask.Id = inspecy.Id;
                renewalinspectiontask.Reference = inspecy.Reference;
                renewalinspectiontask.Status = inspecy.Status;
                renewalinspectiontask.Service = inspecy.Service;
                renewalinspectiontask.LicenseType = licensetype.LicenseName;
                renewalinspectiontask.LicenseRegion = licenseregion.RegionName;
                renewalinspectiontask.TaskId = insptask.Id;
               

              
                renewalinspectiontasks.Add(renewalinspectiontask);


    }



            //List<InspectionViewModel>inspectiontasks = new List<InspectionViewModel>();
            //var insptasks = _db.Tasks.Where(f => f.VerifierId == id && f.Service == "inspection" && f.Status == "assigned").ToList();
            //foreach (var insptask in insptasks)
            //{
            //    var applId = insptask.ApplicationId;
            //    var appinfoq = _db.ApplicationInfo.Where(i => i.Id == applId).FirstOrDefault();
            //    var outletinfoq = _db.OutletInfo.Where(i => i.ApplicationId == applId).FirstOrDefault();
            //    var licensetype = _db.LicenseTypes.Where(a => a.Id == appinfoq.LicenseTypeID).FirstOrDefault();
            //    var licenseregion = _db.LicenseRegions.Where(a => a.Id == appinfoq.ApplicationType).FirstOrDefault();
            //    var inspecy = _db.Inspection.Where(s => s.ApplicationId == applId).OrderByDescending(z => z.DateApplied).FirstOrDefault();
            //    InspectionViewModel inspectiontask = new InspectionViewModel();

            //    inspectiontask.TradingName = outletinfoq.TradingName;
            //    inspectiontask.LLBNumber = appinfoq.LLBNum;
            //    inspectiontask.ApplicationId = applId;
            //    inspectiontask.DateApplied = inspecy.DateApplied;
            //    inspectiontask.Id = inspecy.Id;
            //    inspectiontask.Status = inspecy.Status;
            //    inspectiontask.Service = inspecy.Service;
            //    inspectiontask.LicenseType = licensetype.LicenseName;
            //    inspectiontask.LicenseRegion = licenseregion.RegionName;
            //    inspectiontask.TaskId = insptask.Id;


            //    inspectiontasks.Add(inspectiontask);


            //}
            //verification inspection
                 List<InspectionViewModel> verificationinspection = new List<InspectionViewModel>();
            var verinsptasks = _db.Tasks.Where(f => f.VerifierId == id && f.Service == "Verification Inspection" && f.Status == "assigned").ToList();
            foreach (var verinsptask in verinsptasks)
            {
                var applId = verinsptask.ApplicationId;
                var appinfoq = _db.ApplicationInfo.Where(i => i.Id == applId).FirstOrDefault();
                var outletinfoq = _db.OutletInfo.Where(i => i.ApplicationId == applId).FirstOrDefault();
                var licensetype = _db.LicenseTypes.Where(a => a.Id == appinfoq.LicenseTypeID).FirstOrDefault();
                var licenseregion = _db.LicenseRegions.Where(a => a.Id == appinfoq.ApplicationType).FirstOrDefault();
                var inspecy = _db.Inspection.Where(s => s.ApplicationId == applId).OrderByDescending(z => z.DateApplied).FirstOrDefault();
                InspectionViewModel inspectiontask = new InspectionViewModel();

                inspectiontask.TradingName = outletinfoq.TradingName;
                inspectiontask.LLBNumber = appinfoq.LLBNum;
                inspectiontask.ApplicationId = applId;
                inspectiontask.DateApplied = inspecy.DateApplied;
                inspectiontask.Id = inspecy.Id;
                inspectiontask.Reference = inspecy.Reference;
                inspectiontask.Status = inspecy.Status;
                inspectiontask.Service = inspecy.Service;
                inspectiontask.LicenseType = licensetype.LicenseName;
                inspectiontask.LicenseRegion = licenseregion.RegionName;
                inspectiontask.TaskId = verinsptask.Id;
                inspectiontask.InspectionSchedule = inspecy.InspectionSchedule;

                verificationinspection.Add(inspectiontask);


            }


            //verification inspection
            List<InspectionViewModel> directinspections = new List<InspectionViewModel>();
            var directinspection = _db.Tasks.Where(f => f.VerifierId == id && f.Service == "Inspection" && f.Status == "assigned").ToList();
            foreach (var directinspectiontask in directinspection)
            {
                var applIdd = directinspectiontask.ApplicationId;
                var appinfoqd = _db.ApplicationInfo.Where(i => i.Id == applIdd).FirstOrDefault();
                var outletinfoqd = _db.OutletInfo.Where(i => i.ApplicationId == applIdd).FirstOrDefault();
                var licensetyped = _db.LicenseTypes.Where(a => a.Id == appinfoqd.LicenseTypeID).FirstOrDefault();
                var licenseregiond = _db.LicenseRegions.Where(a => a.Id == appinfoqd.ApplicationType).FirstOrDefault();
                var inspecyd = _db.Inspection.Where(s => s.ApplicationId == applIdd).OrderByDescending(z => z.DateApplied).FirstOrDefault();
                InspectionViewModel inspectiontask = new InspectionViewModel();

                inspectiontask.TradingName = outletinfoqd.TradingName;
                inspectiontask.LLBNumber = appinfoqd.LLBNum;
                inspectiontask.ApplicationId = applIdd;
                inspectiontask.DateApplied = inspecyd.DateApplied;
                inspectiontask.Id = inspecyd.Id;
                inspectiontask.Reference = inspecyd.Reference;
                inspectiontask.Status = inspecyd.Status;
                inspectiontask.Service = inspecyd.Service;
                inspectiontask.LicenseType = licensetyped.LicenseName;
                inspectiontask.LicenseRegion = licenseregiond.RegionName;
                inspectiontask.TaskId = directinspectiontask.Id;
                inspectiontask.InspectionSchedule = inspecyd.InspectionSchedule;

                directinspections.Add(inspectiontask);


            }

            List<DuplicateDownloadViewModel> duplicateDownloads = new List<DuplicateDownloadViewModel>();
            var duplicateReviewTasks = _db.Tasks
                .Where(f => f.VerifierId == id
                    && f.Service == DownloadStatusHelper.DuplicateTaskService
                    && f.Status == "assigned")
                .ToList();
            foreach (var duplicateTask in duplicateReviewTasks)
            {
                var duplicateApplication = _db.ApplicationInfo.Where(a => a.Id == duplicateTask.ApplicationId).FirstOrDefault();
                if (duplicateApplication == null)
                {
                    continue;
                }

                var duplicateOutlet = _db.OutletInfo.Where(i => i.ApplicationId == duplicateApplication.Id).FirstOrDefault();
                var duplicateLicense = _db.LicenseTypes.Where(a => a.Id == duplicateApplication.LicenseTypeID).FirstOrDefault();
                var duplicateRegion = _db.LicenseRegions.Where(a => a.Id == duplicateApplication.ApplicationType).FirstOrDefault();
                var downloadRecord = DownloadStatusHelper.GetLicenseDownload(_db, duplicateApplication.LLBNum);
                var latestPayment = downloadRecord == null
                    ? null
                    : _db.Payments
                        .Where(p => p.ApplicationId == downloadRecord.Id
                            && (p.Service == DownloadStatusHelper.DuplicatePaymentService
                                || p.Service == DownloadStatusHelper.LegacyDuplicatePaymentService))
                        .OrderByDescending(p => p.DateAdded)
                        .FirstOrDefault();

                DuplicateDownloadViewModel duplicateDownload = new DuplicateDownloadViewModel();
                duplicateDownload.ApplicationId = duplicateApplication.Id;
                duplicateDownload.TaskId = duplicateTask.Id;
                duplicateDownload.LLBNumber = duplicateApplication.LLBNum;
                duplicateDownload.TradingName = duplicateOutlet?.TradingName;
                duplicateDownload.LicenseType = duplicateLicense?.LicenseName;
                duplicateDownload.LicenseRegion = duplicateRegion?.RegionName;
                duplicateDownload.DownloadStatus = downloadRecord?.Status;
                duplicateDownload.DuplicateStatus = downloadRecord?.PaymentStatus;
                duplicateDownload.PaymentStatus = latestPayment?.PaymentStatus;
                duplicateDownload.RequestedOn = downloadRecord?.DateApplied;
                duplicateDownloads.Add(duplicateDownload);
            }

            List<ExtendedHoursReviewViewModel> extendedHoursTasks = new List<ExtendedHoursReviewViewModel>();
            var assignedExtendedHoursTasks = _db.Tasks
                .Where(task => task.VerifierId == id
                    && task.Service == "Extended Hours"
                    && task.Status == "assigned")
                .OrderByDescending(task => task.DateAdded)
                .ToList();
            foreach (var extendedHoursTask in assignedExtendedHoursTasks)
            {
                var extendedHours = _db.ExtendedHours.Where(application => application.Id == extendedHoursTask.ApplicationId).FirstOrDefault();
                if (extendedHours == null || string.IsNullOrWhiteSpace(extendedHours.ApplicationId))
                {
                    continue;
                }

                var application = _db.ApplicationInfo.Where(info => info.Id == extendedHours.ApplicationId).FirstOrDefault();
                if (application == null)
                {
                    continue;
                }

                var outlet = _db.OutletInfo.Where(info => info.ApplicationId == application.Id).FirstOrDefault();
                var licenseType = _db.LicenseTypes.Where(licenseRecord => licenseRecord.Id == application.LicenseTypeID).FirstOrDefault();
                var licenseRegion = _db.LicenseRegions.Where(region => region.Id == application.ApplicationType).FirstOrDefault();
                var payment = _db.Payments
                    .Where(record => record.ApplicationId == extendedHours.Id
                        && record.Service == "extended hours")
                    .OrderByDescending(record => record.DateAdded)
                    .FirstOrDefault();

                ExtendedHoursReviewViewModel extendedHoursTaskModel = new ExtendedHoursReviewViewModel();
                extendedHoursTaskModel.Id = extendedHours.Id ?? string.Empty;
                extendedHoursTaskModel.TaskId = extendedHoursTask.Id ?? string.Empty;
                extendedHoursTaskModel.ApplicationId = application.Id ?? string.Empty;
                extendedHoursTaskModel.Reference = extendedHours.Reference ?? string.Empty;
                extendedHoursTaskModel.TradingName = outlet?.TradingName;
                extendedHoursTaskModel.LLBNumber = application.LLBNum;
                extendedHoursTaskModel.LicenseType = licenseType?.LicenseName;
                extendedHoursTaskModel.LicenseRegion = licenseRegion?.RegionName;
                extendedHoursTaskModel.Status = extendedHours.Status;
                extendedHoursTaskModel.PaymentStatus = payment?.PaymentStatus ?? payment?.Status ?? extendedHours.PaymentStatus;
                extendedHoursTaskModel.PaynowReference = payment?.PaynowRef;
                extendedHoursTaskModel.ReasonForExtention = extendedHours.ReasonForExtention;
                extendedHoursTaskModel.PaidFee = extendedHours.PaidFee;
                extendedHoursTaskModel.ExtendedHoursDate = extendedHours.ExtendedHoursDate;
                extendedHoursTaskModel.RequestedOn = extendedHours.DateAdded;
                extendedHoursTasks.Add(extendedHoursTaskModel);
            }

            List<TemporaryRetailReviewViewModel> temporaryRetailTasks = new List<TemporaryRetailReviewViewModel>();
            var assignedTemporaryRetailTasks = _db.Tasks
                .Where(task => task.VerifierId == id
                    && task.Service == "Temporary Retails"
                    && task.Status == "assigned")
                .OrderByDescending(task => task.DateAdded)
                .ToList();
            foreach (var temporaryRetailTask in assignedTemporaryRetailTasks)
            {
                var temporaryRetail = _db.TemporaryRetails.Where(application => application.Id == temporaryRetailTask.ApplicationId).FirstOrDefault();
                if (temporaryRetail == null || string.IsNullOrWhiteSpace(temporaryRetail.ApplicationId))
                {
                    continue;
                }

                var application = _db.ApplicationInfo.Where(info => info.Id == temporaryRetail.ApplicationId).FirstOrDefault();
                if (application == null)
                {
                    continue;
                }

                var outlet = _db.OutletInfo.Where(info => info.ApplicationId == application.Id).FirstOrDefault();
                var licenseType = _db.LicenseTypes.Where(licenseRecord => licenseRecord.Id == application.LicenseTypeID).FirstOrDefault();
                var licenseRegion = _db.LicenseRegions.Where(region => region.Id == application.ApplicationType).FirstOrDefault();
                var payment = _db.Payments
                    .Where(record => record.ApplicationId == temporaryRetail.Id
                        && (record.Service == "temporary retails" || record.Service == "Temporary Retails"))
                    .OrderByDescending(record => record.DateAdded)
                    .FirstOrDefault();

                TemporaryRetailReviewViewModel temporaryRetailTaskModel = new TemporaryRetailReviewViewModel();
                temporaryRetailTaskModel.Id = temporaryRetail.Id ?? string.Empty;
                temporaryRetailTaskModel.TaskId = temporaryRetailTask.Id ?? string.Empty;
                temporaryRetailTaskModel.ApplicationId = application.Id ?? string.Empty;
                temporaryRetailTaskModel.Reference = temporaryRetail.Reference ?? string.Empty;
                temporaryRetailTaskModel.TradingName = outlet?.TradingName;
                temporaryRetailTaskModel.LLBNumber = application.LLBNum;
                temporaryRetailTaskModel.LicenseType = licenseType?.LicenseName;
                temporaryRetailTaskModel.LicenseRegion = licenseRegion?.RegionName;
                temporaryRetailTaskModel.Status = temporaryRetail.Status;
                temporaryRetailTaskModel.PaymentStatus = payment?.PaymentStatus ?? payment?.Status ?? temporaryRetail.PaymentStatus;
                temporaryRetailTaskModel.PaynowReference = payment?.PaynowRef;
                temporaryRetailTaskModel.ReasonForExtention = temporaryRetail.ReasonForExtention;
                temporaryRetailTaskModel.LocationAddress = temporaryRetail.LocationAddress;
                temporaryRetailTaskModel.PaidFee = temporaryRetail.PaidFee;
                temporaryRetailTaskModel.TemporaryRetailDate = temporaryRetail.TemporaryRetailsDate;
                temporaryRetailTaskModel.RequestedOn = temporaryRetail.DateAdded;
                temporaryRetailTasks.Add(temporaryRetailTaskModel);
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
            ViewBag.Renewals = renewaltasks;
            ViewBag.RenewalIns = renewaltasks;
            ViewBag.RenewaInsTask = renewalinspectiontasks;
           // ViewBag.RenewalIns = renewaltasks;
          //  ViewBag.InsTask = inspectiontasks;
            ViewBag.VerificationInsTask = verificationinspection;
            ViewBag.DirectInsTask = directinspections;
            ViewBag.DuplicateTasks = duplicateDownloads;
            ViewBag.ExtendedHoursTasks = extendedHoursTasks;
            ViewBag.TemporaryRetailTasks = temporaryRetailTasks;
            //crete view on dashboard
            ;
            return View();
        }

        
             [HttpPost(("InspectionDate"))]
        public async Task<IActionResult> InspectionDate(string Id,DateTime InspectionDate)
        {
            var inspection = _db.Inspection.Where(a => a.Id == Id).FirstOrDefault();
            inspection.InspectionSchedule = InspectionDate;
            _db.Update(inspection);
            _db.SaveChanges();


            return RedirectToAction("Dashboard", "Home");
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

            var license = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();

            ViewBag.Regions = regions;
            ViewBag.License = license;
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


           // finaldata.ManagersCount = managerscount;
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
            var currentUserId = userManager.GetUserId(User);
            ViewBag.CurrentUser = currentUserId;


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
           // paynow.ResultUrl = "https://localhost:41018/License/Submit?gateway=paynow";
           // paynow.ReturnUrl = "https://localhost:41018/License/Finalising?Id=" + Id + "&gateway=paynow";

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
            if (queries.Stage == "Verify Application")
            {
                return RedirectToAction("Apply", new { Id = queries.ApplicationId, error = error });

            }else if (queries.Stage == "Verify Outlet")
            {
                return RedirectToAction("OutletInfo", new { Id = queries.ApplicationId, error = error });

            }
            else if (queries.Stage == "Verify Managers")
            {
                return RedirectToAction("ManagersInfo", new { Id = queries.ApplicationId, error = error });

            }
           
            else if (queries.Stage == "Verify Attachments")
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
            application.Status = "verified";
            application.ExaminationStatus= "Verification Inspection";
           // application.ExaminationStatus= "recommendation";
            _db.Update(application);
            _db.SaveChanges();

            var task = _db.Tasks.Where(f => f.Id == taskid).FirstOrDefault();
            task.Status = "completed";
            task.VerificationDate = DateTime.Now;
            _db.Update(task);
            _db.SaveChanges();

            Tasks tasks = new Tasks();
            tasks.Id = Guid.NewGuid().ToString();
            tasks.ApplicationId = application.Id;
            //tasks.AssignerId

            ////auto allocation to replace
            //var userId = await userManager.FindByEmailAsync("recommender@recommender.com");
            //tasks.RecommenderId = userId.Id;
            //tasks.AssignerId = "system";
            //tasks.Status = "assigned";
            //tasks.DateAdded = DateTime.Now;
            //tasks.DateUpdated = DateTime.Now;
            //_db.Add(tasks);
            //_db.SaveChanges();


            var managers = _db.ManagersParticulars.Where(a => a.ApplicationId == Id).ToList();
            foreach (var manager in managers)
            {
                manager.Status = "verified";
                manager.EffectiveDate = DateTime.Now;
                _db.Update(manager);
                _db.SaveChanges();
            }

            Inspection newinspection = new Inspection();
           
           

            newinspection.Id = Guid.NewGuid().ToString();
            newinspection.Service = "Verification Inspection"; ;

            newinspection.Status = "submitted";
            newinspection.UserId = application.UserID;
            newinspection.ApplicationId = application.Id; 
            newinspection.DateApplied = DateTime.Now;
            newinspection.DateUpdate = DateTime.Now;
            _db.Add(newinspection);
            _db.SaveChanges();



            //var verifierId = await TaskAllocator()
            Tasks tasksc = new Tasks();
            tasksc.Id = Guid.NewGuid().ToString();
            tasksc.ApplicationId = application.Id;
            //adding task reference

            //tasks.AssignerId

            //auto allocation to replace
           // var userId = userManager.GetUserId(User);
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            //var recommenderWithLeastTasks = await _taskAllocationHelper.GetRecommender(_db,userManager);

            tasksc.Service = "Verification Inspection";
            var userId = userManager.GetUserId(User);
            tasksc.ExaminationStatus = "verification";
            tasksc.VerifierId = userId;
            tasksc.AssignerId = "system";
            tasksc.Status = "assigned";
            tasksc.DateAdded = DateTime.Now;
            tasksc.DateUpdated = DateTime.Now;
            _db.Add(tasksc);
            _db.SaveChanges();


            return RedirectToAction("Dashboard", "Verify");
        }



        //FlagRejection

        [HttpPost("FlagRejection")]
        public async Task<IActionResult> FlagRejection(string id, string rejectionReason)
        {
            var application = _db.ApplicationInfo.Where(a => a.Id == id).FirstOrDefault();
            application.rejectionFlag = true;
            application.rejectionFlagComment = rejectionReason;
            var userId = userManager.GetUserId(User);
            application.FlaggerUserId = userId;
            _db.Update(application);
            _db.SaveChanges();

            return RedirectToAction("Finalising", new { Id = id, error = "application has been flagged for rejection"});

            return View();
        }

        [HttpGet("UnflagRejection")]
        public async Task<IActionResult> UnflagRejection(string id)
        {
            var application = _db.ApplicationInfo.Where(a => a.Id == id).FirstOrDefault();
            application.rejectionFlag = false;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Finalising", new { Id = id, error = "application unflagged" });

            return View();
        }



        [HttpGet("Renewal")]
        public async Task<IActionResult> Renewal(string id, string taskId)
        {

         
                RenewalViewModel getreninfo = new RenewalViewModel();

                var renapps = _db.Renewals.Where(a => a.Id == id).FirstOrDefault();
                var renappinfo = _db.ApplicationInfo.Where(s => s.Id == renapps.ApplicationId).FirstOrDefault();
            //var licensetypeId = renappinfo.LicenseTypeID;
            //var regiontypeId = renappinfo.ApplicantType;
            var reaoutletinfo = _db.OutletInfo.Where(q => q.ApplicationId == renapps.ApplicationId).FirstOrDefault();
                var licensetype = _db.LicenseTypes.Where(w => w.Id == renappinfo.LicenseTypeID).FirstOrDefault();
                var licenseReg = _db.LicenseRegions.Where(e => e.Id == renappinfo.ApplicationType).FirstOrDefault();

                getreninfo.ApplicationId = renapps.ApplicationId;
                getreninfo.Id = renapps.Id;
                getreninfo.Reference = renapps.Reference;
                getreninfo.LLBNumber = renapps.LLBNumber;
                getreninfo.PreviousExpiry = renapps.PreviousExpiry;
                getreninfo.TradingName = reaoutletinfo.TradingName;
                getreninfo.Licensetype = licensetype.LicenseName;
            getreninfo.PenaltyPaid = renapps.PenaltyPaid;
            getreninfo.FeePaid = renapps.FeePaid;
            getreninfo.LicenseRegion = licenseReg.RegionName;
            getreninfo.Licensetype = licensetype.LicenseName;
            getreninfo.Status = renapps.Status;
            getreninfo.HealthCert = renapps.HealthCert;
            getreninfo.CertifiedLicense = renapps.CertifiedLicense;
            getreninfo.OutletName = reaoutletinfo.TradingName;
            //   ViewBag.OutletInfo = outletinfo;
            //ViewBag.Regions = regions;
            //ViewBag.License = license;
            //ViewBag.Applications = appinfo;
            ViewBag.Renewals = getreninfo;
            ViewBag.TaskId = taskId;

            // var renewal
            return View();
        }

        [HttpGet("Duplicate")]
        public IActionResult Duplicate(string id, string taskId)
        {
            var application = _db.ApplicationInfo.Where(a => a.Id == id).FirstOrDefault();
            if (application == null)
            {
                TempData["result"] = "The duplicate request could not be found.";
                return RedirectToAction("Dashboard");
            }

            var outletInfo = _db.OutletInfo.Where(q => q.ApplicationId == application.Id).FirstOrDefault();
            var licenseType = _db.LicenseTypes.Where(w => w.Id == application.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(e => e.Id == application.ApplicationType).FirstOrDefault();
            var download = DownloadStatusHelper.GetLicenseDownload(_db, application.LLBNum);
            var payment = download == null
                ? null
                : _db.Payments
                    .Where(p => p.ApplicationId == download.Id
                        && (p.Service == DownloadStatusHelper.DuplicatePaymentService
                            || p.Service == DownloadStatusHelper.LegacyDuplicatePaymentService))
                    .OrderByDescending(p => p.DateAdded)
                    .FirstOrDefault();

            if (download == null)
            {
                TempData["result"] = "The duplicate request could not be found.";
                return RedirectToAction("Dashboard");
            }

            DuplicateDownloadViewModel duplicate = new DuplicateDownloadViewModel();
            duplicate.ApplicationId = application.Id;
            duplicate.TaskId = taskId;
            duplicate.LLBNumber = application.LLBNum;
            duplicate.TradingName = outletInfo?.TradingName;
            duplicate.LicenseType = licenseType?.LicenseName;
            duplicate.LicenseRegion = licenseRegion?.RegionName;
            duplicate.DownloadStatus = download.Status;
            duplicate.DuplicateStatus = download.PaymentStatus;
            duplicate.PaymentStatus = payment?.PaymentStatus;
            duplicate.RequestedOn = download.DateApplied;

            ViewBag.Duplicate = duplicate;
            ViewBag.Payment = payment;
            return View();
        }

        [HttpGet("ApproveDuplicate")]
        public IActionResult ApproveDuplicate(string id, string taskId)
        {
            var application = _db.ApplicationInfo.Where(a => a.Id == id).FirstOrDefault();
            if (application == null)
            {
                TempData["result"] = "The duplicate request could not be found.";
                return RedirectToAction("Dashboard");
            }

            var download = DownloadStatusHelper.OpenLicenseDownload(_db, application, application.UserID);
            if (download == null)
            {
                TempData["result"] = "The duplicate request could not be approved.";
                return RedirectToAction("Dashboard");
            }

            var task = _db.Tasks.Where(a => a.Id == taskId).FirstOrDefault();
            if (task != null)
            {
                task.VerificationDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                task.Status = "completed";
                _db.Update(task);
                _db.SaveChanges();
            }

            TempData["result"] = "Duplicate request approved. The licence is open for download again.";
            return RedirectToAction("Dashboard");
        }

        [HttpGet("RejectDuplicate")]
        public IActionResult RejectDuplicate(string id, string taskId)
        {
            var application = _db.ApplicationInfo.Where(a => a.Id == id).FirstOrDefault();
            if (application == null)
            {
                TempData["result"] = "The duplicate request could not be found.";
                return RedirectToAction("Dashboard");
            }

            var download = DownloadStatusHelper.GetOrCreateLicenseDownload(_db, application, application.UserID);
            if (download != null)
            {
                download.Status = DownloadStatusHelper.DownloadClosedStatus;
                download.PaymentStatus = DownloadStatusHelper.DuplicateRejectedStatus;
                download.DateUpdated = DateTime.Now;
                _db.Update(download);
                _db.SaveChanges();
            }

            var task = _db.Tasks.Where(a => a.Id == taskId).FirstOrDefault();
            if (task != null)
            {
                task.VerificationDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                task.Status = "completed";
                _db.Update(task);
                _db.SaveChanges();
            }

            TempData["result"] = "Duplicate request rejected. A new payment is required before another request can be examined.";
            return RedirectToAction("Dashboard");
        }

        [HttpGet("ExtendedHours")]
        public IActionResult ExtendedHours(string id, string taskId)
        {
            var model = BuildExtendedHoursReviewModel(id, taskId);
            if (model == null)
            {
                TempData["result"] = "The extended hours application could not be found.";
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }

        [HttpGet("ApproveExtendedHours")]
        public IActionResult ApproveExtendedHours(string id, string taskId)
        {
            var extendedHours = _db.ExtendedHours.Where(application => application.Id == id).FirstOrDefault();
            if (extendedHours == null || string.IsNullOrWhiteSpace(extendedHours.ApplicationId))
            {
                TempData["result"] = "The extended hours application could not be found.";
                return RedirectToAction("Dashboard");
            }

            var payment = _db.Payments
                .Where(record => record.ApplicationId == extendedHours.Id
                    && record.Service == "extended hours")
                .OrderByDescending(record => record.DateAdded)
                .FirstOrDefault();

            var paymentIsPaid = payment != null
                && (string.Equals(payment.Status, "Paid", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(payment.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase));

            if (!paymentIsPaid)
            {
                TempData["result"] = "The extended hours application cannot be approved before payment is confirmed.";
                return RedirectToAction("ExtendedHours", new { id, taskId });
            }

            var currentUserId = userManager.GetUserId(User);
            extendedHours.ApproverId = currentUserId;
            extendedHours.DateOfApproval = DateTime.Now;
            extendedHours.Status = "Approved";
            extendedHours.PaymentStatus = payment?.PaymentStatus ?? payment?.Status ?? extendedHours.PaymentStatus;
            extendedHours.DateUpdated = DateTime.Now;
            _db.Update(extendedHours);

            var task = _db.Tasks.Where(record => record.Id == taskId).FirstOrDefault();
            if (task != null)
            {
                task.VerificationDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                task.Status = "completed";
                _db.Update(task);
            }

            var application = _db.ApplicationInfo.Where(record => record.Id == extendedHours.ApplicationId).FirstOrDefault();
            if (application != null)
            {
                DownloadStatusHelper.OpenLicenseDownload(_db, application, application.UserID);
            }

            _db.SaveChanges();

            TempData["result"] = "Extended hours application approved.";
            return RedirectToAction("Dashboard");
        }

        [HttpGet("RejectExtendedHours")]
        public IActionResult RejectExtendedHours(string id, string taskId)
        {
            var extendedHours = _db.ExtendedHours.Where(application => application.Id == id).FirstOrDefault();
            if (extendedHours == null)
            {
                TempData["result"] = "The extended hours application could not be found.";
                return RedirectToAction("Dashboard");
            }

            var currentUserId = userManager.GetUserId(User);
            extendedHours.ApproverId = currentUserId;
            extendedHours.DateOfApproval = DateTime.Now;
            extendedHours.Status = "Rejected";
            extendedHours.DateUpdated = DateTime.Now;
            _db.Update(extendedHours);

            var task = _db.Tasks.Where(record => record.Id == taskId).FirstOrDefault();
            if (task != null)
            {
                task.VerificationDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                task.Status = "completed";
                _db.Update(task);
            }

            _db.SaveChanges();

            TempData["result"] = "Extended hours application rejected.";
            return RedirectToAction("Dashboard");
        }

        [HttpGet("TemporaryRetails")]
        public IActionResult TemporaryRetails(string id, string taskId)
        {
            var model = BuildTemporaryRetailReviewModel(id, taskId);
            if (model == null)
            {
                TempData["result"] = "The temporary retail application could not be found.";
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }

        [HttpGet("ApproveTemporaryRetails")]
        public IActionResult ApproveTemporaryRetails(string id, string taskId)
        {
            var temporaryRetail = _db.TemporaryRetails.Where(application => application.Id == id).FirstOrDefault();
            if (temporaryRetail == null || string.IsNullOrWhiteSpace(temporaryRetail.ApplicationId))
            {
                TempData["result"] = "The temporary retail application could not be found.";
                return RedirectToAction("Dashboard");
            }

            var payment = _db.Payments
                .Where(record => record.ApplicationId == temporaryRetail.Id
                    && (record.Service == "temporary retails" || record.Service == "Temporary Retails"))
                .OrderByDescending(record => record.DateAdded)
                .FirstOrDefault();

            var paymentIsPaid = payment != null
                && (string.Equals(payment.Status, "Paid", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(payment.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase));

            if (!paymentIsPaid)
            {
                TempData["result"] = "The temporary retail application cannot be approved before payment is confirmed.";
                return RedirectToAction("TemporaryRetails", new { id, taskId });
            }

            var currentUserId = userManager.GetUserId(User);
            temporaryRetail.ApproverId = currentUserId;
            temporaryRetail.DateOfApproval = DateTime.Now;
            temporaryRetail.Status = "Approved";
            temporaryRetail.PaymentStatus = payment?.PaymentStatus ?? payment?.Status ?? temporaryRetail.PaymentStatus;
            temporaryRetail.DateUpdated = DateTime.Now;
            _db.Update(temporaryRetail);

            var task = _db.Tasks.Where(record => record.Id == taskId).FirstOrDefault();
            if (task != null)
            {
                task.VerificationDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                task.Status = "completed";
                _db.Update(task);
            }

            var application = _db.ApplicationInfo.Where(record => record.Id == temporaryRetail.ApplicationId).FirstOrDefault();
            if (application != null)
            {
                DownloadStatusHelper.OpenLicenseDownload(_db, application, application.UserID);
            }

            _db.SaveChanges();

            TempData["result"] = "Temporary retail application approved.";
            return RedirectToAction("Dashboard");
        }

        [HttpGet("RejectTemporaryRetails")]
        public IActionResult RejectTemporaryRetails(string id, string taskId)
        {
            var temporaryRetail = _db.TemporaryRetails.Where(application => application.Id == id).FirstOrDefault();
            if (temporaryRetail == null)
            {
                TempData["result"] = "The temporary retail application could not be found.";
                return RedirectToAction("Dashboard");
            }

            var currentUserId = userManager.GetUserId(User);
            temporaryRetail.ApproverId = currentUserId;
            temporaryRetail.DateOfApproval = DateTime.Now;
            temporaryRetail.Status = "Rejected";
            temporaryRetail.DateUpdated = DateTime.Now;
            _db.Update(temporaryRetail);

            var task = _db.Tasks.Where(record => record.Id == taskId).FirstOrDefault();
            if (task != null)
            {
                task.VerificationDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                task.Status = "completed";
                _db.Update(task);
            }

            _db.SaveChanges();

            TempData["result"] = "Temporary retail application rejected.";
            return RedirectToAction("Dashboard");
        }

        
               [HttpGet("Inspect")]
        public async Task<IActionResult> Inspect(string id, string taskId)
        {


            //   List<InspectionViewModel> renewalinspectiontasks = new List<InspectionViewModel>();
            //var reninsptasks = _db.Tasks.Where(f => f.VerifierId == id && f.Service == "renewal inspection" && f.Status == "assigned").ToList();
            var reninsptasks = _db.Tasks.Where(f => f.Id == taskId).FirstOrDefault();
            //foreach (var insptask in reninsptasks)
            //{
            var applId = reninsptasks.ApplicationId;
                var appinfoq = _db.ApplicationInfo.Where(i => i.Id == applId).FirstOrDefault();
                var outletinfoq = _db.OutletInfo.Where(i => i.ApplicationId == applId).FirstOrDefault();
                var licensetype = _db.LicenseTypes.Where(a => a.Id == appinfoq.LicenseTypeID).FirstOrDefault();
                var licenseregion = _db.LicenseRegions.Where(a => a.Id == appinfoq.ApplicationType).FirstOrDefault();
                var inspecy = _db.Inspection.Where(s => s.ApplicationId == applId).OrderByDescending(z => z.DateApplied).FirstOrDefault();
                InspectionViewModel renewalinspectiontask = new InspectionViewModel();

                renewalinspectiontask.TradingName = outletinfoq.TradingName;
                renewalinspectiontask.LLBNumber = appinfoq.LLBNum;
                renewalinspectiontask.ApplicationId = applId;
                renewalinspectiontask.DateApplied = inspecy.DateApplied;
                renewalinspectiontask.Id = inspecy.Id;
                renewalinspectiontask.Reference = inspecy.Reference;
                renewalinspectiontask.Status = inspecy.Status;
                renewalinspectiontask.Service = inspecy.Service;
                renewalinspectiontask.LicenseType = licensetype.LicenseName;
                renewalinspectiontask.LicenseRegion = licenseregion.RegionName;
                renewalinspectiontask.TaskId = reninsptasks.Id;
            renewalinspectiontask.Comments = inspecy.Comments;


            //  renewalinspectiontasks.Add(renewalinspectiontask);
            

            //   }
            ViewBag.Inspection = renewalinspectiontask;
            ViewBag.TaskId = taskId;
            return View();
        }



        [HttpPost("Inspect")]
        public async Task<IActionResult> Inspect(Inspection inspection, string TaskId)
        {
            var inspec = _db.Inspection.Where(a => a.Id == inspection.Id).FirstOrDefault();
           // inspec = inspection;
            inspec.DateUpdate = DateTime.Now;
            inspec.InspectionDate = DateTime.Now;
            inspec.Status = "Inspected";
            inspec.Ventilation = inspection.Ventilation ;
            var userId = userManager.GetUserId(User);
            inspec.InspectorId = userId;

            inspec.Lighting  = inspection.Lighting;
            inspec.SewageDisposalAndDrainage  =inspection.SewageDisposalAndDrainage;
            inspec.Toilets = inspection.Toilets;
            inspec.WaterSupply = inspection.WaterSupply;
            inspec.RubbishDisposal  = inspection.RubbishDisposal;
            inspec.StandardOfFood  = inspection.StandardOfFood;
            inspec.FoodStorageArrangements  = inspection.FoodStorageArrangements;
            inspec.StaffUniformsAndAccommodation = inspection.StaffUniformsAndAccommodation;
            inspec.EquipmentAndAppointments = inspection.EquipmentAndAppointments; 
            inspec.HygieneStandards = inspection.HygieneStandards;
            inspec.Comments = inspection.Comments;
            inspec.Overall = inspection.Overall;
            _db.Update(inspec);
            _db.SaveChanges();
            if (inspec.Service == "Renewal Inspection")
            {
                var renewal = _db.Renewals.Where(z => z.ApplicationId == inspec.ApplicationId && z.Status == "verified").FirstOrDefault();

                if (inspec.Overall == "true")
                {
                    renewal.Inspector = userId;
                    renewal.DateUpdated = DateTime.Now;
                    _db.Update(renewal);
                    _db.SaveChanges();

                    var existingRecommendationTask = _db.Tasks
                        .Where(task => task.ApplicationId == inspec.ApplicationId
                            && task.Service == "renewal"
                            && task.Status == "assigned")
                        .FirstOrDefault();

                    if (existingRecommendationTask == null)
                    {
                        var recommenderWithLeastTasks = await _taskAllocationHelper.GetRecommender(_db, userManager);

                        Tasks recommendationTask = new Tasks();
                        recommendationTask.Id = Guid.NewGuid().ToString();
                        recommendationTask.ApplicationId = inspec.ApplicationId;
                        recommendationTask.ExaminationStatus = "recommendation";
                        recommendationTask.Service = "renewal";
                        recommendationTask.RecommenderId = recommenderWithLeastTasks;
                        recommendationTask.AssignerId = "system";
                        recommendationTask.Status = "assigned";
                        recommendationTask.DateAdded = DateTime.Now;
                        recommendationTask.DateUpdated = DateTime.Now;
                        _db.Add(recommendationTask);
                        _db.SaveChanges();
                    }

                    var updaterenewal = _db.ApplicationInfo.Where(c => c.Id == inspec.ApplicationId).FirstOrDefault();
                    if (updaterenewal != null)
                    {
                        updaterenewal.RenewalStatus = "recommendation";
                        updaterenewal.DateUpdated = DateTime.Now;
                        _db.Update(updaterenewal);
                        _db.SaveChanges();
                    }

                }
                else
                {
                    renewal.Status = "failed";
                    renewal.Inspector = userId;
                    renewal.DateUpdated = DateTime.Now;
                    _db.Update(renewal);
                    _db.SaveChanges();


                }
            }else if (inspec.Service == "Verification Inspection")
            { //getting rocomende and completing task

                Tasks tasksc = new Tasks();
                tasksc.Id = Guid.NewGuid().ToString();
                tasksc.ApplicationId = inspec.ApplicationId;
                //adding task reference

                //tasks.AssignerId

                var recommenderWithLeastTasks = await _taskAllocationHelper.GetRecommender(_db,userManager);
                tasksc.ExaminationStatus = "recommendation";
                tasksc.Service = "new application";
                tasksc.RecommenderId = recommenderWithLeastTasks;
                tasksc.AssignerId = "system";
                tasksc.Status = "assigned";
                tasksc.DateAdded = DateTime.Now;
                tasksc.DateUpdated = DateTime.Now;
                _db.Add(tasksc);
                _db.SaveChanges();

                var appinfogg = _db.ApplicationInfo.Where(a => a.Id == inspec.ApplicationId).FirstOrDefault();
                appinfogg.ExaminationStatus = "recommendation";
                _db.Update(appinfogg);
                _db.SaveChanges();
                // recommendation

            }
            else if (inspec.Service == "Inspection")
            {
                if (inspec.Overall == "true")
                {
                    var inspectedApplication = _db.ApplicationInfo.Where(a => a.Id == inspec.ApplicationId).FirstOrDefault();
                    DownloadStatusHelper.OpenLicenseDownload(_db, inspectedApplication, inspectedApplication?.UserID);
                }
            }

            var task = _db.Tasks.Where(a => a.Id == TaskId).FirstOrDefault();
            task.VerificationDate = DateTime.Now;
            task.Status = "completed";
            _db.Update(task);
            _db.SaveChanges();



            //   List<InspectionViewModel> renewalinspectiontasks = new List<InspectionViewModel>();
            //var reninsptasks = _db.Tasks.Where(f => f.VerifierId == id && f.Service == "renewal inspection" && f.Status == "assigned").ToList();
           // var reninsptasks = _db.Tasks.Where(f => f.Id == TaskId).FirstOrDefault();
            //foreach (var insptask in reninsptasks)
            //{
            var applId = inspec.ApplicationId;
            var appinfoq = _db.ApplicationInfo.Where(i => i.Id == applId).FirstOrDefault();
            var outletinfoq = _db.OutletInfo.Where(i => i.ApplicationId == applId).FirstOrDefault();
            var licensetype = _db.LicenseTypes.Where(a => a.Id == appinfoq.LicenseTypeID).FirstOrDefault();
            var licenseregion = _db.LicenseRegions.Where(a => a.Id == appinfoq.ApplicationType).FirstOrDefault();
            var inspecy = _db.Inspection.Where(s => s.ApplicationId == applId).OrderByDescending(z => z.DateApplied).FirstOrDefault();
            InspectionViewModel renewalinspectiontask = new InspectionViewModel();

            renewalinspectiontask.TradingName = outletinfoq.TradingName;
            renewalinspectiontask.LLBNumber = appinfoq.LLBNum;
            renewalinspectiontask.ApplicationId = applId;
            renewalinspectiontask.DateApplied = inspecy.DateApplied;
            renewalinspectiontask.Id = inspecy.Id;
            renewalinspectiontask.Status = inspecy.Status;
            renewalinspectiontask.Service = inspecy.Service;
            renewalinspectiontask.LicenseType = licensetype.LicenseName;
            renewalinspectiontask.LicenseRegion = licenseregion.RegionName;
            renewalinspectiontask.TaskId = inspec.Id;
            renewalinspectiontask.Comments = inspecy.Comments;


            //  renewalinspectiontasks.Add(renewalinspectiontask);


            //   }
            ViewBag.Inspection = renewalinspectiontask;
            ViewBag.TaskId = TaskId;

            return View();
        }

        [HttpGet("CompleteRenewalInspection")]
        public IActionResult CompleteRenewalInspection(string taskId)
        {
            var task = _db.Tasks.Where(a => a.Id == taskId).FirstOrDefault();
            if (task != null
                && string.Equals(task.Service, "renewal inspection", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(task.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                task.Status = "completed";
                task.VerificationDate = DateTime.Now;
                task.DateUpdated = DateTime.Now;
                _db.Update(task);
                _db.SaveChanges();
            }

            return RedirectToAction("Dashboard", "Home");
        }


            [HttpGet("ApproveRenewal")]
        public async Task<IActionResult> ApproveRenewal(string Id, string taskId)
        {
            //Add Inspection


            var appinfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();

            var renewalapp = _db.Renewals.Where(a => a.ApplicationId == appinfo.Id && a.Status == "submitted").FirstOrDefault();
            renewalapp.Status = "verified";
            renewalapp.DateUpdated = DateTime.Now;
            _db.Update(renewalapp);
            _db.SaveChanges();

            //


            Inspection inspection = new Inspection();
            inspection.Id  = Guid.NewGuid().ToString();
            inspection.Reference = ReferenceHelper.GeneratePostFormationReferenceNumber(_db, "RNI");
            inspection.renewalId = renewalapp.Id;
            inspection.ApplicationId = Id;
            inspection.Service = "Renewal Inspection";
            inspection.Status = "Awaiting Action";
            inspection.DateApplied = DateTime.Now;

            var userId = userManager.GetUserId(User);
            inspection.UserId = appinfo.UserID;
            _db.Add(inspection);
            _db.SaveChanges();
            //Add to task
            var oldtask = _db.Tasks.Where(q =>q.Id == taskId).FirstOrDefault();
            oldtask.Status = "completed";
            _db.Update(oldtask);
            _db.SaveChanges();


            Tasks tasksc = new Tasks();
            tasksc.Id = Guid.NewGuid().ToString();
            tasksc.ApplicationId =Id;

            //tasks.AssignerId

            //auto allocation to replace
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            var verifierWithLeastTasks = await _taskAllocationHelper.GetVerifier(_db, userManager);
            tasksc.ExaminationStatus = "verification";
            tasksc.Service = "renewal inspection";
            tasksc.VerifierId = verifierWithLeastTasks;
            tasksc.AssignerId = "system";
            tasksc.Status = "assigned";
            tasksc.DateAdded = DateTime.Now;
            tasksc.DateUpdated = DateTime.Now;
            _db.Add(tasksc);
            _db.SaveChanges();
            //
            return RedirectToAction("Dashboard", "Home");
        }

        private ExtendedHoursReviewViewModel? BuildExtendedHoursReviewModel(string extendedHoursId, string taskId)
        {
            var extendedHours = _db.ExtendedHours.Where(application => application.Id == extendedHoursId).FirstOrDefault();
            if (extendedHours == null || string.IsNullOrWhiteSpace(extendedHours.ApplicationId))
            {
                return null;
            }

            var application = _db.ApplicationInfo.Where(record => record.Id == extendedHours.ApplicationId).FirstOrDefault();
            if (application == null)
            {
                return null;
            }

            var outlet = _db.OutletInfo.Where(record => record.ApplicationId == application.Id).FirstOrDefault();
            var licenseType = _db.LicenseTypes.Where(record => record.Id == application.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(record => record.Id == application.ApplicationType).FirstOrDefault();
            var payment = _db.Payments
                .Where(record => record.ApplicationId == extendedHours.Id
                    && record.Service == "extended hours")
                .OrderByDescending(record => record.DateAdded)
                .FirstOrDefault();

            return new ExtendedHoursReviewViewModel
            {
                Id = extendedHours.Id ?? string.Empty,
                TaskId = taskId ?? string.Empty,
                ApplicationId = application.Id ?? string.Empty,
                Reference = extendedHours.Reference ?? string.Empty,
                TradingName = outlet?.TradingName,
                LLBNumber = application.LLBNum,
                LicenseType = licenseType?.LicenseName,
                LicenseRegion = licenseRegion?.RegionName,
                Status = extendedHours.Status,
                PaymentStatus = payment?.PaymentStatus ?? payment?.Status ?? extendedHours.PaymentStatus,
                PaynowReference = payment?.PaynowRef,
                ReasonForExtention = extendedHours.ReasonForExtention,
                PaidFee = extendedHours.PaidFee,
                ExtendedHoursDate = extendedHours.ExtendedHoursDate,
                RequestedOn = extendedHours.DateAdded
            };
        }

        private TemporaryRetailReviewViewModel? BuildTemporaryRetailReviewModel(string temporaryRetailId, string taskId)
        {
            var temporaryRetail = _db.TemporaryRetails.Where(application => application.Id == temporaryRetailId).FirstOrDefault();
            if (temporaryRetail == null || string.IsNullOrWhiteSpace(temporaryRetail.ApplicationId))
            {
                return null;
            }

            var application = _db.ApplicationInfo.Where(record => record.Id == temporaryRetail.ApplicationId).FirstOrDefault();
            if (application == null)
            {
                return null;
            }

            var outlet = _db.OutletInfo.Where(record => record.ApplicationId == application.Id).FirstOrDefault();
            var licenseType = _db.LicenseTypes.Where(record => record.Id == application.LicenseTypeID).FirstOrDefault();
            var licenseRegion = _db.LicenseRegions.Where(record => record.Id == application.ApplicationType).FirstOrDefault();
            var payment = _db.Payments
                .Where(record => record.ApplicationId == temporaryRetail.Id
                    && (record.Service == "temporary retails" || record.Service == "Temporary Retails"))
                .OrderByDescending(record => record.DateAdded)
                .FirstOrDefault();

            return new TemporaryRetailReviewViewModel
            {
                Id = temporaryRetail.Id ?? string.Empty,
                TaskId = taskId ?? string.Empty,
                ApplicationId = application.Id ?? string.Empty,
                Reference = temporaryRetail.Reference ?? string.Empty,
                TradingName = outlet?.TradingName,
                LLBNumber = application.LLBNum,
                LicenseType = licenseType?.LicenseName,
                LicenseRegion = licenseRegion?.RegionName,
                Status = temporaryRetail.Status,
                PaymentStatus = payment?.PaymentStatus ?? payment?.Status ?? temporaryRetail.PaymentStatus,
                PaynowReference = payment?.PaynowRef,
                ReasonForExtention = temporaryRetail.ReasonForExtention,
                LocationAddress = temporaryRetail.LocationAddress,
                PaidFee = temporaryRetail.PaidFee,
                TemporaryRetailDate = temporaryRetail.TemporaryRetailsDate,
                RequestedOn = temporaryRetail.DateAdded
            };
        }

        }
    }
