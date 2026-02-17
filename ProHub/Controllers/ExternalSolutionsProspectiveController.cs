using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using ProHub.Constants;
using ProHub.Models;
using PROHUB.Data;
using System;
using System.Collections.Generic;
using System.Data; 
using System.Linq;
using System.Threading.Tasks;

namespace ProHub.Controllers
{
    public class ExternalSolutionsProspectiveController : Controller
    {
        private readonly IExternalSolutionsProspectiveService _service;
        private readonly ILogger<ExternalSolutionsProspectiveController> _logger;

        public ExternalSolutionsProspectiveController(IExternalSolutionsProspectiveService service, ILogger<ExternalSolutionsProspectiveController> logger)
        {
            _service = service;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        //  LISTS (Index & InProgress)


        [HttpGet]
        public async Task<IActionResult> Index(string search = "", string sortColumn = "PlatformName", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            List<ExternalPlatform> list = await _service.GetProspectiveSolutionsAsync(search ?? "");
            var pagedModel = ProcessList(list, search, sortColumn, sortOrder, page, pageSize);
            return View(pagedModel);
        }

        [HttpGet]
        public async Task<IActionResult> InProgress(string search = "", string sortColumn = "PlatformName", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            List<ExternalPlatform> list = await _service.GetInProgressSolutionsAsync(search ?? "");
            var pagedModel = ProcessList(list, search, sortColumn, sortOrder, page, pageSize);

            ViewBag.Mode = "InProgress";
            return View("Index", pagedModel);
        }


        //  CREATE

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new ExternalPlatform());
        }


        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExternalPlatform model)
        {
            await PopulateDropdowns();


            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid form submission.";
                return View(new ExternalPlatform());
            }


            DefaultMissingProperties(model);



            try
            {
                var newId = await _service.CreateAsync(model);
                if (newId > 0)
                {
                    TempData["SuccessMessage"] = "Solution Created Successfully.";
                    ModelState.Clear();
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("Create returned non-positive id ({NewId})", newId);
                TempData["ErrorMessage"] = "Create returned unexpected result.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating solution.");
                ModelState.AddModelError(string.Empty, $"An error occurred: {GetInnermostMessage(ex)}");
                TempData["ErrorMessage"] = $"Error: {GetInnermostMessage(ex)}";
                return View(model);
            }
        }


        //  EDIT

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Edit action called with ID: {Id}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Invalid ID received: {Id}", id);
                TempData["ErrorMessage"] = "Invalid solution ID.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var model = await _service.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("Solution not found for ID: {Id}", id);
                    TempData["ErrorMessage"] = "Solution not found.";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit form for ID {Id}", id);
                TempData["ErrorMessage"] = $"Error loading form: {ex.Message}. Please try again or contact support.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExternalPlatform model)
        {
            await PopulateDropdowns();

            if (model == null || model.Id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid model.";
                return RedirectToAction(nameof(Index));
            }

            DefaultMissingProperties(model);

            try
            {
                // 1.Main Data updated 
                var success = await _service.UpdateAsync(model);

                if (success)
                {
                    // 2. Save a comment if there is one.
                    if (!string.IsNullOrWhiteSpace(model.Comment)) 
                    {
                        
                        int? updatedById = model.DevelopedById;

                        
                        if (updatedById == 0) updatedById = null;

                        await _service.AddCommentAsync(model.Id, model.Comment, updatedById);
                    }

                    TempData["SuccessMessage"] = "Solution updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["ErrorMessage"] = "Update failed. Record may not exist.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating solution.");
                ModelState.AddModelError(string.Empty, $"Error: {GetInnermostMessage(ex)}");
                return View(model);
            }
        }


        //  DELETE
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.DPO}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest();

            bool success = await _service.DeleteAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Solution successfully deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete solution. It may not exist.";
            }

            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && referer.Contains("InProgress"))
            {
                return RedirectToAction(nameof(InProgress));
            }
            return RedirectToAction(nameof(Index));
        }


        //  VIEW DETAILS

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var model = await _service.GetByIdAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }


        // Get comment 
        [HttpGet]
        public async Task<IActionResult> GetComments(int id) 
        {
            if (id <= 0) return Json(new { success = false, message = "Invalid ID" });

            try
            {
                var commentsList = await _service.GetCommentsAsync(id);

                if (commentsList == null || !commentsList.Any())
                {
                    return Json(new { success = true, hasData = false, message = "No comments found." });
                }

                // Return the list of comments
                return Json(new
                {
                    success = true,
                    hasData = true,
                    comments = commentsList.Select(c => new {
                        comment = c.Comment,
                        updatedBy = c.UpdatedByName,
                        updatedTime = c.UpdatedTime.ToString("MMM dd, yyyy , hh:mm tt")
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comments for ID {Id}", id);
                return Json(new { success = false, message = "Error loading comments." });
            }
        }

        //  EXPORT TO EXCEL

        [HttpGet]
        public async Task<IActionResult> ExportAllToExcel()
        {
            try
            {
                // 1. Get raw data (Now includes names from the updated SQL)
                var dt = await _service.GetProspectiveExportDataAsync();

                // 2. Process data
                var exportList = dt.Rows.Cast<DataRow>().Select(row =>
                {
                    // --- Helper Functions ---
                    decimal GetDecimal(string colName) =>
                        row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value
                        ? Convert.ToDecimal(row[colName]) : 0m;

                    string GetString(string colName) =>
                        row.Table.Columns.Contains(colName) ? row[colName]?.ToString() : "";

                    string GetDate(string colName) =>
                        row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value
                        ? Convert.ToDateTime(row[colName]).ToString("yyyy-MM-dd") : "";

                    // --- Calculations ---
                    decimal otc = GetDecimal("Platform_OTC");
                    decimal mrc = GetDecimal("Platform_MRC");

                    decimal contractPeriod = 0;
                    decimal.TryParse(GetString("Contract_Period"), out contractPeriod);

                    decimal revenue = otc + (mrc * 12 * contractPeriod);

                    // --- Return Object ---
                    return new
                    {
                        PlatformName = GetString("Platform_Name"),
                        CompanyName = GetString("Company_Name"),
                        DevelopedBy = GetString("Developed_By_Name"),
                        DevelopedTeam = GetString("Developed_Team"),
                        SalesTeam = GetString("Sales_Team_Name"),
                        SDLCStage = GetString("SDLC_Phase"),
                        LaunchedDate = GetDate("LaunchedDate"),
                        BilledDate = GetDate("BillingDate"),
                        OTC = otc,
                        MRC = mrc,
                        ContractPeriod = GetString("Contract_Period"),
                        IncentiveEarned = GetDecimal("Incentive_Earned"),
                        IncentiveSharedWith = GetDecimal("Incentive_Share"),
                        ProposalUploaded = GetString("Proposal_Upload"),
                        Revenue = revenue,
                        SoftwareValue = GetDecimal("Software_Value"),
                        DPOHandoverDate = GetDate("DPO_Handover_Date")
                    };
                }).ToList();

                // 3. Generate Excel
                ExcelPackage.License.SetNonCommercialOrganization("ProHub");
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Prospective Solutions");

                ws.Cells[1, 1].LoadFromCollection(exportList, true);

                // 4. Headers
                var headers = new string[]
                {
            "Platform Name", "Company Name", "Developed By", "Developed Team",
            "Sales Team Involved", "SDLC Stage", "Launched Date", "Billed Date",
            "One Time Charge (OTC)", "Monthly Charge (MRC)", "Contract Period",
            "Incentive Earned", "Incentive Shared With", "Proposal Uploaded",
            "Revenue", "Software Value", "DPO Handover Date"
                };

                for (int i = 0; i < headers.Length; i++) ws.Cells[1, i + 1].Value = headers[i];

                // 5. Styling
                using (var rng = ws.Cells[1, 1, 1, headers.Length])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    rng.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                // Format Currency Columns: 9, 10, 12, 13, 15, 16
                int[] currencyCols = { 9, 10, 12, 13, 15, 16 };
                foreach (var col in currencyCols)
                {
                    ws.Column(col).Style.Numberformat.Format = "#,##0.00";
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                byte[] fileBytes = package.GetAsByteArray();
                string fileName = $"External Solutions - Prospective_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed");
                return BadRequest("Error exporting data: " + ex.Message);
            }
        }


        //  HELPERS & UTILS

        private List<ExternalPlatform> ProcessList(List<ExternalPlatform> list, string search, string sortColumn, string sortOrder, int page, int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Max(pageSize, 1);

            sortColumn = sortColumn?.Trim() ?? "PlatformName";
            sortOrder = sortOrder?.ToLowerInvariant() == "desc" ? "desc" : "asc";

            Func<ExternalPlatform, object> keySelector = sortColumn.ToLowerInvariant() switch
            {
                "platformname" => x => x.PlatformName ?? "",
                "developedby" => x => x.DevelopedBy?.EmpName ?? "",
                "company" => x => x.Company?.CompanyName ?? "",
                _ => x => x.PlatformName ?? ""
            };

            var sorted = sortOrder == "asc"
                ? list.OrderBy(keySelector)
                : list.OrderByDescending(keySelector);

            int total = sorted.Count();
            var pageItems = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.SearchTerm = search ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return pageItems;
        }

        private async Task PopulateDropdowns()
        {
            try
            {
                ViewBag.Companies = await _service.GetCompanyAsync() ?? new List<Company>();
                ViewBag.Employees = await _service.GetEmployeesAsync() ?? new List<Employee>();
                ViewBag.SalesTeams = await _service.Getsales_teamAsync() ?? new List<SalesTeam>();
                ViewBag.SdlcPhases = await _service.GetsdlcphasAsync() ?? new List<SDLCPhase>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate dropdowns.");
                ViewBag.Companies = new List<Company>();
                ViewBag.Employees = new List<Employee>();
                ViewBag.SalesTeams = new List<SalesTeam>();
                ViewBag.SdlcPhases = new List<SDLCPhase>();
            }
        }

        private void DefaultMissingProperties(ExternalPlatform model)
        {
            if (model == null) return;
            model.PlatformType ??= string.Empty;
            model.PlatformName ??= string.Empty;
            model.BitBucket ??= string.Empty;
            model.BITBucketRepo ??= string.Empty;
            model.DevelopedTeam ??= string.Empty;
            model.SalesAM ??= string.Empty;
            model.SalesManager ??= string.Empty;
            model.SalesEngineer ??= string.Empty;
            model.PlatformOwner ??= string.Empty;
            model.APP_Owner ??= string.Empty;
            model.ProposalUploaded ??= string.Empty;
            model.ContractPeriod ??= string.Empty;
            model.SLA ??= string.Empty;
            model.DPOHandoverComment ??= string.Empty;
            model.IntegratedApps ??= string.Empty;
            model.DR ??= string.Empty;
            model.Status ??= string.Empty;
        }

        private static string GetInnermostMessage(Exception ex)
        {
            while (ex.InnerException != null) ex = ex.InnerException;
            return ex.Message;
        }
    }
}