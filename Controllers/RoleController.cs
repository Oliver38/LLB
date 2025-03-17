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
    [Route("Roles")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _db;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDNTCaptchaValidatorService _validatorService;

        public RolesController(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,SignInManager<ApplicationUser> signInManager, IDNTCaptchaValidatorService validatorService)
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
        [AllowAnonymous]
        [HttpGet("Client")]

        public async Task<IActionResult> Client()
        {
            var role = new IdentityRole { Name = "client"};
            IdentityResult result = await roleManager.CreateAsync(role);
            return View();
        }

        [AllowAnonymous]
        [HttpGet("AutoRoles")]
        
        public async Task<IActionResult> AutoRoles()
        {
            var roles = new[]
             {
                   "chief accountant", "district accountant", "provincial accountant","internal","super user", "admin", "inspector", "secretary", "client","verifier","recommender", "accountant"
                };

            //var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var roleName in roles)
            {
                

                if (await roleManager.RoleExistsAsync(roleName)) { }
                //var identityRole = new IdentityRole { Name = role };
               else {
                    var role = new IdentityRole { Name = roleName };
                    IdentityResult result = await roleManager.CreateAsync(role);
                        };
            }
        
            return View();
        }

        [AllowAnonymous]
        [HttpGet("UsersRoles")]

        public async Task<IActionResult> UsersRoles()
        {

            ///Adding super user
            var getallUserName = await userManager.FindByEmailAsync("superuser@superuser.com");
            if (getallUserName == null)
            {
                var user = new ApplicationUser
                {
                    Name = "superuser@superuser.com",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "superuser@superuser.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "superuser@superuser.com",
                    PhysicalAddress = "superuser@superuser.com",
                    Email = "superuser@superuser.com",
                    UserEmail = "superuser@superuser.com",
                    UserName = "superuser@superuser.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("super user"))
                    {
                       
                         IdentityResult addclient = await userManager.AddToRoleAsync(user, "super user");                         
                         IdentityResult addclientinternal = await userManager.AddToRoleAsync(user, "internal");                         
                            if (addclient.Succeeded && addclientinternal.Succeeded)
                            {
                                TempData["success"] = "user has successfully been created";
                                                        }
                      
                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;
                       
                    }
                }
            }



            // Adding Inspector
            var getInspector = await userManager.FindByEmailAsync("inspector@inspector.com");
            if (getInspector == null)
            {
                var user = new ApplicationUser
                {
                    Name = "inspector@inspector.com",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "inspector@inspector.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "inspector@inspector.com",
                    PhysicalAddress = "inspector@inspector.com",
                    Email = "inspector@inspector.com",
                    UserEmail = "inspector@inspector.com",
                    UserName = "inspector@inspector.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("inspector"))
                    {

                        IdentityResult addclient = await userManager.AddToRoleAsync(user, "inspector");
                        IdentityResult addinternal = await userManager.AddToRoleAsync(user, "internal");

                        if (addclient.Succeeded && addinternal.Succeeded)
                        {
                            TempData["success"] = "user has successfully been created";
                        }

                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;

                    }
                }
            }


            // Add Secretary
            var getSecretary = await userManager.FindByEmailAsync("secretary@secretary.com");
            if (getSecretary == null)
            {
                var user = new ApplicationUser
                {
                    Name = "secretary@secretary.com",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "secretary@secretary.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "secretary@secretary.com",
                    PhysicalAddress = "secretary@secretary.com",
                    Email = "secretary@secretary.com",
                    UserEmail = "secretary@secretary.com",
                    UserName = "secretary@secretary.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("secretary"))
                    {

                        IdentityResult addclient = await userManager.AddToRoleAsync(user, "inspector");
                        IdentityResult addinternal = await userManager.AddToRoleAsync(user, "internal");
                        if (addclient.Succeeded && addinternal.Succeeded)
                        {
                            TempData["success"] = "user has successfully been created";
                        }

                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;

                    }
                }
            }

            // Admin
            var getAdmin = await userManager.FindByEmailAsync("admin@admin.com");
            if (getAdmin == null)
            {
                var user = new ApplicationUser
                {
                    Name = "admin@admin.com",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "admin@admin.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "admin@admin.com",
                    PhysicalAddress = "admin@admin.com",
                    Email = "admin@admin.com",
                    UserEmail = "admin@admin.com",
                    UserName = "admin@admin.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("admin"))
                    {

                        IdentityResult addclient = await userManager.AddToRoleAsync(user, "admin");
                        IdentityResult addinternal = await userManager.AddToRoleAsync(user, "internal");
                        if (addclient.Succeeded && addinternal.Succeeded) 
                        {
                            TempData["success"] = "user has successfully been created";
                        }

                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;

                    }
                }
            }





            // verifier
            var verifier = await userManager.FindByEmailAsync("verifier@verifier.com");
            if (verifier == null)
            {
                var user = new ApplicationUser
                {
                    Name = "verifier@verifier.com",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "verifier@verifier.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "verifier@verifier.com",
                    PhysicalAddress = "verifier@verifier.com",
                    Email = "verifier@verifier.com",
                    UserEmail = "verifier@verifier.com",
                    UserName = "verifier@verifier.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("verifier"))
                    {

                        IdentityResult addrolea = await userManager.AddToRoleAsync(user, "verifier");
                        IdentityResult addroleb = await userManager.AddToRoleAsync(user, "inspector");
                        IdentityResult addinternal = await userManager.AddToRoleAsync(user, "internal");
                        if (addrolea.Succeeded && addroleb.Succeeded && addinternal.Succeeded)
                        {
                            TempData["success"] = "user has successfully been created";
                        }

                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;

                    }
                }
            }




            // verifier
            var recommender = await userManager.FindByEmailAsync("recommender@recommender.com");
           // var recommender = await userManager.FindByEmailAsync("recommender@recommender.com");
            if (recommender == null)
            {
                var user = new ApplicationUser
                {
                    Name = "recommender@recommender.com",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "recommender@recommender.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "recommender@recommender.com",
                    PhysicalAddress = "recommender@recommender.com",
                    Email = "recommender@recommender.com",
                    UserEmail = "recommender@recommender.com",
                    UserName = "recommender@recommender.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("recommender"))
                    {

                        IdentityResult addrolea = await userManager.AddToRoleAsync(user, "recommender");
                        IdentityResult  addroleb = await userManager.AddToRoleAsync(user, "inspector");
                        IdentityResult addrolebinternal = await userManager.AddToRoleAsync(user, "internal"); 
                        if (addrolea.Succeeded && addroleb.Succeeded && addrolebinternal.Succeeded)
                        {
                            TempData["success"] = "user has successfully been created";
                        }

                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;

                    }
                }
            }




            // accountant
            var accountant = await userManager.FindByEmailAsync("accountant@accountant.com");
            if (accountant == null)
            {
                var user = new ApplicationUser
                {
                    Name = "accountant@accountant.com",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "accountant@accountant.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "accountant@accountant.com",
                    PhysicalAddress = "accountant@accountant.com",
                    Email = "accountant@accountant.com",
                    UserEmail = "accountant@accountant.com",
                    UserName = "accountant@accountant.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("accountant"))
                    {

                        IdentityResult addrolea = await userManager.AddToRoleAsync(user, "accountant");
                        IdentityResult addroleinternal = await userManager.AddToRoleAsync(user, "internal");
                        //IdentityResult addroleb = await userManager.AddToRoleAsync(user, "inspector");
                        if (addrolea.Succeeded && addroleinternal.Succeeded)
                        {
                            TempData["success"] = "user has successfully been created";
                        }

                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;

                    }
                }
            }




            // provincial accountant
            // accountant
            var provincialaccountant = await userManager.FindByEmailAsync("provincialaccountant@provincialaccountant.com");
            if (provincialaccountant == null)
            {
                var user = new ApplicationUser
                {
                    Name = "provincial",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "accountant@accountant.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "accountant",
                    PhysicalAddress = "provincialaccountant@provincialaccountant.com",
                    Email = "provincialaccountant@provincialaccountant.com",
                    UserEmail = "provincialaccountant@provincialaccountant.com",
                    UserName = "provincialaccountant@provincialaccountant.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("provincial accountant"))
                    {

                        IdentityResult addrolea = await userManager.AddToRoleAsync(user, "provincial accountant");
                        IdentityResult addroleinternal = await userManager.AddToRoleAsync(user, "internal");
                        //IdentityResult addroleb = await userManager.AddToRoleAsync(user, "inspector");
                        if (addrolea.Succeeded && addroleinternal.Succeeded)
                        {
                            TempData["success"] = "user has successfully been created";
                        }

                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;

                    }
                }
            }

            //district accountant
            // provincial accountant
            // accountant
            var districtaccountant = await userManager.FindByEmailAsync("districtaccountant@districtaccountant.com");
            if (districtaccountant == null)
            {
                var user = new ApplicationUser
                {
                    Name = "district",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "accountant@accountant.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "accountant",
                    PhysicalAddress = "districtaccountant@districtaccountant.com",
                    Email = "districtaccountant@districtaccountant.com",
                    UserEmail = "districtaccountant@districtaccountant.com",
                    UserName = "districtaccountant@districtaccountant.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("district accountant"))
                    {

                        IdentityResult addrolea = await userManager.AddToRoleAsync(user, "district accountant");
                        IdentityResult addroleinternal = await userManager.AddToRoleAsync(user, "internal");
                        //IdentityResult addroleb = await userManager.AddToRoleAsync(user, "inspector");
                        if (addrolea.Succeeded && addroleinternal.Succeeded)
                        {
                            TempData["success"] = "user has successfully been created";
                        }

                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;

                    }
                }
            }

            //chief accountant
            // accountant
            var chiefaccountant = await userManager.FindByEmailAsync("chiefaccountant@chiefaccountant.com");
            if (chiefaccountant == null)
            {
                var user = new ApplicationUser
                {
                    Name = "chief",
                    Nationality = "Zimbabwean",
                    ApplicationBy = "accountant@accountant.com",
                    LockoutEnd = DateTime.Now,
                    UserPhoneNumber = "0772772772",
                    LastName = "accountant",
                    PhysicalAddress = "chiefaccountant@chiefaccountant.com",
                    Email = "chiefaccountant@chiefaccountant.com",
                    UserEmail = "chiefaccountant@chiefaccountant.com",
                    UserName = "chiefaccountant@chiefaccountant.com",
                    IsActive = true,
                    PhoneNumber = "0772772772",
                    NatID = "63772T36",
                    DateOfApplication = DateTime.Now,
                    DOB = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                    CountryOfResidence = "Zimbabwe",
                    Gender = "male",
                    Province = "Harare"
                };
                var result = await userManager.CreateAsync(user, "Test123!");
                if (result.Succeeded)
                {
                    if (await roleManager.RoleExistsAsync("chief accountant"))
                    {

                        IdentityResult addrolea = await userManager.AddToRoleAsync(user, "chief accountant");
                        IdentityResult addroleinternal = await userManager.AddToRoleAsync(user, "internal");
                        //IdentityResult addroleb = await userManager.AddToRoleAsync(user, "inspector");
                        if (addrolea.Succeeded && addroleinternal.Succeeded)
                        {
                            TempData["success"] = "user has successfully been created";
                        }

                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                        TempData["error"] = error.Description;

                    }
                }
            }
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
        public async Task<IActionResult> Register(RegisterViewModel model)
        {

            var getallUserName = await userManager.FindByEmailAsync(model.Email);
            if (getallUserName == null)
            {


                
                        var user = new ApplicationUser

                        {
                            Id = Guid.NewGuid().ToString(),
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
                            DOB = model.DOB.ToString("yyyy-MM-dd"),
                            CountryOfResidence = model.CountryOfResidence,
                            Gender = model.Gender,
                            Province = model.Province
                        };

                        var result = await userManager.CreateAsync(user, model.Password);
                        if (result.Succeeded)
                        {

                            return RedirectToAction("SignUp", "Home");
                        }
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
               
               
            return View(model);
        }




        
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("LandingPage", "Account");
        }
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

    }
}
