﻿using LLB.Data;
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
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public AccountController(AppDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
        {
            _db = db;
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

        [HttpGet("ChangePassword")]
        public IActionResult ChangePassword()
        {

            return View();
        }
        [HttpPost("ChangePassword")]
        
        public async Task<IActionResult> ChangePassword(ChangePassword model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get the current user
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                // Redirect to login if user is not authenticated
                return RedirectToAction("Login", "Account");
            }

            // Change the password
            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                // Re-sign in the user to refresh the security stamp
                await signInManager.RefreshSignInAsync(user);
                TempData["success"] = "Your password has been changed successfully.";
                return View();
            }
            else
            {
                List<string> errorDescriptions = new List<string>();

                // Handle errors
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    errorDescriptions.Add(error.Description);
                   
                }

                // Combine all error descriptions into a single string (separated by commas or line breaks)
                TempData["error"] = string.Join(", ", errorDescriptions); // Or use '\n' for line breaks if needed

                return View();
            }
            
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
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("LandingPage", "Account");
        }
        
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }
       
        
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

      


        [HttpPost("ChangePasswordccc")]
        public async Task<IActionResult> ChangePasswordccc(ChangePassword model)
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
