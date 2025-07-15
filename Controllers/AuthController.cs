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
    [Route("Auth")]
    public class AuthController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public AuthController(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.signInManager = signInManager;
            _validatorService = validatorService;
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

        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model, string role)
        {

            var getallUserName = await userManager.FindByEmailAsync(model.Email);
            if (getallUserName == null)
            {

                var userid = Guid.NewGuid().ToString();

                var user = new ApplicationUser

                {
                  // Id = userid,
                    Name = model.Name,

                   // UserId = userid,
                    Nationality = model.Nationality,
                   // Nationality = model.CountryOfResidence,
                    ApplicationBy = "",
                    LockoutEnd = DateTime.Now,
                UserPhoneNumber = model.PhoneNumber,
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
                            
                            DOB = model.DOB.ToString("yyyy-MM-dd"),
                            CountryOfResidence = model.CountryOfResidence,
                            Gender = model.Gender,
                            Province = model.Province
                        };

                        var result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                { 
                    if (await roleManager.RoleExistsAsync("client"))
                    {
                        try
                        {
                            // var roleuser = await userManager.FindByEmailAsync(user.Email);
                            IdentityResult addclient = await userManager.AddToRoleAsync(user, "client");
                            // _db.Asp
                            if (addclient.Succeeded)
                            {
                                TempData["success"] = "user has successfully been created";
                                return View();
                            }
                        }catch(Exception e)
                        {
                            TempData["error"] = e;
                            return View();
                            Console.WriteLine(e);
                        }
                    }
                    else
                    {
                        TempData["error"] = "please contact admin";
                        return View();

                    }



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
                    //return RedirectToAction("SignUp", "Home");
                }
                else {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;
                        return View();
                    }
                }
            }
            else
            {
                TempData["error"] = "User Already in system";
                return View();
            }
               
               
            return View(model);
        }
        
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("", "");
        }

        [HttpGet("Login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
    
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
              
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    
                    {
                      
                        return RedirectToAction("Dashboard", "Home");
                    }

                }
               ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
            }
            ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
            TempData["error"] = "please check username and password";
            return View(model);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
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
        //[HttpGet("ForgotPassword")]
        public IActionResult Forgotpassword()
        {
            ViewBag.title = "Forgot Password";
            return View();
        }

        [AllowAnonymous]
        //[HttpPost("ForgotPasswordS")]
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


        [AllowAnonymous]
        [HttpGet("CheckId")]
        public  IActionResult CheckId(string natid)
        {
            var exists =  userManager.Users.Any(u => u.NatID == natid);

            if (exists)
            {
                return Json(new { success = "err", msg = "National ID already exists with another user" });
            }

            if (natid == null)
            {
                return Json(new { success = "err", msg = "Invalid national ID." });

            }
            //////////////////////////////////////////////////////////////////////////
            ///National Id Test
            //////////////////////////////////////////////////////////////////////////////
            ///
            string natidfronnt = natid.Substring(natid.Length - 2);
            string natidback = natid.Substring(0, 2);
            // string natidback = natid.Substring(0, natid.Length - 2);
            var onlyLetters = new String(natid.Where(c => Char.IsLetter(c) && Char.IsUpper(c)).ToArray());

            string modint = string.Empty;
            int val;
            //removing last 3 charachers from national ID
            string modchars = natid.Substring(0, natid.Length - 3);
            for (int i = 0; i < modchars.Length; i++)
            {
                if (Char.IsDigit(modchars[i]))
                    modint += modchars[i];
            }

            if (modint.Length > 0)
                val = Int32.Parse(modint);
            int idnum = Int32.Parse(modint);
            //calculating mod
            // int idnumtest = 24;
            int letternum = (idnum % 23);

            int A = 1; int B = 2; int C = 3; int D = 4; int E = 5;
            int F = 6; int G = 7; int H = 8; int J = 9; int K = 10;
            int L = 11; int M = 12; int N = 13; int P = 14; int Q = 15;
            int R = 16; int S = 17; int T = 18; int V = 19; int W = 20;
            int X = 21; int Y = 22; int Z = 0;

            string message = "";

            string[] dcodes = { "08", "63", "07", "44", "13", "42", "75", "50", "34", "05", "71", "15", "45", "11", "61", "68", "18", "25", "80", "43", "49", "47", "48", "59", "85", "32", "38", "24", "37", "70", "86", "04", "14", "13", "27", "22", "54", "83", "06", "35", "79", "41", "53", "73", "84", "02", "56", "28", "21", "39", "19", "77", "26", "23", "29", "58", "03", "66", "67", "12", "00" };
            if (dcodes.Contains(natidfronnt) && dcodes.Contains(natidback))
            {
                //Process.Start("BLAH BLAH CODE TO ADD PRINTER VIA WINDOWS EXEC"");

                if (letternum == A)
                {
                    if (onlyLetters == "A")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });

                    }
                    else
                    {
                        TempData["flash"] = "1";
                       // //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == B)
                {
                    if (onlyLetters == "B")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        ////TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }


                if (letternum == C)
                {
                    if (onlyLetters == "C")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == D)
                {
                    if (onlyLetters == "D")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == E)
                {
                    if (onlyLetters == "E")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == F)
                {
                    if (onlyLetters == "F")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == G)
                {
                    if (onlyLetters == "G")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == H)
                {
                    if (onlyLetters == "H")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });

                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == J)
                {
                    if (onlyLetters == "J")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == K)
                {
                    if (onlyLetters == "K")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == L)
                {
                    if (onlyLetters == "L")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == M)
                {
                    if (onlyLetters == "M")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });

                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == N)
                {
                    if (onlyLetters == "N")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == P)
                {
                    if (onlyLetters == "P")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == Q)
                {
                    if (onlyLetters == "Q")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == R)
                {
                    if (onlyLetters == "R")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == S)
                {
                    if (onlyLetters == "S")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == T)
                {
                    if (onlyLetters == "T")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == V)
                {
                    if (onlyLetters == "V")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == W)
                {
                    if (onlyLetters == "W")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == X)
                {
                    if (onlyLetters == "X")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == Y)
                {
                    if (onlyLetters == "Y")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }

                if (letternum == Z)
                {
                    if (onlyLetters == "Z")
                    {
                        return Json(new { success = "ok", msg = "Valid national ID" });
                    }
                    else
                    {
                        TempData["flash"] = "1";
                        //TempData["error"] = "Invalid National  ID";
                        return Json(new { success = "err", msg = "Invalid national ID." });
                    }
                }
                else
                {
                    message = " Invalid National ID";
                }


            }
            else
            {
                TempData["flash"] = "1";
                //TempData["error"] = "Invalid National  ID";
                return Json(new { success = "err", msg = "Invalid national ID." });

            }

            return Json(new { success = "err", msg = "Invalid national ID." });
        }



    }
}
