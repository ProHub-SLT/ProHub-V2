// Controllers/ExternalIssueController.cs
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
    public class ExternalIssueController : Controller
    {
        private readonly IExternalIssueService _externalIssueService;
        private readonly EmployeeRepository _empRepo;

        public ExternalIssueController(IExternalIssueService externalIssueService, EmployeeRepository empRepo)
        {
            _externalIssueService = externalIssueService;
            _empRepo = empRepo;
        }

        // ===================================================================
        // INDEX + LIST – Everyone can view (with search & pagination)
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> Index(string search, int page = 1, int pageSize = 10)
            => await LoadIssuesAsync(search, page, pageSize);

        [HttpGet]
        public async Task<IActionResult> List() => RedirectToAction(nameof(Index));

        private async Task<IActionResult> LoadIssuesAsync(string search, int page, int pageSize)
        {
            try
            {
                IEnumerable<ExternalIssue> allIssues = string.IsNullOrEmpty(search)
                    ? await _externalIssueService.GetAllAsync()
                    : await _externalIssueService.SearchAsync(search);

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
                TempData["ErrorMessage"] = "Failed to load external issues.";
                return View("List", new List<ExternalIssue>());
            }
        }

        // ===================================================================
        // DETAILS – Everyone
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue) return NotFound();
            var issue = await _externalIssueService.GetByIdAsync(id.Value);
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
            var model = new ExternalIssue
            {
                EnteredTime = DateTime.Now,
                EnteredBy = GetCurrentEmployeeId() // Auto-fill owner
            };
            return View(model);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExternalIssue model)
        {
            // Force the creator to be the current user
            model.EnteredBy = GetCurrentEmployeeId();

            if (ModelState.IsValid)
            {
                var newId = await _externalIssueService.CreateAsync(model);
                if (newId > 0)
                {
                    TempData["SuccessMessage"] = $"External Issue E{newId:D6} created successfully!";
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
            if (!id.HasValue) return NotFound();

            var issue = await _externalIssueService.GetByIdAsync(id.Value);
            if (issue == null) return NotFound();

            if (!IsOwnerOrAdmin(issue.EnteredBy))
                return Forbid(); // 403 – not allowed

            await LoadDropdownDataAsync();
            return View(issue);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExternalIssue model)
        {
            if (id != model.Id) return BadRequest();

            var existing = await _externalIssueService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            if (!IsOwnerOrAdmin(existing.EnteredBy))
                return Forbid();

            if (ModelState.IsValid)
            {
                var success = await _externalIssueService.UpdateAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Issue {model.FormattedId} updated!";
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        public async Task<IActionResult> Delete(int id)
        {
            var issue = await _externalIssueService.GetByIdAsync(id);
            if (issue == null)
            {
                TempData["ErrorMessage"] = "Issue not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!IsOwnerOrAdmin(issue.EnteredBy))
                return Forbid();

            var success = await _externalIssueService.DeleteAsync(id);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? $"Issue {issue.FormattedId} deleted!" : "Delete failed.";

            return RedirectToAction(nameof(Index));
        }

        // ===================================================================
        // HELPER METHODS
        // ===================================================================
        private async Task LoadDropdownDataAsync()
        {
            ViewBag.Platforms = await _externalIssueService.GetExternalPlatformsAsync();
            ViewBag.Employees = await _externalIssueService.GetEmployeesAsync();
            ViewBag.CustomerContacts = await _externalIssueService.GetCustomerContactsAsync();
        }

        private int? GetCurrentEmployeeId()
        {
            var idStr = User.FindFirst("EmployeeId")?.Value;
            return int.TryParse(idStr, out int id) ? id : null;
        }

        private bool IsOwnerOrAdmin(int? recordOwnerId)
        {
            if (!recordOwnerId.HasValue) return false;
            if (User.IsInRole(AppRoles.Admin)) return true;

            var currentId = GetCurrentEmployeeId();
            return currentId.HasValue && currentId.Value == recordOwnerId.Value;
        }
    }
}