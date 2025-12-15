using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProHub.Models;
using PROHUB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;

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
        
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new ExternalPlatform());
        }

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
                TempData["ErrorMessage"] = "An internal error occurred.";
                return View(model);
            }
        }

        
        //  EDIT
        
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0) return BadRequest();

            try
            {
                var model = await _service.GetByIdAsync(id);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Solution not found.";
                    return RedirectToAction(nameof(Index));
                }

                await PopulateDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit form for ID {Id}", id);
                TempData["ErrorMessage"] = "Error loading form.";
                return RedirectToAction(nameof(Index));
            }
        }

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
                var success = await _service.UpdateAsync(model);
                if (success)
                {
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
        public async Task<IActionResult> ViewDetails(int id)
        {
            var model = await _service.GetByIdAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

     
        //  EXPORT TO EXCEL
        
        [HttpGet]
        public async Task<IActionResult> ExportAllToExcel()
        {
            try
            {
                var dt = await _service.GetProspectiveExportDataAsync();

                ExcelPackage.License.SetNonCommercialOrganization("ProHub");
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Prospective Solutions");

                ws.Cells[1, 1].LoadFromDataTable(dt, true);

                using (var rng = ws.Cells[1, 1, 1, dt.Columns.Count])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    rng.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                byte[] fileBytes = package.GetAsByteArray();
                string fileName = $"External_Solutions_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
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