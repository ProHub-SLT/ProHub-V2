// Controllers/NextWeekPlansController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using ProHub.Constants;           // AppRoles.Admin, AppRoles.Developer, AppRoles.ViewOnly
using ProHub.Data;
using ProHub.Models;
using Newtonsoft.Json;
using System.Security.Claims;

namespace ProHub.Controllers
{
    [Authorize] // Everyone must be logged in
    public class NextWeekPlansController : Controller
    {
        private readonly NextWeekPlanRepository _nextWeekPlanRepo;
        private readonly ExternalSolutionRepository _extRepo;
        private readonly ConsumerPlatformRepository _intRepo;
        private readonly EmployeeRepository _empRepo;

        public NextWeekPlansController(
            NextWeekPlanRepository nextWeekPlanRepo,
            ExternalSolutionRepository extRepo,
            ConsumerPlatformRepository intRepo,
            EmployeeRepository empRepo)
        {
            _nextWeekPlanRepo = nextWeekPlanRepo;
            _extRepo = extRepo;
            _intRepo = intRepo;
            _empRepo = empRepo;
        }

        // ================================
        // 1. INDEX – Everyone can view
        // ================================
        public IActionResult Index(DateTime? weekStart)
        {
            try
            {
                if (!weekStart.HasValue)
                {
                    var today = DateTime.Today;
                    int daysToMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
                    weekStart = today.AddDays(-daysToMonday); // this week Monday
                }

                var plans = _nextWeekPlanRepo.GetByWeek(weekStart.Value);

                ViewBag.SelectedWeek = weekStart;
                ViewBag.Weeks = GenerateWeeks();
                ViewBag.PlanCount = plans.Count;
                ViewBag.RequestedDate = weekStart.Value;

                return View(plans);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading plans: " + ex.Message;
                return View(new List<NextWeekPlan>());
            }
        }

        private List<dynamic> GenerateWeeks()
        {
            var weeks = new List<dynamic>();
            var today = DateTime.Today;
            var monday = today.AddDays(-(((int)today.DayOfWeek - 1 + 7) % 7));

            for (int i = -6; i <= 1; i++) // 6 past weeks + current + next
            {
                var start = monday.AddDays(i * 7);
                var end = start.AddDays(4);
                weeks.Add(new
                {
                    Value = start.ToString("yyyy-MM-dd"),
                    Text = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}"
                });
            }
            return weeks;
        }

        // ================================
        // 2. CREATE – Only Admin + Developer
        // ================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpGet]
        public IActionResult Create()
        {
            var model = new NextWeekPlan
            {
                ExternalPlatforms = _extRepo.GetAll(),
                InternalPlatforms = _intRepo.GetAll()
            };
            ViewBag.EmployeeName = GetCurrentEmployeeName();
            return View(model);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(NextWeekPlan model)
        {
            model.ExternalPlatforms = _extRepo.GetAll();
            model.InternalPlatforms = _intRepo.GetAll();

            // ========================
            // VALIDATION: At least one platform
            // ========================
            if ((model.ExternalProjectIds == null || !model.ExternalProjectIds.Any()) &&
                (model.InternalProjectIds == null || !model.InternalProjectIds.Any()))
            {
                ModelState.AddModelError("", "Please select at least one platform (internal or external).");
                ViewBag.EmployeeName = GetCurrentEmployeeName();
                return View(model);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var externalIds = model.ExternalProjectIds ?? new List<int>();
                    var internalIds = model.InternalProjectIds ?? new List<int>();
                    var allRecords = new List<NextWeekPlan>();

                    // Create records for combinations
                    if (externalIds.Any() && internalIds.Any())
                    {
                        foreach (var ext in externalIds)
                        {
                            foreach (var intr in internalIds)
                            {
                                var plan = new NextWeekPlan
                                {
                                    ExternalPlatform = ext,
                                    InternalApp = intr,
                                    StartDate = model.StartDate,
                                    EndDate = model.EndDate,
                                    WorkPlanDesc = model.WorkPlanDetails
                                };
                                SetUserInformation(plan);
                                allRecords.Add(plan);
                            }
                        }
                    }
                    else if (externalIds.Any())
                    {
                        foreach (var ext in externalIds)
                        {
                            var plan = new NextWeekPlan
                            {
                                ExternalPlatform = ext,
                                InternalApp = null,
                                StartDate = model.StartDate,
                                EndDate = model.EndDate,
                                WorkPlanDesc = model.WorkPlanDetails
                            };
                            SetUserInformation(plan);
                            allRecords.Add(plan);
                        }
                    }
                    else if (internalIds.Any())
                    {
                        foreach (var intr in internalIds)
                        {
                            var plan = new NextWeekPlan
                            {
                                ExternalPlatform = null,
                                InternalApp = intr,
                                StartDate = model.StartDate,
                                EndDate = model.EndDate,
                                WorkPlanDesc = model.WorkPlanDetails
                            };
                            SetUserInformation(plan);
                            allRecords.Add(plan);
                        }
                    }

                    // Save all
                    foreach (var record in allRecords)
                        _nextWeekPlanRepo.Create(record);

                    return RedirectToAction("Create", new { success = "true" });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                }
            }

            ViewBag.EmployeeName = GetCurrentEmployeeName();
            return View(model);
        }

