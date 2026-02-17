using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PROHUB.Data;
using PROHUB.Models;
using ProHub.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using ProHub.Constants;

namespace PROHUB.Controllers
{
    public class InternalSolutionInprogressController : Controller
    {
        private readonly IInternalSolutionInprogressService _service;
        private readonly ILogger<InternalSolutionInprogressController> _logger;

        public InternalSolutionInprogressController(IInternalSolutionInprogressService service, ILogger<InternalSolutionInprogressController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ------------------ GET: Index ------------------
        public async Task<IActionResult> Index(string search, string sortColumn = "AppName", string sortOrder = "asc", int page = 1, int pageSize = 10, string activeTab = "level1")
        {
            try
            {
                // 1. Get Data
                var allItems = await _service.GetInProgressSolutionsAsync(search, activeTab);

                // 2. Apply Search Filter
                var filteredItems = ApplySearchFilter(allItems, search);

                // 3. Setup View Data
                ViewData["SearchTerm"] = search ?? "";
                ViewData["SortColumn"] = sortColumn;
                ViewData["SortOrder"] = sortOrder;
                ViewData["PageSize"] = pageSize;
                ViewData["ActiveTab"] = activeTab;



                // 4. Sorting Logic
                switch (sortColumn)
                {
                    case "ParentProjectGroupName":
                        filteredItems = sortOrder == "desc"
                            ? filteredItems.OrderByDescending(s => s.ParentProject?.ParentProjectGroup).ToList()
                            : filteredItems.OrderBy(s => s.ParentProject?.ParentProjectGroup).ToList();
                        break;

                    case "Developed By":
                        filteredItems = sortOrder == "desc"
                            ? filteredItems.OrderByDescending(s => s.DevelopedBy?.EmpName).ToList()
                            : filteredItems.OrderBy(s => s.DevelopedBy?.EmpName).ToList();
                        break;

                    case "Start":
                        filteredItems = sortOrder == "desc"
                            ? filteredItems.OrderByDescending(s => s.StartDate).ToList()
                            : filteredItems.OrderBy(s => s.StartDate).ToList();
                        break;

                    case "Target":
                        filteredItems = sortOrder == "desc"
                            ? filteredItems.OrderByDescending(s => s.TargetDate).ToList()
                            : filteredItems.OrderBy(s => s.TargetDate).ToList();
                        break;

                    case "UD":
                        filteredItems = sortOrder == "desc"
                            ? filteredItems.OrderByDescending(s => s.PercentageDone).ToList()
                            : filteredItems.OrderBy(s => s.PercentageDone).ToList();
                        break;

                    case "Value":
                        filteredItems = sortOrder == "desc"
                            ? filteredItems.OrderByDescending(s => s.SoftwareValue).ToList()
                            : filteredItems.OrderBy(s => s.SoftwareValue).ToList();
                        break;

                    case "AppName":
                    default:
                        filteredItems = sortOrder == "desc"
                            ? filteredItems.OrderByDescending(s => s.AppName).ToList()
                            : filteredItems.OrderBy(s => s.AppName).ToList();
                        break;
                }

                // 5. Pagination Logic (pagedItems is declared here)
                var totalCount = filteredItems.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                if (totalPages > 0 && page > totalPages) page = totalPages;
                if (page < 1) page = 1;

                var pagedItems = filteredItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewData["CurrentPage"] = page;
                ViewData["TotalPages"] = totalPages;
                ViewData["TotalEntries"] = totalCount;

                // 6. Return the View (Now pagedItems exists)
                return View(pagedItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading in-progress data.");
                TempData["ErrorMessage"] = "Error loading in-progress data.";
                return View(new List<InternalPlatform>());
            }
        }

        // ------------------ Create ------------------
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new InternalPlatform());
        }

       
        
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InternalPlatform solution)
        {
            await PopulateDropdowns();
            DefaultMissingProperties(solution);

            var keysToIgnore = new[]
            {
                "PlatformType", "PlatformName", "BitBucket", "APPOwner", "AppBusinessOwner",
                "BackupOfficer1Id", "BackupOfficer2Id", "SalesTeam", "SalesTeamId", "Company", "CompanyId",
                "ProposalUploaded", "PlatformOwner", "APP_Owner", "IncentiveEarned", "BillingDate",
                "ContractPeriod", "IncentiveShare", "PlatformOTC", "PlatformMRC", "SoftwareValue"
            };

            foreach (var k in keysToIgnore)
            {
                if (ModelState.ContainsKey(k)) ModelState.Remove(k);
            }

            if (!ModelState.IsValid)
            {
                return View(solution);
            }

            try
            {
                var newId = await _service.CreateAsync(solution);
                TempData["SuccessMessage"] = $"Solution created successfully (ID: {newId})!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating InternalPlatform. Model: {@Model}", solution);
                TempData["ErrorMessage"] = "An error occurred while creating the solution. See server logs for details.";
                return View(solution);
            }
        }

        // ------------------ Edit ------------------
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid ID.";
                return RedirectToAction(nameof(Index));
            }

            var solution = await _service.GetByIdAsync(id);
            if (solution == null)
            {
                TempData["ErrorMessage"] = "Solution not found.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns();
            return View(solution);
        }


        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InternalPlatform solution)
        {
            if (id != solution.Id)
            {
                TempData["ErrorMessage"] = "Solution ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns();
            DefaultMissingProperties(solution);

            var keysToIgnore = new[]
            {
                "PlatformType", "PlatformName", "BitBucket", "APPOwner", "AppBusinessOwner",
                "BackupOfficer1Id", "BackupOfficer2Id", "SalesTeam", "SalesTeamId", "Company", "CompanyId",
                "ProposalUploaded", "PlatformOwner", "APP_Owner", "IncentiveEarned", "BillingDate",
                "ContractPeriod", "IncentiveShare", "PlatformOTC", "PlatformMRC", "SoftwareValue"
            };
            foreach (var k in keysToIgnore)
            {
                if (ModelState.ContainsKey(k)) ModelState.Remove(k);
            }

            if (!ModelState.IsValid)
            {
                return View(solution);
            }

            try
            {
                bool updated = await _service.UpdateAsync(solution);
                if (updated)
                {
                    TempData["SuccessMessage"] = "Solution updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update solution (record may not exist or no changes detected).";
                    return View(solution);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update InternalPlatform id {Id}. Model: {@Model}", id, solution);
                TempData["ErrorMessage"] = "An error occurred while updating. See server logs.";
                return View(solution);
            }
        }

        // ------------------ Details ------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0) return BadRequest();
            var solution = await _service.GetByIdAsync(id);
            if (solution == null) return NotFound();
            return View(solution);
        }

        // ------------------ Delete ------------------
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid ID.";
                return RedirectToAction(nameof(Index));
            }

            var solution = await _service.GetByIdAsync(id);
            if (solution == null)
            {
                TempData["ErrorMessage"] = "Solution not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(solution);
        }


        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid ID.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                bool deleted = await _service.DeleteAsync(id);
                if (deleted) TempData["SuccessMessage"] = "Solution deleted successfully!";
                else TempData["ErrorMessage"] = "Delete failed (not found).";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete InternalPlatform id {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting. See server logs.";
            }

            return RedirectToAction(nameof(Index));
        }


        // ------------------ EXPORT TO EXCEL (ClosedXML) ------------------

        public async Task<IActionResult> ExportInProgressToExcel(string search, string activeTab)
        {
            try
            {
                // 1. Get Data from Service
                var filteredItems = await _service.GetInProgressSolutionsAsync(search, activeTab);

                // 2. Create Excel File
                using (var workbook = new XLWorkbook())
                {
                    // Set Sheet Name
                    string sheetName = (activeTab == "level1") ? "Level 1 Solutions" : "Other Solutions";
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    // --- HEADERS (Updated to match your list) ---
                    string[] headers = {
                "Application Group",   // AppGroup
                "Application Name",    // App_Name
                "Developed By",        // Developed_By
                "Application URL",     // App_URL
                "Application IP",      // App_IP
                "SDLC Phase",          // SDLC_Stage
                "Start Date",          // Start_Date
                "Target Date",         // Target_Date
                "VA Date",             // VA_Date
                "Percentage Done",     // Percentage_Done
                "Launched Date",       // Launched_Date
                "Current Status",      // Current_Status
                "Price (Rs)",          // Price
                "Launched Date"        // Launched_Year
            };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cell(1, i + 1).Value = headers[i];
                    }

                    // --- HEADER STYLING ---
                    var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#007BFF");
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int row = 2;
                    foreach (var item in filteredItems)
                    {
                        // 1. AppGroup
                        worksheet.Cell(row, 1).Value = item.ParentProject?.ParentProjectGroup ?? item.AppCategory ?? "-";

                        // 2. App_Name
                        worksheet.Cell(row, 2).Value = item.AppName;

                        // 3. Developed_By
                        worksheet.Cell(row, 3).Value = item.DevelopedBy?.EmpName ?? "-";

                        // 4. App_URL (Verify property name 'AppURL' in your model)
                        worksheet.Cell(row, 4).Value = item.AppURL ?? "-";

                        // 5. App_IP (Verify property name 'AppIP' in your model)
                        worksheet.Cell(row, 5).Value = item.AppIP ?? "-";

                        // 6. SDLC_Stage
                        worksheet.Cell(row, 6).Value = item.SDLCPhase?.Phase ?? "-";

                        // 7. Start_Date
                        if (item.StartDate.HasValue)
                        {
                            worksheet.Cell(row, 7).Value = item.StartDate.Value;
                            worksheet.Cell(row, 7).Style.DateFormat.Format = "MMM dd, yyyy";
                        }

                        // 8. Target_Date
                        if (item.TargetDate.HasValue)
                        {
                            worksheet.Cell(row, 8).Value = item.TargetDate.Value;
                            worksheet.Cell(row, 8).Style.DateFormat.Format = "MMM dd, yyyy";
                        }

                        // 9. VA_Date (Verify property name 'VADate' in your model)
                        if (item.VADate.HasValue)
                        {
                            worksheet.Cell(row, 9).Value = item.VADate.Value;
                            worksheet.Cell(row, 9).Style.DateFormat.Format = "MMM dd, yyyy";
                        }

                        // 10. Percentage_Done (Verify property name 'PercentageDone')
                        worksheet.Cell(row, 10).Value = item.PercentageDone;

                        // 11. Launched_Date
                        if (item.LaunchedDate.HasValue)
                        {
                            worksheet.Cell(row, 11).Value = item.LaunchedDate.Value;
                            worksheet.Cell(row, 11).Style.DateFormat.Format = "MMM dd, yyyy";
                        }

                        // 12. Current_Status (Verify property name 'Status')
                        worksheet.Cell(row, 12).Value = item.Status ?? "-";

                        // 13. Price
                        if (item.Price.HasValue)
                        {
                            worksheet.Cell(row, 13).Value = item.Price.Value;
                            worksheet.Cell(row, 13).Style.NumberFormat.Format = "#,##0.00";
                        }

                        // 14. Launched_Year
                        if (item.LaunchedDate.HasValue)
                        {
                            worksheet.Cell(row, 14).Value = item.LaunchedDate.Value;
                            worksheet.Cell(row, 11).Style.DateFormat.Format = "MMM dd, yyyy";
                        }

                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Position = 0;

                        string nameSuffix = (activeTab == "level1") ? "level 1" : "other";
                        string excelName = $"Internal Solutions - In-Progress {nameSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to export data. Please try again.";
                return RedirectToAction(nameof(Index), new { activeTab = activeTab });
            }
        }

        // ------------------ GetComments ------------------

        [HttpGet]
        public async Task<IActionResult> GetComments(int id)
        {
            var solution = await _service.GetByIdAsync(id);
            if (solution == null || solution.ProjectComments == null)
            {
                return Json(new List<object>());
            }

            var comments = solution.ProjectComments.Select(c => new
            {
                comment = c.Comment,
                updatedBy = c.UpdatedByEmployee?.EmpName ?? "Unknown",
                updatedTime = c.Updated_Time?.ToString("MMM dd, yyyy HH:mm")
            });

            return Json(comments);
        }

        // ------------------ Helper Methods ------------------

        // Helper to centralize search logic (DRY Principle)
        private List<InternalPlatform> ApplySearchFilter(List<InternalPlatform> items, string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return items;
            }

            search = search.ToLower().Trim();

            return items.Where(s =>
                (s.AppName != null && s.AppName.ToLower().Contains(search)) ||
                (s.ParentProject != null && s.ParentProject.ParentProjectGroup != null && s.ParentProject.ParentProjectGroup.ToLower().Contains(search)) ||
                (s.DevelopedBy != null && s.DevelopedBy.EmpName != null && s.DevelopedBy.EmpName.ToLower().Contains(search)) ||
                (s.StartDate.HasValue && s.StartDate.Value.ToString("MMM dd, yyyy").ToLower().Contains(search)) ||
                (s.TargetDate.HasValue && s.TargetDate.Value.ToString("MMM dd, yyyy").ToLower().Contains(search))
            ).ToList();
        }

        private async Task PopulateDropdowns()
        {
            ViewBag.Employees = await _service.GetEmployeesAsync();
            ViewBag.SdlcPhases = await _service.GetSdlcPhasesAsync();
            ViewBag.EndUserTypes = await _service.GetEndUserTypesAsync();
            ViewBag.ParentProjects = await _service.GetParentProjectsAsync();

            var apps = await _service.GetAllInternalPlatformsAsync();
            apps.Insert(0, new InternalPlatform { Id = 0, AppName = "N/A" });
            ViewBag.MainApplications = apps;
        }

        private void DefaultMissingProperties(InternalPlatform model)
        {
            if (model.StartDate == null) model.StartDate = DateTime.Now;
        }
    }
}