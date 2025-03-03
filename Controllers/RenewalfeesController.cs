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
    [Route("Renewalfees")]
    public class RenewalfeesController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public RenewalfeesController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        [HttpGet(("RenewalFees"))]
        public IActionResult RenewalFees()
        {
            var Renewals = _db.RenewalTypes.ToList();
            ViewBag.Renewal = Renewals;
            return View();
        }

        [HttpGet(("Renewals"))]
        public IActionResult Renewals()
        {
            var Renewals = _db.RenewalTypes.ToList();
            ViewBag.Renewal = Renewals;
            return View();
        }

        [HttpGet(("RenewalType"))]
        public IActionResult RenewalType()
        {
            var Renewals = _db.RenewalTypes.ToList();
            ViewBag.Renewal = Renewals;
            return View();
        }

        [HttpGet(("CreateRenewal"))]
        public IActionResult CreateRenewal()
        {

            //ViewBag.Renewal = Renewals;
            return View();
        }

        [HttpPost(("CreateRenewal"))]
        public async Task<IActionResult> CreateRenewalAsync(RenewalTypes types)
        {


            //public string RenewalTypeNameId { get; set; }
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
           // ViewBag.Renewal = Renewals;
            return View();
        }






        [HttpPost(("UpdateFee"))]
        public async Task<IActionResult> UpdateFeeAsync(RenewalTypes types)
        {
            var Renewalfee = _db.RenewalTypes.Where(a => a.Id == types.Id).FirstOrDefault();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            Renewalfee.UserId = id;
            Renewalfee.CityFee = types.CityFee;
            Renewalfee.MunicipaltyFee = types.MunicipaltyFee;
            Renewalfee.RDCFee = types.RDCFee;
            Renewalfee.TownFee = types.TownFee;
            Renewalfee.DateUpdated = DateTime.Now;
            Renewalfee.Status = "active";

            _db.Update(Renewalfee);
            if (_db.SaveChanges()==1)
            {
                return RedirectToAction("RenewalFees", "Renewalfees");
            }
          
            return View();
        }

        /* [HttpPost(("RenewalPrice"))]
         public IActionResult RenewalPrice(double NewFee, string Id)
         {
             var theRenewal = _db.RenewalTypes.Where(a => a.Id == Id).FirstOrDefault();


             theRenewal.FeeId = NewFee;

             _db.Update(theRenewal);
             _db.SaveChanges();


             var Renewals = _db.RenewalTypes.ToList();
             ViewBag.Renewal = Renewals;
             return View();
         }*/



        [HttpGet(("RenewalRegion"))]
        public IActionResult RenewalRegion()
        {
            var RenewalRegions = _db.RenewalRegion.ToList();
            ViewBag.RenewalRegions = RenewalRegions;

            return View();
        }

        [HttpPost("UpdateConditions")]
        public async Task<IActionResult> UpdateConditions(string Conditions, string Id)
        {
            var application = _db.RenewalTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.ConditionList = Conditions.ToUpper(); ;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Renewals", "Settings");

        }

        [HttpPost("UpdateInstructions")]
        public async Task<IActionResult> UpdateInstructions(string instruction, string Id)
        {
            var application = _db.RenewalTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.RenewalInstructions= instruction;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Renewals", "Settings");

        }

        [HttpPost(("RenewalRegion"))]
        public async Task<IActionResult> RenewalRegionAsync(RenewalRegion RenewalRegion)
        {


            //public string RenewalTypeNameId { get; set; }
            RenewalRegion.Id = Guid.NewGuid().ToString();
            RenewalRegion.DateAdded = DateTime.Now;
            RenewalRegion.Status = "active"; var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;


            RenewalRegion.UserId = id;
            _db.Add(RenewalRegion);
            _db.SaveChanges();
            // ViewBag.Renewal = Renewals;
            var RenewalRegions = _db.RenewalRegion.ToList();
            ViewBag.RenewalRegions = RenewalRegions;
            return View();
        }

    }
}
