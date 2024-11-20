using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using LLB.Data;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Identity;
using Webdev.Payments;
using LLB.Models.DataModel;

namespace LLB.Controllers
{
    
    [Route("")]
    [Route("Tasks")]
    public class TasksController : Controller
    {


        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public TasksController(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        [HttpGet("AdminDashboard")]
        public IActionResult AdminDashboard()
        {

            var unassignedtasks = _db.Tasks.Where(a => a.Status == "unassigned").ToList();
            var assignedtasks = _db.Tasks.Where(a => a.Status == "assigned").ToList();
            var completedtasks = _db.Tasks.Where(a => a.Status == "completed").ToList();
            //var unverifiedtasks = _db.Tasks.Where(a => a.Status == "completed").ToList();
            //var completedtasks = _db.Tasks.Where(a => a.Status == "completed").ToList();

            ViewBag.UnAssigned = unassignedtasks;
            ViewBag.Assigned = assignedtasks;
            ViewBag.Completed = completedtasks;

            return View();
        }

        [HttpGet("AssignTask")]
        public async Task<IActionResult> AssignTaskAsync(string Id)
        {

            var task = _db.Tasks.Where(a => a.Id == Id).FirstOrDefault();
            var application = _db.ApplicationInfo.Where(s => s.Id == task.ApplicationId).FirstOrDefault();

            TaskDetails details = new TaskDetails();
            //get examiner details
            //since the colums are different we have to search if taskdoer id is not null
            ApplicationUser examiner = null;
            if (task.VerifierId != null) {
                examiner = await userManager.FindByIdAsync(task.VerifierId);
            }else if (task.RecommenderId != null)
            {
                examiner = await userManager.FindByIdAsync(task.RecommenderId);
            }
            else if (task.ApproverId != null)
            {
                examiner = await userManager.FindByIdAsync(task.ApproverId);
            }
            var examinerfullname = examiner.Name + " " + examiner.LastName;
            details.ExaminerName = examinerfullname;
            details.RefNumber = application.RefNum;

            //getting Bar name
            var outletdetails = _db.OutletInfo.Where(s => s.ApplicationId == task.ApplicationId).FirstOrDefault();
            details.BarName = outletdetails.TradingName;
            details.DateSubmitted = application.ApplicationDate;
            details.TaskStatus = task.Status;
            details.Jobtatus = application.Status;


            var inspector = await userManager.GetUsersInRoleAsync("inspector");
            ViewBag.TaskDetails = details;
            ViewBag.Task = task;
            ViewBag.ApplicationInfo = application;
            ViewBag.Inspectors = inspector;
            ViewBag.Task = task;
            return View();
        }

        [HttpPost("AssignTask")]
        public async Task<IActionResult> AssignTask(Tasks tasks)
        {
            var task = _db.Tasks.Where(a => a.Id == tasks.Id).FirstOrDefault();
            task.DateUpdated = DateTime.Now;
            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            task.AssignerId = id;
            task.Status = "assigned";
           // task.InspectorId = tasks.InspectorId;
            _db.Update(task);
            _db.SaveChanges();

            return RedirectToAction("AdminDashboard", "Tasks");
        }



        }
}