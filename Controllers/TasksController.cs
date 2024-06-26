using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;
using Microsoft.AspNetCore.Identity;
using LLB.Data;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Identity;
using Webdev.Payments;

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
           
            var inspector = await userManager.GetUsersInRoleAsync("inspector");
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
            task.InspectorId = tasks.InspectorId;
            _db.Update(task);
            _db.SaveChanges();

            return RedirectToAction("AdminDashboard", "Tasks");
        }



        }
}