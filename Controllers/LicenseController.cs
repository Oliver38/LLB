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
    [Route("License")]
    public class LicenseController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public LicenseController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }


        [HttpGet(("Apply"))]
        public async Task<IActionResult> ApplyAsync()
        {
            //using Microsoft.AspNetCore.Identity;
            var user = await userManager.FindByEmailAsync(User.Identity.Name);
            var licenses = _db.LicenseTypes.ToList();
            ViewBag.User = user;
            ViewBag.License = licenses;
            return View();
        }

    }
}
