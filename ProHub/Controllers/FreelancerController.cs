using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProHub.Constants;
using PROHUB.Models;
using PROHUB.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PROHUB.Controllers
{
    public class FreelancerController : Controller
    {
        private readonly IFreelancerService _freelancerService;

        public FreelancerController(IFreelancerService freelancerService)
        {
            _freelancerService = freelancerService ?? throw new ArgumentNullException(nameof(freelancerService));
        }

        // GET: /Freelancer/List?search=...&page=1&pageSize=10&sortColumn=Name&sortOrder=asc
        public async Task<IActionResult> List(string search, string sortColumn, string sortOrder, int page = 1, int pageSize = 10)
        {
            // 1. Validation & Defaults
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            // Default sorting if parameters are null
            sortColumn = string.IsNullOrEmpty(sortColumn) ? "FreelancerId" : sortColumn;
            sortOrder = string.IsNullOrEmpty(sortOrder) ? "asc" : sortOrder;

            var allFreelancers = await _freelancerService.GetAllFreelancersAsync();

            // 2. Search / Filter
            // Changed to IEnumerable here so we don't execute .ToList() until after sorting
            var query = string.IsNullOrWhiteSpace(search)
                ? allFreelancers
                : allFreelancers.Where(f =>
                    (f.Name ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (f.FormattedFreelancerId ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
                    || f.FreelancerId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (f.NIC ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (f.ProjectName ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (f.Tasks != null && f.Tasks.Any(t => (t.TaskName ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)))
                );

            // 3. Sorting Logic (Matches the View's <th> tags)
            Func<PROHUB.Models.Freelancer, object> sortSelector = sortColumn.ToUpper() switch
            {
                "NAME" => f => f.Name,
                "NIC" => f => f.NIC,
                "PROJECT NAME" => f => f.ProjectName,
                "START DATE" => f => f.StartDate,
                "END DATE" => f => f.EndDate,
                "DURATION" => f => f.Duration,
                _ => f => f.FreelancerId // Default to ID
            };

            if (sortOrder == "desc")
            {
                query = query.OrderByDescending(sortSelector);
            }
            else
            {
                query = query.OrderBy(sortSelector);
            }

            // 4. Materialize list to calculate totals
            var filteredList = query.ToList();

            var totalEntries = filteredList.Count;
            var totalPages = (int)Math.Ceiling(totalEntries / (double)pageSize);

            if (page > totalPages && totalPages > 0) page = totalPages;

            // 5. Pagination
            var paged = filteredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 6. Populate ViewData
            ViewData["SearchTerm"] = search ?? "";
            ViewData["PageSize"] = pageSize;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages == 0 ? 1 : totalPages;
            ViewData["TotalEntries"] = totalEntries;

            // IMPORTANT: Pass these back so the View knows which arrow to highlight
            ViewData["SortColumn"] = sortColumn;
            ViewData["SortOrder"] = sortOrder;

            return View(paged);
        }

        // Details (simple pass-through)
        public async Task<IActionResult> Details(int freelancerId)
        {
            var freelancer = await _freelancerService.GetFreelancerByIdAsync(freelancerId);
            if (freelancer == null) return NotFound();
            return View(freelancer);
        }

        // Edit (GET)
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        public async Task<IActionResult> Edit(int freelancerId)
        {
            var freelancer = await _freelancerService.GetFreelancerByIdAsync(freelancerId);
            if (freelancer == null) return NotFound();
            return View(freelancer);
        }

        // Edit (POST)
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Freelancer model)
        {
            if (!ModelState.IsValid) return View(model);

            var success = await _freelancerService.UpdateFreelancerAsync(model);
            if (success)
            {
                TempData["SuccessMessage"] = $"Freelancer {model.FormattedFreelancerId} updated successfully.";
                return RedirectToAction(nameof(List));
            }

            TempData["ErrorMessage"] = "Failed to update freelancer.";
            return View(model);
        }

        // Create (GET)
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        public IActionResult Create()
        {
            return View(new Freelancer());
        }

        // Create (POST)
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Freelancer model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var newId = await _freelancerService.AddFreelancerAsync(model);
                if (newId > 0)
                {
                    var formatted = $"F{newId:D6}";
                    TempData["SuccessMessage"] = $"Freelancer {formatted} created successfully.";
                    return RedirectToAction(nameof(List));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create freelancer.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating freelancer: {ex}");
                TempData["ErrorMessage"] = "Error creating freelancer.";
                return View(model);
            }
        }

        // DELETE - supports both normal POST and AJAX (X-Requested-With header)
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int freelancerId)
        {
            if (freelancerId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid freelancer id.";
                return RedirectToAction(nameof(List));
            }

            try
            {
                var deleted = await _freelancerService.DeleteFreelancerAsync(freelancerId);
                if (deleted)
                {
                    var formatted = $"F{freelancerId:D6}";
                    TempData["SuccessMessage"] = $"Freelancer {formatted} deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Freelancer not found or could not be deleted.";
                }

                if (Request.Headers != null && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = deleted, redirectUrl = Url.Action(nameof(List)) });
                }

                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                TempData["ErrorMessage"] = "Error deleting freelancer.";

                if (Request.Headers != null && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, redirectUrl = Url.Action(nameof(List)) });
                }

                return RedirectToAction(nameof(List));
            }
        }
    }
}
