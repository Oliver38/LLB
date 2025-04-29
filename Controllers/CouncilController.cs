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
    [Route("Council")]
    public class CouncilController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public CouncilController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<IActionResult> Wangu()
        //{
        //    HttpClient client = new HttpClient();
        //    var response = await client.GetAsync($"{Globals.Globals.service_end_point}/api/v1/reports/getCompanyInfosx").Result.Content.ReadAsStringAsync();
        //    return View();

        //}

        [HttpGet("AddCouncil")]
        public IActionResult AddCouncil()
        {
            var regions = _db.LicenseRegions.OrderBy(a => a.RegionName).ToList();
            var provices = _db.Province.OrderBy(a => a.Name).ToList();
            var Councils = _db.Council.ToList().OrderBy(a => a.Province);
            ViewBag.Regions = regions;
            ViewBag.Provinces = provices;
            ViewBag.Councils = Councils;

            return View();
        }


        [HttpPost("AddCouncil")]

        public async Task<IActionResult> AddCouncil(Council Councildata)
        {
            Councildata.Id = Guid.NewGuid().ToString();
            Councildata.DateAdded = DateTime.Now;
            Councildata.DateUpdated = DateTime.Now;
            var counc = _db.LicenseRegions.Where(a => a.Id == Councildata.CouncilRegionId).FirstOrDefault();
            Councildata.CouncilRegion = counc.RegionName;
            var prov = _db.Province.Where(g => g.Id == Councildata.ProvinceId).FirstOrDefault();
            Councildata.Province = prov.Name;
            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            Councildata.UserId = id;

            _db.Add(Councildata);
            _db.SaveChanges();
            var regions = _db.LicenseRegions.OrderBy(a => a.RegionName).ToList();
            var provices = _db.Province.OrderBy(a => a.Name).ToList();
            var Councils = _db.Council.ToList().OrderBy(a => a.Province);
            ViewBag.Regions = regions;
            ViewBag.Provinces = provices;
            ViewBag.Councils = Councils;

            return View();


        }

        [HttpPost("UpdateCouncil")]

        public async Task<IActionResult> UpdateCouncil(Council Councildata)
        {


            return RedirectToAction("AddCouncil", "Council");


        }

    }
}
