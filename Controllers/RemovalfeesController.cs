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
    [Route("Removalfees")]
    public class RemovalfeesController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public RemovalfeesController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        [HttpGet(("RemovalFees"))]
        public IActionResult RemovalFees()
        {
            var Removals = _db.RemovalTypes.ToList();
            ViewBag.Removal = Removals;
            return View();
        }

        [HttpGet(("Removals"))]
        public IActionResult Removals()
        {
            var Removals = _db.RemovalTypes.ToList();
            ViewBag.Removal = Removals;
            return View();
        }

        [HttpGet(("RemovalType"))]
        public IActionResult RemovalType()
        {
            var Removals = _db.RemovalTypes.ToList();
            ViewBag.Removal = Removals;
            return View();
        }

        [HttpGet(("CreateRemoval"))]
        public IActionResult CreateRemoval()
        {

            //ViewBag.Removal = Removals;
            return View();
        }

        [HttpPost(("CreateRemoval"))]
        public async Task<IActionResult> CreateRemovalAsync(RemovalTypes types)
        {


            //public string RemovalTypeNameId { get; set; }
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
           // ViewBag.Removal = Removals;
            return View();
        }






        [HttpPost(("UpdateFee"))]
        public async Task<IActionResult> UpdateFeeAsync(RemovalTypes types)
        {
            var Removalfee = _db.RemovalTypes.Where(a => a.Id == types.Id).FirstOrDefault();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            Removalfee.UserId = id;
            Removalfee.CityFee = types.CityFee;
            Removalfee.MunicipaltyFee = types.MunicipaltyFee;
            Removalfee.RDCFee = types.RDCFee;
            Removalfee.TownFee = types.TownFee;
            Removalfee.DateUpdated = DateTime.Now;
            Removalfee.Status = "active";

            _db.Update(Removalfee);
            if (_db.SaveChanges()==1)
            {
                return RedirectToAction("RemovalFees", "Removalfees");
            }
          
            return View();
        }

        /* [HttpPost(("RemovalPrice"))]
         public IActionResult RemovalPrice(double NewFee, string Id)
         {
             var theRemoval = _db.RemovalTypes.Where(a => a.Id == Id).FirstOrDefault();


             theRemoval.FeeId = NewFee;

             _db.Update(theRemoval);
             _db.SaveChanges();


             var Removals = _db.RemovalTypes.ToList();
             ViewBag.Removal = Removals;
             return View();
         }*/



        [HttpGet(("RemovalRegion"))]
        public IActionResult RemovalRegion()
        {
            var RemovalRegions = _db.RemovalRegion.ToList();
            ViewBag.RemovalRegions = RemovalRegions;

            return View();
        }

        [HttpPost("UpdateConditions")]
        public async Task<IActionResult> UpdateConditions(string Conditions, string Id)
        {
            var application = _db.RemovalTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.ConditionList = Conditions.ToUpper(); ;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Removals", "Settings");

        }

        [HttpPost("UpdateInstructions")]
        public async Task<IActionResult> UpdateInstructions(string instruction, string Id)
        {
            var application = _db.RemovalTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.RemovalInstructions= instruction;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Removals", "Settings");

        }

        [HttpPost(("RemovalRegion"))]
        public async Task<IActionResult> RemovalRegionAsync(RemovalRegion RemovalRegion)
        {


            //public string RemovalTypeNameId { get; set; }
            RemovalRegion.Id = Guid.NewGuid().ToString();
            RemovalRegion.DateAdded = DateTime.Now;
            RemovalRegion.Status = "active"; var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;


            RemovalRegion.UserId = id;
            _db.Add(RemovalRegion);
            _db.SaveChanges();
            // ViewBag.Removal = Removals;
            var RemovalRegions = _db.RemovalRegion.ToList();
            ViewBag.RemovalRegions = RemovalRegions;
            return View();
        }

    }
}
