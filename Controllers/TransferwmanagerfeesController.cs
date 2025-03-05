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
    [Route("Transferwmanagerfees")]
    public class TransferwmanagerfeesController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public TransferwmanagerfeesController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        [HttpGet(("TransferwmanagerFees"))]
        public IActionResult TransferwmanagerFees()
        {
            var Transferwmanagers = _db.TransferwmanagerTypes.ToList();
            ViewBag.Transferwmanager = Transferwmanagers;
            return View();
        }

        [HttpGet(("Transferwmanagers"))]
        public IActionResult Transferwmanagers()
        {
            var Transferwmanagers = _db.TransferwmanagerTypes.ToList();
            ViewBag.Transferwmanager = Transferwmanagers;
            return View();
        }

        [HttpGet(("TransferwmanagerType"))]
        public IActionResult TransferwmanagerType()
        {
            var Transferwmanagers = _db.TransferwmanagerTypes.ToList();
            ViewBag.Transferwmanager = Transferwmanagers;
            return View();
        }

        [HttpGet(("CreateTransferwmanager"))]
        public IActionResult CreateTransferwmanager()
        {

            //ViewBag.Transferwmanager = Transferwmanagers;
            return View();
        }

        [HttpPost(("CreateTransferwmanager"))]
        public async Task<IActionResult> CreateTransferwmanagerAsync(TransferwmanagerTypes types)
        {


            //public string TransferwmanagerTypeNameId { get; set; }
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
           // ViewBag.Transferwmanager = Transferwmanagers;
            return View();
        }






        [HttpPost(("UpdateFee"))]
        public async Task<IActionResult> UpdateFeeAsync(TransferwmanagerTypes types)
        {
            var Transferwmanagerfee = _db.TransferwmanagerTypes.Where(a => a.Id == types.Id).FirstOrDefault();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            Transferwmanagerfee.UserId = id;
            Transferwmanagerfee.CityFee = types.CityFee;
            Transferwmanagerfee.MunicipaltyFee = types.MunicipaltyFee;
            Transferwmanagerfee.RDCFee = types.RDCFee;
            Transferwmanagerfee.TownFee = types.TownFee;
            Transferwmanagerfee.DateUpdated = DateTime.Now;
            Transferwmanagerfee.Status = "active";

            _db.Update(Transferwmanagerfee);
            if (_db.SaveChanges()==1)
            {
                return RedirectToAction("TransferwmanagerFees", "Transferwmanagerfees");
            }
          
            return View();
        }

        /* [HttpPost(("TransferwmanagerPrice"))]
         public IActionResult TransferwmanagerPrice(double NewFee, string Id)
         {
             var theTransferwmanager = _db.TransferwmanagerTypes.Where(a => a.Id == Id).FirstOrDefault();


             theTransferwmanager.FeeId = NewFee;

             _db.Update(theTransferwmanager);
             _db.SaveChanges();


             var Transferwmanagers = _db.TransferwmanagerTypes.ToList();
             ViewBag.Transferwmanager = Transferwmanagers;
             return View();
         }*/



        [HttpGet(("TransferwmanagerRegion"))]
        public IActionResult TransferwmanagerRegion()
        {
            var TransferwmanagerRegions = _db.TransferwmanagerRegion.ToList();
            ViewBag.TransferwmanagerRegions = TransferwmanagerRegions;

            return View();
        }

        [HttpPost("UpdateConditions")]
        public async Task<IActionResult> UpdateConditions(string Conditions, string Id)
        {
            var application = _db.TransferwmanagerTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.TransferwmanagerConditionList = Conditions.ToUpper(); ;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Transferwmanagers", "Settings");

        }

        [HttpPost("UpdateInstructions")]
        public async Task<IActionResult> UpdateInstructions(string instruction, string Id)
        {
            var application = _db.TransferwmanagerTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.TransferwmanagerInstructions= instruction;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Transferwmanagers", "Settings");

        }

        [HttpPost(("TransferwmanagerRegion"))]
        public async Task<IActionResult> TransferwmanagerRegionAsync(TransferwmanagerRegion TransferwmanagerRegion)
        {


            //public string TransferwmanagerTypeNameId { get; set; }
            TransferwmanagerRegion.Id = Guid.NewGuid().ToString();
            TransferwmanagerRegion.DateAdded = DateTime.Now;
            TransferwmanagerRegion.Status = "active"; var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;


            TransferwmanagerRegion.UserId = id;
            _db.Add(TransferwmanagerRegion);
            _db.SaveChanges();
            // ViewBag.Transferwmanager = Transferwmanagers;
            var TransferwmanagerRegions = _db.TransferwmanagerRegion.ToList();
            ViewBag.TransferwmanagerRegions = TransferwmanagerRegions;
            return View();
        }

    }
}
