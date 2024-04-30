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


        [HttpGet(("LicenseType"))]
        public IActionResult LicenseType()
        {
            var licenses = _db.licenseTypes.ToList();
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
           // types.Id = Guid.NewGuid();
        types.Status = "active";
            types.FeeId = 0;
            types.DateAdded = DateTime.Now;
            types.DateUpdates = DateTime.Now;
            _db.Add(types);
            _db.SaveChanges();
           // ViewBag.License = licenses;
            return View();
        }



        [HttpGet(("LicensePrice"))]
        public IActionResult LicensePrice()
        {
            var licenses = _db.licenseTypes.ToList();
            ViewBag.License = licenses;
            return View();
        }

        [HttpPost(("LicensePrice"))]
        public IActionResult LicensePrice(double NewFee, string Id)
        {
            var thelicense = _db.licenseTypes.Where(a => a.Id == Id).FirstOrDefault();

            
            thelicense.FeeId = NewFee;
           
            _db.Update(thelicense);
            _db.SaveChanges();


            var licenses = _db.licenseTypes.ToList();
            ViewBag.License = licenses;
            return View();
        }

    }
}
