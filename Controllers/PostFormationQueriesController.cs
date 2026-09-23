using LLB.Data;
using LLB.Helpers;
using LLB.Models;
using LLB.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LLB.Controllers
{
    [Authorize]
    [Route("PostFormationQueries")]
    public class PostFormationQueriesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaskAllocationHelper _taskAllocationHelper;

        public PostFormationQueriesController(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            TaskAllocationHelper taskAllocationHelper)
        {
            _db = db;
            _userManager = userManager;
            _taskAllocationHelper = taskAllocationHelper;
        }

        [HttpPost("Raise")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "verifier,recommender,secretary,admin,super user")]
        public IActionResult Raise(string recordId, string taskId, string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length > 2000)
            {
                TempData["error"] = "Enter a query of no more than 2,000 characters.";
                return RedirectToReviewerDashboard();
            }

            var task = _db.Tasks.FirstOrDefault(item => item.Id == taskId && item.Status == "assigned");
            if (task == null || !CanReviewTask(task))
            {
                return Forbid();
            }

            var record = ResolveRecord(recordId, task);
            if (record == null)
            {
                return NotFound();
            }

            if (PostFormationQueryWorkflow.HasOpenQueries(_db, recordId))
            {
                TempData["error"] = "This post-formation application already has an open query.";
                return RedirectToReviewerDashboard();
            }

            var now = DateTime.Now;
            _db.Queries.Add(new Queries
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationId = recordId,
                InspectorId = _userManager.GetUserId(User),
                TaskId = task.Id,
                Stage = PostFormationQueryWorkflow.BuildStage(task, record.Status),
                Query = query.Trim(),
                Status = ApplicationQueryWorkflow.OpenStatus,
                DateAdded = now,
                DateUpdated = now
            });

            record.SetStatus(ApplicationQueryWorkflow.OpenStatus);
            record.Touch(now);
            task.Status = "completed";
            task.DateUpdated = now;
            _db.Update(task);
            _db.SaveChanges();

            TempData["success"] = $"Query sent to the applicant for the {record.ServiceName} application.";
            return RedirectToReviewerDashboard();
        }

        [HttpGet("")]
        public IActionResult Index(string recordId)
        {
            var queryRecords = GetQueryRecords(recordId);
            if (queryRecords.Count == 0)
            {
                return NotFound();
            }

            var sourceTask = FindSourceTask(queryRecords);
            var record = sourceTask == null ? null : ResolveRecord(recordId, sourceTask);
            if (record == null || !IsOwner(record))
            {
                return NotFound();
            }

            var outlet = string.IsNullOrWhiteSpace(record.RootApplicationId)
                ? null
                : _db.OutletInfo.AsNoTracking()
                    .Where(item => item.ApplicationId == record.RootApplicationId)
                    .OrderByDescending(item => item.Status == "active")
                    .ThenByDescending(item => item.DateUpdated)
                    .FirstOrDefault();

            var model = new PostFormationQueriesViewModel
            {
                RecordId = record.RecordId,
                Reference = record.Reference,
                ServiceName = record.ServiceName,
                TradingName = outlet?.TradingName ?? record.TradingName ?? "Post-formation application",
                Status = record.Status,
                IsCorrectionMode = string.Equals(record.Status, "inprogress", StringComparison.OrdinalIgnoreCase),
                Queries = queryRecords.Select(ToQueryItem).ToList()
            };

            return View(model);
        }

        [HttpPost("BeginCorrection")]
        [ValidateAntiForgeryToken]
        public IActionResult BeginCorrection(string recordId)
        {
            var queryRecords = GetQueryRecords(recordId);
            var sourceTask = FindSourceTask(queryRecords);
            var record = sourceTask == null ? null : ResolveRecord(recordId, sourceTask);
            if (record == null || !IsOwner(record))
            {
                return NotFound();
            }

            if (!queryRecords.Any(query => ApplicationQueryWorkflow.IsOpen(query)))
            {
                TempData["error"] = "There is no open query to correct.";
                return RedirectToAction("Index", new { recordId });
            }

            record.SetStatus("inprogress");
            record.Touch(DateTime.Now);
            _db.SaveChanges();
            return Redirect(record.EditUrl);
        }

        [HttpPost("Resolve")]
        [ValidateAntiForgeryToken]
        public IActionResult Resolve(string recordId)
        {
            var queryRecords = GetQueryRecords(recordId);
            var sourceTask = FindSourceTask(queryRecords);
            var record = sourceTask == null ? null : ResolveRecord(recordId, sourceTask);
            if (record == null || !IsOwner(record))
            {
                return NotFound();
            }

            if (!string.Equals(record.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Open the application and make the requested correction before resolving the query.";
                return RedirectToAction("Index", new { recordId });
            }

            var openQueries = queryRecords.Where(ApplicationQueryWorkflow.IsOpen).ToList();
            if (openQueries.Count == 0)
            {
                TempData["error"] = "There are no open queries to resolve.";
                return RedirectToAction("Index", new { recordId });
            }

            var now = DateTime.Now;
            foreach (var item in openQueries)
            {
                item.Status = ApplicationQueryWorkflow.ResolvedStatus;
                item.DateUpdated = now;
            }

            _db.SaveChanges();
            TempData["success"] = "Post-formation query resolved. You can now resubmit the application.";
            return RedirectToAction("Index", new { recordId });
        }

        [HttpPost("Resubmit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitAsync(string recordId)
        {
            var queryRecords = GetQueryRecords(recordId);
            if (queryRecords.Count == 0 || queryRecords.Any(ApplicationQueryWorkflow.IsOpen))
            {
                TempData["error"] = "Resolve every open query before resubmitting.";
                return RedirectToAction("Index", new { recordId });
            }

            var sourceTask = FindSourceTask(queryRecords);
            var record = sourceTask == null ? null : ResolveRecord(recordId, sourceTask);
            if (record == null || sourceTask == null || !IsOwner(record))
            {
                return NotFound();
            }

            if (!string.Equals(record.Status, "inprogress", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "This application has already been resubmitted for examination.";
                return RedirectToAction("Index", new { recordId });
            }

            var latestQuery = queryRecords.OrderByDescending(item => item.DateAdded).First();
            var reviewStage = PostFormationQueryWorkflow.GetReviewStage(latestQuery);
            var assigneeId = reviewStage switch
            {
                "approval" => await _taskAllocationHelper.GetSecretary(_db, _userManager),
                "recommendation" => await _taskAllocationHelper.GetRecommender(_db, _userManager),
                _ => await _taskAllocationHelper.GetVerifier(_db, _userManager)
            };
            if (string.IsNullOrWhiteSpace(assigneeId))
            {
                TempData["error"] = "No examiner is currently available. Please try again later.";
                return RedirectToAction("Index", new { recordId });
            }

            var now = DateTime.Now;
            var task = new Tasks
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationId = sourceTask.ApplicationId,
                AssignerId = "system",
                Service = sourceTask.Service,
                ExaminationStatus = reviewStage,
                Status = "assigned",
                DateAdded = now,
                DateUpdated = now
            };
            if (reviewStage == "approval")
            {
                task.ApproverId = assigneeId;
            }
            else if (reviewStage == "recommendation")
            {
                task.RecommenderId = assigneeId;
            }
            else
            {
                task.VerifierId = assigneeId;
            }

            record.SetStatus(PostFormationQueryWorkflow.GetPreviousStatus(latestQuery, sourceTask.Service ?? record.ServiceName));
            record.Touch(now);
            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            TempData["success"] = $"{record.ServiceName} application resubmitted for examination.";
            return RedirectToAction("Dashboard", "Home", new { tab = "services-pane" });
        }

        private List<Queries> GetQueryRecords(string recordId)
        {
            return _db.Queries
                .Where(query => query.ApplicationId == recordId)
                .OrderBy(query => query.DateAdded)
                .ToList()
                .Where(PostFormationQueryWorkflow.IsPostFormationQuery)
                .ToList();
        }

        private Tasks? FindSourceTask(IEnumerable<Queries> queries)
        {
            var taskId = queries
                .OrderByDescending(query => query.DateAdded)
                .Select(query => query.TaskId)
                .Where(taskId => !string.IsNullOrWhiteSpace(taskId))
                .FirstOrDefault();
            return string.IsNullOrWhiteSpace(taskId)
                ? null
                : _db.Tasks.FirstOrDefault(task => task.Id == taskId);
        }

        private bool IsOwner(PostFormationRecord record)
        {
            return string.Equals(record.UserId, _userManager.GetUserId(User), StringComparison.OrdinalIgnoreCase);
        }

        private bool CanReviewTask(Tasks task)
        {
            if (User.IsInRole("admin") || User.IsInRole("super user"))
            {
                return true;
            }

            var userId = _userManager.GetUserId(User);
            return PostFormationQueryWorkflow.GetReviewStage(task) switch
            {
                "approval" => string.Equals(task.ApproverId, userId, StringComparison.OrdinalIgnoreCase),
                "recommendation" => string.Equals(task.RecommenderId, userId, StringComparison.OrdinalIgnoreCase),
                _ => string.Equals(task.VerifierId, userId, StringComparison.OrdinalIgnoreCase)
            };
        }

        private IActionResult RedirectToReviewerDashboard()
        {
            if (User.IsInRole("secretary"))
            {
                return RedirectToAction("Dashboard", "Approval");
            }

            if (User.IsInRole("recommender"))
            {
                return RedirectToAction("Dashboard", "Recommend");
            }

            return RedirectToAction("Dashboard", "Verify");
        }

        private static ApplicationQueryItemViewModel ToQueryItem(Queries query)
        {
            var resolved = ApplicationQueryWorkflow.IsResolved(query);
            return new ApplicationQueryItemViewModel
            {
                Id = query.Id ?? string.Empty,
                Message = query.Query ?? string.Empty,
                ReviewerStage = PostFormationQueryWorkflow.GetReviewerLabel(query),
                DateRaised = query.DateAdded,
                DateResolved = resolved ? query.DateUpdated : null,
                IsResolved = resolved
            };
        }

        private PostFormationRecord? ResolveRecord(string recordId, Tasks task)
        {
            var service = task.Service?.Trim() ?? string.Empty;
            if (string.Equals(service, "renewal", StringComparison.OrdinalIgnoreCase))
            {
                var renewal = _db.Renewals.FirstOrDefault(item => item.Id == recordId && item.ApplicationId == task.ApplicationId);
                return renewal == null ? null : new PostFormationRecord(
                    recordId, renewal.UserId, renewal.ApplicationId, renewal.Reference, renewal.Status, "Renewal",
                    null, $"/Postprocess/Renewal?id={renewal.ApplicationId}&process=RNW&renid={renewal.Id}",
                    status => renewal.Status = status, date => renewal.DateUpdated = date);
            }

            if (string.Equals(service, "inspection", StringComparison.OrdinalIgnoreCase)
                || string.Equals(service, "renewal inspection", StringComparison.OrdinalIgnoreCase))
            {
                var inspection = _db.Inspection.FirstOrDefault(item => item.Id == recordId && item.ApplicationId == task.ApplicationId);
                return inspection == null ? null : new PostFormationRecord(
                    recordId, inspection.UserId, inspection.ApplicationId, inspection.Reference, inspection.Status,
                    inspection.Service ?? "Inspection", null,
                    $"/Postprocess/Inspection?id={inspection.ApplicationId}&process=INP",
                    status => inspection.Status = status, date => inspection.DateUpdate = date);
            }

            if (string.Equals(service, "Extended Hours", StringComparison.OrdinalIgnoreCase))
            {
                var record = _db.ExtendedHours.FirstOrDefault(item => item.Id == recordId && item.Id == task.ApplicationId);
                return record == null ? null : new PostFormationRecord(
                    recordId, record.UserId, record.ApplicationId, record.Reference, record.Status, "Extended Hours", null,
                    $"/Postprocess/ExtendedHours?id={record.ApplicationId}&process=EXH&extId={record.Id}",
                    status => record.Status = status, date => record.DateUpdated = date);
            }

            if (string.Equals(service, "Temporary Retails", StringComparison.OrdinalIgnoreCase))
            {
                var record = _db.TemporaryRetails.FirstOrDefault(item => item.Id == recordId && item.Id == task.ApplicationId);
                return record == null ? null : new PostFormationRecord(
                    recordId, record.UserId, record.ApplicationId, record.Reference, record.Status, "Temporary Retail", null,
                    $"/Postprocess/TemporaryRetails?id={record.ApplicationId}&process=TRL&temporaryRetailId={record.Id}",
                    status => record.Status = status, date => record.DateUpdated = date);
            }

            if (string.Equals(service, "Manager Change", StringComparison.OrdinalIgnoreCase))
            {
                var record = _db.ChangeManaager.FirstOrDefault(item => item.Id == recordId && item.Id == task.ApplicationId);
                return record == null ? null : new PostFormationRecord(
                    recordId, record.UserId, record.ApplicationId, record.Reference, record.Status, "Manager Change", null,
                    $"/Managers/ManagerChange?id={record.ApplicationId}&process=APM&changeId={record.Id}",
                    status => record.Status = status, date => record.DateUpdated = date.ToString("O"));
            }

            if (string.Equals(service, "Permission to Alter", StringComparison.OrdinalIgnoreCase)
                || string.Equals(service, "Extra Counter", StringComparison.OrdinalIgnoreCase))
            {
                var record = _db.ExtraCounter.FirstOrDefault(item => item.Id == recordId && item.Id == task.ApplicationId);
                var process = string.Equals(service, "Extra Counter", StringComparison.OrdinalIgnoreCase) ? "EXC" : "ECF";
                return record == null ? null : new PostFormationRecord(
                    recordId, record.UserId, record.ApplicationId, record.Reference, record.Status, service, null,
                    $"/Extracounter/Extracounter?id={record.ApplicationId}&process={process}&ecId={record.Id}",
                    status => record.Status = status, date => record.DateUpdated = date);
            }

            if (string.Equals(service, GovernmentPermitHelper.ServiceName, StringComparison.OrdinalIgnoreCase))
            {
                var record = _db.GovernmentPermit.FirstOrDefault(item => item.Id == recordId && item.Id == task.ApplicationId);
                return record == null ? null : new PostFormationRecord(
                    recordId, record.UserId, record.ApplicationId, record.Reference, record.Status, GovernmentPermitHelper.ServiceName,
                    record.LocationName, $"/GovernmentPermit/Apply?id={record.Id}",
                    status => record.Status = status, date => record.DateUpdated = date);
            }

            if (string.Equals(service, TemporaryTransferHelper.ServiceName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(service, TemporaryRemovalHelper.ServiceName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(service, AgentLicenseHelper.ServiceName, StringComparison.OrdinalIgnoreCase))
            {
                var record = _db.ApplicationInfo.FirstOrDefault(item => item.Id == recordId && item.Id == task.ApplicationId);
                if (record == null)
                {
                    return null;
                }

                var editUrl = string.Equals(service, TemporaryTransferHelper.ServiceName, StringComparison.OrdinalIgnoreCase)
                    ? $"/TemporaryTransfer/Apply?id={record.Id}&edit=true"
                    : string.Equals(service, TemporaryRemovalHelper.ServiceName, StringComparison.OrdinalIgnoreCase)
                        ? $"/TemporaryRemoval/Apply?recordId={record.Id}"
                        : $"/AgentLicense/Apply?id={record.Id}&fromPostFormation=true";
                return new PostFormationRecord(
                    recordId, record.UserID, record.CompanyNumber ?? record.Id, record.RefNum, record.Status, service,
                    record.BusinessName, editUrl,
                    status => record.Status = status, date => record.DateUpdated = date);
            }

            return null;
        }

        private sealed class PostFormationRecord
        {
            public PostFormationRecord(
                string recordId,
                string? userId,
                string? rootApplicationId,
                string? reference,
                string? status,
                string serviceName,
                string? tradingName,
                string editUrl,
                Action<string> setStatus,
                Action<DateTime> touch)
            {
                RecordId = recordId;
                UserId = userId ?? string.Empty;
                RootApplicationId = rootApplicationId ?? string.Empty;
                Reference = reference ?? recordId;
                Status = status ?? string.Empty;
                ServiceName = serviceName;
                TradingName = tradingName;
                EditUrl = editUrl;
                SetStatus = statusValue =>
                {
                    Status = statusValue;
                    setStatus(statusValue);
                };
                Touch = touch;
            }

            public string RecordId { get; }
            public string UserId { get; }
            public string RootApplicationId { get; }
            public string Reference { get; }
            public string Status { get; private set; }
            public string ServiceName { get; }
            public string? TradingName { get; }
            public string EditUrl { get; }
            public Action<string> SetStatus { get; }
            public Action<DateTime> Touch { get; }
        }
    }
}
