using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        public async Task<IActionResult> Index()
        {
            try
            {
                var solutions = await _externalSolutionService.GetAllAsync();
                return View(solutions ?? new List<ExternalPlatform>());
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
            // populate dropdowns (so view can re-render)
            await PopulateDropdowns();

            if (externalSolution == null)
            {
                TempData["ErrorMessage"] = "Invalid form submission.";
                return View(new ExternalPlatform());
            }

            // set defaults for string properties that might be null and would otherwise cause issues
            DefaultMissingProperties(externalSolution);

            // Remove modelstate keys that are not provided by the create form and are OK to be left null/empty
            RemoveDbOnlyModelStateKeys();

            // If modelstate still invalid, log and return to view
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
                    // Keep user on the Create page and show a success popup via TempData
                    TempData["SuccessMessage"] = $"External Solution Created.";

                    // clear ModelState so the form appears fresh/empty
                    ModelState.Clear();

                    // return a fresh model to the view
                    var freshModel = new ExternalPlatform();

                    // re-populate dropdowns (PopulateDropdowns uses service calls)
                    await PopulateDropdowns();

                    return View(freshModel);
                }

                _logger.LogWarning("Create returned non-positive id ({NewId}) for model {@Model}", newId, externalSolution);
                ModelState.AddModelError(string.Empty, "Create returned an unexpected result. Operation not completed.");
                TempData["ErrorMessage"] = "Create returned unexpected result.";
                return View(externalSolution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ExternalPlatform. Model: {@Model}", externalSolution);
                ModelState.AddModelError(string.Empty, $"An error occurred while creating the external solution: {GetInnermostMessage(ex)}");
                TempData["ErrorMessage"] = "An internal error occurred while creating the external solution. Check server logs for details.";
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
                TempData["ErrorMessage"] = "An error occurred while updating. See server logs.";
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
                TempData["ErrorMessage"] = "An error occurred while deleting. See logs.";
            }

            return RedirectToAction(nameof(Index));
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

            // Strings that your DB or later logic may expect non-null
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

            // numeric/foreign keys: keep as-is (nullable)
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

                // The fields you reported as causing validation errors:
                nameof(ExternalPlatform.DevelopedById),    // DevelopedBy
                nameof(ExternalPlatform.SDLCStageId),      // SDLCStage
                nameof(ExternalPlatform.Status),           // Status (string)
                nameof(ExternalPlatform.IntegratedApps),   // IntegratedApps
                nameof(ExternalPlatform.DR),               // DR
                nameof(ExternalPlatform.CompanyId),        // Company
                nameof(ExternalPlatform.SalesTeamId),      // SalesTeam
                nameof(ExternalPlatform.BackupOfficer1Id), // BackupOfficer1 (may not exist in DB)
                nameof(ExternalPlatform.BackupOfficer2Id)  // BackupOfficer2 (may not exist in DB)
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
