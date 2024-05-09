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
        public IActionResult CreateLicense(LicenseTypes types)
        {


            //public string LicenseTypeNameId { get; set; }
            types.Id = Guid.NewGuid().ToString();
        types.Status = "inactive";
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
        public IActionResult UpdateFee(LicenseTypes types)
        {
            var licensefee = _db.LicenseTypes.Where(a => a.Id == types.Id).FirstOrDefault();
            licensefee.CityFee = types.CityFee;
            licensefee.MunicipaltyFee = types.MunicipaltyFee;
            licensefee.RDCFee = types.RDCFee;
            licensefee.TownFee = types.TownFee;
            licensefee.DateUpdated = DateTime.Now;
            _db.Update(licensefee);
            if (_db.SaveChanges()==1)
            {
                return RedirectToAction("Licenses", "Settings");
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

        [HttpPost(("LicenseRegion"))]
        public IActionResult LicenseRegion(LicenseRegion licenseRegion)
        {


            //public string LicenseTypeNameId { get; set; }
            licenseRegion.Id = Guid.NewGuid().ToString();
            licenseRegion.DateAdded = DateTime.Now;
            licenseRegion.status = "active";
            _db.Add(licenseRegion);
            _db.SaveChanges();
            // ViewBag.License = licenses;
            var licenseRegions = _db.LicenseRegions.ToList();
            ViewBag.LicenseRegions = licenseRegions;
            return View();
        }

    }
}
