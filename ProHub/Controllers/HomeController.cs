using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProHub.Data;
using ProHub.Data.Repositories;
using System.Security.Claims;
using MySql.Data.MySqlClient;
using System;
using PROHUB.Data;

namespace ProHub.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly EmployeeRepository _employeeRepository;
        private readonly IConfiguration _configuration;
        private readonly IRecentlyLaunchedService _recentlyLaunchedService;
        private readonly IInternalSolutionInprogressService _inprogressService;
        private readonly IInternalSolutionsActivitiesService _activityService;
        private readonly IExternalSolutionsActivitiesService _extActivityService;
        private readonly NextWeekPlanRepository _nextWeekRepo;

        public HomeController(
            ILogger<HomeController> logger, 
            EmployeeRepository employeeRepository, 
            IConfiguration configuration, 
            IRecentlyLaunchedService recentlyLaunchedService, 
            IInternalSolutionInprogressService inprogressService, 
            IInternalSolutionsActivitiesService activityService,
            IExternalSolutionsActivitiesService extActivityService,
            NextWeekPlanRepository nextWeekRepo)
        {
            _logger = logger;
            _employeeRepository = employeeRepository;
            _configuration = configuration;
            _recentlyLaunchedService = recentlyLaunchedService;
            _inprogressService = inprogressService;
            _activityService = activityService;
            _extActivityService = extActivityService;
            _nextWeekRepo = nextWeekRepo;
        }

        public async Task<IActionResult> Index()
        {
            // ===============================
            // 1️⃣ User info (DisplayName, Email)
            // ===============================
            string fullName = "Unknown User";
            string empIdStr = User.FindFirst("EmployeeId")?.Value;
            int? empId = null;
            if (int.TryParse(empIdStr, out int parsedId)) empId = parsedId;

            if (User.Identity.IsAuthenticated)
            {
                var email = User.FindFirst("preferred_username")?.Value
                            ?? User.FindFirst("upn")?.Value
                            ?? User.FindFirst(ClaimTypes.Email)?.Value;

                string displayName = "Unknown";

                if (!string.IsNullOrEmpty(email))
                {
                    fullName = _employeeRepository.GetEmployeeNameByEmail(email);
                    if (!string.IsNullOrEmpty(fullName) && fullName != "Unknown User")
                    {
                        displayName = fullName.Split(' ')[0];
                    }
                    else
                    {
                        displayName = User.FindFirst("name")?.Value ?? "User";
                        if (displayName.Contains(" "))
                            displayName = displayName.Split(' ')[0];
                            
                        fullName = User.FindFirst("name")?.Value ?? "Unknown User";
                    }
                }
                else
                {
                    displayName = User.FindFirst("name")?.Value ?? "User";
                    fullName = User.FindFirst("name")?.Value ?? "Unknown User";
                }

                ViewBag.DisplayName = displayName;
                ViewBag.Email = email ?? "Unknown";
                ViewBag.FullUserName = fullName;
                
                // Fetch Count of Recently Launched Projects for User
                try 
                {
                    var allProjects = await _recentlyLaunchedService.GetRecentlyLaunchedAsync();
                    if (allProjects != null)
                    {
                        var myCount = allProjects.Count(x => !string.IsNullOrEmpty(x.DevelopedByName) && x.DevelopedByName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
                        ViewBag.MyLaunchedCount = myCount;
                    }
                    else
                    {
                        ViewBag.MyLaunchedCount = 0;
                    }
                }
                catch(Exception ex)
                {
                     _logger.LogError(ex, "Error fetching recently launched count");
                     ViewBag.MyLaunchedCount = 0;
                }

                // Fetch Count of In-Progress Projects for User
                try
                {
                    // Fetch both tabs to get total count
                    var level1Projects = await _inprogressService.GetInProgressSolutionsAsync("", "level1") ?? new List<ProHub.Models.InternalPlatform>();
                    var otherProjects = await _inprogressService.GetInProgressSolutionsAsync("", "other") ?? new List<ProHub.Models.InternalPlatform>();
                    
                    var allInProgress = level1Projects.Concat(otherProjects);

                    var myInProgressCount = allInProgress.Count(x => x.DevelopedBy != null && 
                                                                     !string.IsNullOrEmpty(x.DevelopedBy.EmpName) && 
                                                                     x.DevelopedBy.EmpName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
                    
                    ViewBag.MyInProgressCount = myInProgressCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching in-progress count");
                    ViewBag.MyInProgressCount = 0;
                }

                // Fetch Count of INTERNAL Activities Assigned to User
                try
                {
                    var platforms = await _activityService.GetMainPlatformsAsync();
                    var platform = platforms.FirstOrDefault(p =>
                        p.Platforms != null &&
                        p.Platforms.Contains("Internal", StringComparison.OrdinalIgnoreCase));

                    int? internalSolutionId = platform?.ID;

                    var allActivities = await _activityService.GetAllAsync(
                        filterPlatformId: internalSolutionId);

                    var myActivitiesCount = allActivities.Count(x => 
                        (empId.HasValue && x.AssignedTo == empId.Value) ||
                        (!string.IsNullOrEmpty(x.AssignedToName) && x.AssignedToName.Equals(fullName, StringComparison.OrdinalIgnoreCase))
                    );

                    ViewBag.MyActivityCount = myActivitiesCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching internal activities count");
                    ViewBag.MyActivityCount = 0;
                }

                 // Fetch Count of EXTERNAL Activities Assigned to User
                try
                {
                    // Look explicitly for "External" platform similar to Internal logic
                    // If not found, fallback to ID 2 as per controller code 
                    var platforms = await _extActivityService.GetMainPlatformsAsync();
                    var platform = platforms.FirstOrDefault(p =>
                        p.Platforms != null &&
                        p.Platforms.Contains("External", StringComparison.OrdinalIgnoreCase));

                    int? externalSolutionId = platform?.ID ?? 2; 

                    var allExtActivities = await _extActivityService.GetAllAsync(
                        filterPlatformId: externalSolutionId);

                    var myExtActivitiesCount = allExtActivities.Count(x => 
                        (empId.HasValue && x.AssignedTo == empId.Value) ||
                        (!string.IsNullOrEmpty(x.AssignedToName) && x.AssignedToName.Equals(fullName, StringComparison.OrdinalIgnoreCase))
                    );

                    ViewBag.MyExtActivityCount = myExtActivitiesCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching external activities count");
                    ViewBag.MyExtActivityCount = 0;
                }

                // Fetch Count of User's Plans for THIS WEEK
                try
                {
                    var today = DateTime.Today;
                    int daysToMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
                    var weekStart = today.AddDays(-daysToMonday);

                    var plans = _nextWeekRepo.GetByWeek(weekStart);
                    
                    // Filter by user name (UpdatedByName) or UpdatedBy (ID)
                    var myWeekPlansCount = plans.Count(x => 
                        (empId.HasValue && x.UpdatedBy == empId.Value) ||
                        (!string.IsNullOrEmpty(x.UpdatedByName) && x.UpdatedByName.Equals(fullName, StringComparison.OrdinalIgnoreCase))
                    );

                     ViewBag.MyWeekPlanCount = myWeekPlansCount;
                }
                 catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching week plan count");
                    ViewBag.MyWeekPlanCount = 0;
                }
            }
            else
            {
                ViewBag.DisplayName = "Guest";
                ViewBag.Email = "Not logged in";
                ViewBag.MyLaunchedCount = 0;
                ViewBag.MyInProgressCount = 0;
                ViewBag.MyActivityCount = 0;
                ViewBag.MyExtActivityCount = 0;
                ViewBag.MyWeekPlanCount = 0;
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
