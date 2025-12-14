using Microsoft.AspNetCore.Mvc;
using ProHub.Models;
using PROHUB.Data;
using PROHUB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PROHUB.Controllers
{
    public class ExternalSolutionsActivitiesController : Controller
    {
        private readonly IExternalSolutionsActivitiesService _activityService;

        public ExternalSolutionsActivitiesController(IExternalSolutionsActivitiesService activityService)
        {
            _activityService = activityService;
        }

        // ✅ GET: ExternalSolutionsActivities/Index

        [HttpGet]
        public async Task<IActionResult> Index(string search, string sortColumn = "CreatedTime", string sortOrder = "desc", int page = 1, int pageSize = 10)
        {

            var allActivities = await _activityService.GetAllAsync(search, sortColumn, sortOrder, filterPlatformId: 2);

            Console.WriteLine($"Found {allActivities.Count} records.");

            var totalItemCount = allActivities.Count;
            var totalPages = (int)Math.Ceiling(totalItemCount / (double)pageSize);

            if (totalItemCount > 0)
            {
                page = Math.Max(1, Math.Min(page, totalPages));
            }
            else
            {
                page = 1;
            }

            var pagedActivities = allActivities.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewData["Search"] = search;
            ViewData["SortColumn"] = sortColumn;
            ViewData["SortOrder"] = sortOrder;
            ViewData["PageSize"] = pageSize;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalEntries"] = totalItemCount;

            return View(pagedActivities);
        }
        // ✅ POST: ExternalSolutionsActivities/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int Activity_ID, string Comment)
        {
            try
            {
                if (Activity_ID > 0 && !string.IsNullOrWhiteSpace(Comment))
                {
                    // You might want to get the actual logged-in user ID here
                    int? currentUserId = 1;
                    await _activityService.AddCommentAsync(Activity_ID, Comment, currentUserId);
                    TempData["SuccessMessage"] = "Comment added successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid comment data.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding comment: {ex.Message}");
                TempData["ErrorMessage"] = "Error saving comment.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ GET: ExternalSolutionsActivities/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var activity = await _activityService.GetByIdAsync(id);
                if (activity == null)
                {
                    TempData["ErrorMessage"] = "Activity not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(activity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading details for {id}: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading activity details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ✅ GET: ExternalSolutionsActivities/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                await LoadDropdownDataAsync();

                var model = new ProjectActivity
                {
                    CreatedTime = DateTime.Now
                };

                if (ViewBag.Platforms is List<MainPlatform> platforms)
                {
                    var externalSolution = platforms.FirstOrDefault(p => p.Platforms != null && p.Platforms.Contains("External", StringComparison.OrdinalIgnoreCase));
                    if (externalSolution != null)
                    {
                        model.PlatformId = externalSolution.ID;
                    }
                }

                // Logic: Auto-select current user for CreatedBy
                if (User.Identity.IsAuthenticated)
                {
                    var email = User.FindFirst("preferred_username")?.Value ?? User.FindFirst("upn")?.Value;
                    if (!string.IsNullOrEmpty(email))
                    {
                        var employee = await _activityService.GetEmployeeByEmailAsync(email);
                        if (employee != null)
                        {
                            model.CreatedBy = employee.EmpId;
                        }
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading form: {ex.Message}");
                TempData["ErrorMessage"] = $"Error loading form: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ✅ POST: ExternalSolutionsActivities/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectActivity model)
        {
            try
            {
                model.CreatedTime = DateTime.Now;

                if (ModelState.IsValid)
                {
                    int newId = await _activityService.CreateAsync(model);
                    if (newId > 0)
                    {
                        TempData["SuccessMessage"] = "Activity created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to create activity.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please check the form for errors.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating activity: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while saving.";
            }

            await LoadDropdownDataAsync();
            return View(model);
        }

        // ✅ GET: ExternalSolutionsActivities/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var activity = await _activityService.GetByIdAsync(id);
                if (activity == null)
                {
                    TempData["ErrorMessage"] = "Activity not found.";
                    return RedirectToAction(nameof(Index));
                }

                await LoadDropdownDataAsync();

                // Logic: Auto-select current user for UpdatedBy
                if (User.Identity.IsAuthenticated)
                {
                    var email = User.FindFirst("preferred_username")?.Value ?? User.FindFirst("upn")?.Value;
                    if (!string.IsNullOrEmpty(email))
                    {
                        var employee = await _activityService.GetEmployeeByEmailAsync(email);
                        if (employee != null)
                        {
                            // Default the UpdatedBy dropdown to the current user
                            activity.UpdatedBy = employee.EmpId;
                        }
                    }
                }

                return View(activity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading edit for {id}: {ex.Message}");
                TempData["ErrorMessage"] = "Error loading activity.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ✅ POST: ExternalSolutionsActivities/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectActivity model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "ID Mismatch.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (ModelState.IsValid)
                {
                    model.UpdatedDate = DateTime.Now;

                    bool success = await _activityService.UpdateAsync(model);

                    if (success && !string.IsNullOrWhiteSpace(model.LatestComment))
                    {
                        await _activityService.AddCommentAsync(model.Id, model.LatestComment, model.UpdatedBy);
                    }

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Activity updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update activity.";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating activity {id}: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while updating.";
            }

            await LoadDropdownDataAsync();
            return View(model);
        }

        // ✅ POST: ExternalSolutionsActivities/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                bool success = await _activityService.DeleteAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Activity deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete activity.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting activity {id}: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting record.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ Helper: Load Dropdowns
        private async Task LoadDropdownDataAsync()
        {
            try
            {
                ViewBag.Platforms = await _activityService.GetMainPlatformsAsync();
                ViewBag.Employees = await _activityService.GetEmployeesAsync();
                ViewBag.ExternalSolutions = await _activityService.GetExternalSolutionsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dropdowns: {ex.Message}");
                ViewBag.Platforms = new List<MainPlatform>();
                ViewBag.Employees = new List<Employee>();
                ViewBag.ExternalSolutions = new List<ExternalPlatform>();

                TempData["ErrorMessage"] = "Error loading dropdown data. Some options may not be available.";
            }
        }
    }
}