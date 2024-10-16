using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using LLB.Data;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Identity;

namespace LLB.Controllers
{
    
    [Route("")]
    [Route("Home")]
    public class HomeController : Controller
    {
        

        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public HomeController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("")]
        [HttpGet("LandingPage")]
        [AllowAnonymous]
        public IActionResult LandingPage()
        {
            return View();
        }

        [HttpGet(("SignUp"))]
        [AllowAnonymous]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpGet("SignIn")]
        [AllowAnonymous]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpGet("AccessDenied")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [HttpGet("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> DashboardAsync()
        {

            if (User.IsInRole("admin"))
            {
                return RedirectToAction("AdminDashboard", "Tasks");
            }
           
            else if (User.IsInRole("verifier"))
            {
                return RedirectToAction("Dashboard", "Verify");
            }

            else if (User.IsInRole("recommender"))
            {
                return RedirectToAction("Dashboard", "Recommend");
            }
            else if (User.IsInRole("secretary"))
            {
                return RedirectToAction("Dashboard", "Approval");
            }
            else if (User.IsInRole("inspector"))
            {
                return RedirectToAction("Dashboard", "Examination");
            }
            else if (User.IsInRole("accountant"))
            {
                return RedirectToAction("Dashboard", "Accountant");
            }

            
            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            var applications = _db.ApplicationInfo.Where(a => a.UserID == id).ToList();
            var outletinfo  = _db.OutletInfo.ToList();
            var license = _db.LicenseTypes.ToList();
            var regions = _db.LicenseRegions.ToList();
            var user = await userManager.FindByEmailAsync(User.Identity.Name);

            ViewBag.User = user;
            ViewBag.OutletInfo = outletinfo;
            ViewBag.Regions = regions;
            ViewBag.License= license;
            ViewBag.Applications = applications;
            return View();
        }

        private void GetUserId()
        {
            throw new NotImplementedException();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}