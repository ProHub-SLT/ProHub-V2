using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProHub.Constants;
using ProHub.Models;
using PROHUB.Data;
using PROHUB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PROHUB.Controllers
{
    [Authorize] // 🔐 Everyone must be logged in
    public class InternalSolutionsActivitiesController : Controller
    {
        private readonly IInternalSolutionsActivitiesService _activityService;

        public InternalSolutionsActivitiesController(
            IInternalSolutionsActivitiesService activityService)
        {
            _activityService = activityService;
        }

        // =============================================
        // 1️⃣ INDEX – Everyone can view
        // =============================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string search,
            string sortColumn = "CreatedTime",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                int? internalSolutionId = null;

                var platforms = await _activityService.GetMainPlatformsAsync();
                var platform = platforms.FirstOrDefault(p =>
                    p.Platforms != null &&
                    p.Platforms.Contains("Internal", StringComparison.OrdinalIgnoreCase));

                if (platform != null)
                    internalSolutionId = platform.ID;

                var allActivities = await _activityService.GetAllAsync(
                    search,
                    sortColumn,
                    sortOrder,
                    filterPlatformId: internalSolutionId);

                int totalCount = allActivities.Count;
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                page = totalCount == 0 ? 1 : Math.Clamp(page, 1, totalPages);

                var paged = allActivities
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewData["Search"] = search;
                ViewData["SortColumn"] = sortColumn;
                ViewData["SortOrder"] = sortOrder;
                ViewData["PageSize"] = pageSize;
                ViewData["CurrentPage"] = page;
                ViewData["TotalPages"] = totalPages;
                ViewData["TotalEntries"] = totalCount;

                return View(paged);
            }
            catch
            {
                TempData["ErrorMessage"] = "Error loading activities.";
                return View(new List<ProjectActivity>());
            }
        }

        // =============================================
        // 2️⃣ DETAILS – Everyone can view
        // =============================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var activity = await _activityService.GetByIdAsync(id);
            if (activity == null)
            {
                TempData["ErrorMessage"] = "Activity not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(activity);
        }

        // =============================================
        // 3️⃣ CREATE – Admin, Developer, NonDeveloper
        // =============================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownDataAsync();

            var model = new ProjectActivity
            {
                CreatedTime = DateTime.Now
            };

            SetCurrentUser(model, isCreate: true);
            AutoSelectInternalPlatform(model);

            return View(model);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectActivity model)
        {
            model.CreatedTime = DateTime.Now;
            SetCurrentUser(model, isCreate: true);

            if (model.PlatformId == 0)
                ModelState.AddModelError("PlatformId", "Invalid platform selected.");

            if (ModelState.IsValid)
            {
                await _activityService.CreateAsync(model);
                TempData["SuccessMessage"] = "Activity created successfully!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownDataAsync();
            return View(model);
        }

        // =============================================
        // 4️⃣ EDIT – Admin, Developer, NonDeveloper
        // =============================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var activity = await _activityService.GetByIdAsync(id);
            if (activity == null)
                return NotFound();

            await LoadDropdownDataAsync();
            SetCurrentUser(activity, isCreate: false);

            return View(activity);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectActivity model)
        {
            var existing = await _activityService.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            model.UpdatedDate = DateTime.Now;

            if (ModelState.IsValid)
            {
                await _activityService.UpdateAsync(model);

                if (!string.IsNullOrWhiteSpace(model.LatestComment))
                {
                    await _activityService.AddCommentAsync(
                        model.Id,
                        model.LatestComment,
                        model.UpdatedBy);
                }

                TempData["SuccessMessage"] = "Activity updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownDataAsync();
            return View(model);
        }

        // =============================================
        // 5️⃣ DELETE – Admin, Developer, NonDeveloper
        // =============================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var activity = await _activityService.GetByIdAsync(id);
            if (activity == null)
                return NotFound();

            await _activityService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Activity deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // =============================================
        // 🔧 HELPERS
        // =============================================
        private async Task LoadDropdownDataAsync()
        {
            ViewBag.Platforms = await _activityService.GetMainPlatformsAsync();
            ViewBag.Employees = await _activityService.GetEmployeesAsync();
            ViewBag.InternalSolutions = await _activityService.GetInternalSolutionsAsync();
        }

        private void AutoSelectInternalPlatform(ProjectActivity model)
        {
            if (ViewBag.Platforms is List<MainPlatform> platforms)
            {
                var internalPlatform = platforms.FirstOrDefault(p =>
                    p.Platforms != null &&
                    p.Platforms.Contains("Internal", StringComparison.OrdinalIgnoreCase));

                if (internalPlatform != null)
                    model.PlatformId = internalPlatform.ID;
            }
        }

        private void SetCurrentUser(ProjectActivity model, bool isCreate)
        {
            if (!User.Identity.IsAuthenticated)
                return;

            var email = User.FindFirst("preferred_username")?.Value
                        ?? User.FindFirst("upn")?.Value;

            if (string.IsNullOrEmpty(email))
                return;

            var employee = _activityService.GetEmployeeByEmailAsync(email).Result;
            if (employee == null)
                return;

            if (isCreate)
                model.CreatedBy = employee.EmpId;
            else
                model.UpdatedBy = employee.EmpId;
        }
    }
}
