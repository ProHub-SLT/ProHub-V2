using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProHub.Constants;
using ProHub.Data.Interfaces;
using ProHub.Models;
using System.Security.Claims;

namespace ProHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly IEmployeePermissionRepository _employeeRepo;

        public AccountController(IEmployeePermissionRepository employeeRepo)
        {
            _employeeRepo = employeeRepo;
        }

        public IActionResult Index() => View();

        [AllowAnonymous]
        public IActionResult SignIn()
        {
            var redirectUrl = Url.Action("PostLogin", "Account");
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl },
                             OpenIdConnectDefaults.AuthenticationScheme);
        }

        [AllowAnonymous]
        public IActionResult PostLogin()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Index");

            return RedirectToAction("Index", "Home");
        }

        // In AccountController.cs
        [AllowAnonymous]
        [Route("Account/AccessDenied")]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            // Get current user's role for nice message
            string userRole = "View Only";
            if (User.IsInRole(AppRoles.Admin))
                userRole = "Administrator";
            else if (User.IsInRole(AppRoles.Developer))
                userRole = "Developer";

            ViewBag.UserRole = userRole;
            ViewBag.UserName = User.FindFirst("name")?.Value ?? "User";


            return View();
        }

        // Authentication error handler
        public IActionResult AuthError(string reason = null)
        {
            ViewBag.ErrorReason = reason ?? "unknown";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignOut()
        {
            
            HttpContext.Session.Clear();


            var callbackUrl = Url.Action(nameof(Index), "Account", values: null, protocol: Request.Scheme);
            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}