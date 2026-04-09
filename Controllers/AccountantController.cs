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
using static System.Net.Mime.MediaTypeNames;
using Microsoft.EntityFrameworkCore;
using LLB.Helpers;
using System.Globalization;
using IronPdf;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace LLB.Controllers
{
    [Authorize]
    [Route("Accountant")]
    public class AccountantController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;
        private readonly TaskAllocationHelper _taskAllocationHelper;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountantController(TaskAllocationHelper taskAllocationHelper, AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
            _taskAllocationHelper = taskAllocationHelper;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("Dashboard")]
        
        public async Task<IActionResult> DashboardAsync(CancellationToken cancellationToken)
        {
            var todayStart = DateTime.Today;
            var tomorrowStart = todayStart.AddDays(1);
            int userCount = (await userManager.Users.ToListAsync()).Count;
            var rejected = _db.Payments.Where(a => a.PaymentStatus == "Rejected").ToList();
            var paid = _db.Payments.Where(a => a.Status == "Paid").ToList();
            var paidnow = _db.Payments.Where(a => a.Status == "Paid" && a.DateAdded.Month == DateTime.Now.Month).ToList();
            var approved = _db.Payments.Where(a => a.PaymentStatus == "Approved").ToList();
            var notpaid = _db.Payments.Where(a => a.Status == "not paid").ToList();
            var Cancelled = _db.Payments.Where(a => a.Status == "Cancelled").ToList();
            var awaitin = _db.Payments.Where(a => a.Status == "awaiting verification").ToList();
            var transfer = _db.Payments.Where(a => a.PollUrl == "transfer").ToList();
            var manual = _db.Payments.Where(a => a.PollUrl == "manual").ToList();
            var paynow = _db.Payments.Where(a => a.PaynowRef != "").ToList();
            var paynownow = _db.Payments.Where(a => a.PaynowRef != "" && a.DateAdded.Month == DateTime.Now.Month).ToList();

            ViewBag.TotalPaidnow = paidnow.Sum(a => a.Amount);
            ViewBag.TotalPaid = paid.Sum(a => a.Amount);
            ViewBag.TotalPaynow = paynow.Sum(a => a.Amount);
            ViewBag.TotalPaynownow = paynownow.Sum(a => a.Amount);
            ViewBag.Rejected = rejected;
            ViewBag.SystemUsers = userCount;
            ViewBag.Paid = paid;
            ViewBag.Notpaid = notpaid;
            ViewBag.Cancelled = Cancelled;
            ViewBag.Awaiting = awaitin;
            ViewBag.Approved = approved;
            ViewBag.Transfer = transfer;
            ViewBag.Paynow = paynow;
            var todaysExchangeRate = await _db.ExchangeRate
                .AsNoTracking()
                .Where(rate => rate.DateAdded >= todayStart && rate.DateAdded < tomorrowStart)
                .OrderByDescending(rate => rate.DateUpdated)
                .ThenByDescending(rate => rate.DateAdded)
                .FirstOrDefaultAsync(cancellationToken);
            var todaysExchangeRateUserName = await ResolveUserNameAsync(todaysExchangeRate?.UserId, cancellationToken);
            ViewBag.TodaysExchangeRate = todaysExchangeRate;
            ViewBag.TodaysExchangeRateUserName = todaysExchangeRateUserName;
            ViewBag.ExchangeRateNeedsUpdate = todaysExchangeRate == null;
            return View();
        }

        [Authorize(Roles = "accountant,chief accountant")]
        [HttpGet("ExchangeRate")]
        public async Task<IActionResult> ExchangeRateAsync(CancellationToken cancellationToken)
        {
            var model = await BuildUsdZwgExchangeRateViewModelAsync(cancellationToken);
            ViewData["Title"] = "USD/ZWG Exchange Rate";
            ViewData["Subtitle"] = "Live RBZ interbank rate scanned from the Reserve Bank of Zimbabwe website";
            return View(model);
        }

        [Authorize(Roles = "accountant,chief accountant")]
        [HttpPost("UpdateExchangeRate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExchangeRateAsync(decimal? exchangeRate, CancellationToken cancellationToken)
        {
            if (!exchangeRate.HasValue || exchangeRate.Value <= 0)
            {
                TempData["error"] = "Provide a valid USD/ZWG exchange rate before updating.";
                return RedirectToAction("ExchangeRate");
            }

            var currentUserId = userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var todayStart = DateTime.Today;
            var tomorrowStart = todayStart.AddDays(1);
            var now = DateTime.Now;

            var existingRate = await _db.ExchangeRate
                .Where(rate => rate.DateAdded >= todayStart && rate.DateAdded < tomorrowStart)
                .OrderByDescending(rate => rate.DateUpdated)
                .ThenByDescending(rate => rate.DateAdded)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingRate == null)
            {
                existingRate = new ExchangeRate
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = currentUserId,
                    Status = "active",
                    ZWGrate = (double)exchangeRate.Value,
                    DateAdded = now,
                    DateUpdated = now
                };

                _db.Add(existingRate);
                TempData["success"] = $"USD/ZWG exchange rate for {todayStart:dd-MM-yyyy} has been saved.";
            }
            else
            {
                existingRate.UserId = currentUserId;
                existingRate.ZWGrate = (double)exchangeRate.Value;
                existingRate.Status = "active";
                existingRate.DateUpdated = now;
                _db.Update(existingRate);
                TempData["success"] = $"USD/ZWG exchange rate for {todayStart:dd-MM-yyyy} has been updated.";
            }

            await _db.SaveChangesAsync(cancellationToken);
            return RedirectToAction("ExchangeRate");
        }

        [HttpGet("VerifyPayments")]

        public IActionResult VerifyPayments( string Id, string status)
        {
            var awaitedfor = _db.ApplicationInfo.Where(a => a.PaymentStatus == "payment verification").ToList();

            List<PaymentStatus> mystatuses = new List<PaymentStatus>();
            foreach(var payment in awaitedfor)
            {
                PaymentStatus paydetail = new PaymentStatus();
                paydetail.ApplicationId = payment.Id;
                paydetail.Amount = payment.PaymentFee;

                var licenseType = _db.LicenseTypes.Where(s => s.Id == payment.LicenseTypeID).FirstOrDefault();
                paydetail.LicenseType = licenseType.LicenseName;
                var licenseArea = _db.LicenseRegions.Where(s => s.Id == payment.ApplicationType).FirstOrDefault();
                paydetail.LicenseArea = licenseArea.RegionName;
                paydetail.PaymentId = payment.PaymentId;
                paydetail.Status = payment.PaymentStatus;
                paydetail.ApplicationRefNum = payment.RefNum;
                var transaction = _db.Payments.Where(d => d.Id == payment.PaymentId).FirstOrDefault();
                paydetail.PopDoc = transaction.PopDoc;

                mystatuses.Add(paydetail);


            }
            ViewBag.PaymentLogs = mystatuses;

            return View();
        }

        [HttpGet("Verify")]

        public async Task<IActionResult> VerifyAsync(string ApplicationId, string status, string paymentId)
        {
            var application = _db.ApplicationInfo.Where(a => a.Id == ApplicationId).FirstOrDefault();
            
            if (status == "approved")
            {
                
                application.PaymentId = paymentId;
                application.PaymentStatus = "Paid";
                application.Status = "submitted";
                application.ExaminationStatus = "verification";
                _db.Update(application);
                _db.SaveChanges();


                var payment = _db.Payments.Where(s => s.Id == paymentId).FirstOrDefault();
                payment.PaymentStatus= "Approved";
                payment.Status = "Paid";
                _db.Update(payment);
                _db.SaveChanges();
                //payment.SystemRef =

                var applicationcc = _db.ApplicationInfo.Where(a => a.Id == ApplicationId).FirstOrDefault();
                var newReferenceNumber = ReferenceHelper.GenerateReferenceNumber(_db);

                //applicationcc.RefNum = newReferenceNumber;
                applicationcc.Status = "submitted";
                applicationcc.ExaminationStatus = "verification";
                _db.Update(applicationcc);
                _db.SaveChanges();

                //var managers = _db.ManagersParticulars.Where(a => a.ApplicationId == Id).ToList();
                //foreach (var manager in managers)
                //{
                //    manager.Status = "submitted";
                //    manager.EffectiveDate = DateTime.Now;
                //    _db.Update(manager);
                //    _db.SaveChanges();
                //}

                var managers = _db.ManagersParticulars.Where(a => a.ApplicationId == ApplicationId).ToList();
                foreach (var manager in managers)
                {
                    manager.Status = "submitted";
                    manager.EffectiveDate = DateTime.Now;
                    _db.Update(manager);
                    _db.SaveChanges();
                }

                // running the task allocation method, to be optimised
               

              
                //var verifierId = await TaskAllocator()
                Tasks tasks = new Tasks();
                tasks.Id = Guid.NewGuid().ToString();
                tasks.ApplicationId = application.Id;
                //tasks.AssignerId

                //auto allocation to replace
                // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
                // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
               
                var verifierWithLeastTasks = await _taskAllocationHelper.GetVerifier(_db, userManager);
                tasks.VerifierId = verifierWithLeastTasks;
                tasks.Service = "new application";
                tasks.AssignerId = "system";
                tasks.Status = "assigned";
                tasks.DateAdded = DateTime.Now;
                tasks.DateUpdated = DateTime.Now;
                _db.Add(tasks);
                _db.SaveChanges();

            }
            else if( status== "rejected")
            {
                application.PaymentId = "";
                application.PaymentStatus = "";
                application.Status = "inprogress";
                _db.Update(application);
                _db.SaveChanges();


                var payment = _db.Payments.Where(s => s.Id == paymentId).FirstOrDefault();
                payment.PaymentStatus = "Rejected";
                payment.Status = "not paid";
                _db.Update(payment);
                _db.SaveChanges();
            }
            return RedirectToAction("VerifyPayments");
        }

            //[HttpGet]
            //[AllowAnonymous]
            //public async Task<IActionResult> Wangu()
            //{
            //    HttpClient client = new HttpClient();
            //    var response = await client.GetAsync($"{Globals.Globals.service_end_point}/api/v1/reports/getCompanyInfosx").Result.Content.ReadAsStringAsync();
            //    return View();

        //}
            [HttpGet("Register")]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }
        [AcceptVerbs("Get", "Post")]
        [AllowAnonymous]
        public async Task<IActionResult> IsEmailInUse(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Json(true);
            }
            else
            {
                return Json($"Email {email} is already in use");
            }
        }
