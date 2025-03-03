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
    [Route("Transferfees")]
    public class TransferfeesController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public TransferfeesController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        [HttpGet(("TransferFees"))]
        public IActionResult TransferFees()
        {
            var Transfers = _db.TransferTypes.ToList();
            ViewBag.Transfer = Transfers;
            return View();
        }

        [HttpGet(("Transfers"))]
        public IActionResult Transfers()
        {
            var Transfers = _db.TransferTypes.ToList();
            ViewBag.Transfer = Transfers;
            return View();
        }

        [HttpGet(("TransferType"))]
        public IActionResult TransferType()
        {
            var Transfers = _db.TransferTypes.ToList();
            ViewBag.Transfer = Transfers;
            return View();
        }

        [HttpGet(("CreateTransfer"))]
        public IActionResult CreateTransfer()
        {

            //ViewBag.Transfer = Transfers;
            return View();
        }

        [HttpPost(("CreateTransfer"))]
        public async Task<IActionResult> CreateTransferAsync(TransferTypes types)
        {


            //public string TransferTypeNameId { get; set; }
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
           // ViewBag.Transfer = Transfers;
            return View();
        }






        [HttpPost(("UpdateFee"))]
        public async Task<IActionResult> UpdateFeeAsync(TransferTypes types)
        {
            var Transferfee = _db.TransferTypes.Where(a => a.Id == types.Id).FirstOrDefault();

            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            Transferfee.UserId = id;
            Transferfee.CityFee = types.CityFee;
            Transferfee.MunicipaltyFee = types.MunicipaltyFee;
            Transferfee.RDCFee = types.RDCFee;
            Transferfee.TownFee = types.TownFee;
            Transferfee.DateUpdated = DateTime.Now;
            Transferfee.Status = "active";

            _db.Update(Transferfee);
            if (_db.SaveChanges()==1)
            {
                return RedirectToAction("TransferFees", "Transferfees");
            }
          
            return View();
        }

        /* [HttpPost(("TransferPrice"))]
         public IActionResult TransferPrice(double NewFee, string Id)
         {
             var theTransfer = _db.TransferTypes.Where(a => a.Id == Id).FirstOrDefault();


             theTransfer.FeeId = NewFee;

             _db.Update(theTransfer);
             _db.SaveChanges();


             var Transfers = _db.TransferTypes.ToList();
             ViewBag.Transfer = Transfers;
             return View();
         }*/



        [HttpGet(("TransferRegion"))]
        public IActionResult TransferRegion()
        {
            var TransferRegions = _db.TransferRegion.ToList();
            ViewBag.TransferRegions = TransferRegions;

            return View();
        }

        [HttpPost("UpdateConditions")]
        public async Task<IActionResult> UpdateConditions(string Conditions, string Id)
        {
            var application = _db.TransferTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.ConditionList = Conditions.ToUpper(); ;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Transfers", "Settings");

        }

        [HttpPost("UpdateInstructions")]
        public async Task<IActionResult> UpdateInstructions(string instruction, string Id)
        {
            var application = _db.TransferTypes.Where(a => a.Id == Id).FirstOrDefault();
            application.TransferInstructions= instruction;
            application.DateUpdated = DateTime.Now;
            _db.Update(application);
            _db.SaveChanges();
            return RedirectToAction("Transfers", "Settings");

        }

        [HttpPost(("TransferRegion"))]
        public async Task<IActionResult> TransferRegionAsync(TransferRegion TransferRegion)
        {


            //public string TransferTypeNameId { get; set; }
            TransferRegion.Id = Guid.NewGuid().ToString();
            TransferRegion.DateAdded = DateTime.Now;
            TransferRegion.Status = "active"; var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;


            TransferRegion.UserId = id;
            _db.Add(TransferRegion);
            _db.SaveChanges();
            // ViewBag.Transfer = Transfers;
            var TransferRegions = _db.TransferRegion.ToList();
            ViewBag.TransferRegions = TransferRegions;
            return View();
        }

    }
}
