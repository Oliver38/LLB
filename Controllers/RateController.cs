using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using LLB.Data;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using LLB.Helpers;
using static System.Net.Mime.MediaTypeNames;

namespace LLB.Controllers
{

    [Route("")]
    [Route("Rate")]
    public class RateController : Controller
    {


        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public RateController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        [HttpGet("RateDashboard")]
        public async Task<IActionResult> RateDashboardAsync()
        {

            var todaysrate = _db.ExchangeRate.Where(a => a.DateAdded.Day == DateTime.Now.Day).OrderByDescending(x => x.DateAdded).FirstOrDefault();
            HttpClient client = new  HttpClient();
            var response = await client.GetStringAsync($"https://zimrate.tyganeutronics.com/api/v1");
            var ratesResponse = JsonConvert.DeserializeObject<RatesResponse>(response);

            ViewBag.RateResponse = ratesResponse;
            ViewBag.Response = response;
            ViewBag.TodaysRate = todaysrate;
            return View();
        }

        [HttpPost("AddRate")]
        public async Task<IActionResult> UseRate(double rate)
        {
            ExchangeRate newrate = new ExchangeRate();
            newrate.Id = Guid.NewGuid().ToString();
            newrate.UserId = await userManager.FindByEmailAsync(User.Identity.Name);
            //tasks.AssignerI
            newrate.ZWGrate = rate;
            newrate.DateAdded = DateTime.Now;
            newrate.Status = "active";


            _db.Add(newrate);
            _db.SaveChanges();

            return View();
            RedirectToAction("", "");
        }

    }
}