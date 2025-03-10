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
    [Route("Province")]
    public class ProvinceController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public ProvinceController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
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

        [HttpGet("AddProvince")]
        public IActionResult AddProvince()
        {
            var provinces = _db.Province.ToList().OrderBy(a => a.Name);
            ViewBag.Provinces = provinces;

            return View();
        }
        [HttpPost("AddProvince")]

        public async Task<IActionResult> AddProvince(Province provincedata)
        {
            provincedata.Id = Guid.NewGuid().ToString();
            provincedata.DateAdded = DateTime.Now;
            provincedata.DateUpdated = DateTime.Now;
            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            provincedata.UserId = id;

            _db.Add(provincedata);
            _db.SaveChanges();
            var provinces = _db.Province.ToList().OrderBy(a => a.Name); ;
            ViewBag.Provinces = provinces;

            return View();


        }

        [HttpPost("UpdateProvince")]

        public async Task<IActionResult> UpdateProvince(Province provincedata)
        {


            return RedirectToAction("AddProvince", "Province");


        }

    }
}
