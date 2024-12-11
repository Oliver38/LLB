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

namespace LLB.Controllers
{
    [Authorize]
    [Route("Settings")]
    public class SettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public SettingsController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        [HttpGet(("LicenseFees"))]
        public IActionResult LicenseFees()
        {
            var licenses = _db.LicenseTypes.ToList();
            ViewBag.License = licenses;
            return View();
        }

        [HttpGet(("Licenses"))]
        public IActionResult Licenses()
        {
            var licenses = _db.LicenseTypes.ToList();
            ViewBag.License = licenses;
            return View();
        }

        [HttpGet(("LicenseType"))]
        public IActionResult LicenseType()
        {
            var licenses = _db.LicenseTypes.ToList();
            ViewBag.License = licenses;
            return View();
        }

        [HttpGet(("CreateLicense"))]
        public IActionResult CreateLicense()
        {

            //ViewBag.License = licenses;
            return View();
        }

        [HttpPost(("CreateLicense"))]
        public async Task<IActionResult> CreateLicenseAsync(LicenseTypes types)
        {


            //public string LicenseTypeNameId { get; set; }
            types.Id = Guid.NewGuid().ToString();
        types.Status = "inactive";

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            types.UserId = id;
            types.TownFee = 0;
            types.CityFee = 0;
            types.RDCFee = 0;
            types.MunicipaltyFee = 0;

            types.DateAdded = DateTime.Now;
            types.DateUpdated = DateTime.Now;
            _db.Add(types);
            _db.SaveChanges();
           // ViewBag.License = licenses;
            return View();
        }






        [HttpPost(("UpdateFee"))]
        public async Task<IActionResult> UpdateFeeAsync(LicenseTypes types)
        {
            var licensefee = _db.LicenseTypes.Where(a => a.Id == types.Id).FirstOrDefault();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            licensefee.UserId = id;
            licensefee.CityFee = types.CityFee;
            licensefee.MunicipaltyFee = types.MunicipaltyFee;
            licensefee.RDCFee = types.RDCFee;
            licensefee.TownFee = types.TownFee;
            licensefee.DateUpdated = DateTime.Now;
            licensefee.Status = "active";

            _db.Update(licensefee);
            if (_db.SaveChanges()==1)
            {
                return RedirectToAction("LicenseFees", "Settings");
            }
          
            return View();
        }

        /* [HttpPost(("LicensePrice"))]
         public IActionResult LicensePrice(double NewFee, string Id)
         {
             var thelicense = _db.LicenseTypes.Where(a => a.Id == Id).FirstOrDefault();


             thelicense.FeeId = NewFee;

             _db.Update(thelicense);
             _db.SaveChanges();


             var licenses = _db.LicenseTypes.ToList();
             ViewBag.License = licenses;
             return View();
         }*/



        [HttpGet(("LicenseRegion"))]
        public IActionResult LicenseRegion()
        {
            var licenseRegions = _db.LicenseRegions.ToList();
            ViewBag.LicenseRegions = licenseRegions;

            return View();
        }

        [HttpPost("UpdateConditions")]
        public async Task<IActionResult> UpdateConditions(string Conditions, string Id)
        {
            var application = _db.LicenseTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.ConditionList = Conditions.ToUpper(); ;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Licenses", "Settings");

        }

        [HttpPost("UpdateInstructions")]
        public async Task<IActionResult> UpdateInstructions(string instruction, string Id)
        {
            var application = _db.LicenseTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.LicenseInstructions= instruction;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Licenses", "Settings");

        }

        [HttpPost(("LicenseRegion"))]
        public async Task<IActionResult> LicenseRegionAsync(LicenseRegion licenseRegion)
        {


            //public string LicenseTypeNameId { get; set; }
            licenseRegion.Id = Guid.NewGuid().ToString();
            licenseRegion.DateAdded = DateTime.Now;
            licenseRegion.Status = "active"; var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;


            licenseRegion.UserId = id;
            _db.Add(licenseRegion);
            _db.SaveChanges();
            // ViewBag.License = licenses;
            var licenseRegions = _db.LicenseRegions.ToList();
            ViewBag.LicenseRegions = licenseRegions;
            return View();
        }

    }
}
