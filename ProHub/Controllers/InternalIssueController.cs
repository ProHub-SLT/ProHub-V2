// Controllers/InternalIssueController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProHub.Constants;                    // AppRoles.Admin, AppRoles.Developer
using ProHub.Data;
using PROHUB.Data;
using PROHUB.Models;
using System.Security.Claims;

namespace PROHUB.Controllers
{
    [Authorize] // Everyone must be logged in
    public class InternalIssueController : Controller
    {
        private readonly IInternalIssueService _internalIssueService;
        private readonly ProHub.Data.EmployeeRepository _empRepo;

        public InternalIssueController(IInternalIssueService internalIssueService, EmployeeRepository empRepo)
        {
            _internalIssueService = internalIssueService;
            _empRepo = empRepo;
        }

        // ===================================================================
        // INDEX + LIST – Everyone can view (with search & pagination)
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 10)
            => await LoadIssuesAsync(search, page, pageSize);

        [HttpGet]
        public IActionResult List() => RedirectToAction(nameof(Index));

        private async Task<IActionResult> LoadIssuesAsync(string search, int page, int pageSize)
        {
            try
            {
                var allIssues = string.IsNullOrEmpty(search)
                    ? await _internalIssueService.GetAllAsync()
                    : await _internalIssueService.SearchAsync(search);

                ViewData["SearchTerm"] = search ?? "";

                var totalCount = allIssues.Count();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

                var pagedIssues = allIssues
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewData["CurrentPage"] = page;
                ViewData["TotalPages"] = totalPages;
                ViewData["PageSize"] = pageSize;
                ViewData["TotalEntries"] = totalCount;

                return View("List", pagedIssues);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to load internal issues.";
                return View("List", new List<InternalIssue>());
            }
        }

        // ===================================================================
        // DETAILS – Everyone can view
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue || id <= 0) return NotFound();

            var issue = await _internalIssueService.GetByIdAsync(id.Value);
            return issue == null ? NotFound() : View(issue);
        }

        // ===================================================================
        // CREATE – Only Admin + Developer
        // ===================================================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownDataAsync();
            var model = new InternalIssue
            {
                EnteredTime = DateTime.Now,
                EnteredBy = GetCurrentEmployeeId() // Auto-set creator
            };
            return View(model);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InternalIssue model)
        {
            // Force current user as creator
            model.EnteredBy = GetCurrentEmployeeId();

            if (ModelState.IsValid)
            {
                var newId = await _internalIssueService.CreateAsync(model);
                if (newId > 0)
                {
                    TempData["SuccessMessage"] = $"Internal Issue I{newId:D6} created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "Failed to create issue.";
            }

            await LoadDropdownDataAsync();
            return View(model);
        }

        // ===================================================================
        // EDIT – Only Admin OR the person who entered it (Entered_By)
        // ===================================================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue || id <= 0) return NotFound();

            var issue = await _internalIssueService.GetByIdAsync(id.Value);
            if (issue == null) return NotFound();

            if (!IsOwnerOrAdmin(issue.EnteredBy))
                return Forbid(); // 403

            await LoadDropdownDataAsync();
            return View(issue);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InternalIssue model)
        {
            if (id != model.Id) return BadRequest();

            var existing = await _internalIssueService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (!IsOwnerOrAdmin(existing.EnteredBy))
                return Forbid();

            if (ModelState.IsValid)
            {
                var success = await _internalIssueService.UpdateAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Internal Issue {model.FormattedId} updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "Update failed.";
            }

            await LoadDropdownDataAsync();
            return View(model);
        }

        // ===================================================================
        // DELETE – Only Admin OR Owner
        // ===================================================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var issue = await _internalIssueService.GetByIdAsync(id);
            if (issue == null)
            {
                TempData["ErrorMessage"] = "Issue not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!IsOwnerOrAdmin(issue.EnteredBy))
                return Forbid();

            var success = await _internalIssueService.DeleteAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? $"Internal Issue {issue.FormattedId} deleted!" : "Delete failed (may be referenced).";

            return RedirectToAction(nameof(Index));
        }

        // ===================================================================
        // HELPER METHODS
        // ===================================================================
        private async Task LoadDropdownDataAsync()
        {
            ViewBag.InternalApps = await _internalIssueService.GetInternalPlatformsAsync();
            ViewBag.Employees = await _internalIssueService.GetEmployeesAsync();
        }

        private int? GetCurrentEmployeeId()
        {
            var claimValue = User.FindFirst("EmployeeId")?.Value;
            return int.TryParse(claimValue, out int id) ? id : null;
        }

        private bool IsOwnerOrAdmin(int? recordOwnerId)
        {
            if (!recordOwnerId.HasValue) return false;

            // Admin can do anything
            if (User.IsInRole(AppRoles.Admin)) return true;

            // Otherwise, must be the creator
            var currentId = GetCurrentEmployeeId();
            return currentId.HasValue && currentId.Value == recordOwnerId.Value;
        }
    }
}