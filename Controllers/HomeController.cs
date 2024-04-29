using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LLB.Models;

namespace LLB.Controllers
{
    [AllowAnonymous]
    [Route("")]
    [Route("Home")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("")]
        [HttpGet("LandingPage")]
        [AllowAnonymous]
        public IActionResult LandingPage()
        {
            return View();
        }

        [HttpGet(("SignUp"))]
        [AllowAnonymous]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpGet("SignIn")]
        [AllowAnonymous]
        public IActionResult SignIn()
        {
            return View();
        }
        [HttpGet("Privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}