        // ================================
        // 3. EDIT – Only Admin OR Owner
        // ================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var plan = _nextWeekPlanRepo.GetById(id);
            if (plan == null) return NotFound();

            if (!IsOwnerOrAdmin(plan.UpdatedBy))
                return Forbid();

            plan.ExternalPlatforms = _extRepo.GetAll();
            plan.InternalPlatforms = _intRepo.GetAll();

            plan.ExternalProjectIds = plan.ExternalPlatform.HasValue
                ? new List<int> { plan.ExternalPlatform.Value }
                : new List<int>();

            plan.InternalProjectIds = plan.InternalApp.HasValue
                ? new List<int> { plan.InternalApp.Value }
                : new List<int>();

            ViewBag.EmployeeName = GetCurrentEmployeeName();
            return View(plan);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(NextWeekPlan model)
        {
            var existing = _nextWeekPlanRepo.GetById(model.ID);
            if (existing == null) return NotFound();

            if (!IsOwnerOrAdmin(existing.UpdatedBy))
                return Forbid();

            // ========================
            // VALIDATION: At least one platform
            // ========================
            if ((model.ExternalProjectIds == null || !model.ExternalProjectIds.Any()) &&
                (model.InternalProjectIds == null || !model.InternalProjectIds.Any()))
            {
                ModelState.AddModelError("", "Please select at least one platform (internal or external).");
                model.ExternalPlatforms = _extRepo.GetAll();
                model.InternalPlatforms = _intRepo.GetAll();
                ViewBag.EmployeeName = GetCurrentEmployeeName();
                return View(model);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update first record only for simplicity
                    existing.StartDate = model.StartDate;
                    existing.EndDate = model.EndDate;
                    existing.WorkPlanDesc = model.WorkPlanDetails;
                    existing.ExternalPlatform = model.ExternalProjectIds?.FirstOrDefault();
                    existing.InternalApp = model.InternalProjectIds?.FirstOrDefault();
                    existing.UpdatedOn = DateTime.Now;

                    _nextWeekPlanRepo.Update(existing);

                    TempData["Success"] = "Plan updated successfully!";
                    return RedirectToAction("Index", new { weekStart = model.StartDate });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                }
            }

