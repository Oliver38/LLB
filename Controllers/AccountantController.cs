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

        public AccountantController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
        }

        [HttpGet("Dashboard")]
        
        public async Task<IActionResult> DashboardAsync()
        {
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
            return View();
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
                var refnum = _db.ReferenceNumbers.First();
                var newrefnum = refnum.Number + 1;
                refnum.Number = newrefnum;
                _db.Update(refnum); 
                int curentnum = (int)refnum.Number;
                var reference = $"D{curentnum.ToString("D4")}";

                application.RefNum = reference;
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

                // running the task allocation method, to be optimised
                var verifiers = await userManager.GetUsersInRoleAsync("Verifier");

                // Get task counts for each verifier
                var taskCounts = await _db.Tasks
                    .Where(t => verifiers.Select(v => v.Id).Contains(t.VerifierId) &&
                    t.DateAdded.Month == DateTime.Now.Month)
                    .GroupBy(t => t.VerifierId)
                    .Select(g => new { VerifierId = g.Key, TaskCount = g.Count() })
                    .ToDictionaryAsync(x => x.VerifierId, x => x.TaskCount);

                // Find the verifier with the least tasks
                IdentityUser selectedUser = null;
                int minTaskCount = int.MaxValue;

                foreach (var verifier in verifiers)
                {
                    if (verifier.LeaveStatus == "onleave" && verifier.IsActive == false)  { }
                    else
                    {
                        int taskCount = taskCounts.ContainsKey(verifier.Id) ? taskCounts[verifier.Id] : 0;

                        if (taskCount < minTaskCount)
                        {
                            minTaskCount = taskCount;
                            selectedUser = verifier;



                            //var verifierId = await TaskAllocator()
                            Tasks tasks = new Tasks();
                            tasks.Id = Guid.NewGuid().ToString();
                            tasks.ApplicationId = application.Id;
                            //tasks.AssignerId

                            //auto allocation to replace
                            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
                            // var userId = await userManager.FindByEmailAsync("verifier@verifier.com");
                            tasks.VerifierId = selectedUser.Id;
                            tasks.AssignerId = "system";
                            tasks.Status = "assigned";
                            tasks.DateAdded = DateTime.Now;
                            tasks.DateUpdated = DateTime.Now;
                            _db.Add(tasks);
                            _db.SaveChanges();

                        }
                    }
                }

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

        [HttpGet("ChangePasswordx")]
        public IActionResult ChangePasswordx()
        {
          
            return View();
        }


        [HttpPost("ChangePasswordx")]
        public async Task<IActionResult> ChangePasswordx(ChangePasswords model)
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