/*
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {

            var getallUserName = await userManager.FindByEmailAsync(model.Email);
            if (getallUserName == null)
            {



                if (ModelState.IsValid)
                {
                    var user = new ApplicationUser 
                   
                    {
                        Name = model.Name,

                        LastName = model.LastName,
                        PhysicalAddress = model.PhysicalAddress,
                        Email = model.Email,
                        UserEmail = model.Email,
                        UserName = model.Email,
                        IsActive = true,
                        //ClientId = user.Id,
                        //  var userId = userManager.GetUserId(User);
                        //ApplicationBy = user.Id,
                        PhoneNumber = model.PhoneNumber,
                        NatID = model.NatID,
                        // Nationality = model.Nationality,

                        DateOfApplication = DateTime.Now,
                        DOB = model.DOB,
                        CountryOfResidence = model.CountryOfResidence,
                        Gender = model.Gender,
                        Province = model.Province
                    };
                   
                    var result = await userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                       
                        //modelx.Status = Constants.ApplicationStatus.Pending.ToString();
                        //_db.AddAsync(modelx);
                        //_db.SaveChanges();

                        //if (result.Succeeded)
                        //{
                        //    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                        //    var link = Url.Action(nameof(VerifyEmail), "Auth", new { userId = user.Id, code }, Request.Scheme, Request.Host.ToString());


                        //    SmtpClient client = new SmtpClient("mail.ttcsglobal.com");
                        //    client.UseDefaultCredentials = false;
                        //    client.Credentials = new NetworkCredential("companiesonlinezw", "N3wPr0ducts@1");
                        //    // client.Credentials = new NetworkCredential("username", "password");

                        //    MailMessage mailMessage = new MailMessage();
                        //    mailMessage.From = new MailAddress("companiesonlinezw@ttcsglobal.com");
                        //    mailMessage.To.Add(user.Email);
                        //    mailMessage.IsBodyHtml = true;
                        //    mailMessage.Body = ("<!DOCTYPE html> " +
                        //                        "<html xmlns=\"http://www.w3.org/1999/xhtml\">" +
                        //                        "<head>" +
                        //                        "<title>Email</title>" +
                        //                        "</head>" +
                        //                        "<body style=\"font-family:'Century Gothic'\">" +
                        //                        "<p><b>Hi Dear valued Customer</b></p>" +
                        //                        "<p>Your new password is " + $"<a href=\"{link}\">Verify Email</a> </p>" +
                        //                        "<p> Thank You For Your Support</p> " +
                        //                        "<p>Regards</p>" +
                        //                        "<p>CIPZ</p>" +
                        //                        "</body>" +
                        //                        "</html>"); //GetFormattedMessageHTML();
                        //    mailMessage.Subject = "Email Confirmation";
                        //    client.Send(mailMessage);

                        //    TempData["error"] = "Email Has Been Verified";
                        //    TempData["flash"] = "2";
                        //    return RedirectToAction("Login", "Account");

                        //}

                        //await signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("login", "account");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View(model);
        }
        */
    
       
        
        [HttpPost("Login")]
       
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (!_validatorService.HasRequestValidCaptchaEntry() == true)
                {
                    //this.ModelState.AddModelError(DNTCaptchaTagHelper.CaptchaInputName, "Please Enter Valid Captcha.");
                    return RedirectToAction("Login", "Account");
                }
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else 
                    {
                        return RedirectToAction("index", "home");
                    }

                }
               ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
            }
            ModelState.AddModelError(string.Empty, "Invalid Login Attempt");

            return View(model);
        }
        [HttpGet("AccessDenied")]
        
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet("LandingPage")]
        [AllowAnonymous]
        public IActionResult LandingPage()
        {
            return View();
        }

        [HttpGet("PaynowTransactions")]
        public async Task<IActionResult> PaynowTransactions()
        {
            var transactions = _db.Payments.Where(a => a.PollUrl != "transfer" && a.DateAdded.Month == DateTime.Now.Month).ToList().OrderBy(a => a.DateAdded); ;
            List<PaynowDetails> AllDetails = new List<PaynowDetails>();
            foreach (var tran in transactions)
            {
                PaynowDetails detail =new PaynowDetails();
                var application = _db.ApplicationInfo.Where(s => s.Id == tran.ApplicationId).FirstOrDefault();
                detail.ApplicationRef = application.RefNum;
                var userDetail = await userManager.FindByIdAsync(application.UserID);
                detail.Payer = userDetail.Name + " " + userDetail.LastName;
                detail.PollUrl = tran.PollUrl;
                detail.PaynowRef = tran.PaynowRef;
                detail.PaymentStatus = tran.PaymentStatus;
                detail.Amount = (decimal)tran.Amount;
                detail.TransDate = tran.DateAdded;
                AllDetails.Add(detail);
            }
            ViewBag.Details = AllDetails;
            return View();
        }


        [HttpPost("PaynowTransactions")]
        public async Task<IActionResult> PaynowTransactions(DateTime startdate, DateTime enddate)
        {
            var transactions = _db.Payments.Where(a => a.PollUrl != "transfer" && a.DateAdded.Date >= startdate.Date && a.DateAdded.Date <= enddate).ToList().OrderBy(a => a.DateAdded);
            List<PaynowDetails> AllDetails = new List<PaynowDetails>();
            //var requestedDetails = _db.Payments.Where(a => a.PollUrl != "transfer" && a.DateAdded.Month == DateTime.Now.Month).ToList();
            foreach (var tran in transactions)
            {
                PaynowDetails detail = new PaynowDetails();
                var application = _db.ApplicationInfo.Where(s => s.Id == tran.ApplicationId).FirstOrDefault();
                detail.ApplicationRef = application.RefNum;
                var userDetail = await userManager.FindByIdAsync(application.UserID);
                detail.Payer = userDetail.Name + " " + userDetail.LastName;
                detail.PollUrl = tran.PollUrl;
                detail.PaynowRef = tran.PaynowRef;
                detail.PaymentStatus = tran.PaymentStatus;
                detail.Amount = (decimal)tran.Amount;
                detail.TransDate = tran.DateAdded;
                AllDetails.Add(detail);
            }
            ViewBag.Details = AllDetails;
            return View();
        }

        [Authorize(Roles = "accountant,chief accountant")]
        [HttpGet("Reports")]
        public async Task<IActionResult> Reports(DateTime? startDate, DateTime? endDate, string? service, string? channel, string? status)
        {
            var model = await BuildFinancialReportsViewModelAsync(startDate, endDate, service, channel, status);
            return View(model);
        }

        [Authorize(Roles = "accountant,chief accountant")]
        [HttpGet("ExportReportsCsv")]
        public async Task<FileResult> ExportReportsCsv(DateTime? startDate, DateTime? endDate, string? service, string? channel, string? status)
        {
            var model = await BuildFinancialReportsViewModelAsync(startDate, endDate, service, channel, status);
            var csv = new StringBuilder();

            csv.AppendLine("Transaction Date,Reference,Application Reference,Trading Name,Payer,Service,Channel,Status,Amount,Paynow Reference,System Reference");

            foreach (var transaction in model.Transactions)
            {
                csv.AppendLine(string.Join(",",
                    EscapeCsv(transaction.TransactionDate.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv(transaction.Reference),
                    EscapeCsv(transaction.ApplicationReference),
                    EscapeCsv(transaction.TradingName),
                    EscapeCsv(transaction.Payer),
                    EscapeCsv(transaction.Service),
                    EscapeCsv(transaction.Channel),
                    EscapeCsv(transaction.Status),
                    transaction.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                    EscapeCsv(transaction.PaynowReference),
                    EscapeCsv(transaction.SystemReference)));
            }

            var fileName = $"financial-reports-{model.StartDate:yyyyMMdd}-{model.EndDate:yyyyMMdd}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        private async Task<AccountantFinancialReportsViewModel> BuildFinancialReportsViewModelAsync(DateTime? startDate, DateTime? endDate, string? serviceFilter, string? channelFilter, string? statusFilter)
        {
            var today = DateTime.Today;
            var reportStart = (startDate ?? new DateTime(today.Year, today.Month, 1)).Date;
            var reportEnd = (endDate ?? today).Date;

            if (reportEnd < reportStart)
            {
                (reportStart, reportEnd) = (reportEnd, reportStart);
            }

            var reportEndExclusive = reportEnd.AddDays(1);
            var payments = await _db.Payments
                .AsNoTracking()
                .Where(payment => payment.DateAdded >= reportStart && payment.DateAdded < reportEndExclusive)
                .OrderByDescending(payment => payment.DateAdded)
                .ToListAsync();

            var allRows = await BuildFinancialReportRowsAsync(payments);

            var filteredRows = allRows
                .Where(row => MatchesFilter(row.Service, serviceFilter))
                .Where(row => MatchesFilter(row.Channel, channelFilter))
                .Where(row => MatchesFilter(row.Status, statusFilter))
                .OrderByDescending(row => row.TransactionDate)
                .ToList();

            return new AccountantFinancialReportsViewModel
            {
                StartDate = reportStart,
                EndDate = reportEnd,
                ServiceFilter = serviceFilter,
                ChannelFilter = channelFilter,
                StatusFilter = statusFilter,
                ServiceOptions = allRows.Select(row => row.Service).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList(),
                ChannelOptions = allRows.Select(row => row.Channel).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList(),
                StatusOptions = allRows.Select(row => row.Status).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToList(),
                TotalTransactions = allRows.Count,
                FilteredTransactions = filteredRows.Count,
                TotalAmount = allRows.Sum(row => row.Amount),
                FilteredAmount = filteredRows.Sum(row => row.Amount),
                PaidAmount = filteredRows.Where(row => IsPaidStatus(row.Status)).Sum(row => row.Amount),
                PendingAmount = filteredRows.Where(row => IsPendingStatus(row.Status)).Sum(row => row.Amount),
                FailedAmount = filteredRows.Where(row => IsFailedStatus(row.Status)).Sum(row => row.Amount),
                ServiceBreakdown = filteredRows
                    .GroupBy(row => row.Service)
                    .Select(group => new AccountantFinancialSummaryItemViewModel
                    {
                        Name = group.Key,
                        Count = group.Count(),
                        Amount = group.Sum(row => row.Amount)
                    })
                    .OrderByDescending(item => item.Amount)
                    .ThenByDescending(item => item.Count)
                    .ThenBy(item => item.Name)
                    .ToList(),
                ChannelBreakdown = filteredRows
                    .GroupBy(row => row.Channel)
                    .Select(group => new AccountantFinancialSummaryItemViewModel
                    {
                        Name = group.Key,
                        Count = group.Count(),
                        Amount = group.Sum(row => row.Amount)
                    })
                    .OrderByDescending(item => item.Amount)
                    .ThenByDescending(item => item.Count)
                    .ThenBy(item => item.Name)
                    .ToList(),
                StatusBreakdown = filteredRows
                    .GroupBy(row => row.Status)
                    .Select(group => new AccountantFinancialSummaryItemViewModel
                    {
                        Name = group.Key,
                        Count = group.Count(),
                        Amount = group.Sum(row => row.Amount)
                    })
                    .OrderByDescending(item => item.Amount)
                    .ThenByDescending(item => item.Count)
                    .ThenBy(item => item.Name)
                    .ToList(),
                Transactions = filteredRows
            };
        }

        private async Task<List<AccountantFinancialReportRowViewModel>> BuildFinancialReportRowsAsync(List<Payments> payments)
        {
            if (payments.Count == 0)
            {
                return new List<AccountantFinancialReportRowViewModel>();
            }

            var transactionIds = payments
                .Select(payment => payment.ApplicationId)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var renewals = transactionIds.Count == 0
                ? new List<Renewals>()
                : await _db.Renewals
                    .AsNoTracking()
                    .Where(renewal => renewal.ApplicationId != null && transactionIds.Contains(renewal.ApplicationId))
                    .OrderByDescending(renewal => renewal.DateApplied)
                    .ToListAsync();

            var extendedHours = transactionIds.Count == 0
                ? new List<ExtendedHours>()
                : await _db.ExtendedHours
                    .AsNoTracking()
                    .Where(item => item.Id != null && transactionIds.Contains(item.Id))
                    .ToListAsync();

            var temporaryRetails = transactionIds.Count == 0
                ? new List<TemporaryRetails>()
                : await _db.TemporaryRetails
                    .AsNoTracking()
                    .Where(item => item.Id != null && transactionIds.Contains(item.Id))
                    .ToListAsync();

            var changeManagers = transactionIds.Count == 0
                ? new List<ChangeManaager>()
                : await _db.ChangeManaager
                    .AsNoTracking()
                    .Where(item => item.Id != null && transactionIds.Contains(item.Id))
                    .ToListAsync();

            var extraCounters = transactionIds.Count == 0
                ? new List<ExtraCounter>()
                : await _db.ExtraCounter
                    .AsNoTracking()
                    .Where(item => item.Id != null && transactionIds.Contains(item.Id))
                    .ToListAsync();

            var temporaryTransfers = transactionIds.Count == 0
                ? new List<ApplicationInfo>()
                : await _db.ApplicationInfo
                    .AsNoTracking()
                    .Where(item =>
                        item.Id != null
                        && transactionIds.Contains(item.Id)
                        && item.ExaminationStatus == TemporaryTransferHelper.ServiceName)
                    .ToListAsync();

            var inspections = transactionIds.Count == 0
                ? new List<Inspection>()
                : await _db.Inspection
                    .AsNoTracking()
                    .Where(item =>
                        (item.Id != null && transactionIds.Contains(item.Id))
                        || (item.ApplicationId != null && transactionIds.Contains(item.ApplicationId)))
                    .ToListAsync();

            var downloads = transactionIds.Count == 0
                ? new List<Downloads>()
                : await _db.Downloads
                    .AsNoTracking()
                    .Where(item => item.Id != null && transactionIds.Contains(item.Id))
                    .ToListAsync();

            var rootApplicationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var transactionId in transactionIds)
            {
                rootApplicationIds.Add(transactionId);
            }

            foreach (var item in extendedHours.Where(item => !string.IsNullOrWhiteSpace(item.ApplicationId)))
            {
                rootApplicationIds.Add(item.ApplicationId);
            }

            foreach (var item in temporaryRetails.Where(item => !string.IsNullOrWhiteSpace(item.ApplicationId)))
            {
                rootApplicationIds.Add(item.ApplicationId);
            }

            foreach (var item in changeManagers.Where(item => !string.IsNullOrWhiteSpace(item.ApplicationId)))
            {
                rootApplicationIds.Add(item.ApplicationId);
            }

            foreach (var item in extraCounters.Where(item => !string.IsNullOrWhiteSpace(item.ApplicationId)))
            {
                rootApplicationIds.Add(item.ApplicationId);
            }

            foreach (var item in temporaryTransfers.Where(item => !string.IsNullOrWhiteSpace(item.CompanyNumber)))
            {
                rootApplicationIds.Add(item.CompanyNumber);
            }

            foreach (var item in inspections.Where(item => !string.IsNullOrWhiteSpace(item.ApplicationId)))
            {
                rootApplicationIds.Add(item.ApplicationId);
            }

            var applications = rootApplicationIds.Count == 0
                ? new List<ApplicationInfo>()
                : await _db.ApplicationInfo
                    .AsNoTracking()
                    .Where(application => application.Id != null && rootApplicationIds.Contains(application.Id))
                    .ToListAsync();

            var downloadLlbNumbers = downloads
                .Select(download => download.LLBNUM)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (downloadLlbNumbers.Count > 0)
            {
                var downloadApplications = await _db.ApplicationInfo
                    .AsNoTracking()
                    .Where(application => application.LLBNum != null && downloadLlbNumbers.Contains(application.LLBNum))
                    .ToListAsync();

                applications = applications
                    .Concat(downloadApplications)
                    .GroupBy(application => application.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList();
            }

            var applicationIds = applications
                .Select(application => application.Id)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var outlets = applicationIds.Count == 0
                ? new List<OutletInfo>()
                : await _db.OutletInfo
                    .AsNoTracking()
                    .Where(outlet => outlet.ApplicationId != null && applicationIds.Contains(outlet.ApplicationId))
                    .ToListAsync();

            var userIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var payment in payments.Where(payment => !string.IsNullOrWhiteSpace(payment.UserId)))
            {
                userIds.Add(payment.UserId);
            }

            foreach (var application in applications.Where(application => !string.IsNullOrWhiteSpace(application.UserID)))
            {
                userIds.Add(application.UserID);
            }

            foreach (var download in downloads.Where(download => !string.IsNullOrWhiteSpace(download.UserId)))
            {
                userIds.Add(download.UserId);
            }

            var users = userIds.Count == 0
                ? new List<ApplicationUser>()
                : await userManager.Users
                    .Where(user => userIds.Contains(user.Id))
                    .ToListAsync();

            var applicationsById = applications
                .Where(application => !string.IsNullOrWhiteSpace(application.Id))
                .GroupBy(application => application.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key!, group => group.OrderByDescending(item => item.DateUpdated).First(), StringComparer.OrdinalIgnoreCase);

            var applicationsByLlbNumber = applications
                .Where(application => !string.IsNullOrWhiteSpace(application.LLBNum))
                .GroupBy(application => application.LLBNum, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key!,
                    group => group
                        .OrderBy(item => TemporaryTransferHelper.IsTemporaryTransferApplication(item))
                        .ThenByDescending(item => item.DateUpdated)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);

            var outletsByApplicationId = outlets
                .Where(outlet => !string.IsNullOrWhiteSpace(outlet.ApplicationId))
                .GroupBy(outlet => outlet.ApplicationId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key!,
                    group => group
                        .OrderByDescending(item => item.Status != null && item.Status.ToLower() == "active")
                        .ThenByDescending(item => item.DateUpdated)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);

            var usersById = users
                .Where(user => !string.IsNullOrWhiteSpace(user.Id))
                .GroupBy(user => user.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key!, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var renewalsByApplicationId = renewals
                .Where(renewal => !string.IsNullOrWhiteSpace(renewal.ApplicationId))
                .GroupBy(renewal => renewal.ApplicationId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key!,
                    group => group.OrderByDescending(item => item.DateApplied).ToList(),
                    StringComparer.OrdinalIgnoreCase);

            var extendedHoursById = extendedHours
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key!, group => group.OrderByDescending(entry => entry.DateAdded).First(), StringComparer.OrdinalIgnoreCase);

            var temporaryRetailsById = temporaryRetails
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key!, group => group.OrderByDescending(entry => entry.DateAdded).First(), StringComparer.OrdinalIgnoreCase);

            var changeManagersById = changeManagers
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key!, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var extraCountersById = extraCounters
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key!, group => group.OrderByDescending(entry => entry.DateAdded).First(), StringComparer.OrdinalIgnoreCase);

            var temporaryTransfersById = temporaryTransfers
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key!, group => group.OrderByDescending(entry => entry.DateUpdated).First(), StringComparer.OrdinalIgnoreCase);

            var inspectionsById = inspections
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key!, group => group.OrderByDescending(entry => entry.DateApplied).First(), StringComparer.OrdinalIgnoreCase);

            var downloadsById = downloads
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key!, group => group.OrderByDescending(entry => entry.DateUpdated).First(), StringComparer.OrdinalIgnoreCase);

            var rows = new List<AccountantFinancialReportRowViewModel>();

            foreach (var payment in payments)
            {
                var normalizedService = NormalizeFinancialService(payment.Service);
                ApplicationInfo application = null;
                OutletInfo outlet = null;
                Downloads duplicateDownload = null;
                string itemReference = string.Empty;

                if (string.Equals(normalizedService, "Renewal", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(payment.ApplicationId) && applicationsById.TryGetValue(payment.ApplicationId, out var renewalApplication))
                    {
                        application = renewalApplication;
                    }

                    var renewal = GetMatchingRenewal(renewalsByApplicationId, payment);
                    itemReference = FirstNonEmpty(renewal?.Reference, application?.RefNum, application?.LLBNum, payment.ApplicationId, payment.Id);
                }
                else if (string.Equals(normalizedService, "Extended Hours", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(payment.ApplicationId) && extendedHoursById.TryGetValue(payment.ApplicationId, out var extendedHoursItem))
                    {
                        itemReference = FirstNonEmpty(extendedHoursItem.Reference, payment.ApplicationId, payment.Id);

                        if (!string.IsNullOrWhiteSpace(extendedHoursItem.ApplicationId) && applicationsById.TryGetValue(extendedHoursItem.ApplicationId, out var extendedHoursApplication))
                        {
                            application = extendedHoursApplication;
                        }
                    }
                }
                else if (string.Equals(normalizedService, "Temporary Retail", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(payment.ApplicationId) && temporaryRetailsById.TryGetValue(payment.ApplicationId, out var temporaryRetailItem))
                    {
                        itemReference = FirstNonEmpty(temporaryRetailItem.Reference, payment.ApplicationId, payment.Id);

                        if (!string.IsNullOrWhiteSpace(temporaryRetailItem.ApplicationId) && applicationsById.TryGetValue(temporaryRetailItem.ApplicationId, out var temporaryRetailApplication))
                        {
                            application = temporaryRetailApplication;
                        }
                    }
                }
                else if (string.Equals(normalizedService, "Manager Change", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(payment.ApplicationId) && changeManagersById.TryGetValue(payment.ApplicationId, out var managerChange))
                    {
                        itemReference = FirstNonEmpty(managerChange.Reference, payment.ApplicationId, payment.Id);

                        if (!string.IsNullOrWhiteSpace(managerChange.ApplicationId) && applicationsById.TryGetValue(managerChange.ApplicationId, out var managerChangeApplication))
                        {
                            application = managerChangeApplication;
                        }
                    }
                }
                else if (string.Equals(normalizedService, "Temporary Transfer", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(payment.ApplicationId) && temporaryTransfersById.TryGetValue(payment.ApplicationId, out var temporaryTransfer))
                    {
                        itemReference = FirstNonEmpty(temporaryTransfer.RefNum, payment.ApplicationId, payment.Id);

                        if (!string.IsNullOrWhiteSpace(temporaryTransfer.CompanyNumber) && applicationsById.TryGetValue(temporaryTransfer.CompanyNumber, out var temporaryTransferApplication))
                        {
                            application = temporaryTransferApplication;
                        }
                    }
                }
                else if (string.Equals(normalizedService, "Extra Counter", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(normalizedService, "Permission to Alter", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(payment.ApplicationId) && extraCountersById.TryGetValue(payment.ApplicationId, out var extraCounter))
                    {
                        itemReference = FirstNonEmpty(extraCounter.Reference, payment.ApplicationId, payment.Id);

                        if (!string.IsNullOrWhiteSpace(extraCounter.ApplicationId) && applicationsById.TryGetValue(extraCounter.ApplicationId, out var extraCounterApplication))
                        {
                            application = extraCounterApplication;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(payment.ApplicationId) && extendedHoursById.TryGetValue(payment.ApplicationId, out var legacyExtraCounter))
                    {
                        itemReference = FirstNonEmpty(legacyExtraCounter.Reference, payment.ApplicationId, payment.Id);

                        if (!string.IsNullOrWhiteSpace(legacyExtraCounter.ApplicationId) && applicationsById.TryGetValue(legacyExtraCounter.ApplicationId, out var legacyExtraCounterApplication))
                        {
                            application = legacyExtraCounterApplication;
                        }
                    }
                }
                else if (string.Equals(normalizedService, "License Duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(payment.ApplicationId) && downloadsById.TryGetValue(payment.ApplicationId, out var download))
                    {
                        duplicateDownload = download;
                        itemReference = FirstNonEmpty(download.LLBNUM, payment.ApplicationId, payment.Id);

                        if (!string.IsNullOrWhiteSpace(download.LLBNUM) && applicationsByLlbNumber.TryGetValue(download.LLBNUM, out var duplicateApplication))
                        {
                            application = duplicateApplication;
                        }
                    }
                }
                else if (string.Equals(normalizedService, "Inspection", StringComparison.OrdinalIgnoreCase) || normalizedService.EndsWith("Inspection", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(payment.ApplicationId) && inspectionsById.TryGetValue(payment.ApplicationId, out var inspection))
                    {
                        itemReference = FirstNonEmpty(inspection.Reference, payment.ApplicationId, payment.Id);

                        var inspectionApplicationId = FirstNonEmpty(inspection.ApplicationId, inspection.Application);
                        if (!string.IsNullOrWhiteSpace(inspectionApplicationId) && applicationsById.TryGetValue(inspectionApplicationId, out var inspectionApplication))
                        {
                            application = inspectionApplication;
                        }
                    }
                }

                if (application == null && !string.IsNullOrWhiteSpace(payment.ApplicationId) && applicationsById.TryGetValue(payment.ApplicationId, out var directApplication))
                {
                    application = directApplication;
                }

                if (application != null && !string.IsNullOrWhiteSpace(application.Id) && outletsByApplicationId.TryGetValue(application.Id, out var applicationOutlet))
                {
                    outlet = applicationOutlet;
                }

                var payerUserId = FirstNonEmpty(payment.UserId, application?.UserID, duplicateDownload?.UserId);
                usersById.TryGetValue(payerUserId, out var payer);

                rows.Add(new AccountantFinancialReportRowViewModel
                {
                    PaymentId = payment.Id ?? string.Empty,
                    Reference = FirstNonEmpty(itemReference, application?.RefNum, application?.LLBNum, payment.ApplicationId, payment.Id),
                    ApplicationReference = FirstNonEmpty(application?.RefNum, application?.LLBNum, payment.ApplicationId, "N/A"),
                    TradingName = FirstNonEmpty(outlet?.TradingName, application?.BusinessName, "N/A"),
                    Payer = BuildPayerName(payer),
                    Service = normalizedService,
                    Channel = NormalizePaymentChannel(payment),
                    Status = NormalizeFinancialStatus(payment),
                    PaynowReference = FirstNonEmpty(payment.PaynowRef, "N/A"),
                    SystemReference = FirstNonEmpty(payment.SystemRef, payment.PollUrl, "N/A"),
                    Amount = payment.Amount ?? 0m,
                    TransactionDate = payment.DateAdded
                });
            }

            return rows;
        }

        private async Task<AccountantExchangeRateViewModel> BuildUsdZwgExchangeRateViewModelAsync(CancellationToken cancellationToken)
        {
            var model = new AccountantExchangeRateViewModel
            {
                RetrievedAt = DateTime.Now
            };

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LLB Exchange Rate Scanner/1.0");

            var scanErrors = new List<string>();
            var homepageError = await TryPopulateExchangeRateFromHtmlSourceAsync(
                client,
                model,
                model.SourceUrl,
                "Reserve Bank of Zimbabwe",
                cancellationToken);

            if (!model.HasRate && !string.IsNullOrWhiteSpace(homepageError))
            {
                scanErrors.Add(homepageError);
            }

            if (!model.HasRate)
            {
                var archivePageUrl = "https://www.rbz.co.zw/index.php/research/markets/exchange-rates";
                var archiveHtmlError = await TryPopulateExchangeRateFromHtmlSourceAsync(
                    client,
                    model,
                    archivePageUrl,
                    "Reserve Bank of Zimbabwe Exchange Rates",
                    cancellationToken);

                if (!model.HasRate && !string.IsNullOrWhiteSpace(archiveHtmlError))
                {
                    scanErrors.Add(archiveHtmlError);
                }
            }

            if (!model.HasRate)
            {
                var pdfFallbackError = await TryPopulateExchangeRateFromPdfArchiveAsync(client, model, cancellationToken);
                if (!model.HasRate && !string.IsNullOrWhiteSpace(pdfFallbackError))
                {
                    scanErrors.Add(pdfFallbackError);
                }
            }

            if (!model.HasRate)
            {
                model.ErrorMessage = scanErrors.LastOrDefault()
                    ?? "USD/ZWG interbank data could not be found on the RBZ website.";
            }

            var storedRates = await _db.ExchangeRate
                .AsNoTracking()
                .OrderByDescending(rate => rate.DateAdded)
                .ThenByDescending(rate => rate.DateUpdated)
                .Take(30)
                .ToListAsync(cancellationToken);

            var updaterUserNames = await ResolveUserNamesAsync(
                storedRates.Select(rate => rate.UserId),
                cancellationToken);

            model.StoredRates = storedRates
                .Select(rate => new AccountantExchangeRateHistoryItemViewModel
                {
                    Id = rate.Id ?? string.Empty,
                    Date = rate.DateAdded,
                    UpdaterUserName = !string.IsNullOrWhiteSpace(rate.UserId)
                        && updaterUserNames.TryGetValue(rate.UserId, out var updaterUserName)
                            ? updaterUserName
                            : "N/A",
                    ExchangeRate = rate.ZWGrate.HasValue ? Convert.ToDecimal(rate.ZWGrate.Value) : null,
                    UpdatedAt = rate.DateUpdated
                })
                .ToList();

            var todayStoredRate = model.StoredRates
                .Where(rate => rate.Date.Date == DateTime.Today)
                .OrderByDescending(rate => rate.UpdatedAt)
                .ThenByDescending(rate => rate.Date)
                .FirstOrDefault();

            model.TodayStoredRate = todayStoredRate;
            return model;
        }

        private static Renewals GetMatchingRenewal(IReadOnlyDictionary<string, List<Renewals>> renewalsByApplicationId, Payments payment)
        {
            if (string.IsNullOrWhiteSpace(payment.ApplicationId) || !renewalsByApplicationId.TryGetValue(payment.ApplicationId, out var renewals))
            {
                return null;
            }

            return renewals.FirstOrDefault(renewal => renewal.DateApplied <= payment.DateAdded.AddMinutes(5))
                ?? renewals.FirstOrDefault();
        }

        private static bool MatchesFilter(string? value, string? filter)
        {
            return string.IsNullOrWhiteSpace(filter)
                || string.Equals(value, filter, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPaidStatus(string? status)
        {
            return string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPendingStatus(string? status)
        {
            return string.Equals(status, "Not Paid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Awaiting Verification", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Awaiting Payment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Sent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Created", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Initiated", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Unknown", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFailedStatus(string? status)
        {
            return string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeFinancialService(string? service)
        {
            if (string.IsNullOrWhiteSpace(service))
            {
                return "Unspecified";
            }

            return service.Trim().ToLowerInvariant() switch
            {
                "new application" => "New Application",
                "renewal" => "Renewal",
                "extended hours" => "Extended Hours",
                "temporary retails" => "Temporary Retail",
                "temporary retail" => "Temporary Retail",
                "temporary transfer" => "Temporary Transfer",
                "extra counter" => "Permission to Alter",
                "permission to alter" => "Permission to Alter",
                "changemanager" => "Manager Change",
                "inspection" => "Inspection",
                "verification inspection" => "Verification Inspection",
                "renewal inspection" => "Renewal Inspection",
                "license duplicate" => "License Duplicate",
                "download" => "License Duplicate",
                _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(service.Trim().ToLowerInvariant())
            };
        }

        private static string NormalizePaymentChannel(Payments payment)
        {
            if (string.Equals(payment.PollUrl, "transfer", StringComparison.OrdinalIgnoreCase))
            {
                return "Bank Transfer / POP";
            }

            if (string.Equals(payment.PollUrl, "manual", StringComparison.OrdinalIgnoreCase))
            {
                return "Manual";
            }

            if (!string.IsNullOrWhiteSpace(payment.PaynowRef) || !string.IsNullOrWhiteSpace(payment.SystemRef))
            {
                return "Paynow";
            }

            return "Unspecified";
        }

        private static string NormalizeFinancialStatus(Payments payment)
        {
            if (HasAnyStatus(payment, "Paid"))
            {
                return "Paid";
            }

            if (HasAnyStatus(payment, "Approved"))
            {
                return "Approved";
            }

            if (HasAnyStatus(payment, "payment verification", "awaiting verification"))
            {
                return "Awaiting Verification";
            }

            if (HasAnyStatus(payment, "Awaiting Payment"))
            {
                return "Awaiting Payment";
            }

            if (HasAnyStatus(payment, "Rejected"))
            {
                return "Rejected";
            }

            if (HasAnyStatus(payment, "Cancelled"))
            {
                return "Cancelled";
            }

            if (HasAnyStatus(payment, "Expired"))
            {
                return "Expired";
            }

            if (HasAnyStatus(payment, "Failed"))
            {
                return "Failed";
            }

            if (HasAnyStatus(payment, "not paid"))
            {
                return "Not Paid";
            }

            if (!string.IsNullOrWhiteSpace(payment.PaymentStatus))
            {
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(payment.PaymentStatus.Trim().ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(payment.Status))
            {
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(payment.Status.Trim().ToLowerInvariant());
            }

            return "Unknown";
        }

        private static bool HasAnyStatus(Payments payment, params string[] values)
        {
            return values.Any(value =>
                string.Equals(payment.PaymentStatus, value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(payment.Status, value, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildPayerName(ApplicationUser? user)
        {
            if (user == null)
            {
                return "N/A";
            }

            var fullName = $"{user.Name} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName)
                ? FirstNonEmpty(user.Email, user.UserName, "N/A")
                : fullName;
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private async Task<string> ResolveUserNameAsync(string? userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return "N/A";
            }

            var usernamesById = await ResolveUserNamesAsync(new[] { userId }, cancellationToken);
            return usernamesById.TryGetValue(userId, out var userName)
                ? userName
                : "N/A";
        }

        private async Task<Dictionary<string, string>> ResolveUserNamesAsync(
            IEnumerable<string?> userIds,
            CancellationToken cancellationToken)
        {
            var distinctUserIds = userIds
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Select(userId => userId!)
                .Distinct()
                .ToList();

            if (distinctUserIds.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            return await userManager.Users
                .AsNoTracking()
                .Where(user => distinctUserIds.Contains(user.Id))
                .ToDictionaryAsync(
                    user => user.Id,
                    user => FirstNonEmpty(user.UserName, user.Email, "N/A"),
                    cancellationToken);
        }

        private async Task<string?> TryPopulateExchangeRateFromHtmlSourceAsync(
            HttpClient client,
            AccountantExchangeRateViewModel model,
            string sourceUrl,
            string sourceName,
            CancellationToken cancellationToken)
        {
            try
            {
                var html = await client.GetStringAsync(sourceUrl, cancellationToken);
                if (string.IsNullOrWhiteSpace(html))
                {
                    return $"The RBZ page at {sourceUrl} returned an empty response.";
                }

                var lines = ExtractReadableLinesFromHtml(html);
                var normalizedText = NormalizeReadableText(string.Join(' ', lines));
                if (TryBuildUsdZwgHomepageSnapshotFromHtml(html, sourceUrl, sourceName, out var snapshot)
                    || TryBuildUsdZwgHomepageSnapshot(normalizedText, sourceUrl, sourceName, out snapshot)
                    || TryBuildUsdZwgSnapshot(lines, sourceUrl, sourceName, null, out snapshot))
                {
                    ApplyExchangeRateSnapshot(model, snapshot);
                }
            }
            catch (Exception)
            {
                return $"The RBZ page at {sourceUrl} could not be scanned right now.";
            }

            return null;
        }

        private async Task<string?> TryPopulateExchangeRateFromPdfArchiveAsync(
            HttpClient client,
            AccountantExchangeRateViewModel model,
            CancellationToken cancellationToken)
        {
            const string archiveUrl = "https://www.rbz.co.zw/index.php/research/markets/exchange-rates";

            try
            {
                var archiveHtml = await client.GetStringAsync(archiveUrl, cancellationToken);
                if (string.IsNullOrWhiteSpace(archiveHtml))
                {
                    return "The RBZ exchange-rate archive returned an empty response.";
                }

                var monthTargets = new[]
                {
                    DateTime.Today,
                    DateTime.Today.AddMonths(-1)
                };

                foreach (var monthTarget in monthTargets)
                {
                    var monthPageUrl = TryFindMonthArchiveUrl(archiveHtml, archiveUrl, monthTarget);
                    if (string.IsNullOrWhiteSpace(monthPageUrl))
                    {
                        continue;
                    }

                    var monthHtml = await client.GetStringAsync(monthPageUrl, cancellationToken);
                    if (string.IsNullOrWhiteSpace(monthHtml))
                    {
                        continue;
                    }

                    var pdfUrl = TryFindBestExchangeRatePdfUrl(monthHtml, monthPageUrl, DateTime.Today);
                    if (string.IsNullOrWhiteSpace(pdfUrl))
                    {
                        continue;
                    }

                    DateTime? fallbackDate = TryParseDateFromPdfUrl(pdfUrl, out var pdfDate)
                        ? pdfDate
                        : null;

                    var pdfText = await TryExtractPdfTextAsync(client, pdfUrl, cancellationToken);
                    if (string.IsNullOrWhiteSpace(pdfText))
                    {
                        continue;
                    }

                    var lines = ExtractReadableLinesFromText(pdfText);
                    if (TryBuildUsdZwgSnapshot(lines, pdfUrl, "Reserve Bank of Zimbabwe PDF Bulletin", fallbackDate, out var snapshot))
                    {
                        ApplyExchangeRateSnapshot(model, snapshot);
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return "The RBZ exchange-rate PDF fallback could not be scanned right now.";
            }

            return "The RBZ website did not expose a readable USD/ZWG exchange-rate archive for the current period.";
        }

        private static void ApplyExchangeRateSnapshot(AccountantExchangeRateViewModel model, RbzUsdZwgSnapshot snapshot)
        {
            model.ExchangeDate = snapshot.ExchangeDate;
            model.Bid = snapshot.Bid;
            model.Ask = snapshot.Ask;
            model.Average = snapshot.Average;
            model.SourceUrl = snapshot.SourceUrl;
            model.SourceName = snapshot.SourceName;
            model.ErrorMessage = null;
        }

        private static bool TryBuildUsdZwgSnapshot(
            IReadOnlyList<string> lines,
            string sourceUrl,
            string sourceName,
            DateTime? fallbackDate,
            out RbzUsdZwgSnapshot snapshot)
        {
            snapshot = null;

            if (lines.Count == 0)
            {
                return false;
            }

            var exchangeDate = TryExtractExchangeDate(lines) ?? fallbackDate;
            if (!TryExtractUsdZwgValues(lines, out var bid, out var ask, out var average))
            {
                return false;
            }

            if (!exchangeDate.HasValue || !bid.HasValue || !ask.HasValue || !average.HasValue)
            {
                return false;
            }

            snapshot = new RbzUsdZwgSnapshot
            {
                ExchangeDate = exchangeDate,
                Bid = bid.Value,
                Ask = ask.Value,
                Average = average.Value,
                SourceUrl = sourceUrl,
                SourceName = sourceName
            };

            return true;
        }

        private static bool TryBuildUsdZwgHomepageSnapshot(
            string normalizedText,
            string sourceUrl,
            string sourceName,
            out RbzUsdZwgSnapshot snapshot)
        {
            snapshot = null;

            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                return false;
            }

            var homepageBlockMatch = Regex.Match(
                normalizedText,
                @"EXCHANGE\s+RATES\s+(?<date>\d{1,2}[-/]\d{1,2}[-/]\d{4}).{0,300}?INTERBANK\s+RATES.{0,200}?CURRENCY\s+BID\s+ASK\s+AVG.{0,200}?USD\s*/\s*ZWG\s+(?<bid>[\d,]+(?:\.\d+)?)\s+(?<ask>[\d,]+(?:\.\d+)?)\s+(?<avg>[\d,]+(?:\.\d+)?)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!homepageBlockMatch.Success)
            {
                return false;
            }

            if (!DateTime.TryParseExact(
                    homepageBlockMatch.Groups["date"].Value,
                    new[] { "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var exchangeDate))
            {
                return false;
            }

            var bid = TryParseExchangeRateValue(homepageBlockMatch.Groups["bid"].Value);
            var ask = TryParseExchangeRateValue(homepageBlockMatch.Groups["ask"].Value);
            var average = TryParseExchangeRateValue(homepageBlockMatch.Groups["avg"].Value);

            if (!bid.HasValue || !ask.HasValue || !average.HasValue)
            {
                return false;
            }

            snapshot = new RbzUsdZwgSnapshot
            {
                ExchangeDate = exchangeDate,
                Bid = bid.Value,
                Ask = ask.Value,
                Average = average.Value,
                SourceUrl = sourceUrl,
                SourceName = sourceName
            };

            return true;
        }

        private static bool TryBuildUsdZwgHomepageSnapshotFromHtml(
            string html,
            string sourceUrl,
            string sourceName,
            out RbzUsdZwgSnapshot snapshot)
        {
            snapshot = null;

            if (string.IsNullOrWhiteSpace(html))
            {
                return false;
            }

            var exchangePanelMatch = Regex.Match(
                html,
                @"id\s*=\s*[""']baTab0[""'](?<panel>[\s\S]*?)(?:id\s*=\s*[""']baTab1[""']|</div>\s*</div>\s*</div>)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var exchangePanelHtml = exchangePanelMatch.Success
                ? exchangePanelMatch.Groups["panel"].Value
                : html;

            if (!Regex.IsMatch(exchangePanelHtml, @"INTERBANK\s+RATES", RegexOptions.IgnoreCase))
            {
                return false;
            }

            var dateMatch = Regex.Match(
                exchangePanelHtml,
                @"EXCHANGE\s+RATES\s*(?<date>\d{1,2}[-/]\d{1,2}[-/]\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!dateMatch.Success
                || !DateTime.TryParseExact(
                    dateMatch.Groups["date"].Value,
                    new[] { "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var exchangeDate))
            {
                return false;
            }

            var usdRowMatch = Regex.Match(
                exchangePanelHtml,
                @"<tr[^>]*>\s*<td[^>]*>[\s\S]*?<strong>\s*USD\s*/\s*ZWG\s*</strong>[\s\S]*?</td>\s*<td[^>]*>(?<bid>[\s\S]*?)</td>\s*<td[^>]*>(?<ask>[\s\S]*?)</td>\s*<td[^>]*>(?<avg>[\s\S]*?)</td>\s*</tr>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!usdRowMatch.Success)
            {
                return false;
            }

            var bid = ExtractFirstDecimalValueFromHtml(usdRowMatch.Groups["bid"].Value);
            var ask = ExtractFirstDecimalValueFromHtml(usdRowMatch.Groups["ask"].Value);
            var average = ExtractFirstDecimalValueFromHtml(usdRowMatch.Groups["avg"].Value);

            if (!bid.HasValue || !ask.HasValue || !average.HasValue)
            {
                return false;
            }

            snapshot = new RbzUsdZwgSnapshot
            {
                ExchangeDate = exchangeDate,
                Bid = bid.Value,
                Ask = ask.Value,
                Average = average.Value,
                SourceUrl = sourceUrl,
                SourceName = sourceName
            };

            return true;
        }

        private static DateTime? TryExtractExchangeDate(IEnumerable<string> lines)
        {
            var exactDateFormats = new[] { "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy" };
            var longDateFormats = new[] { "MMMM d, yyyy", "MMMM dd, yyyy" };

            foreach (var line in lines)
            {
                var explicitDateMatch = Regex.Match(line, @"EXCHANGE\s+RATES\s+(?<date>\d{1,2}[-/]\d{1,2}[-/]\d{4})", RegexOptions.IgnoreCase);
                if (explicitDateMatch.Success
                    && DateTime.TryParseExact(
                        explicitDateMatch.Groups["date"].Value,
                        exactDateFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var exactDate))
                {
                    return exactDate;
                }

                var longDateMatch = Regex.Match(
                    line,
                    @"(?:(?:Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday),?\s+)?(?<date>[A-Za-z]+\s+\d{1,2},\s+\d{4})",
                    RegexOptions.IgnoreCase);

                if (longDateMatch.Success
                    && DateTime.TryParseExact(
                        longDateMatch.Groups["date"].Value,
                        longDateFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var longDate))
                {
                    return longDate;
                }
            }

            return null;
        }

        private static bool TryExtractUsdZwgValues(
            IReadOnlyList<string> lines,
            out decimal? bid,
            out decimal? ask,
            out decimal? average)
        {
            bid = null;
            ask = null;
            average = null;

            foreach (var rawLine in lines)
            {
                var line = NormalizeReadableText(rawLine);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var pairIndex = line.IndexOf("USD/ZWG", StringComparison.OrdinalIgnoreCase);
                if (pairIndex >= 0)
                {
                    var pairSegment = line[(pairIndex + "USD/ZWG".Length)..];
                    var values = ExtractDecimalValues(pairSegment);
                    if (values.Count >= 3)
                    {
                        bid = values[0];
                        ask = values[1];
                        average = values[2];
                        return true;
                    }
                }

                if (Regex.IsMatch(line, @"^\s*USD\b", RegexOptions.IgnoreCase))
                {
                    var values = ExtractDecimalValues(line);
                    if (values.Count >= 3)
                    {
                        var selectedValues = values.Count >= 6
                            ? values.Skip(values.Count - 3).Take(3).ToList()
                            : values;

                        bid = selectedValues[0];
                        ask = selectedValues[1];
                        average = selectedValues[2];
                        return true;
                    }
                }
            }

            var combinedText = NormalizeReadableText(string.Join(' ', lines));
            if (string.IsNullOrWhiteSpace(combinedText))
            {
                return false;
            }

            var homepagePattern = Regex.Match(
                combinedText,
                @"USD\s*/\s*ZWG\s+(?<bid>[\d,]+(?:\.\d+)?)\s+(?<ask>[\d,]+(?:\.\d+)?)\s+(?<avg>[\d,]+(?:\.\d+)?)",
                RegexOptions.IgnoreCase);

            if (homepagePattern.Success)
            {
                bid = TryParseExchangeRateValue(homepagePattern.Groups["bid"].Value);
                ask = TryParseExchangeRateValue(homepagePattern.Groups["ask"].Value);
                average = TryParseExchangeRateValue(homepagePattern.Groups["avg"].Value);
                return bid.HasValue && ask.HasValue && average.HasValue;
            }

            var pdfPattern = Regex.Match(
                combinedText,
                @"\bUSD\b(?:\s+[\d,]+(?:\.\d+)?){3}\s+(?<bid>[\d,]+(?:\.\d+)?)\s+(?<ask>[\d,]+(?:\.\d+)?)\s+(?<avg>[\d,]+(?:\.\d+)?)",
                RegexOptions.IgnoreCase);

            if (pdfPattern.Success)
            {
                bid = TryParseExchangeRateValue(pdfPattern.Groups["bid"].Value);
                ask = TryParseExchangeRateValue(pdfPattern.Groups["ask"].Value);
                average = TryParseExchangeRateValue(pdfPattern.Groups["avg"].Value);
                return bid.HasValue && ask.HasValue && average.HasValue;
            }

            return false;
        }

        private static List<string> ExtractReadableLinesFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return new List<string>();
            }

            var withoutScripts = Regex.Replace(html, @"<script[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
            var withoutStyles = Regex.Replace(withoutScripts, @"<style[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
            var withLineBreaks = Regex.Replace(
                withoutStyles,
                @"<(br|/p|/div|/li|/tr|/td|/th|/section|/article|/h[1-6])\b[^>]*>",
                "\n",
                RegexOptions.IgnoreCase);

            var decoded = WebUtility.HtmlDecode(withLineBreaks);
            var withoutTags = Regex.Replace(decoded, @"<[^>]+>", " ");

            return ExtractReadableLinesFromText(withoutTags);
        }

        private static List<string> ExtractReadableLinesFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            var normalized = text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Replace('\u00A0', ' ')
                .Replace('\u200B', ' ')
                .Replace('\u200C', ' ')
                .Replace('\u200D', ' ')
                .Replace('\ufeff', ' ');

            return normalized
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeReadableText)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
        }

        private static string NormalizeReadableText(string text)
        {
            return Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim();
        }

        private static List<decimal> ExtractDecimalValues(string text)
        {
            return Regex.Matches(text, @"(?<!\d)(?:\d{1,3}(?:,\d{3})+|\d+)(?:\.\d+)?(?!\d)")
                .Select(match => TryParseExchangeRateValue(match.Value))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();
        }

        private static decimal? ExtractFirstDecimalValueFromHtml(string html)
        {
            var decoded = WebUtility.HtmlDecode(html ?? string.Empty).Replace('\u00A0', ' ');
            var withoutTags = Regex.Replace(decoded, @"<[^>]+>", " ");
            var normalized = NormalizeReadableText(withoutTags);
            var values = ExtractDecimalValues(normalized);
            return values.Count == 0 ? null : values[0];
        }

        private static string? TryFindMonthArchiveUrl(string html, string baseUrl, DateTime targetMonth)
        {
            var targetLabel = targetMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
            var anchorMatches = Regex.Matches(
                html,
                @"<a[^>]+href\s*=\s*[""'](?<url>[^""']+)[""'][^>]*>\s*(?<text>[^<]+?)\s*</a>",
                RegexOptions.IgnoreCase);

            foreach (Match match in anchorMatches)
            {
                var label = NormalizeReadableText(WebUtility.HtmlDecode(match.Groups["text"].Value));
                if (!string.Equals(label, targetLabel, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return ResolveAbsoluteUrl(baseUrl, match.Groups["url"].Value);
            }

            return null;
        }

        private static string? TryFindBestExchangeRatePdfUrl(string html, string baseUrl, DateTime targetDate)
        {
            var matches = Regex.Matches(
                html,
                @"href\s*=\s*[""'](?<url>[^""']+\.pdf(?:\?[^""']*)?)[""']",
                RegexOptions.IgnoreCase);

            var candidates = matches
                .Select(match => ResolveAbsoluteUrl(baseUrl, match.Groups["url"].Value))
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(url => new RbzPdfCandidate
                {
                    Url = url!,
                    Date = TryParseDateFromPdfUrl(url!, out var pdfDate) ? pdfDate : null
                })
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates
                .Where(candidate => candidate.Date.HasValue && candidate.Date.Value.Date <= targetDate.Date)
                .OrderByDescending(candidate => candidate.Date)
                .Select(candidate => candidate.Url)
                .FirstOrDefault()
                ?? candidates
                    .OrderByDescending(candidate => candidate.Date)
                    .Select(candidate => candidate.Url)
                    .FirstOrDefault();
        }

        private static bool TryParseDateFromPdfUrl(string pdfUrl, out DateTime parsedDate)
        {
            parsedDate = default;

            var match = Regex.Match(
                pdfUrl,
                @"RATES_(?<day>\d{1,2})_(?<month>[A-Z]+)_(?<year>\d{4})\.pdf",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return false;
            }

            var monthName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(match.Groups["month"].Value.ToLowerInvariant());
            var rawDate = $"{match.Groups["day"].Value} {monthName} {match.Groups["year"].Value}";

            return DateTime.TryParseExact(
                rawDate,
                "d MMMM yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDate);
        }

        private async Task<string?> TryExtractPdfTextAsync(HttpClient client, string pdfUrl, CancellationToken cancellationToken)
        {
            var pdfBytes = await client.GetByteArrayAsync(pdfUrl, cancellationToken);
            if (pdfBytes.Length == 0)
            {
                return null;
            }

            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"rbz-rate-{Guid.NewGuid():N}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempPath, pdfBytes, cancellationToken);

            try
            {
                var pdfDocument = PdfDocument.FromFile(tempPath);
                return pdfDocument.ExtractAllText();
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(tempPath);
                }
                catch
                {
                }
            }
        }

        private static string ResolveAbsoluteUrl(string baseUrl, string candidateUrl)
        {
            if (string.IsNullOrWhiteSpace(candidateUrl))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(candidateUrl, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            return new Uri(new Uri(baseUrl), candidateUrl).ToString();
        }

        private static decimal? TryParseExchangeRateValue(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            var normalizedValue = rawValue.Replace(",", string.Empty).Trim();
            return decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue)
                ? parsedValue
                : null;
        }

        private static string EscapeCsv(string? value)
        {
            var sanitized = value ?? string.Empty;
            return $"\"{sanitized.Replace("\"", "\"\"")}\"";
        }

        private sealed class RbzUsdZwgSnapshot
        {
            public DateTime? ExchangeDate { get; set; }
            public decimal Bid { get; set; }
            public decimal Ask { get; set; }
            public decimal Average { get; set; }
            public string SourceUrl { get; set; } = string.Empty;
            public string SourceName { get; set; } = string.Empty;
        }

        private sealed class RbzPdfCandidate
        {
            public string Url { get; set; } = string.Empty;
            public DateTime? Date { get; set; }
        }

        [HttpPost("ChangePasswordx")]
        public async Task<IActionResult> ChangePasswordx(ChangePassword model)
        {
            ViewBag.title = "Account / Change Password";

            if (ModelState.IsValid)
            {
                var user = await userManager.GetUserAsync(User);

                if (user == null)
                {
                    return RedirectToAction("NotFound");
                }

                var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View();
                }
                await signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Success", "AProperty");
            }

            return View(model);

            //if ()
            //{
            //    TempData["tmsg"] = "Password Changed";
            //    TempData["type"] = "success";
            //    await _signInManager.SignOutAsync();
            //    return RedirectToAction("ChangePassword", "Acoount");
            //}
            //else
            //{
            //    TempData["tmsg"] = "Password Changed Failed";
            //    TempData["type"] = "error";
            //}
            //return View();


        }

        [AllowAnonymous]
        [HttpGet("ForgotPassword")]
        public IActionResult Forgotpassword()
        {
            ViewBag.title = "Forgot Password";
            return View();
        }

        [AllowAnonymous]
        [HttpPost("ForgotPasswordS")]
        public async Task<IActionResult> ForgotpasswordS(string email)
        {
            //generating new password
            var pwdb = new Password();
            var new_password = pwdb.Next();

            // Calling identity methods or functions for change password
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                TempData["flash"] = "1";
                TempData["error"] = "User is unavailable in system";
                return View();
            }
            else
            {
                string code = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, code, new_password);

                if (result.Succeeded)
                {
                    SmtpClient client = new SmtpClient("smtp.gmail.com", 465);
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential("ftagwirei24@gmail.com", "kwjxjbsrahhtqfwd");
                    // client.Credentials = new NetworkCredential("username", "password");

                    MailMessage mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress("ftagwirei24@gmail.com");
                    mailMessage.To.Add(email);
                    mailMessage.IsBodyHtml = true;
                    mailMessage.Body = ("<!DOCTYPE html> " +
                                        "<html xmlns=\"http://www.w3.org/1999/xhtml\">" +
                                        "<head>" +
                                        "<title>Email</title>" +
                                        "</head>" +
                                        "<body style=\"font-family:'Century Gothic'\">" +
                                        "<p><b>Hi Dear valued Customer</b></p>" +
                                        "<p>Your new password is " + new_password + "</p>" +
                                        "<p>Kindly use the link below to access your account.</p>" +
                                        "<a>https://localhost:7223/Account/Login </a>" +
                                        "<p>as a security measure we recomend a that you change your password after login</p>" +
                                        "<p> Enjoy our services.</p> " +
                                        "<p>Regards</p>" +
                                        "<p>DCIP</p>" +
                                        "</body>" +
                                        "</html>"); //GetFormattedMessageHTML();
                    mailMessage.Subject = "Password successfully changed";
                    client.Send(mailMessage);

                    TempData["error"] = "Password has been changed, please check email..=";
                    TempData["flash"] = "2";
                    return View();
                }
                else
                {
                    TempData["error"] = "Password Changed Failed";
                    TempData["flash"] = "1";
                    return View();
                }
            }


            ViewBag.title = "Forgot Password";
            return RedirectToAction("Register", "Auth");
        }

    }
}