            model.ExternalPlatforms = _extRepo.GetAll();
            model.InternalPlatforms = _intRepo.GetAll();
            ViewBag.EmployeeName = GetCurrentEmployeeName();
            return View(model);
        }

        // ================================
        // 4. DELETE – Only Admin OR Owner
        // ================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var plan = _nextWeekPlanRepo.GetById(id);
            if (plan == null)
                return Json(new { success = false, message = "Plan not found" });

            if (!IsOwnerOrAdmin(plan.UpdatedBy))
                return Json(new { success = false, message = "You can only delete your own plans" });

            _nextWeekPlanRepo.PermanentDelete(id);
            return Json(new { success = true });
        }

        // ================================
        // 5. EXPORT & BACKUP – Everyone can use
        // ================================
        [HttpGet]
        public IActionResult Export(DateTime? weekStart)
        {
            if (!weekStart.HasValue) return BadRequest("Week start date is required.");

            var plans = _nextWeekPlanRepo.GetByWeek(weekStart.Value);
            if (!plans.Any()) return NotFound("No plans found.");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Plans");

            ws.Cells[1, 1].Value = "Week";
            ws.Cells[1, 2].Value = "Employee";
            ws.Cells[1, 3].Value = "External";
            ws.Cells[1, 4].Value = "Internal";
            ws.Cells[1, 5].Value = "Work Plan";

            int row = 2;
            foreach (var p in plans)
            {
                ws.Cells[row, 1].Value = $"{p.StartDate:dd/MM} - {p.EndDate:dd/MM}";
                ws.Cells[row, 2].Value = p.UpdatedByName;

                ws.Cells[row, 3].Value = p.ExternalPlatform.HasValue
                    ? _extRepo.GetAll().FirstOrDefault(x => x.Id == p.ExternalPlatform)?.PlatformName ?? "None"
                    : "None";

                ws.Cells[row, 4].Value = p.InternalApp.HasValue
                    ? _intRepo.GetAll().FirstOrDefault(x => x.Id == p.InternalApp)?.AppName ?? "None"
                    : "None";

                if (!string.IsNullOrWhiteSpace(p.WorkPlanDesc))
                {
                    try
                    {
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(p.WorkPlanDesc);
                        var lines = dict.Select(kv => $"{kv.Value?.name} ({kv.Value?.type}): {kv.Value?.details}");
                        ws.Cells[row, 5].Value = string.Join("\r\n", lines);
                    }
                    catch
                    {
                        ws.Cells[row, 5].Value = p.WorkPlanDesc;
                    }
                }
                row++;
            }

            ws.Cells.AutoFitColumns();
            var stream = new MemoryStream(package.GetAsByteArray());
            var fileName = $"WeekPlan_{weekStart:yyyy-MM-dd}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public IActionResult DownloadBackupMatrix()
        {
            try
            {
                var email = User.FindFirst("preferred_username")?.Value ?? User.FindFirst("upn")?.Value;
                if (string.IsNullOrEmpty(email))
                    return BadRequest("User email not found.");

                var empId = _empRepo.GetEmployeeIdByEmail(email);
                if (empId == 0)
                    return BadRequest("Employee record not found.");

                var platforms = _intRepo.GetInternalPlatformsByBackupOfficer(empId);
                ExcelPackage.License.SetNonCommercialPersonal("ProHub");

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Backup Matrix");

                ws.Cells[1, 1].Value = "Application Name";
                ws.Cells[1, 2].Value = "Developed By";
                ws.Cells[1, 3].Value = "Backup Person 1";
                ws.Cells[1, 4].Value = "Backup Person 2";

                int row = 2;
                foreach (var p in platforms)
                {
                    ws.Cells[row, 1].Value = p.AppName;
                    ws.Cells[row, 2].Value = p.DevelopedBy?.EmpName ?? "";
                    ws.Cells[row, 3].Value = p.BackupOfficer1?.EmpName ?? "";
                    ws.Cells[row, 4].Value = p.BackupOfficer2?.EmpName ?? "";
                    row++;
                }

                ws.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Backup_Matrix_{DateTime.Now:yyyy-MM-dd}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating file: {ex.Message}");
            }
        }

        // ================================
        // Helper Methods
        // ================================
        private string GetCurrentEmployeeName()
        {
            var email = User.FindFirst("preferred_username")?.Value
                        ?? User.FindFirst("upn")?.Value;

            return !string.IsNullOrEmpty(email)
                ? _empRepo.GetEmployeeNameByEmail(email) ?? "Unknown User"
                : "Unknown User";
        }

        private void SetUserInformation(NextWeekPlan model)
        {
            if (!User.Identity.IsAuthenticated) return;

            var email = User.FindFirst("preferred_username")?.Value
                        ?? User.FindFirst("upn")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                model.UpdatedByName = User.FindFirst("name")?.Value ?? "Unknown";
                return;
            }

            model.UpdatedByName = _empRepo.GetEmployeeNameByEmail(email);

            var empId = _empRepo.GetEmployeeIdByEmail(email);
            if (empId > 0)
                model.UpdatedBy = empId;
        }

        private bool IsOwnerOrAdmin(int? recordOwnerId)
        {
            if (!recordOwnerId.HasValue) return false;

            if (User.IsInRole(AppRoles.Admin)) return true;

            if (!int.TryParse(User.FindFirst("EmployeeId")?.Value, out int currentUserId))
                return false;

            return recordOwnerId.Value == currentUserId;
        }
    }
}
