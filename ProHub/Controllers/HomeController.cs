using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProHub.Data;
using System.Security.Claims;
using MySql.Data.MySqlClient;
using System;

namespace ProHub.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly EmployeeRepository _employeeRepository;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, EmployeeRepository employeeRepository, IConfiguration configuration)
        {
            _logger = logger;
            _employeeRepository = employeeRepository;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // ===============================
            // 1️⃣ User info (DisplayName, Email)
            // ===============================
            if (User.Identity.IsAuthenticated)
            {
                var email = User.FindFirst("preferred_username")?.Value
                            ?? User.FindFirst("upn")?.Value
                            ?? User.FindFirst(ClaimTypes.Email)?.Value;

                string displayName = "Unknown";

                if (!string.IsNullOrEmpty(email))
                {
                    var fullName = _employeeRepository.GetEmployeeNameByEmail(email);
                    if (!string.IsNullOrEmpty(fullName) && fullName != "Unknown User")
                    {
                        displayName = fullName.Split(' ')[0];
                    }
                    else
                    {
                        displayName = User.FindFirst("name")?.Value ?? "User";
                        if (displayName.Contains(" "))
                            displayName = displayName.Split(' ')[0];
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





            if (User.Identity.IsAuthenticated)
            {
                var employeeId = User.FindFirst("EmployeeId")?.Value;

                if (!string.IsNullOrEmpty(employeeId))
                {
                    // 🔒 INSERT LOGIN ONLY ONCE
                    if (HttpContext.Session.GetString("LoginLogged") != "true")
                    {
                        try
                        {
                            using var conn = new MySqlConnection(
                                _configuration.GetConnectionString("DefaultConnection")
                            );
                            conn.Open();

                            var insertSql = @"
                    INSERT INTO Log (Log_Time, Log_By, Log_Text, Log_Type)
                    VALUES (UTC_TIMESTAMP(), @EmpId, 'User logged in', 'Login')";

                            using var cmd = new MySqlCommand(insertSql, conn);
                            cmd.Parameters.AddWithValue("@EmpId", employeeId);
                            cmd.ExecuteNonQuery();

                            var cleanupSql = @"
                    DELETE FROM Log
                    WHERE Log_By = @EmpId
                    AND Log_ID NOT IN (
                        SELECT Log_ID FROM (
                            SELECT Log_ID
                            FROM Log
                            WHERE Log_By = @EmpId
                            ORDER BY Log_Time DESC
                            LIMIT 2
                        ) t
                    )";

                            using var cleanupCmd = new MySqlCommand(cleanupSql, conn);
                            cleanupCmd.Parameters.AddWithValue("@EmpId", employeeId);
                            cleanupCmd.ExecuteNonQuery();

                            HttpContext.Session.SetString("LoginLogged", "true");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Login log error");
                        }
                    }

                    // 📌 FETCH LAST LOGIN (second most recent)
                    try
                    {
                        using var conn = new MySqlConnection(
                            _configuration.GetConnectionString("DefaultConnection")
                        );
                        conn.Open();

                        var sql = @"
                SELECT Log_Time
                FROM Log
                WHERE Log_By = @EmpId
                ORDER BY Log_Time DESC
                LIMIT 1 OFFSET 1";

                        using var cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@EmpId", employeeId);

                        var result = cmd.ExecuteScalar();
                        ViewBag.LastLoginTime = result != null
                            ? Convert.ToDateTime(result).ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                            : "First login";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Last login fetch error");
                        ViewBag.LastLoginTime = "Unavailable";
                    }
                }
            }






            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new Models.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => $"{c.Type} : {c.Value}");
            return Content(string.Join("\n", claims), "text/plain");
        }
    }
}
