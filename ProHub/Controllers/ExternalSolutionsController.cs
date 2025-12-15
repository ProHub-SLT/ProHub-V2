using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using ProHub.Models;
using PROHUB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PROHUB.Controllers
{
    public class ExternalSolutionsController : Controller
    {
        private readonly IExternalSolutionService _externalSolutionService;
        private readonly ILogger<ExternalSolutionsController> _logger;

        public ExternalSolutionsController(IExternalSolutionService externalSolutionService, ILogger<ExternalSolutionsController> logger)
        {
            _externalSolutionService = externalSolutionService ?? throw new ArgumentNullException(nameof(externalSolutionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /ExternalSolutions
        [HttpGet]
        public async Task<IActionResult> Index(string search = "", string sortColumn = "PlatformName", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            try
            {
                var allSolutions = await _externalSolutionService.GetAllAsync();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    allSolutions = allSolutions.Where(s =>
                        (s.PlatformName != null && s.PlatformName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (s.DevelopedByName != null && s.DevelopedByName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (s.CompanyName != null && s.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // Apply sorting
                allSolutions = sortColumn switch
                {
                    "PlatformName" => sortOrder == "asc" ? allSolutions.OrderBy(s => s.PlatformName).ToList() : allSolutions.OrderByDescending(s => s.PlatformName).ToList(),
                    "DevelopedByName" => sortOrder == "asc" ? allSolutions.OrderBy(s => s.DevelopedByName).ToList() : allSolutions.OrderByDescending(s => s.DevelopedByName).ToList(),
                    "ContractPeriod" => sortOrder == "asc" ? allSolutions.OrderBy(s => s.ContractPeriod).ToList() : allSolutions.OrderByDescending(s => s.ContractPeriod).ToList(),
                    "Billed" => sortOrder == "asc" ? allSolutions.OrderBy(s => s.ContractPeriod).ToList() : allSolutions.OrderByDescending(s => s.ContractPeriod).ToList(),
                    "DPOHandoverDate" => sortOrder == "asc" ? allSolutions.OrderBy(s => s.DPOHandoverDate).ToList() : allSolutions.OrderByDescending(s => s.DPOHandoverDate).ToList(),
                    _ => allSolutions.OrderBy(s => s.PlatformName).ToList()
                };

                // Apply pagination
                var totalRecords = allSolutions.Count;
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));
                var pagedSolutions = allSolutions.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Set ViewBag values for the view
                ViewBag.PageSize = pageSize;
                ViewBag.CurrentPage = page;
                ViewBag.TotalRecords = totalRecords;
                ViewBag.TotalPages = totalPages;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortOrder = sortOrder;
                ViewBag.SearchTerm = search;

                return View(pagedSolutions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load external solutions list.");
                TempData["ErrorMessage"] = "Error loading external solutions. See server logs.";
                return View(new List<ExternalPlatform>());
            }
        }

        // GET: /ExternalSolutions/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new ExternalPlatform());
        }

        // POST: /ExternalSolutions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExternalPlatform externalSolution)
        {
            await PopulateDropdowns();

            if (externalSolution == null)
            {
                TempData["ErrorMessage"] = "Invalid form submission.";
                return View(new ExternalPlatform());
            }

            DefaultMissingProperties(externalSolution);
            RemoveDbOnlyModelStateKeys();

            if (!ModelState.IsValid)
            {
                LogModelStateErrors("Create", externalSolution);
                ModelState.AddModelError(string.Empty, "Please fix the validation errors shown below.");
                TempData["ErrorMessage"] = "Validation failed. See field errors.";
                return View(externalSolution);
            }

            try
            {
                var newId = await _externalSolutionService.CreateAsync(externalSolution);
                if (newId > 0)
                {
                    TempData["SuccessMessage"] = $"External Solution Created.";
                    ModelState.Clear();
                    var freshModel = new ExternalPlatform();
                    await PopulateDropdowns();
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("Create returned non-positive id ({NewId}) for model {@Model}", newId, externalSolution);
                ModelState.AddModelError(string.Empty, "Create returned an unexpected result. Operation not completed.");
                TempData["ErrorMessage"] = "Create returned unexpected result.";
                return View(externalSolution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ExternalPlatform. Model: {@Model}", externalSolution);
                ModelState.AddModelError(string.Empty, $"An error occurred while creating: {GetInnermostMessage(ex)}");
                TempData["ErrorMessage"] = "An internal error occurred.";
                return View(externalSolution);
            }
        }

        // GET: /ExternalSolutions/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0) return BadRequest();

            try
            {
                var model = await _externalSolutionService.GetByIdAsync(id);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "External solution not found.";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ExternalPlatform id {Id}", id);
                TempData["ErrorMessage"] = "Error loading form. See logs.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /ExternalSolutions/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExternalPlatform externalSolution)
        {
            await PopulateDropdowns();

            if (externalSolution == null || externalSolution.Id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid model.";
                return RedirectToAction(nameof(Index));
            }

            DefaultMissingProperties(externalSolution);
            RemoveDbOnlyModelStateKeys();

            if (!ModelState.IsValid)
            {
                LogModelStateErrors("Edit", externalSolution);
                ModelState.AddModelError(string.Empty, "Please fix the validation errors shown below.");
                TempData["ErrorMessage"] = "Validation failed. See field errors.";
                return View(externalSolution);
            }

            try
            {
                var ok = await _externalSolutionService.UpdateAsync(externalSolution);
                if (ok)
                {
                    TempData["SuccessMessage"] = "External solution updated.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("Update returned false for model {@Model}", externalSolution);
                ModelState.AddModelError(string.Empty, "Update failed (record may not exist).");
                TempData["ErrorMessage"] = "Update failed (record may not exist).";
                return View(externalSolution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ExternalPlatform. Model: {@Model}", externalSolution);
                ModelState.AddModelError(string.Empty, $"An error occurred while updating: {GetInnermostMessage(ex)}");
                TempData["ErrorMessage"] = "An error occurred while updating.";
                return View(externalSolution);
            }
        }

        // GET: /ExternalSolutions/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0) return BadRequest();

            try
            {
                var model = await _externalSolutionService.GetByIdAsync(id);
                if (model == null) return NotFound();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for ExternalPlatform id {Id}", id);
                TempData["ErrorMessage"] = "Error loading details. See logs.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /ExternalSolutions/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid ID.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var deleted = await _externalSolutionService.DeleteAsync(id);
                if (deleted) TempData["SuccessMessage"] = "External solution deleted.";
                else TempData["ErrorMessage"] = "Delete failed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting ExternalPlatform id {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /ExternalSolutions/ExportToExcel
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string search = "", string sortColumn = "PlatformName", string sortOrder = "asc")
        {
            try
            {
                // 1. Fetch Data
                var allSolutions = await _externalSolutionService.GetAllAsync();

                // 2. Filter Logic (In-Memory)
                if (!string.IsNullOrEmpty(search))
                {
                    allSolutions = allSolutions.Where(s =>
                        (s.PlatformName != null && s.PlatformName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (s.DevelopedByName != null && s.DevelopedByName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (s.CompanyName != null && s.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // 3. Sort Logic
                allSolutions = sortColumn switch
                {
                    "PlatformName" => sortOrder == "asc" ? allSolutions.OrderBy(s => s.PlatformName).ToList() : allSolutions.OrderByDescending(s => s.PlatformName).ToList(),
                    "DevelopedByName" => sortOrder == "asc" ? allSolutions.OrderBy(s => s.DevelopedByName).ToList() : allSolutions.OrderByDescending(s => s.DevelopedByName).ToList(),
                    _ => allSolutions.OrderBy(s => s.PlatformName).ToList()
                };

                // 4. Create Excel File using ClosedXML
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("External Solutions");

                    // --- Header Row ---
                    var headers = new[]
                    {
                "Platform Name", "Developed By", "Company", "Launched Date",
                "Billed Date", "OTC", "MRC", "Contract Period",
                "Revenue", "Software Value", "Billed Status", "DPO Handover Date"
            };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    // --- Data Rows ---
                    for (int i = 0; i < allSolutions.Count; i++)
                    {
                        var solution = allSolutions[i];
                        int row = i + 2;

                        worksheet.Cell(row, 1).Value = solution.PlatformName ?? "";
                        worksheet.Cell(row, 2).Value = solution.DevelopedByName ?? "";
                        worksheet.Cell(row, 3).Value = solution.CompanyName ?? "";
                        worksheet.Cell(row, 4).Value = solution.LaunchedDate?.ToString("yyyy-MM-dd") ?? "";
                        worksheet.Cell(row, 5).Value = solution.BillingDate?.ToString("yyyy-MM-dd") ?? "";

                        // Numeric values stored as numbers for Excel math
                        worksheet.Cell(row, 6).Value = solution.PlatformOTC ?? 0;
                        worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";

                        worksheet.Cell(row, 7).Value = solution.PlatformMRC ?? 0;
                        worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

                        worksheet.Cell(row, 8).Value = solution.ContractPeriod ?? "";

                        worksheet.Cell(row, 9).Value = solution.IncentiveEarned ?? 0;
                        worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";

                        worksheet.Cell(row, 10).Value = solution.SoftwareValue ?? 0;
                        worksheet.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";

                        worksheet.Cell(row, 11).Value = solution.BillingDate.HasValue ? "Yes" : "No";

                        // Color code the "No" status for better visibility
                        if (!solution.BillingDate.HasValue)
                        {
                            worksheet.Cell(row, 11).Style.Font.FontColor = XLColor.Red;
                            worksheet.Cell(row, 11).Style.Font.Bold = true;
                        }

                        worksheet.Cell(row, 12).Value = solution.DPOHandoverDate?.ToString("yyyy-MM-dd") ?? "";
                    }

                    // --- Final Formatting ---
                    worksheet.Columns().AdjustToContents(); // Auto-fit columns

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        var fileName = $"External_Solutions_Operational_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error exporting external solutions to Excel."); // Uncomment if you have logger
                TempData["ErrorMessage"] = "Error exporting to Excel. Please check server logs.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ---------- Helpers ----------

        private async Task PopulateDropdowns()
        {
            try
            {
                var companies = await _externalSolutionService.GetCompanyAsync() ?? new List<Company>();
                var employees = await _externalSolutionService.GetEmployeesAsync() ?? new List<Employee>();
                var salesTeams = await _externalSolutionService.Getsales_teamAsync() ?? new List<SalesTeam>();
                var sdlcPhases = await _externalSolutionService.GetsdlcphasAsync() ?? new List<SDLCPhase>();

                ViewBag.Companies = companies;
                ViewBag.Employees = employees;
                ViewBag.SalesTeams = salesTeams;
                ViewBag.SdlcPhases = sdlcPhases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate ExternalSolutions dropdowns.");
                ViewBag.Companies = new List<Company>();
                ViewBag.Employees = new List<Employee>();
                ViewBag.SalesTeams = new List<SalesTeam>();
                ViewBag.SdlcPhases = new List<SDLCPhase>();
            }
        }

        private void DefaultMissingProperties(ExternalPlatform model)
        {
            if (model == null) return;

            model.PlatformType = model.PlatformType ?? string.Empty;
            model.PlatformName = model.PlatformName ?? string.Empty;
            model.BitBucket = model.BitBucket ?? string.Empty;
            model.BITBucketRepo = model.BITBucketRepo ?? string.Empty;
            model.DevelopedTeam = model.DevelopedTeam ?? string.Empty;
            model.SalesAM = model.SalesAM ?? string.Empty;
            model.SalesManager = model.SalesManager ?? string.Empty;
            model.SalesEngineer = model.SalesEngineer ?? string.Empty;
            model.PlatformOwner = model.PlatformOwner ?? string.Empty;
            model.APP_Owner = model.APP_Owner ?? string.Empty;
            model.ProposalUploaded = model.ProposalUploaded ?? string.Empty;
            model.ContractPeriod = model.ContractPeriod ?? string.Empty;
            model.SLA = model.SLA ?? string.Empty;
            model.DPOHandoverComment = model.DPOHandoverComment ?? string.Empty;
            model.IntegratedApps = model.IntegratedApps ?? string.Empty;
            model.DR = model.DR ?? string.Empty;
            model.Status = model.Status ?? string.Empty;
        }

        private void RemoveDbOnlyModelStateKeys()
        {
            var keysToIgnore = new[]
            {
                nameof(ExternalPlatform.PlatformType),
                nameof(ExternalPlatform.BitBucket),
                nameof(ExternalPlatform.BITBucketRepo),
                nameof(ExternalPlatform.PlatformOwner),
                nameof(ExternalPlatform.APP_Owner),
                nameof(ExternalPlatform.ProposalUploaded),
                nameof(ExternalPlatform.IncentiveEarned),
                nameof(ExternalPlatform.IncentiveShare),
                nameof(ExternalPlatform.BillingDate),
                nameof(ExternalPlatform.SSLCertificateExpDate),
                nameof(ExternalPlatform.SoftwareValue),
                nameof(ExternalPlatform.DevelopedById),
                nameof(ExternalPlatform.SDLCStageId),
                nameof(ExternalPlatform.Status),
                nameof(ExternalPlatform.IntegratedApps),
                nameof(ExternalPlatform.DR),
                nameof(ExternalPlatform.CompanyId),
                nameof(ExternalPlatform.SalesTeamId),
                nameof(ExternalPlatform.BackupOfficer1Id),
                nameof(ExternalPlatform.BackupOfficer2Id)
            };

            foreach (var k in keysToIgnore)
            {
                if (ModelState.ContainsKey(k)) ModelState.Remove(k);
            }
        }

        private void LogModelStateErrors(string actionName, ExternalPlatform model)
        {
            try
            {
                var modelStateDetails = ModelState
                    .Where(ms => ms.Value.Errors.Count > 0)
                    .Select(ms => new
                    {
                        Key = ms.Key,
                        Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToArray(),
                        AttemptedValue = Request?.Form?[ms.Key].FirstOrDefault()
                    })
                    .ToArray();

                _logger.LogWarning("{Action} ModelState invalid for ExternalPlatform. Model: {@Model}. Details: {@Details}",
                    actionName, model, modelStateDetails);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to log ModelState errors for action {Action}", actionName);
            }
        }

        private static string GetInnermostMessage(Exception ex)
        {
            if (ex == null) return string.Empty;
            while (ex.InnerException != null) ex = ex.InnerException;
            return $"{ex.Message} ({ex.GetType().Name})";
        }
    }
}