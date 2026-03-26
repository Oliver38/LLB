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
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using System.Drawing;
using static System.Runtime.InteropServices.JavaScript.JSType;
using PdfToSvg;

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
        public async Task<IActionResult> AdminDashboardAsync()
        {

            var unassignedtasks = _db.Tasks.Where(a => a.Status == "unassigned").ToList();
            //var assignedtasks = _db.Tasks.Where(a => a.Status == "assigned" || a.Status == "reassigned").ToList();
            var reassignedtasks = _db.Tasks.Where(a => a.Status == "reassigned").ToList();
            var completedtasks = _db.Tasks.Where(a => a.Status == "completed").ToList();
            //var unverifiedtasks = _db.Tasks.Where(a => a.Status == "completed").ToList();
            //var completedtasks = _db.Tasks.Where(a => a.Status == "completed").ToList();





            var assignedtasks = _db.Tasks.Where(a => a.Status == "assigned" ).ToList();

            List<TaskDetails> Alldetails = new List<TaskDetails>();
            //List<ApplicationUser> examinerslist = new List<ApplicationUser>();

            //if (stage == "verification")
            //{
            //    examinerslist = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("verifier");

            //}
            //else if (stage == "recommendation")
            //{
            //    examinerslist = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("recommender");

            //}
            //else if (stage == "approval")
            //{
            //    examinerslist = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("secretary");

            //}

            foreach (var taskass in assignedtasks)
            {
                var application = ResolveTaskApplication(taskass);
                if (application == null)
                {
                    continue;
                }

                TaskDetails details = new TaskDetails();
                ApplicationUser examiner = null;
                if (taskass.VerifierId != null)
                {
                    examiner = await userManager.FindByIdAsync(taskass.VerifierId);
                }
                else if (taskass.RecommenderId != null)
                {
                    examiner = await userManager.FindByIdAsync(taskass.RecommenderId);
                }
                else if (taskass.ApproverId != null)
                {
                    examiner = await userManager.FindByIdAsync(taskass.ApproverId);
                }

                var examinerfullname = examiner == null
                    ? "Unassigned"
                    : $"{examiner.Name} {examiner.LastName}".Trim();
                details.ExaminerName = examinerfullname;
                details.RefNumber = ResolveTaskReference(taskass, application);
                details.Id = taskass.Id;
                details.ApplicationName = taskass.Service ?? "Task";

                var rootApplicationId = ResolveTaskRootApplicationId(taskass);
                var outletdetails = !string.IsNullOrWhiteSpace(rootApplicationId)
                    ? _db.OutletInfo.Where(s => s.ApplicationId == rootApplicationId).FirstOrDefault()
                    : null;

                details.BarName = outletdetails?.TradingName ?? application.BusinessName ?? "N/A";
                details.DateSubmitted = application.ApplicationDate;
                details.TaskStatus = taskass.Status ?? "Unknown";
                details.JobStatus = taskass.ExaminationStatus ?? application.ExaminationStatus ?? "N/A";
                details.DateCreated = taskass.DateAdded;

                var licenseType = _db.LicenseTypes.Where(w => w.Id == application.LicenseTypeID).FirstOrDefault();
                details.LicenseType = licenseType?.LicenseName ?? "N/A";

                //var inspector = await userManager.GetUsersInRoleAsync("inspector");
                List<ApplicationUser> examiners = new List<ApplicationUser>();
                if (taskass.ExaminationStatus == "verification")
                {
                    examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("verifier");

                }
                else if (taskass.ExaminationStatus == "recommendation")
                {
                    examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("recommender");

                }
                else if (taskass.ExaminationStatus == "approval")
                {
                    examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("secretary");
                }

                if (taskass.AssignerId == "system")
                {
                    details.Assigner = "System";
                }
                else if (taskass.AssignerId == null || taskass.AssignerId == "")
                {
                    details.Assigner = "";
                }
                else
                {
                    var person = await userManager.FindByIdAsync(taskass.AssignerId);
                    details.Assigner = person.Name + " " + person.LastName;
                }


                if (taskass.ReAssignerId == null || taskass.ReAssignerId == "")
                {
                    details.ReAssigner = "";
                }
                else
                {
                    var person = await userManager.FindByIdAsync(taskass.ReAssignerId);
                    details.ReAssigner = person.Name + " " + person.LastName;
                }

                Alldetails.Add(details);
            }

            ViewBag.UnAssigned = unassignedtasks;
            ViewBag.Assigned = assignedtasks;
            ViewBag.Completed = completedtasks;
            ViewBag.AllDetails = Alldetails;
            return View();
        }

        [HttpGet("AssignTask")]
        public async Task<IActionResult> AssignTaskAsync(string Id)
        {

            var task = _db.Tasks.Where(a => a.Id == Id).FirstOrDefault();
            if (task == null)
            {
                TempData["result"] = "Task could not be found.";
                return RedirectToAction("AdminDashboard", "Tasks");
            }

            var application = ResolveTaskApplication(task);
            if (application == null)
            {
                TempData["result"] = "The task's application record could not be found.";
                return RedirectToAction("AdminDashboard", "Tasks");
            }

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
            var examinerfullname = examiner == null
                ? "Unassigned"
                : $"{examiner.Name} {examiner.LastName}".Trim();
            details.ExaminerName = examinerfullname;
            details.RefNumber = ResolveTaskReference(task, application);
            details.Id = task.Id;
            //getting Bar name
            var rootApplicationId = ResolveTaskRootApplicationId(task);
            var outletdetails = !string.IsNullOrWhiteSpace(rootApplicationId)
                ? _db.OutletInfo.Where(s => s.ApplicationId == rootApplicationId).FirstOrDefault()
                : null;
            details.BarName = outletdetails?.TradingName ?? application.BusinessName ?? "N/A";
            details.DateSubmitted = application.ApplicationDate;
            details.TaskStatus = task.Status ?? "Unknown";
            details.JobStatus = task.ExaminationStatus ?? application.ExaminationStatus ?? "N/A";


            // getting licnse type
            var licenseType = _db.LicenseTypes.Where(w => w.Id == application.LicenseTypeID).FirstOrDefault();
            details.LicenseType = licenseType?.LicenseName ?? "N/A";
            details.DateCreated = task.DateAdded;

            //var inspector = await userManager.GetUsersInRoleAsync("inspector");
            List<ApplicationUser> examiners = new List<ApplicationUser>();
            if(task.ExaminationStatus == "verification")
            {
                examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("verifier");

            }
            else if (task.ExaminationStatus == "recommendation")
            {
                examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("recommender");

            }
            else if (task.ExaminationStatus == "approval")
            {
                examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("secretary");

            }
             else if (task.ExaminationStatus == "inspection")
            {
                examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("inspector");

            }

            if(task.AssignerId == "system" )
            {
                details.Assigner = "System";
            }
            else if(task.AssignerId == null || task.AssignerId == "")
            {
                details.Assigner = "";
            }
            else
            {
                var person = await userManager.FindByIdAsync(task.AssignerId);
                details.Assigner = person.Name + " " + person.LastName;
            }


            if(task.ReAssignerId == null || task.ReAssignerId == "")
            {
                details.ReAssigner = "";
            }
            else
            {
                var person = await userManager.FindByIdAsync(task.ReAssignerId);
                details.ReAssigner = person.Name + " " + person.LastName;
            }
            ViewBag.TaskDetails = details;
            ViewBag.Task = task;
            ViewBag.ApplicationInfo = application;
            ViewBag.Examiners = examiners;
            ViewBag.Task = task;
            return View();
        }

        [HttpPost("AssignTask")]
        public async Task<IActionResult> AssignTask(Tasks tasks, string reassignedto,string stage)
        {
            var task = _db.Tasks.Where(a => a.Id == tasks.Id).FirstOrDefault();
            task.DateUpdated = DateTime.Now;
            var userId = await userManager.FindByEmailAsync(User.Identity.Name);
            string id = userId.Id;
            task.ReAssignerId = id;
            task.Status = "reassigned";
           // task.InspectorId = tasks.InspectorId;
            _db.Update(task);
            _db.SaveChanges();

            Tasks newtask = new Tasks();

            newtask.Id = Guid.NewGuid().ToString();
            newtask.ApplicationId = task.ApplicationId;
            newtask.ExaminationStatus = task.ExaminationStatus;
            //tasks.AssignerId

            //auto allocation to replace
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            if (task.ExaminationStatus == "verification")
            {
               // examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("verifier");
                newtask.VerifierId = reassignedto;

            }
            else if (task.ExaminationStatus == "recommendation")
            {
                newtask.RecommenderId = reassignedto;
               // examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("recommender");

            }
            else if (task.ExaminationStatus == "approval")
            {
                newtask.ApproverId = reassignedto;
                //examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("secretary");

            }

            newtask.Service = task.Service;
            newtask.AssignerId= id;
            newtask.Status = "assigned";
            newtask.DateAdded = DateTime.Now;
            newtask.DateUpdated = DateTime.Now;
            _db.Add(newtask);
            _db.SaveChanges();

            return RedirectToAction("AdminDashboard", "Tasks");
        }


        [HttpGet("BulkReassignment")]
        public async Task<IActionResult> BulkReassignment(string stage)
        {
            //var assignedtasks = _db.Tasks.Where(a => a.Status == "assigned" || a.Status == "reassigned").ToList();
            var assignedtasks = _db.Tasks.Where(a => a.Status == "assigned").ToList();

            List<TaskDetails> Alldetails = new List<TaskDetails>();
            List<ApplicationUser> examinerslist = new List<ApplicationUser>();

            if (stage == "verification")
            {
                examinerslist = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("verifier");

            }
            else if (stage == "recommendation")
            {
                examinerslist = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("recommender");

            }
            else if (stage == "approval")
            {
                examinerslist = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("secretary");

            }

            foreach (var taskass in assignedtasks)
            {
                if (!string.IsNullOrWhiteSpace(stage)
                    && !string.Equals(taskass.ExaminationStatus, stage, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var application = ResolveTaskApplication(taskass);
                if (application == null)
                {
                    continue;
                }

                TaskDetails details = new TaskDetails();
                ApplicationUser examiner = null;
                if (taskass.VerifierId != null)
                {
                    examiner = await userManager.FindByIdAsync(taskass.VerifierId);
                }
                else if (taskass.RecommenderId != null)
                {
                    examiner = await userManager.FindByIdAsync(taskass.RecommenderId);
                }
                else if (taskass.ApproverId != null)
                {
                    examiner = await userManager.FindByIdAsync(taskass.ApproverId);
                }

                var examinerfullname = examiner == null
                    ? "Unassigned"
                    : $"{examiner.Name} {examiner.LastName}".Trim();
                details.ExaminerName = examinerfullname;
                details.RefNumber = ResolveTaskReference(taskass, application);
                details.Id = taskass.Id;

                var rootApplicationId = ResolveTaskRootApplicationId(taskass);
                var outletdetails = !string.IsNullOrWhiteSpace(rootApplicationId)
                    ? _db.OutletInfo.Where(s => s.ApplicationId == rootApplicationId).FirstOrDefault()
                    : null;
                details.BarName = outletdetails?.TradingName ?? application.BusinessName ?? "N/A";
                details.DateSubmitted = application.ApplicationDate;
                details.TaskStatus = taskass.Status ?? "Unknown";
                details.JobStatus = taskass.ExaminationStatus ?? application.ExaminationStatus ?? "N/A";
                details.DateCreated = taskass.DateAdded;
                var licenseType = _db.LicenseTypes.Where(w => w.Id == application.LicenseTypeID).FirstOrDefault();
                details.LicenseType = licenseType?.LicenseName ?? "N/A";

                //var inspector = await userManager.GetUsersInRoleAsync("inspector");
                List<ApplicationUser> examiners = new List<ApplicationUser>();
                if (taskass.ExaminationStatus == "verification")
                {
                    examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("verifier");

                }
                else if (taskass.ExaminationStatus == "recommendation")
                {
                    examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("recommender");

                }
                else if (taskass.ExaminationStatus == "approval")
                {
                    examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("secretary");
                }

                if (taskass.AssignerId == "system")
                {
                    details.Assigner = "System";
                }
                else if (taskass.AssignerId == null || taskass.AssignerId == "")
                {
                    details.Assigner = "";
                }
                else
                {
                    var person = await userManager.FindByIdAsync(taskass.AssignerId);
                    details.Assigner = person.Name + " " + person.LastName;
                }


                if (taskass.ReAssignerId == null || taskass.ReAssignerId == "")
                {
                    details.ReAssigner = "";
                }
                else
                {
                    var person = await userManager.FindByIdAsync(taskass.ReAssignerId);
                    details.ReAssigner = person.Name + " " + person.LastName;
                }

                Alldetails.Add(details);
            }
            ViewBag.Stage = stage;
            ViewBag.Examiners = examinerslist;
            ViewBag.AllDetails = Alldetails;
            return View();
        }


            [HttpPost("BulkReassignment")]
        public async Task<IActionResult> BulkReassignmentAsync(string stage )
        {
            //var assignedtasks = _db.Tasks.Where(a => a.Status == "assigned" || a.Status == "reassigned").ToList();
            var assignedtasks = _db.Tasks.Where(a => a.Status == "assigned").ToList();

            List<TaskDetails> Alldetails = new List<TaskDetails>();
            List<ApplicationUser> examinerslist = new List<ApplicationUser>();

            if (stage == "verification")
            {
                examinerslist = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("verifier");

            }
            else if (stage == "recommendation")
            {
                examinerslist = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("recommender");

            }
            else if (stage == "approval")
            {
                examinerslist = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("secretary");

            }
           
            foreach (var taskass in assignedtasks)
            {
                if (!string.IsNullOrWhiteSpace(stage)
                    && !string.Equals(taskass.ExaminationStatus, stage, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var application = ResolveTaskApplication(taskass);
                if (application == null)
                {
                    continue;
                }

                TaskDetails details = new TaskDetails();
                ApplicationUser examiner = null;
                if (taskass.VerifierId != null)
                {
                    examiner = await userManager.FindByIdAsync(taskass.VerifierId);
                }
                else if (taskass.RecommenderId != null)
                {
                    examiner = await userManager.FindByIdAsync(taskass.RecommenderId);
                }
                else if (taskass.ApproverId != null)
                {
                    examiner = await userManager.FindByIdAsync(taskass.ApproverId);
                }

                var examinerfullname = examiner == null
                    ? "Unassigned"
                    : $"{examiner.Name} {examiner.LastName}".Trim();
                details.ExaminerName = examinerfullname;
                details.RefNumber = ResolveTaskReference(taskass, application);
                details.Id = taskass.Id;

                var rootApplicationId = ResolveTaskRootApplicationId(taskass);
                var outletdetails = !string.IsNullOrWhiteSpace(rootApplicationId)
                    ? _db.OutletInfo.Where(s => s.ApplicationId == rootApplicationId).FirstOrDefault()
                    : null;
                details.BarName = outletdetails?.TradingName ?? application.BusinessName ?? "N/A";
                details.DateSubmitted = application.ApplicationDate;
                details.TaskStatus = taskass.Status ?? "Unknown";
                details.JobStatus = taskass.ExaminationStatus ?? application.ExaminationStatus ?? "N/A";
                details.DateCreated = taskass.DateAdded;
                var licenseType = _db.LicenseTypes.Where(w => w.Id == application.LicenseTypeID).FirstOrDefault();
                details.LicenseType = licenseType?.LicenseName ?? "N/A";

                //var inspector = await userManager.GetUsersInRoleAsync("inspector");
                List<ApplicationUser> examiners = new List<ApplicationUser>();
                if (taskass.ExaminationStatus == "verification")
                {
                    examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("verifier");

                }
                else if (taskass.ExaminationStatus == "recommendation")
                {
                    examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("recommender");

                }
                else if (taskass.ExaminationStatus == "approval")
                {
                    examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("secretary");
                }

                if (taskass.AssignerId == "system")
                {
                    details.Assigner = "System";
                }
                else if (taskass.AssignerId == null || taskass.AssignerId == "")
                {
                    details.Assigner = "";
                }
                else
                {
                    var person = await userManager.FindByIdAsync(taskass.AssignerId);
                    details.Assigner = person.Name + " " + person.LastName;
                }


                if (taskass.ReAssignerId == null || taskass.ReAssignerId == "")
                {
                    details.ReAssigner = "";
                }
                else
                {
                    var person = await userManager.FindByIdAsync(taskass.ReAssignerId);
                    details.ReAssigner = person.Name + " " + person.LastName;
                }

                Alldetails.Add(details);
            }
            ViewBag.Stage = stage;
            ViewBag.Examiners = examinerslist;
            ViewBag.AllDetails = Alldetails;
            return View();
        }

        [HttpPost("BulkAction")]
        public async Task<IActionResult> BulkAction(List<string> taskIds, string stage, string examiner)
        {

            foreach( var  taskId in taskIds)
            {
                var task = _db.Tasks.Where(a => a.Id == taskId).FirstOrDefault();
                task.DateUpdated = DateTime.Now;
                var userId = await userManager.FindByEmailAsync(User.Identity.Name);
                string id = userId.Id;
                task.ReAssignerId = id;
                task.Status = "reassigned";
                // task.InspectorId = tasks.InspectorId;
                _db.Update(task);
                _db.SaveChanges();

                Tasks newtask = new Tasks();

                newtask.Id = Guid.NewGuid().ToString();
                newtask.ApplicationId = task.ApplicationId;
                //tasks.AssignerId
                newtask.ExaminationStatus = task.ExaminationStatus;
                //auto allocation to replace
                // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
                // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
                if (stage == "verification")
                {
                    // examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("verifier");
                    newtask.VerifierId = examiner;

                }
                else if (stage == "recommendation")
                {
                    newtask.RecommenderId = examiner;
                    // examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("recommender");

                }
                else if (stage == "approval")
                {
                    newtask.ApproverId = examiner;
                    //examiners = (List<ApplicationUser>)await userManager.GetUsersInRoleAsync("secretary");

                }
                newtask.Service = task.Service;
                newtask.AssignerId = id;
                newtask.Status = "assigned";
                newtask.DateAdded = DateTime.Now;
                newtask.DateUpdated = DateTime.Now;
                _db.Add(newtask);
                _db.SaveChanges();
            }

            return RedirectToAction("BulkReassignment", "Tasks", new { stage = stage });
            //Tasks tasks = new Tasks();
            //tasks.Id = Guid.NewGuid().ToString();
            //tasks.ApplicationId = application.Id;
            ////tasks.AssignerId

            ////auto allocation to replace
            //// var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            //// var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
            //tasks.VerifierId = selectedUser.Id;
            //tasks.AssignerId = "system";
            //tasks.Status = "assigned";
            //tasks.DateAdded = DateTime.Now;
            //tasks.DateUpdated = DateTime.Now;
            //_db.Add(tasks);
            //_db.SaveChanges();
            return View();
        }

        private ApplicationInfo? ResolveTaskApplication(Tasks task)
        {
            var rootApplicationId = ResolveTaskRootApplicationId(task);
            if (string.IsNullOrWhiteSpace(rootApplicationId))
            {
                return null;
            }

            return _db.ApplicationInfo.Where(s => s.Id == rootApplicationId).FirstOrDefault();
        }

        private string? ResolveTaskRootApplicationId(Tasks task)
        {
            if (task == null || string.IsNullOrWhiteSpace(task.ApplicationId))
            {
                return null;
            }

            var service = task.Service?.Trim().ToLowerInvariant();

            return service switch
            {
                "extended hours" => _db.ExtendedHours.Where(x => x.Id == task.ApplicationId).Select(x => x.ApplicationId).FirstOrDefault(),
                "temporary retails" => _db.TemporaryRetails.Where(x => x.Id == task.ApplicationId).Select(x => x.ApplicationId).FirstOrDefault(),
                "extra counter" => _db.ExtraCounter.Where(x => x.Id == task.ApplicationId).Select(x => x.ApplicationId).FirstOrDefault()
                    ?? _db.ExtendedHours.Where(x => x.Id == task.ApplicationId).Select(x => x.ApplicationId).FirstOrDefault(),
                "changemanager" => _db.ChangeManaager.Where(x => x.Id == task.ApplicationId).Select(x => x.ApplicationId).FirstOrDefault()
                    ?? _db.ChangeManaager.Where(x => x.ApplicationId == task.ApplicationId).OrderByDescending(x => x.DateApplied).Select(x => x.ApplicationId).FirstOrDefault()
                    ?? task.ApplicationId,
                "change manager" => _db.ChangeManaager.Where(x => x.Id == task.ApplicationId).Select(x => x.ApplicationId).FirstOrDefault()
                    ?? _db.ChangeManaager.Where(x => x.ApplicationId == task.ApplicationId).OrderByDescending(x => x.DateApplied).Select(x => x.ApplicationId).FirstOrDefault()
                    ?? task.ApplicationId,
                "manager change" => _db.ChangeManaager.Where(x => x.Id == task.ApplicationId).Select(x => x.ApplicationId).FirstOrDefault()
                    ?? _db.ChangeManaager.Where(x => x.ApplicationId == task.ApplicationId).OrderByDescending(x => x.DateApplied).Select(x => x.ApplicationId).FirstOrDefault()
                    ?? task.ApplicationId,
                _ => task.ApplicationId
            };
        }

        private string ResolveTaskReference(Tasks task, ApplicationInfo? application)
        {
            if (task == null)
            {
                return application?.RefNum ?? string.Empty;
            }

            var service = task.Service?.Trim().ToLowerInvariant();
            string? reference = service switch
            {
                "renewal" => _db.Renewals
                    .Where(x => x.ApplicationId == task.ApplicationId)
                    .OrderByDescending(x => x.DateUpdated)
                    .Select(x => x.Reference)
                    .FirstOrDefault(),
                "renewal inspection" => _db.Inspection
                    .Where(x => x.ApplicationId == task.ApplicationId && x.Service == "Renewal Inspection")
                    .OrderByDescending(x => x.DateApplied)
                    .Select(x => x.Reference)
                    .FirstOrDefault(),
                "inspection" => _db.Inspection
                    .Where(x => x.ApplicationId == task.ApplicationId && x.Service == "Inspection")
                    .OrderByDescending(x => x.DateApplied)
                    .Select(x => x.Reference)
                    .FirstOrDefault()
                    ?? _db.Inspection
                        .Where(x => x.ApplicationId == task.ApplicationId)
                        .OrderByDescending(x => x.DateApplied)
                        .Select(x => x.Reference)
                        .FirstOrDefault(),
                "extended hours" => _db.ExtendedHours
                    .Where(x => x.Id == task.ApplicationId)
                    .Select(x => x.Reference)
                    .FirstOrDefault(),
                "temporary retails" => _db.TemporaryRetails
                    .Where(x => x.Id == task.ApplicationId)
                    .Select(x => x.Reference)
                    .FirstOrDefault(),
                "extra counter" => _db.ExtraCounter
                    .Where(x => x.Id == task.ApplicationId)
                    .Select(x => x.Reference)
                    .FirstOrDefault()
                    ?? _db.ExtendedHours
                        .Where(x => x.Id == task.ApplicationId)
                        .Select(x => x.Reference)
                        .FirstOrDefault(),
                "changemanager" => _db.ChangeManaager
                    .Where(x => x.Id == task.ApplicationId)
                    .Select(x => x.Reference)
                    .FirstOrDefault()
                    ?? _db.ChangeManaager
                        .Where(x => x.ApplicationId == task.ApplicationId)
                        .OrderByDescending(x => x.DateApplied)
                        .Select(x => x.Reference)
                        .FirstOrDefault(),
                "change manager" => _db.ChangeManaager
                    .Where(x => x.Id == task.ApplicationId)
                    .Select(x => x.Reference)
                    .FirstOrDefault()
                    ?? _db.ChangeManaager
                        .Where(x => x.ApplicationId == task.ApplicationId)
                        .OrderByDescending(x => x.DateApplied)
                        .Select(x => x.Reference)
                        .FirstOrDefault(),
                "manager change" => _db.ChangeManaager
                    .Where(x => x.Id == task.ApplicationId)
                    .Select(x => x.Reference)
                    .FirstOrDefault()
                    ?? _db.ChangeManaager
                        .Where(x => x.ApplicationId == task.ApplicationId)
                        .OrderByDescending(x => x.DateApplied)
                        .Select(x => x.Reference)
                        .FirstOrDefault(),
                "license duplicate" => application?.LLBNum,
                _ => null
            };

            return string.IsNullOrWhiteSpace(reference)
                ? application?.RefNum ?? string.Empty
                : reference;
        }


        }
}
