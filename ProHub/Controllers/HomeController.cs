using Microsoft.AspNetCore.Authorization; // <-- add this
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using ProHub.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Security.Claims;


namespace ProHub.Controllers
{
    [Authorize]  // <-- Force login for all actions in this controller
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GraphServiceClient _graphServiceClient;

        public HomeController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Read user info from claims
                var displayName = User.FindFirst("name")?.Value ?? "Unknown";
                var email = User.FindFirst("preferred_username")?.Value
                            ?? User.FindFirst("upn")?.Value
                            ?? "Unknown";

                ViewBag.DisplayName = displayName;
                ViewBag.Email = email;
            }
            else
            {
                ViewBag.DisplayName = "Guest";
                ViewBag.Email = "Not logged in";
            }

            return View();
        }



        [AllowAnonymous] // Optional: allow public access to Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous] // Optional: allow public access to Error page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }




        [AllowAnonymous]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => $"{c.Type} : {c.Value}");
            return Content(string.Join("\n", claims), "text/plain");
        }


    }
}
