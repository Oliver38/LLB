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
using LLB.Services;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using static Google.Protobuf.Compiler.CodeGeneratorResponse.Types;
using NuGet.Packaging.Core;


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
        private readonly ICompanyRegistryClient _companyRegistryClient;

        public LicenseController(
            ICompanyRegistryClient companyRegistryClient,
            TaskAllocationHelper taskAllocationHelper,
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
            _taskAllocationHelper = taskAllocationHelper;
            _companyRegistryClient = companyRegistryClient;
        }


        [AllowAnonymous]
        [HttpGet(("Hex"))]
        public async Task<IActionResult> Hex(string Id)
        {

            //string hex = "FE99A6"; // take last 4 bytes
            //byte[] bytes = Enumerable.Range(0, hex.Length / 2)
            //                         .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16))
            //                         .ToArray();
            //Array.Reverse(bytes); // little-endian
            //string reversedHex = BitConverter.ToString(bytes).Replace("-", "");
            //uint tagId = Convert.ToUInt32(reversedHex, 16);  // use uint to avoid overflow
            //string padded = tagId.ToString("D10");
            //Console.WriteLine(padded);  // should give 0008715929
           // 4E8C
            string hex = "0F007FFA068C"; // last 3 bytes
            string newhex = hex.Substring(4, 6);
        

            int tagId = (int)Convert.ToInt32(newhex, 16);
            // string padded = tagId.ToString("D10");
            //int padded = tagId - 118334;
           // Console.WriteLine(padded);
            ViewBag.Padded = tagId;
            ViewBag.Hex = hex;
            //Console.WriteLine(padded);  // should give 0008715929
            return View();
        }

            [HttpGet(("Apply"))]
        public async Task<IActionResult> ApplyAsync(string Id, bool queryMode = false)
        {

            var userId = userManager.GetUserId(User);
            var incompleteapplications = _db.ApplicationInfo.Where(a => a.UserID == userId && a.Status == "inprogress").ToList();
            if(incompleteapplications.Count() > 2)
            {
                //condition will be set
            }
            //using Microsoft.AspNetCore.Identity;
            var application = string.IsNullOrWhiteSpace(Id)
                ? null
                : _db.ApplicationInfo.FirstOrDefault(a => a.Id == Id && a.UserID == userId);
            if (!string.IsNullOrWhiteSpace(Id) && application == null)
            {
                return NotFound();
            }
            var user = await userManager.FindByEmailAsync(User.Identity.Name);
            var licenses = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var queries = _db.Queries.Where(x => x.ApplicationId == Id && x.Status == "Has Query").ToList();
            var provinces = _db.Province.ToList().OrderBy(a =>a.Name);

            ViewBag.Queries = queries;
            ViewBag.ApplicationInfo = application;
            ViewBag.User = user;
            ViewBag.Regions = regions;
            ViewBag.License = licenses;
            ViewBag.Provinces = provinces;
            ViewBag.QueryStagePanel = BuildApplicantQueryStagePanel(
                Id,
                ApplicationQueryWorkflow.ApplicantStage,
                queryMode);
            return View();
        }



        [HttpPost(("Apply"))]
        public async Task<IActionResult> ApplyAsync(ApplicationInfo info,IFormFile IdCopyb,IFormFile Fingerprints, IFormFile FormFF, bool queryMode = false)
        {
            var currentUserId = userManager.GetUserId(User);

            if (string.Equals(info.ApplicantType, "Company", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(info.Id))
            {
                return BadRequest("Verify and adopt the company before submitting the application.");
            }

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
                if (IdCopyb != null)
                {
                    string picb = System.IO.Path.GetFileName(IdCopyb.FileName);
                    string dicb = System.IO.Path.GetExtension(IdCopyb.FileName);
                    string newname = "IDCopy_" + info.Id;
                    string path = System.IO.Path.Combine($"ApplicantInfo", newname + dicb);
                    string docpath = System.IO.Path.Combine($"wwwroot/ApplicantInfo", newname + dicb);
                    info.IdCopy = path;
                    using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                    {
                        await IdCopyb.CopyToAsync(fileStream);
                    }
                }
                else
                {
                   info.IdCopy = "";
                }

                //fingerprints
                if (Fingerprints != null)
                {
                    string picb = System.IO.Path.GetFileName(Fingerprints.FileName);
                    string dicb = System.IO.Path.GetExtension(Fingerprints.FileName);
                    string newname = "Fingerprints_" + info.Id;
                    string path = System.IO.Path.Combine($"ApplicantInfo",  newname + dicb);
                    string docpath = System.IO.Path.Combine($"wwwroot/ApplicantInfo", newname + dicb);
                    info.Fingerprints = path;
                    using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                    {
                        await Fingerprints.CopyToAsync(fileStream);
                    }
                }
                else
                {
                    info.Fingerprints = "";
                }

                //form 55
                if (FormFF != null)
                {
                    string picb = System.IO.Path.GetFileName(FormFF.FileName);
                    string dicb = System.IO.Path.GetExtension(FormFF.FileName);
                    string newname = "FormFF_" + info.Id;
                    string path = System.IO.Path.Combine($"ApplicantInfo", newname + dicb);
                    string docpath = System.IO.Path.Combine($"wwwroot/ApplicantInfo", newname + dicb);
                    info.FormFF = path;
                    using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                    {
                        await FormFF.CopyToAsync(fileStream);
                    }
                }
                else
                {
                    info.FormFF = "";
                }

                //        public string? idCopy { get; set; }
                //public string? pclearance { get; set; }
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
                var updateinfo = _db.ApplicationInfo
                    .FirstOrDefault(x => x.Id == info.Id && x.UserID == currentUserId);
                if (updateinfo == null)
                {
                    return NotFound();
                }

                updateinfo.OperationAddress = info.OperationAddress;
                updateinfo.BusinessName = info.BusinessName;
                updateinfo.LicenseTypeID = info.LicenseTypeID;
                updateinfo.ApplicationType = info.ApplicationType;
                updateinfo.ApplicantType = info.ApplicantType;

                updateinfo.PlaceOfBirth = "";
                //updateinfo.DateofEntryIntoZimbabwe = "";
                //updateinfo.PlaceOfEntry = "";

                if (!queryMode)
                {
                    updateinfo.Status = "inprogress";
                    updateinfo.ApplicationDate = DateTime.Now;
                    updateinfo.InspectorID = "";

                    //public DateTime InspectionDate
                    updateinfo.Secretary = "";
                }
                updateinfo.DateUpdated = DateTime.Now;

                if (updateinfo.IdCopy == null || updateinfo.IdCopy == "" || (queryMode && IdCopyb != null)) {
                    if (IdCopyb != null)
                    {
                        string picb = System.IO.Path.GetFileName(IdCopyb.FileName);
                        string dicb = System.IO.Path.GetExtension(IdCopyb.FileName);
                        string newname = "IDCopy_" + updateinfo.Id;
                        string path = System.IO.Path.Combine($"ApplicantInfo", newname + dicb);
                        string docpath = System.IO.Path.Combine($"wwwroot/ApplicantInfo", newname + dicb);
                        updateinfo.IdCopy = path;
                        using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                        {
                            await IdCopyb.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        info.IdCopy = "";
                    }
                }
                else
                { }


               // Fingerprints
                if(updateinfo.Fingerprints == null || updateinfo.Fingerprints == "" || (queryMode && Fingerprints != null)) {
                if (Fingerprints != null)
                {
                    string picb = System.IO.Path.GetFileName(Fingerprints.FileName);
                    string dicb = System.IO.Path.GetExtension(Fingerprints.FileName);
                    string newname = "Fingerprints_" + updateinfo.Id;
                    string path = System.IO.Path.Combine($"ApplicantInfo",  newname + dicb);
                    string docpath = System.IO.Path.Combine($"wwwroot/ApplicantInfo", newname + dicb);
                    updateinfo.Fingerprints = path;
                    using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                    {
                        await Fingerprints.CopyToAsync(fileStream);
                    }
                }
                else
                {
                    info.Fingerprints = "";
                }
                }
                else { }

                //form 55
                if (updateinfo.FormFF == null || updateinfo.FormFF == "" || (queryMode && FormFF != null))
                {
                    if (FormFF != null)
                    {
                        string picb = System.IO.Path.GetFileName(FormFF.FileName);
                        string dicb = System.IO.Path.GetExtension(FormFF.FileName);
                        string newname = "FormFF_" + updateinfo.Id;
                        string path = System.IO.Path.Combine($"ApplicantInfo", newname + dicb);
                        string docpath = System.IO.Path.Combine($"wwwroot/ApplicantInfo", newname + dicb);
                        updateinfo.FormFF = path;
                        using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                        {
                            await FormFF.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        info.FormFF = "";
                    }
                }
                else { }
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
                if (queryMode)
                {
                    return RedirectToAction("Apply", new { Id = info.Id, queryMode = true });
                }
                return View();
                //return View();
            }
        }


        //deleting clerance
        
            [HttpGet(("AppinfoFormFF"))]
        public IActionResult AppinfoClearance(string Id)
        {
            var appinfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            appinfo.FormFF = "";
            _db.Update(appinfo);
            _db.SaveChanges();


            return RedirectToAction("Apply","License", new {Id = Id });
        }


        [HttpGet(("AppinfoFingerprints"))]
        public IActionResult AppinfoFingerprints(string Id)
        {
            var appinfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            appinfo.Fingerprints = "";
            _db.Update(appinfo);
            _db.SaveChanges();


            return RedirectToAction("Apply", "License", new { Id = Id });
        }
        //deleting national id or passport
        [HttpGet(("AppinfoIdCopy"))]
        public IActionResult AppinfoIdCopy(string Id)
        {
            var appinfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            appinfo.IdCopy = "";
            _db.Update(appinfo);
            _db.SaveChanges();


            return RedirectToAction("Apply", "License", new { Id = Id });
        }

        [HttpGet(("OutletInfo"))]
        public IActionResult OutletInfo(string Id, bool queryMode = false)
        {
            // 00826805 - 0853 - 45c5 - 9fe3 - bce855854091
            var currentUserId = userManager.GetUserId(User);
            var application = _db.ApplicationInfo
                .FirstOrDefault(a => a.Id == Id && a.UserID == currentUserId);
            if (application == null)
            {
                return NotFound();
            }
            var outletInfo = _db.OutletInfo.Where(b => b.ApplicationId == Id).FirstOrDefault();
            var directorsInfo = _db.DirectorDetails.Where(b => b.ApplicationId == Id).ToList();
            var licenses = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            // var application = await _db.ApplicationInfo.FindAsync(dd.Id);
            var queries = _db.Queries.Where(x => x.ApplicationId == Id && x.Status == "Has Query").ToList();
            var provinces = _db.Province.ToList().OrderBy(a => a.Name);

            var region="";
            var regiondata = _db.LicenseRegions.Where(a => a.Id == application.ApplicationType).FirstOrDefault();
            if(regiondata != null)
            {
                region = regiondata.RegionName;
            }

            ViewBag.Region = region;
                ViewBag.Queries = queries;
            ViewBag.Application = application;
            ViewBag.OutletInfo = outletInfo;
            ViewBag.Directors = directorsInfo;
            ViewBag.Regions = regions;
            ViewBag.License = licenses;
            ViewBag.DirectorsCount = directorsInfo.Count();
            ViewBag.Provinces = provinces;
            ViewBag.QueryStagePanel = BuildApplicantQueryStagePanel(
                Id,
                ApplicationQueryWorkflow.OutletStage,
                queryMode);

            //ViewBag.User = user;

            return View();
        }

        [HttpPost(("OutletInfo"))]
        public async Task<IActionResult> OutletInfoAsync(OutletInfo outletInfo, bool queryMode = false)
        {

            var currentUserId = userManager.GetUserId(User);
            var ownedApplication = _db.ApplicationInfo
                .FirstOrDefault(application => application.Id == outletInfo.ApplicationId
                    && application.UserID == currentUserId);
            if (ownedApplication == null)
            {
                return NotFound();
            }

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
                var outletInfocb = _db.OutletInfo.Where(b => b.ApplicationId == outletInfo.ApplicationId).FirstOrDefault();
                if(outletInfocb != null)
                {
                   // return RedirectPermanent(Url.Action("OutletInfo", "Apply", new { Id = outletInfocb.ApplicationId }));
                    
            //return RedirectToAction("Finalising", new { Id = Id, error = error, gateway = gateway });

                      return RedirectToAction("Apply", new { Id = outletInfocb.ApplicationId });
                }
                outletInfo.Id = Guid.NewGuid().ToString();

                var userId = await userManager.FindByEmailAsync(User.Identity.Name);
                string id = userId.Id;
                outletInfo.UserId = id;
                outletInfo.Status = "Unsubmitted";
                outletInfo.DirectorNames = "";
                outletInfo.DateAdded = DateTime.Now;
                outletInfo.DateUpdated = DateTime.Now;
                if (!IsHotelLicense(outletInfo.LicenseTypeID))
                {
                    outletInfo.HotelDoubleRooms = null;
                    outletInfo.HotelSingleRooms = null;
                }

                _db.Add(outletInfo);
                _db.SaveChanges();


                // 00826805 - 0853 - 45c5 - 9fe3 - bce855854091
                var application = _db.ApplicationInfo.Where(a => a.Id == outletInfo.ApplicationId).FirstOrDefault();
                application.ApplicationType = outletInfo.ApplicationType;
                application.LicenseTypeID = outletInfo.LicenseTypeID;
                _db.Update(application);
                _db.SaveChanges();

                var outletInfob = _db.OutletInfo.Where(b => b.ApplicationId == outletInfo.ApplicationId).FirstOrDefault();
                var directorsInfo = _db.DirectorDetails.Where(b => b.ApplicationId == outletInfo.ApplicationId).ToList();
                // var application = await _db.ApplicationInfo.FindAsync(dd.Id);
                var queries = _db.Queries.Where(x => x.ApplicationId == outletInfo.ApplicationId && x.Status == "Has Query").ToList();
                var licenses = _db.LicenseTypes.ToList();
                var regions = _db.LicenseRegions.ToList();
                var provinces = _db.Province.ToList().OrderBy(a => a.Name);
                var region = "";
                var regiondata = _db.LicenseRegions.Where(a => a.Id == application.ApplicationType).FirstOrDefault();
                if (regiondata != null)
                {
                    region = regiondata.RegionName;
                }

                ViewBag.Region = region;
                ViewBag.Queries = queries;
                ViewBag.Application = application;
                ViewBag.OutletInfo = outletInfob;
                ViewBag.Directors = directorsInfo;
                ViewBag.Regions = regions;
                ViewBag.License = licenses;
                ViewBag.DirectorsCount = directorsInfo.Count();
                ViewBag.Provinces = provinces;

                TempData["result"] = "Applicant details successfully added";
                if (queryMode)
                {
                    return RedirectToAction("OutletInfo", new { Id = outletInfo.ApplicationId, queryMode = true });
                }
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
                updateOutletInfo.ApplicationType = outletInfo.ApplicationType;
                updateOutletInfo.LicenseTypeID = outletInfo.LicenseTypeID;
                if (IsHotelLicense(outletInfo.LicenseTypeID))
                {
                    updateOutletInfo.HotelDoubleRooms = outletInfo.HotelDoubleRooms;
                    updateOutletInfo.HotelSingleRooms = outletInfo.HotelSingleRooms;
                }
                else
                {
                    updateOutletInfo.HotelDoubleRooms = null;
                    updateOutletInfo.HotelSingleRooms = null;
                }

                updateOutletInfo.DateUpdated = DateTime.Now;
                _db.Update(updateOutletInfo);
                _db.SaveChanges();


             //   var applicationb = _db.ApplicationInfo.Where(a => a.Id == outletInfo.ApplicationId).FirstOrDefault();
               

                // 00826805 - 0853 - 45c5 - 9fe3 - bce855854091
                var application = _db.ApplicationInfo.Where(a => a.Id == outletInfo.ApplicationId).FirstOrDefault();
                application.ApplicationType = outletInfo.ApplicationType;
                application.LicenseTypeID = outletInfo.LicenseTypeID;
                _db.Update(application);
                _db.SaveChanges();

                var outletInfob = _db.OutletInfo.Where(b => b.ApplicationId == outletInfo.ApplicationId).FirstOrDefault();
                var directorsInfo = _db.DirectorDetails.Where(b => b.ApplicationId == outletInfo.ApplicationId).ToList();
                // var application = await _db.ApplicationInfo.FindAsync(dd.Id);
                var queries = _db.Queries.Where(x => x.ApplicationId == outletInfo.ApplicationId && x.Status == "Has Query").ToList();
                var licenses = _db.LicenseTypes.ToList();
                var regions = _db.LicenseRegions.ToList();
                var provinces = _db.Province.ToList().OrderBy(a => a.Name);

                var region = "";
                var regiondata = _db.LicenseRegions.Where(a => a.Id == application.ApplicationType).FirstOrDefault();
                if (regiondata != null)
                {
                    region = regiondata.RegionName;
                }

                ViewBag.Region = region;
                ViewBag.Queries = queries;
                ViewBag.Application = application;
                ViewBag.OutletInfo = outletInfob;
                ViewBag.Directors = directorsInfo;
                ViewBag.Regions = regions;
                ViewBag.License = licenses;
                ViewBag.DirectorsCount = directorsInfo.Count();
                ViewBag.Provinces = provinces;

                TempData["result"] = "Applicant details successfully Updated";
                if (queryMode)
                {
                    return RedirectToAction("OutletInfo", new { Id = outletInfo.ApplicationId, queryMode = true });
                }
                return View();

            }
            //ViewBag.User = user;


        }

        private bool IsHotelLicense(string? licenseTypeId)
        {
            if (string.IsNullOrWhiteSpace(licenseTypeId))
            {
                return false;
            }

            var licenseName = _db.LicenseTypes
                .Where(item => item.Id == licenseTypeId)
                .Select(item => item.LicenseName)
                .FirstOrDefault();

            return licenseName?.IndexOf("hotel", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task<ApplicationCompletenessResult> EvaluateApplicationCompletenessAsync(
            ApplicationInfo application,
            ApplicationUser applicant,
            CancellationToken cancellationToken)
        {
            var outlet = await _db.OutletInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ApplicationId == application.Id, cancellationToken);
            var directors = await _db.DirectorDetails
                .AsNoTracking()
                .Where(item => item.ApplicationId == application.Id)
                .ToListAsync(cancellationToken);
            var managers = await _db.ManagersParticulars
                .AsNoTracking()
                .Where(item => item.ApplicationId == application.Id)
                .ToListAsync(cancellationToken);
            var attachments = await _db.AttachmentInfo
                .AsNoTracking()
                .Where(item => item.ApplicationId == application.Id)
                .ToListAsync(cancellationToken);

            var applicantIsForeign = !string.Equals(
                applicant.Nationality?.Trim(),
                "Zimbabwe",
                StringComparison.OrdinalIgnoreCase);
            var companyNumberComplete = !string.Equals(
                    application.ApplicantType?.Trim(),
                    "Company",
                    StringComparison.OrdinalIgnoreCase)
                || IsCompletedValue(application.CompanyNumber);
            var entryInformationComplete = !applicantIsForeign
                || (IsCompletedValue(application.PlaceOfEntry)
                    && IsCompletedValue(application.DateofEntryIntoZimbabwe));

            var applicantInformationComplete =
                IsCompletedValue(applicant.Name)
                && IsCompletedValue(applicant.LastName)
                && IsCompletedValue(applicant.DOB)
                && IsCompletedValue(applicant.Gender)
                && IsCompletedValue(applicant.Nationality)
                && IsCompletedValue(applicant.NatID)
                && IsCompletedValue(application.ApplicantType)
                && IsCompletedValue(application.BusinessName)
                && IsCompletedValue(application.OperationAddress)
                && companyNumberComplete
                && entryInformationComplete
                && HasExistingWebRootFile(application.IdCopy)
                && HasExistingWebRootFile(application.Fingerprints)
                && HasExistingWebRootFile(application.FormFF);

            var directorInformationComplete = directors.Count >= 2
                && directors.All(director =>
                    IsCompletedValue(director.Name)
                    && IsCompletedValue(director.Surname)
                    && IsCompletedValue(director.NationalId)
                    && IsCompletedValue(director.Address)
                    && HasExistingWebRootFile(director.NatId)
                    && HasExistingWebRootFile(director.FingerPrints)
                    && HasExistingWebRootFile(director.Form55));

            var hotelRoomsComplete = outlet == null
                || !IsHotelLicense(outlet.LicenseTypeID ?? application.LicenseTypeID)
                || (outlet.HotelDoubleRooms.HasValue && outlet.HotelSingleRooms.HasValue);
            var outletFieldsComplete = outlet != null
                && IsCompletedValue(outlet.TradingName)
                && IsCompletedValue(outlet.Latitude)
                && IsCompletedValue(outlet.Longitude)
                && IsCompletedValue(outlet.LicenseTypeID)
                && IsCompletedValue(outlet.ApplicationType)
                && IsCompletedValue(outlet.Address)
                && IsCompletedValue(outlet.Province)
                && IsCompletedValue(outlet.City)
                && IsCompletedValue(outlet.Council)
                && hotelRoomsComplete;

            var managerInformationComplete = managers.Count >= 1
                && managers.All(manager =>
                    IsCompletedValue(manager.Name)
                    && IsCompletedValue(manager.Surname)
                    && IsCompletedValue(manager.NationalId)
                    && IsCompletedValue(manager.Address)
                    && HasExistingWebRootFile(manager.Attachment)
                    && HasExistingWebRootFile(manager.Fingerprints)
                    && HasExistingWebRootFile(manager.Form55));

            var documentInformationComplete = attachments.Count > 0
                && attachments.All(attachment =>
                    IsCompletedValue(attachment.DocumentTitle)
                    && HasExistingWebRootFile(attachment.DocumentLocation));

            return new ApplicationCompletenessResult
            {
                ApplicantInformationComplete = applicantInformationComplete,
                DirectorsComplete = directorInformationComplete,
                OutletInformationComplete = outletFieldsComplete && directorInformationComplete,
                ManagerInformationComplete = managerInformationComplete,
                DocumentInformationComplete = documentInformationComplete
            };
        }

        private static bool IsCompletedValue(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private sealed class ApplicationCompletenessResult
        {
            public bool ApplicantInformationComplete { get; init; }
            public bool DirectorsComplete { get; init; }
            public bool OutletInformationComplete { get; init; }
            public bool ManagerInformationComplete { get; init; }
            public bool DocumentInformationComplete { get; init; }

            public bool AllRequiredSectionsComplete =>
                ApplicantInformationComplete
                && OutletInformationComplete
                && ManagerInformationComplete
                && DocumentInformationComplete;
        }

        [HttpPost(("Director"))]
        public async Task<IActionResult> DirectorAsync(DirectorDetails directorDetails, IFormFile natid, IFormFile fingerprints , IFormFile form)
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


            if (natid != null)
            {
                string picb = System.IO.Path.GetFileName(natid.FileName);
                string dicb = System.IO.Path.GetExtension(natid.FileName);
                string newname = "NatId_" + directorDetails.Id;
                string path = System.IO.Path.Combine($"DirectorFingerprints", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/DirectorFingerprints", newname + dicb);
                directorDetails.NatId = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await natid.CopyToAsync(fileStream);
                }
            }
            else
            {
                directorDetails.NatId = "";
            }

            if (form != null)
            {
                string picb = System.IO.Path.GetFileName(form.FileName);
                string dicb = System.IO.Path.GetExtension(form.FileName);
                string newname = "Form55_" + directorDetails.Id;
                string path = System.IO.Path.Combine($"DirectorFingerprints", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/DirectorFingerprints", newname + dicb);
                directorDetails.Form55 = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await form.CopyToAsync(fileStream);
                }
            }
            else
            {
                directorDetails.Form55 = "";
            }


            if (fingerprints != null)
            {
                string picb = System.IO.Path.GetFileName(fingerprints.FileName);
                string dicb = System.IO.Path.GetExtension(fingerprints.FileName);
                string newname = "FingerPrints_"+directorDetails.Id;
                string path = System.IO.Path.Combine($"DirectorFingerprints", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/DirectorFingerprints", newname + dicb);
                directorDetails.FingerPrints = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await fingerprints.CopyToAsync(fileStream);
                }
            }
            else
            {
                directorDetails.FingerPrints = "";
            }

            _db.Add(directorDetails);
            _db.SaveChanges();

            return RedirectToAction("OutletInfo", new { Id = directorDetails.ApplicationId });
        }

        [HttpGet("directors/{id}")]
        public async Task<IActionResult> GetDirector(string id, CancellationToken cancellationToken)
        {
            var director = await FindOwnedDirectorAsync(id, cancellationToken);
            if (director == null)
            {
                return NotFound(new { success = false, message = "The director was not found." });
            }

            return Json(new { success = true, director = DirectorPayload(director) });
        }

        [HttpPost("directors/{id}")]
        public async Task<IActionResult> UpdateDirector(
            string id,
            [FromForm] UpdateDirectorRequest request,
            CancellationToken cancellationToken)
        {
            var director = await FindOwnedDirectorAsync(id, cancellationToken);
            if (director == null)
            {
                return NotFound(new { success = false, message = "The director was not found." });
            }

            if (string.IsNullOrWhiteSpace(request.Name)
                || string.IsNullOrWhiteSpace(request.Surname)
                || string.IsNullOrWhiteSpace(request.NationalId)
                || string.IsNullOrWhiteSpace(request.Address))
            {
                return BadRequest(new { success = false, message = "Name, surname, national ID and address are required." });
            }

            try
            {
                ValidateDirectorDocument(request.NatIdCopy);
                ValidateDirectorDocument(request.Fingerprints);
                ValidateDirectorDocument(request.Form55);
            }
            catch (InvalidDataException exception)
            {
                return BadRequest(new { success = false, message = exception.Message });
            }

            var previousNatId = director.NatId;
            var previousFingerprints = director.FingerPrints;
            var previousForm55 = director.Form55;

            director.Name = request.Name.Trim();
            director.Surname = request.Surname.Trim();
            director.NationalId = request.NationalId.Trim();
            director.Address = request.Address.Trim();
            director.Status = "active";
            director.DateUpdated = DateTime.Now;

            if (request.NatIdCopy is not null)
            {
                director.NatId = await SaveDirectorDocumentAsync(
                    request.NatIdCopy,
                    "NatId",
                    director.Id!,
                    cancellationToken);
            }

            if (request.Fingerprints is not null)
            {
                director.FingerPrints = await SaveDirectorDocumentAsync(
                    request.Fingerprints,
                    "FingerPrints",
                    director.Id!,
                    cancellationToken);
            }

            if (request.Form55 is not null)
            {
                director.Form55 = await SaveDirectorDocumentAsync(
                    request.Form55,
                    "Form55",
                    director.Id!,
                    cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);

            DeleteReplacedDirectorDocument(previousNatId, director.NatId);
            DeleteReplacedDirectorDocument(previousFingerprints, director.FingerPrints);
            DeleteReplacedDirectorDocument(previousForm55, director.Form55);

            return Json(new
            {
                success = true,
                message = "Director details updated successfully.",
                director = DirectorPayload(director)
            });
        }

        [HttpDelete("directors/{id}")]
        public async Task<IActionResult> DeleteDirector(string id, CancellationToken cancellationToken)
        {
            var director = await FindOwnedDirectorAsync(id, cancellationToken);
            if (director == null)
            {
                return NotFound(new { success = false, message = "The director was not found." });
            }

            var applicationId = director.ApplicationId;
            var documents = new[] { director.NatId, director.FingerPrints, director.Form55 };

            _db.DirectorDetails.Remove(director);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var document in documents)
            {
                TryDeleteDirectorDocument(document);
            }

            return Json(new
            {
                success = true,
                message = "Director deleted successfully.",
                applicationId
            });
        }

        private async Task<DirectorDetails?> FindOwnedDirectorAsync(string directorId, CancellationToken cancellationToken)
        {
            var userId = userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await (
                from director in _db.DirectorDetails
                join application in _db.ApplicationInfo
                    on director.ApplicationId equals application.Id
                where director.Id == directorId && application.UserID == userId
                select director)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private object DirectorPayload(DirectorDetails director)
        {
            return new
            {
                id = director.Id,
                applicationId = director.ApplicationId,
                name = director.Name,
                surname = director.Surname,
                nationalId = director.NationalId,
                address = director.Address,
                documents = new
                {
                    natIdCopyUrl = DirectorDocumentUrl(director.NatId),
                    fingerprintsUrl = DirectorDocumentUrl(director.FingerPrints),
                    form55Url = DirectorDocumentUrl(director.Form55)
                }
            };
        }

        private string? DirectorDocumentUrl(string? path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? null
                : Url.Content($"~/{path.Replace('\\', '/')}");
        }

        private static void ValidateDirectorDocument(IFormFile? file)
        {
            if (file == null)
            {
                return;
            }

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".jpg", ".jpeg", ".png"
            };
            var extension = Path.GetExtension(file.FileName);
            if (file.Length <= 0 || !allowedExtensions.Contains(extension))
            {
                throw new InvalidDataException("Director documents must be non-empty PDF, JPG or PNG files.");
            }
        }

        private static async Task<string> SaveDirectorDocumentAsync(
            IFormFile file,
            string prefix,
            string directorId,
            CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{prefix}_{directorId}{extension}";
            var relativePath = $"DirectorFingerprints/{fileName}";
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "DirectorFingerprints");
            Directory.CreateDirectory(directory);

            var absolutePath = Path.Combine(directory, fileName);
            await using var fileStream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(fileStream, cancellationToken);
            return relativePath;
        }

        private static void DeleteReplacedDirectorDocument(string? previousPath, string? currentPath)
        {
            if (!string.IsNullOrWhiteSpace(previousPath)
                && !string.Equals(previousPath, currentPath, StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteDirectorDocument(previousPath);
            }
        }

        private static void TryDeleteDirectorDocument(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var webRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            var normalizedPath = relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var absolutePath = Path.GetFullPath(Path.Combine(webRoot, normalizedPath));
            var allowedRoot = Path.Combine(webRoot, "DirectorFingerprints") + Path.DirectorySeparatorChar;

            if (absolutePath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase)
                && System.IO.File.Exists(absolutePath))
            {
                try
                {
                    System.IO.File.Delete(absolutePath);
                }
                catch (IOException)
                {
                    // The database operation has already succeeded; leave cleanup for maintenance.
                }
                catch (UnauthorizedAccessException)
                {
                    // The database operation has already succeeded; leave cleanup for maintenance.
                }
            }
        }


        // ManagersInfo

        [HttpGet(("ManagersInfo"))]
        public IActionResult ManagersInfo(string Id, bool queryMode = false)
        {
            var currentUserId = userManager.GetUserId(User);
            var applicationInfo = _db.ApplicationInfo
                .FirstOrDefault(a => a.Id == Id && a.UserID == currentUserId);
            if (applicationInfo == null)
            {
                return NotFound();
            }
            var managersInfo = _db.ManagersParticulars.Where(b => b.ApplicationId == Id).ToList();

            var queries = _db.Queries.Where(x => x.ApplicationId == Id && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
            ViewBag.ApplicationInfo = applicationInfo;
            ViewBag.ManagersInfo = managersInfo;
            ViewBag.QueryStagePanel = BuildApplicantQueryStagePanel(
                Id,
                ApplicationQueryWorkflow.ManagersStage,
                queryMode);


            return View();
        }



        [HttpPost("ManagersInfo")]
        public async Task<IActionResult> ManagersInfoAsync(ManagersParticulars manager, IFormFile file, IFormFile fileb , IFormFile form55, bool queryMode = false)
        {
            var currentUserId = userManager.GetUserId(User);
            var ownedApplication = _db.ApplicationInfo
                .FirstOrDefault(application => application.Id == manager.ApplicationId
                    && application.UserID == currentUserId);
            if (ownedApplication == null)
            {
                return NotFound();
            }
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

            manager.Id = Guid.NewGuid().ToString();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            manager.UserId = id;
            manager.Status = "UnSubmitted";
            manager.DateAdded = DateTime.Now;
            manager.DateUpdated = DateTime.Now;

            if (file != null)
            {
                string pic = System.IO.Path.GetFileName(file.FileName);
                string dic = System.IO.Path.GetExtension(file.FileName);
                string newname ="NatId_"+ manager.Id;
                string path = System.IO.Path.Combine($"ManagerFingerprints", newname + dic);
                string docpath = System.IO.Path.Combine($"wwwroot/ManagerFingerprints", newname + dic);
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
                string newname = "Fingerprints_" + manager.Id;
                string path = System.IO.Path.Combine($"ManagerFingerprints", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/ManagerFingerprints", newname + dicb);
                manager.Fingerprints = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await fileb.CopyToAsync(fileStream);
                }
            }
            else
            {
                manager.Fingerprints = "";
            }


            if (form55 != null)
            {
                string picb = System.IO.Path.GetFileName(form55.FileName);
                string dicb = System.IO.Path.GetExtension(form55.FileName);
                string newname = "Form55_" + manager.Id;
                string path = System.IO.Path.Combine($"ManagerFingerprints", newname + dicb);
                string docpath = System.IO.Path.Combine($"wwwroot/ManagerFingerprints", newname + dicb);
                manager.Form55 = path;
                using (Stream fileStream = new FileStream(docpath, FileMode.Create))
                {
                    await form55.CopyToAsync(fileStream);
                }
            }
            else
            {
                manager.Form55 = "";
            }


          
            _db.Add(manager);
            _db.SaveChanges();

            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == manager.ApplicationId).FirstOrDefault();
            var managersInfo = _db.ManagersParticulars.Where(b => b.ApplicationId == manager.ApplicationId).ToList();

            var queries = _db.Queries.Where(x => x.ApplicationId == manager.ApplicationId && x.Status == "Has Query").ToList();
            ViewBag.Queries = queries;
            ViewBag.ApplicationInfo = applicationInfo;
            ViewBag.ManagersInfo = managersInfo;
            TempData["result"] = "Manager details successfully added";

            if (queryMode)
            {
                return RedirectToAction("ManagersInfo", new { Id = manager.ApplicationId, queryMode = true });
            }

            return View();
        }

        [HttpPost("UpdateManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateManagerAsync(
            ManagersParticulars manager,
            IFormFile? file,
            IFormFile? fileb,
            IFormFile? form55,
            bool queryMode = false)
        {
            var currentUserId = userManager.GetUserId(User);
            var existingManager = await _db.ManagersParticulars
                .FirstOrDefaultAsync(item => item.Id == manager.Id
                    && item.ApplicationId == manager.ApplicationId
                    && _db.ApplicationInfo.Any(application => application.Id == item.ApplicationId
                        && application.UserID == currentUserId));
            if (existingManager == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(manager.Name)
                || string.IsNullOrWhiteSpace(manager.Surname)
                || string.IsNullOrWhiteSpace(manager.NationalId)
                || string.IsNullOrWhiteSpace(manager.Address))
            {
                TempData["error"] = "Complete the manager's name, surname, national ID and address.";
                return RedirectToAction("ManagersInfo", new { Id = manager.ApplicationId, queryMode });
            }

            existingManager.Name = manager.Name.Trim();
            existingManager.Surname = manager.Surname.Trim();
            existingManager.NationalId = manager.NationalId.Trim();
            existingManager.Address = manager.Address.Trim();
            existingManager.DateUpdated = DateTime.Now;

            try
            {
                if (file != null)
                {
                    existingManager.Attachment = await SaveManagerQueryDocumentAsync(file, "NatId", existingManager.Id!);
                }

                if (fileb != null)
                {
                    existingManager.Fingerprints = await SaveManagerQueryDocumentAsync(fileb, "Fingerprints", existingManager.Id!);
                }

                if (form55 != null)
                {
                    existingManager.Form55 = await SaveManagerQueryDocumentAsync(form55, "Form55", existingManager.Id!);
                }
            }
            catch (InvalidDataException exception)
            {
                TempData["error"] = exception.Message;
                return RedirectToAction("ManagersInfo", new { Id = manager.ApplicationId, queryMode });
            }

            await _db.SaveChangesAsync();
            TempData["result"] = "Manager details successfully updated";
            return RedirectToAction("ManagersInfo", new { Id = manager.ApplicationId, queryMode });
        }

        private static async Task<string> SaveManagerQueryDocumentAsync(
            IFormFile file,
            string prefix,
            string managerId)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".jpg", ".jpeg", ".png"
            };
            if (file.Length <= 0 || !allowedExtensions.Contains(extension))
            {
                throw new InvalidDataException("Manager documents must be non-empty PDF, JPG or PNG files.");
            }

            var fileName = $"{prefix}_{managerId}{extension}";
            var relativePath = Path.Combine("ManagerFingerprints", fileName);
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ManagerFingerprints");
            Directory.CreateDirectory(directory);
            var absolutePath = Path.Combine(directory, fileName);

            await using var fileStream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(fileStream);
            return relativePath;
        }


        [HttpGet(("Attachments"))]
        public async Task<IActionResult> AttachmentsAsync(string Id, bool queryMode = false)
        {
            var currentUserId = userManager.GetUserId(User);
            var ownedApplication = _db.ApplicationInfo
                .FirstOrDefault(application => application.Id == Id
                    && application.UserID == currentUserId);
            if (ownedApplication == null)
            {
                return NotFound();
            }

            var attachments = _db.AttachmentInfo.Where(b => b.ApplicationId == Id).ToList();
            var getinfo = _db.ApplicationInfo.Where(w => w.Id == Id).FirstOrDefault();

            if (attachments.Count() <= 0)
            {
                var userm = await userManager.FindByEmailAsync(User.Identity.Name);
                //string[] documents = null;
                if (userm.Nationality == "Zimbabwe")
                {

                    string[] documents = {
                        "Local Authority Letter of approval",
                        "Health Report",
                        "A3 Plan approved by local Environmental Health",
                        "Proof of publication in the Government Gazette",
                        "Advert",

                        "Lease/Deed documents",
                        "Tie Affidavit",


                };

                    //if company
                    if(getinfo.ApplicantType == "Company" )
                    {

                        string[] companydocs =
                        {
                            "CR6",
                            "CR14",
                            "Company Resolution",
                        };
                        documents = documents.Concat(companydocs).ToArray();
                    }

                    if (getinfo.ApplicantType == "Organisation" || getinfo.ApplicantType == "Club" || getinfo.ApplicantType == "Partnership")
                    {

                        string[] companydocs =
                        {
                           "CR6",
                            "CR14",
                            "Constitution",
                        };
                        documents = documents.Concat(companydocs).ToArray();
                    }
                   
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


                    string[] documents = {
                        "Vetted fingerprints",

                        //"Affidavit by transferee",
                        "Permit from the minister",
                        "Lease/Deed documents",
                        "Proof of publication in the Government Gazette",
                        "Advert",
                        "A3 Plan approved by local Environmental Health",
                        //"Manager Applicant Fingerprints",
                        "Letter From the Minister",
                         "Health Report",
                         "Tie Affidavit",

                };

                    //if company
                    if (getinfo.ApplicantType == "Company")
                    {

                        string[] companydocs =
                        {
                            "CR6",
                            "CR14",
                            "Company Resolution",
                        };
                        documents = documents.Concat(companydocs).ToArray();
                    }


                    if (getinfo.ApplicantType == "Organisation" || getinfo.ApplicantType == "Club" || getinfo.ApplicantType == "Partnership")
                    {

                        string[] companydocs =
                        {
                           "CR6",
                            "CR14",
                            "Constitution",
                        };
                        documents = documents.Concat(companydocs).ToArray();
                    }


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
            ViewBag.QueryStagePanel = BuildApplicantQueryStagePanel(
                Id,
                ApplicationQueryWorkflow.AttachmentsStage,
                queryMode);


            return View();
        }


        [HttpGet(("RemoveAttachments"))]
        public async Task<IActionResult> RemoveAttachments(string Id)
        {
            var attachment = _db.AttachmentInfo.Where(a => a.Id == Id).FirstOrDefault();
            attachment.DocumentLocation = "";
            attachment.DateUpdated = DateTime.Now;
            _db.Update(attachment);
            _db.SaveChanges();


            return RedirectToAction("Attachments", "License" , new {Id= attachment.ApplicationId });
        }


        [RequestSizeLimit(4194304)]
        [HttpPost("Attachments")]
        public async Task<IActionResult> AttachmentsAsync(AttachmentInfo attachment, IFormFile file, bool queryMode = false)
        {
            var currentUserId = userManager.GetUserId(User);
            var attachmentUpdate = _db.AttachmentInfo
                .FirstOrDefault(item => item.Id == attachment.Id
                    && item.ApplicationId == attachment.ApplicationId
                    && _db.ApplicationInfo.Any(application => application.Id == item.ApplicationId
                        && application.UserID == currentUserId));
            if (attachmentUpdate == null)
            {
                return NotFound();
            }
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

            if (queryMode)
            {
                return RedirectToAction("Attachments", new { Id = attachment.ApplicationId, queryMode = true });
            }

            return View();
        }



        [HttpGet("Finalising")]
        public async Task<IActionResult> FinalisingAsync(string Id, string error, string gateway)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var applicationInfo = await _db.ApplicationInfo
                .FirstOrDefaultAsync(
                    application => application.Id == Id && application.UserID == currentUser.Id);
            if (applicationInfo == null)
            {
                return NotFound();
            }

            if (gateway == "paynow")
            {
                var paymentTrans = _db.Payments.Where(s => s.ApplicationId == Id).OrderByDescending(x => x.DateAdded).FirstOrDefault();
                if (paymentTrans == null)
                {
                    TempData["result"] = "The Paynow payment was created in a different environment and cannot be verified here. Start the payment again from the live site.";
                }
                else
                {
                    try
                    {
                        var status = PaynowCurrencyHelper.PollTransaction(paymentTrans);

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
                    catch (Exception ex) when (PaynowCurrencyHelper.IsHashMismatchException(ex) || ex is InvalidOperationException)
                    {
                        TempData["result"] = "The Paynow payment could not be verified. If this was started from localhost, start the payment again from the live site.";
                    }
                }
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
                    try
                    {
                        var statusb = PaynowCurrencyHelper.PollTransaction(paymentTransb);

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
                    catch (Exception ex) when (PaynowCurrencyHelper.IsHashMismatchException(ex) || ex is InvalidOperationException)
                    {
                        TempData["result"] = "The Paynow payment could not be verified. If this was started from localhost, start the payment again from the live site.";
                    }
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
            finaldata.UserId = currentUser.Id;

            var completeness = await EvaluateApplicationCompletenessAsync(
                applicationInfo,
                currentUser,
                HttpContext.RequestAborted);
            finaldata.IsApplicantInformationComplete = completeness.ApplicantInformationComplete;
            finaldata.IsOutletInformationComplete = completeness.OutletInformationComplete;
            finaldata.IsManagerInformationComplete = completeness.ManagerInformationComplete;
            finaldata.IsDocumentInformationComplete = completeness.DocumentInformationComplete;
            finaldata.ApplicantInfo = completeness.ApplicantInformationComplete ? "Complete" : "Incomplete";
            finaldata.OutletInfo = completeness.OutletInformationComplete ? "Complete" : "Incomplete";
            finaldata.DirectorsInfo = completeness.DirectorsComplete ? "Complete" : "Incomplete";
            finaldata.ManagersInfo = completeness.ManagerInformationComplete ? "Complete" : "Incomplete";
            finaldata.DocumentInfo = completeness.DocumentInformationComplete ? "Complete" : "Incomplete";

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
            ViewBag.LatestZwgExchangeRate = await _db.ExchangeRate
                .Where(rate => rate.ZWGrate.HasValue && rate.ZWGrate.Value > 0)
                .OrderByDescending(rate => rate.DateUpdated)
                .ThenByDescending(rate => rate.DateAdded)
                .Select(rate => rate.ZWGrate)
                .FirstOrDefaultAsync();


            return View();
        }


        [HttpGet("PaynowPayment")]
        public async Task<IActionResult> PaynowPaymentAsync(string Id, double amount, string? currency = null)
        {
            //Id = "84aecb8d-4ec2-4ad5-86e8-971070a66b00";
            //amount = 55.7;
            PaynowCurrencyContext paymentCurrency;
            try
            {
                // The payment environment is controlled centrally by the accountant.
                paymentCurrency = PaynowCurrencyHelper.BuildPaymentContext(_db, (decimal)amount, currency);
            }
            catch (InvalidOperationException ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("Finalising", new { Id });
            }

            var paynow = PaynowCurrencyHelper.CreatePaynow(paymentCurrency);

            paynow.ResultUrl = PaynowCurrencyHelper.BuildReturnUrl("/License/Submit?gateway=paynow", paymentCurrency.PaymentMode);
            paynow.ReturnUrl = PaynowCurrencyHelper.BuildReturnUrl("/License/Finalising?Id=" + Id + "&gateway=paynow", paymentCurrency.PaymentMode);
           // paynow.ResultUrl = "https://localhost:41018/License/Submit?gateway=paynow";
            //paynow.ReturnUrl = "https://localhost:41018/License/Finalising?Id=" + Id + "&gateway=paynow";

            // The return url can be set at later stages. You might want to do this if you want to pass data to the return url (like the reference of the transaction)


            // Create a new payment 
            var payment = paynow.CreatePayment("12345");

            //payment.AuthEmail = "chimukaoliver@gmail.com";
            var applicationInfo = _db.ApplicationInfo.Where(a => a.Id == Id).FirstOrDefault();
            var licenseType = _db.LicenseTypes.Where(s => s.Id == applicationInfo.LicenseTypeID).FirstOrDefault();

            // Add items to the payment
            payment.Add(licenseType.LicenseName, paymentCurrency.PaynowAmount);
            
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
                PaynowCurrencyHelper.ApplyCurrency(transaction, paymentCurrency);
                transaction.ApplicationId = Id;
                transaction.Service = "new application";
                //   transaction.PaynowRef = payment.Reference;
                transaction.PollUrl = response.PollUrl();
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
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var submissionApplication = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    application => application.Id == Id && application.UserID == currentUser.Id);
            if (submissionApplication == null)
            {
                return NotFound();
            }

            var completeness = await EvaluateApplicationCompletenessAsync(
                submissionApplication,
                currentUser,
                HttpContext.RequestAborted);
            if (!completeness.AllRequiredSectionsComplete)
            {
                return RedirectToAction("Finalising", new
                {
                    Id,
                    error = "Complete every required section before submitting the application."
                });
            }

            var payment = _db.Payments.Where(s => s.ApplicationId == Id ).OrderByDescending(x => x.DateAdded).FirstOrDefault();
            //var paymentvet = _db.Payments.Where(c => c.ApplicationId == Id && c.Status == "POP").FirstOrDefault();

            if (payment == null || payment.PaymentStatus == "not paid" || payment.PaymentStatus == "Cancelled")
            {
                string error = "Please make payment to submit application";
                return RedirectToAction("Finalising", new { Id = Id, error = error });

                // var applicationInfo =
            }
            else if (payment.PaymentStatus == "Awaiting Delivery" || payment.PaymentStatus == "Cancelled")
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
                tasks.ExaminationStatus = "verification";
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

        [HttpGet("Queries")]
        public async Task<IActionResult> Queries(string applicationId)
        {
            var currentUserId = userManager.GetUserId(User);
            var application = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == applicationId && item.UserID == currentUserId);
            if (application == null)
            {
                return NotFound();
            }

            var outlet = await _db.OutletInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ApplicationId == applicationId);
            var queryRecords = await _db.Queries
                .AsNoTracking()
                .Where(item => item.ApplicationId == applicationId)
                .OrderBy(item => item.DateAdded)
                .ToListAsync();
            if (!string.Equals(application.Status, ApplicationQueryWorkflow.OpenStatus, StringComparison.OrdinalIgnoreCase))
            {
                queryRecords = queryRecords
                    .Where(query => !ApplicationQueryWorkflow.IsOpen(query))
                    .ToList();
            }

            var stages = ApplicationQueryWorkflow.StageOrder
                .Select(stageKey => new ApplicationQueryStageViewModel
                {
                    Key = stageKey,
                    Title = ApplicationQueryWorkflow.GetStageTitle(stageKey),
                    Description = ApplicationQueryWorkflow.GetStageDescription(stageKey),
                    Icon = ApplicationQueryWorkflow.GetStageIcon(stageKey),
                    Action = ApplicationQueryWorkflow.GetStageAction(stageKey),
                    Queries = queryRecords
                        .Where(query => ApplicationQueryWorkflow.NormalizeStageKey(query.Stage) == stageKey)
                        .Select(ToQueryItemViewModel)
                        .ToList()
                })
                .Where(stage => stage.Queries.Count > 0)
                .OrderBy(stage => stage.IsResolved ? 1 : 0)
                .ThenBy(stage => Array.IndexOf(ApplicationQueryWorkflow.StageOrder, stage.Key))
                .ToList();

            var model = new ApplicationQueriesViewModel
            {
                ApplicationId = application.Id ?? string.Empty,
                Reference = string.IsNullOrWhiteSpace(application.RefNum) ? "Draft" : application.RefNum,
                TradingName = outlet?.TradingName
                    ?? application.BusinessName
                    ?? "Licence application",
                ApplicationStatus = application.Status ?? string.Empty,
                ExaminationStatus = application.ExaminationStatus ?? string.Empty,
                Stages = stages
            };

            return View(model);
        }

        [HttpGet("InspectionQuery")]
        public async Task<IActionResult> InspectionQuery(string Id)
        {
            var currentUserId = userManager.GetUserId(User);
            var application = await _db.ApplicationInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == Id && item.UserID == currentUserId);
            if (application == null)
            {
                return NotFound();
            }

            var openInspectionQueries = await _db.Queries
                .AsNoTracking()
                .Where(query => query.ApplicationId == Id
                    && query.Status == ApplicationQueryWorkflow.OpenStatus)
                .OrderByDescending(query => query.DateAdded)
                .ToListAsync();
            openInspectionQueries = openInspectionQueries
                .Where(query => ApplicationQueryWorkflow.NormalizeStageKey(query.Stage)
                    == ApplicationQueryWorkflow.InspectionStage)
                .ToList();

            if (!string.Equals(application.Status, ApplicationQueryWorkflow.OpenStatus, StringComparison.OrdinalIgnoreCase)
                || openInspectionQueries.Count == 0)
            {
                TempData["error"] = "There is no open verification inspection query for this application.";
                return RedirectToAction("Queries", new { applicationId = Id });
            }

            var inspection = await _db.Inspection
                .AsNoTracking()
                .Where(record => record.ApplicationId == Id
                    && record.Service == "Verification Inspection")
                .OrderByDescending(record => record.InspectionDate)
                .ThenByDescending(record => record.DateApplied)
                .FirstOrDefaultAsync();
            var outlet = await _db.OutletInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(record => record.ApplicationId == Id);

            var failedItems = inspection == null
                ? new List<string>()
                : new (string Label, string? Value)[]
                {
                    ("Ventilation", inspection.Ventilation),
                    ("Lighting", inspection.Lighting),
                    ("Sewage disposal and drainage", inspection.SewageDisposalAndDrainage),
                    ("Toilets", inspection.Toilets),
                    ("Water supply", inspection.WaterSupply),
                    ("Rubbish disposal", inspection.RubbishDisposal),
                    ("Standard of food", inspection.StandardOfFood),
                    ("Food storage arrangements", inspection.FoodStorageArrangements),
                    ("Staff uniforms and accommodation", inspection.StaffUniformsAndAccommodation),
                    ("Equipment and appointments", inspection.EquipmentAndAppointments),
                    ("Hygiene standards", inspection.HygieneStandards)
                }
                .Where(item => string.Equals(item.Value, "false", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Label)
                .ToList();

            var model = new VerificationInspectionQueryViewModel
            {
                ApplicationId = application.Id ?? string.Empty,
                Reference = string.IsNullOrWhiteSpace(application.RefNum) ? "Draft" : application.RefNum,
                TradingName = outlet?.TradingName ?? application.BusinessName ?? "Licence application",
                InspectionReference = inspection?.Reference ?? "Not assigned",
                InspectionDate = inspection?.InspectionDate ?? default,
                InspectorComments = string.IsNullOrWhiteSpace(inspection?.Comments)
                    ? "Review the query instructions below and correct the premises before requesting another inspection."
                    : inspection.Comments,
                FailedItems = failedItems,
                Queries = openInspectionQueries.Select(ToQueryItemViewModel).ToList()
            };

            return View(model);
        }

        [HttpPost("ResolveQueryStage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveQueryStage(string applicationId, string stage)
        {
            var stageKey = ApplicationQueryWorkflow.NormalizeStageKey(stage);
            if (!ApplicationQueryWorkflow.IsKnownStage(stageKey))
            {
                return BadRequest();
            }

            var currentUserId = userManager.GetUserId(User);
            var application = await _db.ApplicationInfo
                .FirstOrDefaultAsync(item => item.Id == applicationId && item.UserID == currentUserId);
            if (application == null)
            {
                return NotFound();
            }

            if (!string.Equals(application.Status, ApplicationQueryWorkflow.OpenStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This application is not currently awaiting query corrections.";
                return RedirectToAction("Queries", new { applicationId });
            }

            var stageQueries = await _db.Queries
                .Where(query => query.ApplicationId == applicationId
                    && query.Status == ApplicationQueryWorkflow.OpenStatus)
                .ToListAsync();
            stageQueries = stageQueries
                .Where(query => ApplicationQueryWorkflow.NormalizeStageKey(query.Stage) == stageKey)
                .ToList();

            if (stageQueries.Count == 0)
            {
                TempData["error"] = "There are no open queries for that stage.";
                return RedirectToAction("Queries", new { applicationId });
            }

            var now = DateTime.Now;
            foreach (var query in stageQueries)
            {
                query.Status = ApplicationQueryWorkflow.ResolvedStatus;
                query.DateUpdated = now;
            }

            await _db.SaveChangesAsync();
            TempData["success"] = $"{ApplicationQueryWorkflow.GetStageTitle(stageKey)} marked as resolved.";
            return RedirectToAction("Queries", new { applicationId });
        }

        [HttpPost("ResubmitQueriedApplication")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitQueriedApplication(string applicationId)
        {
            var currentUserId = userManager.GetUserId(User);
            var application = await _db.ApplicationInfo
                .FirstOrDefaultAsync(item => item.Id == applicationId && item.UserID == currentUserId);
            if (application == null)
            {
                return NotFound();
            }

            if (!string.Equals(application.Status, ApplicationQueryWorkflow.OpenStatus, StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This application is not ready for query resubmission.";
                return RedirectToAction("Queries", new { applicationId });
            }

            var applicationQueries = await _db.Queries
                .Where(query => query.ApplicationId == applicationId)
                .ToListAsync();
            if (applicationQueries.Count == 0
                || applicationQueries.Any(ApplicationQueryWorkflow.IsOpen))
            {
                TempData["error"] = "Resolve every open stage before resubmitting the application.";
                return RedirectToAction("Queries", new { applicationId });
            }

            var isVerificationInspectionResubmission = string.Equals(
                    application.ExaminationStatus,
                    "Verification Inspection Query",
                    StringComparison.OrdinalIgnoreCase)
                && applicationQueries.Any(query => ApplicationQueryWorkflow.NormalizeStageKey(query.Stage)
                    == ApplicationQueryWorkflow.InspectionStage);

            var verifiers = await userManager.GetUsersInRoleAsync("verifier");
            if (verifiers.Count == 0)
            {
                TempData["error"] = "No verification examiner is currently available. Please try again later.";
                return RedirectToAction("Queries", new { applicationId });
            }

            var verifierIds = verifiers.Select(verifier => verifier.Id).ToList();
            var assignedCounts = await _db.Tasks
                .Where(task => task.Status == "assigned"
                    && task.VerifierId != null
                    && verifierIds.Contains(task.VerifierId))
                .GroupBy(task => task.VerifierId!)
                .Select(group => new { VerifierId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.VerifierId, item => item.Count);
            var selectedVerifier = verifiers
                .OrderBy(verifier => assignedCounts.TryGetValue(verifier.Id, out var count) ? count : 0)
                .ThenBy(verifier => verifier.Id)
                .First();

            var now = DateTime.Now;
            var existingActiveTasks = await _db.Tasks
                .Where(task => task.ApplicationId == applicationId && task.Status == "assigned")
                .ToListAsync();
            foreach (var activeTask in existingActiveTasks)
            {
                activeTask.Status = "completed";
                activeTask.DateUpdated = now;
            }

            application.Status = isVerificationInspectionResubmission ? "verified" : "submitted";
            application.ExaminationStatus = isVerificationInspectionResubmission
                ? "Verification Inspection"
                : "verification";
            application.DateUpdated = now;

            if (isVerificationInspectionResubmission)
            {
                _db.Inspection.Add(new Inspection
                {
                    Id = Guid.NewGuid().ToString(),
                    Service = "Verification Inspection",
                    Status = "submitted",
                    UserId = application.UserID,
                    ApplicationId = application.Id,
                    DateApplied = now,
                    DateUpdate = now
                });
            }

            _db.Tasks.Add(new Tasks
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationId = application.Id,
                AssignerId = "system",
                VerifierId = selectedVerifier.Id,
                ExaminationStatus = "verification",
                Service = isVerificationInspectionResubmission
                    ? "Verification Inspection"
                    : "new application",
                Status = "assigned",
                DateAdded = now,
                DateUpdated = now
            });

            await _db.SaveChangesAsync();
            TempData["success"] = isVerificationInspectionResubmission
                ? "Corrections submitted and a new verification inspection has been allocated."
                : "Application resubmitted and allocated for verification.";
            return RedirectToAction("Dashboard", "Home");
        }

        [HttpGet("ResolveQuery")]
        public IActionResult ResolveQuery(string applicationId)
        {
            return RedirectToAction("Queries", new { applicationId });
        }

        [HttpGet("ResolveApplication")]
        public IActionResult ResolveApplication(string applicationId)
        {
            return RedirectToAction("Queries", new { applicationId });
        }

        private ApplicantQueryStagePanelViewModel? BuildApplicantQueryStagePanel(
            string? applicationId,
            string stageKey,
            bool queryMode)
        {
            if (!queryMode || string.IsNullOrWhiteSpace(applicationId))
            {
                return null;
            }

            var applicationHasQueries = _db.ApplicationInfo.Any(application => application.Id == applicationId
                && application.Status == ApplicationQueryWorkflow.OpenStatus);
            if (!applicationHasQueries)
            {
                return null;
            }

            var queries = _db.Queries
                .AsNoTracking()
                .Where(query => query.ApplicationId == applicationId
                    && query.Status == ApplicationQueryWorkflow.OpenStatus)
                .OrderBy(query => query.DateAdded)
                .ToList()
                .Where(query => ApplicationQueryWorkflow.NormalizeStageKey(query.Stage) == stageKey)
                .Select(ToQueryItemViewModel)
                .ToList();

            if (queries.Count == 0)
            {
                return null;
            }

            return new ApplicantQueryStagePanelViewModel
            {
                ApplicationId = applicationId,
                StageKey = stageKey,
                StageTitle = ApplicationQueryWorkflow.GetStageTitle(stageKey),
                Queries = queries
            };
        }

        private static ApplicationQueryItemViewModel ToQueryItemViewModel(Queries query)
        {
            var resolved = ApplicationQueryWorkflow.IsResolved(query);
            return new ApplicationQueryItemViewModel
            {
                Id = query.Id ?? string.Empty,
                Message = query.Query ?? string.Empty,
                ReviewerStage = ApplicationQueryWorkflow.GetReviewerLabel(query),
                DateRaised = query.DateAdded,
                DateResolved = resolved ? query.DateUpdated : null,
                IsResolved = resolved
            };
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




        [HttpGet("GetDistCounc")]
        public async Task<IActionResult> GetDistCounc(string Province)
        {
            var district = _db.District.Where(a => a.Province == Province).ToList();
            var council = _db.Council.Where(a => a.Province == Province).ToList();
            return Json(new { districts= district, councils = council });
            //return View();
        }

        

             [HttpGet("GetCouncData")]
        public async Task<IActionResult> GetCouncData(string council)
        {
            //var district = _db.District.Where(a => a.Province == Province).ToList();
            var councildata = _db.Council.Where(a => a.Name == council).FirstOrDefault();
            return Json(new { councildata = councildata });
            //return View();
        }




        [HttpGet("details")]
        public async Task<IActionResult> Details(string companyNumber, CancellationToken cancellationToken)
        {
            try
            {
                var company = await _companyRegistryClient.GetCompanyAsync(companyNumber, cancellationToken);
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        companyInfo = company.CompanyInfo,
                        directors = company.Directors.Select(director => new
                        {
                            names = director.Names,
                            surname = director.Surname,
                            nationalId = director.NationalId,
                            address = director.Address,
                            email = director.Email
                        })
                    }
                });
            }
            catch (CompanyRegistryException exception)
            {
                return CompanyRegistryError(exception);
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    success = false,
                    message = "The company registry is currently unavailable. Please try again."
                });
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout, new
                {
                    success = false,
                    message = "The company registry took too long to respond. Please try again."
                });
            }
        }

        [HttpPost("adopt-company")]
        public async Task<IActionResult> AdoptCompany(
            [FromBody] AdoptCompanyRequest request,
            CancellationToken cancellationToken)
        {
            var userId = userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            CompanyRegistryResult registryResult;
            try
            {
                registryResult = await _companyRegistryClient.GetCompanyAsync(
                    request.CompanyNumber ?? string.Empty,
                    cancellationToken);
            }
            catch (CompanyRegistryException exception)
            {
                return CompanyRegistryError(exception);
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    success = false,
                    message = "The company registry is currently unavailable. Please try again."
                });
            }

            var application = string.IsNullOrWhiteSpace(request.ApplicationId)
                ? null
                : await _db.ApplicationInfo.FirstOrDefaultAsync(
                    item => item.Id == request.ApplicationId && item.UserID == userId,
                    cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.ApplicationId) && application is null)
            {
                return NotFound(new { success = false, message = "The licence application was not found." });
            }

            var now = DateTime.Now;
            var company = registryResult.CompanyInfo;
            var companyNumber = company.CompanyNumber?.Trim().ToUpperInvariant();
            var companyAddress = FirstNotEmpty(company.PhysicalAddress, company.PostalAddress);

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            if (application is null)
            {
                application = new ApplicationInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    UserID = userId,
                    ApplicantType = "Company",
                    BusinessName = company.Name?.Trim(),
                    CompanyNumber = companyNumber,
                    OperationAddress = companyAddress,
                    PaymentId = string.Empty,
                    PaymentStatus = string.Empty,
                    RefNum = string.Empty,
                    PaymentFee = 0,
                    PlaceOfBirth = string.Empty,
                    RejectionReason = string.Empty,
                    Status = "inprogress",
                    ApplicationDate = now,
                    DateUpdated = now,
                    InspectorID = string.Empty,
                    Secretary = string.Empty,
                    IdCopy = string.Empty,
                    Fingerprints = string.Empty,
                    FormFF = string.Empty
                };
                _db.ApplicationInfo.Add(application);
            }
            else
            {
                application.ApplicantType = "Company";
                application.BusinessName = company.Name?.Trim();
                application.CompanyNumber = companyNumber;
                application.OperationAddress = companyAddress;
                application.DateUpdated = now;
            }

            var existingDirectors = await _db.DirectorDetails
                .Where(item => item.ApplicationId == application.Id)
                .ToListAsync(cancellationToken);
            _db.DirectorDetails.RemoveRange(existingDirectors);

            var directors = registryResult.Directors
                .GroupBy(director => DirectorIdentity(director), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Select(director => new DirectorDetails
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ApplicationId = application.Id,
                    Name = director.Names?.Trim(),
                    Surname = director.Surname?.Trim(),
                    NationalId = director.NationalId?.Trim(),
                    Address = director.Address?.Trim(),
                    Status = "active",
                    NatId = string.Empty,
                    Form55 = string.Empty,
                    FingerPrints = string.Empty,
                    DateAdded = now,
                    DateUpdated = now
                })
                .ToList();

            _db.DirectorDetails.AddRange(directors);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Json(new
            {
                success = true,
                applicationId = application.Id,
                companyNumber,
                businessName = application.BusinessName,
                operationAddress = application.OperationAddress,
                directorCount = directors.Count,
                nextUrl = Url.Action(nameof(OutletInfo), "License", new { Id = application.Id })
            });
        }

        private IActionResult CompanyRegistryError(CompanyRegistryException exception)
        {
            var statusCode = exception.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => StatusCodes.Status400BadRequest,
                System.Net.HttpStatusCode.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status502BadGateway
            };

            return StatusCode(statusCode, new { success = false, message = exception.Message });
        }

        private static string FirstNotEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
        }

        private static string DirectorIdentity(CompanyRegistryDirector director)
        {
            return $"{director.NationalId?.Trim()}|{director.Names?.Trim()}|{director.Surname?.Trim()}";
        }


        [HttpGet("ManDetails")]
        public async Task<IActionResult> ManDetails(
            string nationalid,
            string applicationId,
            CancellationToken cancellationToken)
        {
            var userId = userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "Please sign in again." });
            }

            var applicationExists = await _db.ApplicationInfo
                .AsNoTracking()
                .AnyAsync(item => item.Id == applicationId && item.UserID == userId, cancellationToken);
            if (!applicationExists)
            {
                return NotFound(new { success = false, message = "The licence application was not found." });
            }

            var normalizedNationalId = NormalizeNationalId(nationalid);
            if (normalizedNationalId.Length < 5)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Enter a valid National ID number. Spaces and punctuation may be omitted."
                });
            }

            var existingNationalIds = await _db.ManagersParticulars
                .AsNoTracking()
                .Where(item => item.ApplicationId == applicationId && item.NationalId != null)
                .Select(item => item.NationalId!)
                .ToListAsync(cancellationToken);
            if (existingNationalIds.Any(item => NormalizeNationalId(item) == normalizedNationalId))
            {
                return Conflict(new
                {
                    success = false,
                    message = "This person is already listed as a manager for this application."
                });
            }

            var source = await FindManagerLookupSourceAsync(normalizedNationalId, cancellationToken);
            if (source == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "No matching record was found in managers, directors or users."
                });
            }

            var natIdCopyUrl = ExistingManagerSourceDocumentUrl(source.NatIdCopy);
            var fingerprintsUrl = ExistingManagerSourceDocumentUrl(source.Fingerprints);
            var form55Url = ExistingManagerSourceDocumentUrl(source.Form55);
            var profileComplete = !string.IsNullOrWhiteSpace(source.Name)
                && !string.IsNullOrWhiteSpace(source.Surname)
                && !string.IsNullOrWhiteSpace(source.NationalId)
                && !string.IsNullOrWhiteSpace(source.Address);

            return Json(new
            {
                success = true,
                match = new
                {
                    sourceType = source.SourceType,
                    sourceId = source.SourceId,
                    sourceLabel = source.SourceLabel,
                    name = source.Name,
                    surname = source.Surname,
                    nationalId = source.NationalId,
                    address = source.Address,
                    profileComplete,
                    documents = new
                    {
                        natIdCopyUrl,
                        fingerprintsUrl,
                        form55Url
                    }
                }
            });
        }

        [HttpPost("confirm-manager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmManager(
            [FromForm] ConfirmManagerLookupRequest request,
            CancellationToken cancellationToken)
        {
            var userId = userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "Please sign in again." });
            }

            if (string.IsNullOrWhiteSpace(request.ApplicationId)
                || string.IsNullOrWhiteSpace(request.SourceType)
                || string.IsNullOrWhiteSpace(request.SourceId))
            {
                return BadRequest(new { success = false, message = "Search for the manager again before confirming." });
            }

            var applicationExists = await _db.ApplicationInfo
                .AsNoTracking()
                .AnyAsync(
                    item => item.Id == request.ApplicationId && item.UserID == userId,
                    cancellationToken);
            if (!applicationExists)
            {
                return NotFound(new { success = false, message = "The licence application was not found." });
            }

            var source = await ResolveManagerLookupSourceAsync(
                request.SourceType,
                request.SourceId,
                cancellationToken);
            if (source == null
                || NormalizeNationalId(source.NationalId) != NormalizeNationalId(request.NationalId))
            {
                return BadRequest(new { success = false, message = "The matched record changed. Search for the manager again." });
            }

            if (string.IsNullOrWhiteSpace(source.Name)
                || string.IsNullOrWhiteSpace(source.Surname)
                || string.IsNullOrWhiteSpace(source.NationalId)
                || string.IsNullOrWhiteSpace(source.Address))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The matched record does not contain all required manager details."
                });
            }

            var normalizedNationalId = NormalizeNationalId(source.NationalId);
            var existingNationalIds = await _db.ManagersParticulars
                .AsNoTracking()
                .Where(item => item.ApplicationId == request.ApplicationId && item.NationalId != null)
                .Select(item => item.NationalId!)
                .ToListAsync(cancellationToken);
            if (existingNationalIds.Any(item => NormalizeNationalId(item) == normalizedNationalId))
            {
                return Conflict(new
                {
                    success = false,
                    message = "This person is already listed as a manager for this application."
                });
            }

            try
            {
                ValidateManagerLookupDocument(request.NatIdCopy, "National ID Copy");
                ValidateManagerLookupDocument(request.Fingerprints, "Fingerprints");
                ValidateManagerLookupDocument(request.Form55, "Form 55");
            }
            catch (InvalidDataException exception)
            {
                return BadRequest(new { success = false, message = exception.Message });
            }

            if (!HasExistingWebRootFile(source.NatIdCopy) && request.NatIdCopy == null)
            {
                return BadRequest(new { success = false, message = "Upload the National ID Copy before confirming." });
            }

            if (!HasExistingWebRootFile(source.Fingerprints) && request.Fingerprints == null)
            {
                return BadRequest(new { success = false, message = "Upload the fingerprints before confirming." });
            }

            if (!HasExistingWebRootFile(source.Form55) && request.Form55 == null)
            {
                return BadRequest(new { success = false, message = "Upload Form 55 before confirming." });
            }

            var now = DateTime.Now;
            var manager = new ManagersParticulars
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                ApplicationId = request.ApplicationId,
                Name = source.Name.Trim(),
                Surname = source.Surname.Trim(),
                NationalId = source.NationalId.Trim(),
                Address = source.Address.Trim(),
                Status = "UnSubmitted",
                DateAdded = now,
                DateUpdated = now
            };

            var createdDocuments = new List<string>();
            try
            {
                manager.Attachment = await MaterializeManagerDocumentAsync(
                    source.NatIdCopy,
                    request.NatIdCopy,
                    "NatId",
                    manager.Id,
                    cancellationToken);
                createdDocuments.Add(manager.Attachment);

                manager.Fingerprints = await MaterializeManagerDocumentAsync(
                    source.Fingerprints,
                    request.Fingerprints,
                    "Fingerprints",
                    manager.Id,
                    cancellationToken);
                createdDocuments.Add(manager.Fingerprints);

                manager.Form55 = await MaterializeManagerDocumentAsync(
                    source.Form55,
                    request.Form55,
                    "Form55",
                    manager.Id,
                    cancellationToken);
                createdDocuments.Add(manager.Form55);

                _db.ManagersParticulars.Add(manager);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                foreach (var document in createdDocuments)
                {
                    TryDeleteWebRootDocument(document);
                }

                throw;
            }

            return Json(new
            {
                success = true,
                message = "Manager details added successfully.",
                manager = new
                {
                    manager.Id,
                    manager.Name,
                    manager.Surname,
                    manager.NationalId,
                    manager.Address,
                    attachmentUrl = ExistingManagerSourceDocumentUrl(manager.Attachment),
                    fingerprintsUrl = ExistingManagerSourceDocumentUrl(manager.Fingerprints),
                    form55Url = ExistingManagerSourceDocumentUrl(manager.Form55)
                }
            });
        }

        private async Task<ManagerLookupSource?> FindManagerLookupSourceAsync(
            string normalizedNationalId,
            CancellationToken cancellationToken)
        {
            var manager = await FindManagerByNationalIdAsync(normalizedNationalId, cancellationToken);
            if (manager != null)
            {
                return ManagerLookupSource.FromManager(manager);
            }

            var director = await FindDirectorByNationalIdAsync(normalizedNationalId, cancellationToken);
            if (director != null)
            {
                return ManagerLookupSource.FromDirector(director);
            }

            var user = await FindUserByNationalIdAsync(normalizedNationalId, cancellationToken);
            return user == null
                ? null
                : await ManagerLookupSourceFromUserAsync(user, cancellationToken);
        }

        private async Task<ManagerLookupSource?> ResolveManagerLookupSourceAsync(
            string sourceType,
            string sourceId,
            CancellationToken cancellationToken)
        {
            switch (sourceType.Trim().ToLowerInvariant())
            {
                case "manager":
                    var manager = await _db.ManagersParticulars
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == sourceId, cancellationToken);
                    return manager == null ? null : ManagerLookupSource.FromManager(manager);

                case "director":
                    var director = await _db.DirectorDetails
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == sourceId, cancellationToken);
                    return director == null ? null : ManagerLookupSource.FromDirector(director);

                case "user":
                    var user = await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == sourceId, cancellationToken);
                    return user == null
                        ? null
                        : await ManagerLookupSourceFromUserAsync(user, cancellationToken);

                default:
                    return null;
            }
        }

        private async Task<ManagersParticulars?> FindManagerByNationalIdAsync(
            string normalizedNationalId,
            CancellationToken cancellationToken)
        {
            var manager = await _db.ManagersParticulars
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.NationalId != null
                    && item.NationalId.Replace(" ", string.Empty)
                        .Replace("-", string.Empty)
                        .Replace("/", string.Empty)
                        .Replace(".", string.Empty)
                        .Replace("_", string.Empty)
                        .Replace("(", string.Empty)
                        .Replace(")", string.Empty)
                        .ToUpper() == normalizedNationalId,
                    cancellationToken);
            if (manager != null)
            {
                return manager;
            }

            var candidates = await _db.ManagersParticulars
                .AsNoTracking()
                .Where(item => item.NationalId != null)
                .Select(item => new { item.Id, item.NationalId })
                .ToListAsync(cancellationToken);
            var candidateId = candidates
                .FirstOrDefault(item => NormalizeNationalId(item.NationalId) == normalizedNationalId)
                ?.Id;
            return candidateId == null
                ? null
                : await _db.ManagersParticulars.AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == candidateId, cancellationToken);
        }

        private async Task<DirectorDetails?> FindDirectorByNationalIdAsync(
            string normalizedNationalId,
            CancellationToken cancellationToken)
        {
            var director = await _db.DirectorDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.NationalId != null
                    && item.NationalId.Replace(" ", string.Empty)
                        .Replace("-", string.Empty)
                        .Replace("/", string.Empty)
                        .Replace(".", string.Empty)
                        .Replace("_", string.Empty)
                        .Replace("(", string.Empty)
                        .Replace(")", string.Empty)
                        .ToUpper() == normalizedNationalId,
                    cancellationToken);
            if (director != null)
            {
                return director;
            }

            var candidates = await _db.DirectorDetails
                .AsNoTracking()
                .Where(item => item.NationalId != null)
                .Select(item => new { item.Id, item.NationalId })
                .ToListAsync(cancellationToken);
            var candidateId = candidates
                .FirstOrDefault(item => NormalizeNationalId(item.NationalId) == normalizedNationalId)
                ?.Id;
            return candidateId == null
                ? null
                : await _db.DirectorDetails.AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == candidateId, cancellationToken);
        }

        private async Task<ApplicationUser?> FindUserByNationalIdAsync(
            string normalizedNationalId,
            CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.NatID != null
                    && item.NatID.Replace(" ", string.Empty)
                        .Replace("-", string.Empty)
                        .Replace("/", string.Empty)
                        .Replace(".", string.Empty)
                        .Replace("_", string.Empty)
                        .Replace("(", string.Empty)
                        .Replace(")", string.Empty)
                        .ToUpper() == normalizedNationalId,
                    cancellationToken);
            if (user != null)
            {
                return user;
            }

            var candidates = await _db.Users
                .AsNoTracking()
                .Where(item => item.NatID != null)
                .Select(item => new { item.Id, item.NatID })
                .ToListAsync(cancellationToken);
            var candidateId = candidates
                .FirstOrDefault(item => NormalizeNationalId(item.NatID) == normalizedNationalId)
                ?.Id;
            return candidateId == null
                ? null
                : await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == candidateId, cancellationToken);
        }

        private async Task<ManagerLookupSource> ManagerLookupSourceFromUserAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var documents = await _db.ApplicationInfo
                .AsNoTracking()
                .Where(item => item.UserID == user.Id)
                .OrderByDescending(item => item.DateUpdated)
                .Select(item => new { item.IdCopy, item.Fingerprints, item.FormFF })
                .FirstOrDefaultAsync(cancellationToken);

            return ManagerLookupSource.FromUser(
                user,
                documents?.IdCopy,
                documents?.Fingerprints,
                documents?.FormFF);
        }

        private static string NormalizeNationalId(string? value)
        {
            return string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();
        }

        private string? ExistingManagerSourceDocumentUrl(string? relativePath)
        {
            return TryResolveExistingWebRootFile(relativePath, out _)
                ? Url.Content($"~/{relativePath!.TrimStart('/', '\\').Replace('\\', '/')}")
                : null;
        }

        private static bool HasExistingWebRootFile(string? relativePath)
        {
            return TryResolveExistingWebRootFile(relativePath, out _);
        }

        private static bool TryResolveExistingWebRootFile(string? relativePath, out string absolutePath)
        {
            absolutePath = string.Empty;
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return false;
            }

            var webRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
                .TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var normalizedPath = relativePath
                .TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var candidatePath = Path.GetFullPath(Path.Combine(webRoot, normalizedPath));
            if (!candidatePath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase)
                || !System.IO.File.Exists(candidatePath))
            {
                return false;
            }

            absolutePath = candidatePath;
            return true;
        }

        private static void ValidateManagerLookupDocument(IFormFile? file, string label)
        {
            if (file == null)
            {
                return;
            }

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".jpg", ".jpeg", ".png"
            };
            var extension = Path.GetExtension(file.FileName);
            if (file.Length <= 0 || !allowedExtensions.Contains(extension))
            {
                throw new InvalidDataException($"{label} must be a non-empty PDF, JPG or PNG file.");
            }
        }

        private static async Task<string> MaterializeManagerDocumentAsync(
            string? sourcePath,
            IFormFile? upload,
            string prefix,
            string managerId,
            CancellationToken cancellationToken)
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ManagerFingerprints");
            Directory.CreateDirectory(directory);

            if (TryResolveExistingWebRootFile(sourcePath, out var sourceAbsolutePath))
            {
                var sourceExtension = Path.GetExtension(sourceAbsolutePath).ToLowerInvariant();
                var copiedFileName = $"{prefix}_{managerId}{sourceExtension}";
                var copiedAbsolutePath = Path.Combine(directory, copiedFileName);

                await using var sourceStream = new FileStream(
                    sourceAbsolutePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                await using var destinationStream = new FileStream(
                    copiedAbsolutePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);
                await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                return $"ManagerFingerprints/{copiedFileName}";
            }

            if (upload == null)
            {
                throw new InvalidDataException($"A {prefix} document is required.");
            }

            var uploadExtension = Path.GetExtension(upload.FileName).ToLowerInvariant();
            var uploadedFileName = $"{prefix}_{managerId}{uploadExtension}";
            var uploadedAbsolutePath = Path.Combine(directory, uploadedFileName);
            await using var uploadStream = new FileStream(
                uploadedAbsolutePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);
            await upload.CopyToAsync(uploadStream, cancellationToken);
            return $"ManagerFingerprints/{uploadedFileName}";
        }

        private static void TryDeleteWebRootDocument(string? relativePath)
        {
            if (!TryResolveExistingWebRootFile(relativePath, out var absolutePath))
            {
                return;
            }

            try
            {
                System.IO.File.Delete(absolutePath);
            }
            catch (IOException)
            {
                // The failed manager operation should not be hidden by cleanup errors.
            }
            catch (UnauthorizedAccessException)
            {
                // The failed manager operation should not be hidden by cleanup errors.
            }
        }

        private sealed class ManagerLookupSource
        {
            public required string SourceType { get; init; }
            public required string SourceId { get; init; }
            public required string SourceLabel { get; init; }
            public string? Name { get; init; }
            public string? Surname { get; init; }
            public string? NationalId { get; init; }
            public string? Address { get; init; }
            public string? NatIdCopy { get; init; }
            public string? Fingerprints { get; init; }
            public string? Form55 { get; init; }

            public static ManagerLookupSource FromManager(ManagersParticulars manager)
            {
                return new ManagerLookupSource
                {
                    SourceType = "manager",
                    SourceId = manager.Id ?? string.Empty,
                    SourceLabel = "Managers table",
                    Name = manager.Name,
                    Surname = manager.Surname,
                    NationalId = manager.NationalId,
                    Address = manager.Address,
                    NatIdCopy = manager.Attachment,
                    Fingerprints = manager.Fingerprints,
                    Form55 = manager.Form55
                };
            }

            public static ManagerLookupSource FromDirector(DirectorDetails director)
            {
                return new ManagerLookupSource
                {
                    SourceType = "director",
                    SourceId = director.Id ?? string.Empty,
                    SourceLabel = "Directors table",
                    Name = director.Name,
                    Surname = director.Surname,
                    NationalId = director.NationalId,
                    Address = director.Address,
                    NatIdCopy = director.NatId,
                    Fingerprints = director.FingerPrints,
                    Form55 = director.Form55
                };
            }

            public static ManagerLookupSource FromUser(
                ApplicationUser user,
                string? natIdCopy,
                string? fingerprints,
                string? form55)
            {
                return new ManagerLookupSource
                {
                    SourceType = "user",
                    SourceId = user.Id,
                    SourceLabel = "Users table",
                    Name = user.Name,
                    Surname = user.LastName,
                    NationalId = user.NatID,
                    Address = user.PhysicalAddress,
                    NatIdCopy = natIdCopy,
                    Fingerprints = fingerprints,
                    Form55 = form55
                };
            }
        }
    }
}

