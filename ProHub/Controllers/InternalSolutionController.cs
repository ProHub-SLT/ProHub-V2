using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using ProHub.Models;
using PROHUB.Data;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PROHUB.Controllers
{
    public class InternalSolutionController : Controller
    {
        private readonly IInternalSolutionService _dataAccess;
        private readonly ILogger<InternalSolutionController> _logger;
        private readonly int[] _allowedPageSizes = new[] { 10, 25, 50, 100 };

        // Allowed columns for sorting security
        private readonly HashSet<string> _allowedSortColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ParentProjectGroupName", "AppName", "DevelopedByName", "LaunchedDate", "VADate", "Price", "StartDate", "TargetDate", "SDLCPhaseName"
        };

        public InternalSolutionController(IInternalSolutionService dataAccess, ILogger<InternalSolutionController> logger)
        {
            _dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ------------------ Index / Listing ------------------
        [HttpGet]
        public async Task<IActionResult> Index(
            string tab = "operational",
            int page = 1,
            int pageSize = 10,
            string sortColumn = "AppName",
            string sortOrder = "asc",
            string search = ""
        )
        {
            try
            {
                // 1. Normalize Inputs
                tab = (tab ?? "operational").Trim().ToLowerInvariant();
                sortColumn = string.IsNullOrWhiteSpace(sortColumn) ? "AppName" : sortColumn.Trim();
                sortOrder = (sortOrder ?? "asc").Trim().ToLowerInvariant();
                search = search?.Trim() ?? string.Empty;

                if (!_allowedPageSizes.Contains(pageSize)) pageSize = 10;

                // 2. Retrieve Data
                var all = await _dataAccess.GetAllAsync() ?? new List<InternalPlatform>();
                IEnumerable<InternalPlatform> filtered = all;

                // 3. Tab Logic (Filter by Category)
                if (tab == "operational")
                {
                    filtered = filtered.Where(x => string.Equals(x.SDLCPhaseName, "Maintenance", StringComparison.OrdinalIgnoreCase));
                }
                else if (tab == "withoutcr" || tab == "without_cr")
                {
                    filtered = filtered.Where(x => string.Equals(x.SDLCPhaseName, "Maintenance", StringComparison.OrdinalIgnoreCase));
                    filtered = filtered.Where(x => string.Equals(x.AppCategory, "Main Application", StringComparison.OrdinalIgnoreCase));
                }

                // 4. Search Logic
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.AppName) && x.AppName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.DevelopedByName) && x.DevelopedByName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.ParentProjectGroupName) && x.ParentProjectGroupName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.RequestNo) && x.RequestNo.Contains(search, StringComparison.OrdinalIgnoreCase))
                    );
                }

                // 5. Sorting Logic
                bool ascending = sortOrder == "asc";
                if (!_allowedSortColumns.Contains(sortColumn)) sortColumn = "AppName";

                filtered = sortColumn switch
                {
                    "ParentProjectGroupName" => ascending ? filtered.OrderBy(x => x.ParentProjectGroupName) : filtered.OrderByDescending(x => x.ParentProjectGroupName),
                    "AppName" => ascending ? filtered.OrderBy(x => x.AppName) : filtered.OrderByDescending(x => x.AppName),
                    "DevelopedByName" => ascending ? filtered.OrderBy(x => x.DevelopedByName) : filtered.OrderByDescending(x => x.DevelopedByName),
                    "LaunchedDate" => ascending ? filtered.OrderBy(x => x.LaunchedDate) : filtered.OrderByDescending(x => x.LaunchedDate),
                    "StartDate" => ascending ? filtered.OrderBy(x => x.StartDate) : filtered.OrderByDescending(x => x.StartDate),
                    "TargetDate" => ascending ? filtered.OrderBy(x => x.TargetDate) : filtered.OrderByDescending(x => x.TargetDate),
                    "VADate" => ascending ? filtered.OrderBy(x => x.VADate) : filtered.OrderByDescending(x => x.VADate),
                    "Price" => ascending ? filtered.OrderBy(x => x.Price) : filtered.OrderByDescending(x => x.Price),
                    _ => ascending ? filtered.OrderBy(x => x.AppName) : filtered.OrderByDescending(x => x.AppName)
                };

                // 6. Pagination Logic
                var totalRecords = filtered.Count();
                var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 1;
                if (totalPages == 0) totalPages = 1;

                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedList = filtered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 7. View Data
                ViewBag.PageSize = pageSize;
                ViewBag.CurrentPage = page;
                ViewBag.TotalRecords = totalRecords;
                ViewBag.TotalPages = totalPages;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortOrder = sortOrder;
                ViewBag.SearchTerm = search;
                ViewBag.Tab = tab;

                return View("Index", pagedList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Internal Solutions list.");
                TempData["ErrorMessage"] = "Failed to load internal solutions. Check logs.";
                return View("Index", new List<InternalPlatform>());
            }
        }

        // ------------------ Create ------------------

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new InternalPlatform());
        }

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
                var newId = await _dataAccess.CreateAsync(solution);
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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid ID.";
                return RedirectToAction(nameof(Index));
            }

            var solution = await _dataAccess.GetByIdAsync(id);
            if (solution == null)
            {
                TempData["ErrorMessage"] = "Solution not found.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns();
            return View(solution);
        }

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
                bool updated = await _dataAccess.UpdateAsync(solution);
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

            var solution = await _dataAccess.GetByIdAsync(id);
            if (solution == null) return NotFound();

            return View(solution);
        }

        // ------------------ Delete ------------------

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid ID.";
                return RedirectToAction(nameof(Index));
            }

            var solution = await _dataAccess.GetByIdAsync(id);
            if (solution == null)
            {
                TempData["ErrorMessage"] = "Solution not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(solution);
        }

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
                bool deleted = await _dataAccess.DeleteAsync(id);
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

        // ------------------ Misc Actions ------------------

        [HttpGet]
        public IActionResult Folder(int id)
        {

            return RedirectToAction("Index", "InternalDocuments", new { solutionId = id });
        }



        [HttpGet]
        public IActionResult Settings(int id)
        {
            TempData["SuccessMessage"] = $"Settings for ID {id} (placeholder).";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetParentProjectByMainApp(int mainAppId)
        {
            var mainApp = await _dataAccess.GetByIdAsync(mainAppId);

            if (mainApp != null)
            {
                return Json(new { parentProjectId = mainApp.ParentProjectID });
            }

            return Json(new { parentProjectId = "" });
        }

        // ------------------ Change Requests ------------------

        [HttpGet]
        public async Task<IActionResult> ChangeRequests(
            int mainAppId,
            int page = 1,
            int pageSize = 10,
            string sortColumn = "AppName",
            string sortOrder = "asc",
            string search = ""
        )
        {
            if (mainAppId <= 0) return BadRequest();

            try
            {
                var mainApp = await _dataAccess.GetByIdAsync(mainAppId);
                ViewBag.MainAppName = mainApp?.AppName ?? "Unknown Application";
                ViewBag.MainAppId = mainAppId;

                sortColumn = string.IsNullOrWhiteSpace(sortColumn) ? "AppName" : sortColumn.Trim();
                sortOrder = (sortOrder ?? "asc").Trim().ToLowerInvariant();
                search = search?.Trim() ?? string.Empty;
                if (!_allowedPageSizes.Contains(pageSize)) pageSize = 10;

                var all = await _dataAccess.GetAllAsync() ?? new List<InternalPlatform>();
                var filtered = all.Where(x => x.MainAppID == mainAppId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.AppName) && x.AppName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.DevelopedByName) && x.DevelopedByName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.RequestNo) && x.RequestNo.Contains(search, StringComparison.OrdinalIgnoreCase))
                    );
                }

                bool ascending = sortOrder == "asc";
                filtered = sortColumn switch
                {
                    "AppName" => ascending ? filtered.OrderBy(x => x.AppName) : filtered.OrderByDescending(x => x.AppName),
                    "DevelopedByName" => ascending ? filtered.OrderBy(x => x.DevelopedByName) : filtered.OrderByDescending(x => x.DevelopedByName),
                    "Price" => ascending ? filtered.OrderBy(x => x.Price) : filtered.OrderByDescending(x => x.Price),
                    "SDLCPhaseName" => ascending ? filtered.OrderBy(x => x.SDLCPhaseName) : filtered.OrderByDescending(x => x.SDLCPhaseName),
                    _ => ascending ? filtered.OrderBy(x => x.AppName) : filtered.OrderByDescending(x => x.AppName)
                };

                var totalRecords = filtered.Count();
                var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 1;
                if (totalPages == 0) totalPages = 1;
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedList = filtered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.PageSize = pageSize;
                ViewBag.CurrentPage = page;
                ViewBag.TotalRecords = totalRecords;
                ViewBag.TotalPages = totalPages;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortOrder = sortOrder;
                ViewBag.SearchTerm = search;

                return View(pagedList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Change Requests for MainAppId {Id}", mainAppId);
                TempData["ErrorMessage"] = "Failed to load Change Requests.";
                return RedirectToAction(nameof(Index));
            }
        }
        // ------------------ Export Excel ------------------
        public async Task<IActionResult> ExportToExcel(
            string tab = "operational",
            string sortColumn = "AppName",
            string sortOrder = "asc",
            string search = "")
        {
            try
            {
                // 1. Setup Filters
                tab = (tab ?? "operational").Trim().ToLowerInvariant();
                search = search?.Trim() ?? string.Empty;

                var all = await _dataAccess.GetAllAsync() ?? new List<InternalPlatform>();
                IEnumerable<InternalPlatform> filtered = all;

                // --- FILTERING LOGIC ---
                string fileNamePrefix = "InternalSolutions";

                if (tab == "operational")
                {
                    // Filter: Maintenance Only
                    filtered = filtered.Where(x => string.Equals(x.SDLCPhaseName, "Maintenance", StringComparison.OrdinalIgnoreCase));
                    fileNamePrefix = "Internal Solutions - Operational";
                }
                else if (tab == "withoutcr" || tab == "without_cr")
                {
                    // Filter: Maintenance AND Main Application (No CRs)
                    filtered = filtered.Where(x => string.Equals(x.SDLCPhaseName, "Maintenance", StringComparison.OrdinalIgnoreCase));
                    filtered = filtered.Where(x => string.Equals(x.AppCategory, "Main Application", StringComparison.OrdinalIgnoreCase));
                    fileNamePrefix = "Internal Solutions - Operational Without CR";
                }

                // ... (Search Logic & Sort Logic remain the same) ...
                // --- SEARCH LOGIC ---
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.AppName) && x.AppName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.DevelopedByName) && x.DevelopedByName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.ParentProjectGroupName) && x.ParentProjectGroupName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.RequestNo) && x.RequestNo.Contains(search, StringComparison.OrdinalIgnoreCase))
                    );
                }

                // --- SORTING LOGIC ---
                bool ascending = sortOrder == "asc";
                filtered = sortColumn switch
                {
                    "ParentProjectGroupName" => ascending ? filtered.OrderBy(x => x.ParentProjectGroupName) : filtered.OrderByDescending(x => x.ParentProjectGroupName),
                    "AppName" => ascending ? filtered.OrderBy(x => x.AppName) : filtered.OrderByDescending(x => x.AppName),
                    "DevelopedByName" => ascending ? filtered.OrderBy(x => x.DevelopedByName) : filtered.OrderByDescending(x => x.DevelopedByName),
                    "LaunchedDate" => ascending ? filtered.OrderBy(x => x.LaunchedDate) : filtered.OrderByDescending(x => x.LaunchedDate),
                    "VADate" => ascending ? filtered.OrderBy(x => x.VADate) : filtered.OrderByDescending(x => x.VADate),
                    "StartDate" => ascending ? filtered.OrderBy(x => x.StartDate) : filtered.OrderByDescending(x => x.StartDate),
                    "Price" => ascending ? filtered.OrderBy(x => x.Price) : filtered.OrderByDescending(x => x.Price),
                    _ => ascending ? filtered.OrderBy(x => x.AppName) : filtered.OrderByDescending(x => x.AppName)
                };

                var dataList = filtered.ToList();

                // 2. Create Excel File
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Solutions");
                    int colIndex = 1;

                    // --- A. DEFINE HEADERS ---

                    var headers = new List<string> {
                "Application Group",    // 1
                "Application Name",     // 2
                "Application Category", // 3
                "Developed By",         // 4
                "Application URL",      // 5
                "Hosted Server IP",     // 6
                "SDLC Phase",           // 7
                "Start Date",           // 8
                "Target Date",          // 9
                "VA Date",              // 10
                "Launched Date",        // 11
                "Percentage Done",      // 12
                "Current Status",       // 13
                "Price (Rs)"            // 14
            };


                    bool showMainApp = !(tab == "withoutcr" || tab == "without_cr");

                    if (showMainApp)
                    {
                        headers.Add("Main App"); // 15
                    }

                    // --- B. WRITE HEADERS ---
                    foreach (var header in headers)
                    {
                        worksheet.Cell(1, colIndex).Value = header;
                        colIndex++;
                    }

                    // --- C. STYLING HEADERS ---
                    var headerRange = worksheet.Range(1, 1, 1, headers.Count);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#007BFF");
                    headerRange.Style.Font.FontColor = XLColor.White;

                    // --- D. WRITE DATA ROWS ---
                    int row = 2;
                    foreach (var item in dataList)
                    {
                        int col = 1;

                        worksheet.Cell(row, col++).Value = item.ParentProjectGroupName;
                        worksheet.Cell(row, col++).Value = item.AppName;
                        worksheet.Cell(row, col++).Value = item.AppCategory;
                        worksheet.Cell(row, col++).Value = item.DevelopedByName;
                        worksheet.Cell(row, col++).Value = item.AppURL;
                        worksheet.Cell(row, col++).Value = item.AppIP;
                        worksheet.Cell(row, col++).Value = item.SDLCPhaseName;
                        worksheet.Cell(row, col++).Value = item.StartDate;
                        worksheet.Cell(row, col++).Value = item.TargetDate;
                        worksheet.Cell(row, col++).Value = item.VADate;
                        worksheet.Cell(row, col++).Value = item.LaunchedDate;
                        worksheet.Cell(row, col++).Value = item.PercentageDone;
                        worksheet.Cell(row, col++).Value = item.Status;
                        worksheet.Cell(row, col++).Value = item.Price;


                        if (showMainApp)
                        {
                            worksheet.Cell(row, col++).Value = item.MainAppName;
                        }

                        row++;
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        string excelName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed");
                TempData["ErrorMessage"] = "Failed to export data: " + ex.Message;
                return RedirectToAction(nameof(Index), new { tab = tab });
            }
        }

        // ------------------ Helpers ------------------

        private async Task PopulateDropdowns()
        {
            try
            {
                var employees = await _dataAccess.GetEmployeesAsync() ?? new List<Employee>();
                var sdlcPhases = await _dataAccess.GetSdlcPhasesAsync() ?? new List<SDLCPhase>();
                var endUserTypes = await _dataAccess.GetEndUserTypesAsync() ?? new List<TargetEndUser>();
                var mainApps = await _dataAccess.GetMainApplicationsAsync() ?? new List<InternalPlatform>();
                var parentProjects = await _dataAccess.GetParentProjectsAsync() ?? new List<ParentProject>();

                ViewBag.Employees = employees;
                ViewBag.SdlcPhases = sdlcPhases;
                ViewBag.EndUserTypes = endUserTypes;

                ViewBag.MainApplications = mainApps
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.AppName ?? string.Empty })
                    .ToList();

                ViewBag.ParentProjects = parentProjects
                    .Select(p => new SelectListItem { Value = p.ParentProjectID.ToString(), Text = p.ParentProjectGroup ?? string.Empty })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate dropdowns for InternalSolution views.");
                ViewBag.Employees = new List<Employee>();
                ViewBag.SdlcPhases = new List<SDLCPhase>();
                ViewBag.EndUserTypes = new List<TargetEndUser>();
                ViewBag.MainApplications = new List<SelectListItem>();
                ViewBag.ParentProjects = new List<SelectListItem>();
            }
        }

        private void DefaultMissingProperties(InternalPlatform solution)
        {
            if (solution == null) return;

            solution.PlatformType = solution.PlatformType ?? string.Empty;
            solution.PlatformName = solution.PlatformName ?? string.Empty;
            solution.BitBucket = solution.BitBucket ?? string.Empty;
            solution.BitBucketRepo = solution.BitBucketRepo ?? string.Empty;
            solution.APPOwner = solution.APPOwner ?? string.Empty;
            solution.AppBusinessOwner = solution.AppBusinessOwner ?? string.Empty;
            solution.ContractPeriod = solution.ContractPeriod ?? string.Empty;
            solution.ProposalUploaded = solution.ProposalUploaded ?? string.Empty;
            solution.PlatformOwner = solution.PlatformOwner ?? string.Empty;
            solution.APP_Owner = solution.APP_Owner ?? string.Empty;
            solution.SLA = solution.SLA ?? string.Empty;
            solution.DR = solution.DR ?? string.Empty;
            solution.WAF = solution.WAF ?? string.Empty;
            solution.Scope = solution.Scope ?? string.Empty;
            solution.RequestNo = solution.RequestNo ?? string.Empty;
        }
    }
}