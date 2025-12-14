using Microsoft.AspNetCore.Authorization; // <-- add this
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using ProHub.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using ProHub.Data;


namespace ProHub.Controllers
{
    [Authorize]  // <-- Force login for all actions in this controller
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly EmployeeRepository _employeeRepository;

        public HomeController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, EmployeeRepository employeeRepository)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
            _employeeRepository = employeeRepository;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Read user info from claims
                var email = User.FindFirst("preferred_username")?.Value
                            ?? User.FindFirst("upn")?.Value
                            ?? User.FindFirst(ClaimTypes.Email)?.Value;

                string displayName = "Unknown";
                
                if (!string.IsNullOrEmpty(email))
                {
                    // Fetch full name from database
                    var fullName = _employeeRepository.GetEmployeeNameByEmail(email);
                    
                    // Extract First Name (take part before first space)
                    if (!string.IsNullOrEmpty(fullName) && fullName != "Unknown User")
                    {
                        displayName = fullName.Split(' ')[0];
                    }
                    else
                    {
                        // Fallback to claim name if DB lookup fails or user not found
                         displayName = User.FindFirst("name")?.Value ?? "User";
                         // Extract first name from claim if possible
                         if (displayName.Contains(" "))
                         {
                             displayName = displayName.Split(' ')[0];
                         }
                    }
                }
                else
                {
                     displayName = User.FindFirst("name")?.Value ?? "User";
                }

                ViewBag.DisplayName = displayName;
                ViewBag.Email = email ?? "Unknown";
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
