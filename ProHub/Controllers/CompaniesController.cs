using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProHub.Constants;
using ProHub.Data;
using ProHub.Models;
using System;
using System.Linq;

namespace ProHub.Controllers
{
    public class CompaniesController : Controller
    {
        private readonly CompanyRepository _repo;

        public CompaniesController(CompanyRepository repo)
        {
            _repo = repo;
        }

        // GET: /Companies/
        public IActionResult Index(string search = "", int page = 1, int pageSize = 10, string sortColumn = "CompanyName", string sortOrder = "asc")
        {
            // 1. Fetch Data 
            var companyList = _repo.GetCompanies(search) ?? new List<Company>();

            // 2. Sorting Logic 

            switch (sortColumn)
            {
                case "CompanyName":
                    companyList = sortOrder == "asc" ? companyList.OrderBy(c => c.CompanyName).ToList()
                                                     : companyList.OrderByDescending(c => c.CompanyName).ToList();
                    break;

                default:
                    companyList = companyList.OrderBy(c => c.CompanyName).ToList();
                    break;
            }

            // 3. Pagination Logic
            var totalRecords = companyList.Count;
            var paginatedList = companyList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 4. Set ViewBag 
            ViewBag.SearchTerm = search ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = totalRecords > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 1;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(paginatedList);
        }

        // GET: /Companies/Create
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        public IActionResult Create()
        {
            return View();
        }

        // POST: /Companies/Create
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Company company)
        {
            if (!ModelState.IsValid)
            {
                return View(company);
            }

            try
            {
                _repo.CreateCompany(company);
                TempData["SuccessMessage"] = "Company Created Successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
            }

            return View(company);
        }

        // GET: /Companies/Edit/5
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        public IActionResult Edit(int id)
        {
            var company = _repo.GetCompanyById(id);
            if (company == null)
            {
                return NotFound();
            }
            return View(company);
        }

        // POST: /Companies/Edit/5
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Company company)
        {
            if (id != company.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(company);
            }

            try
            {
                _repo.UpdateCompany(company);

                TempData["SuccessMessage"] = "Company Updated Successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating: " + ex.Message);
            }

            return View(company);
        }

        // POST: /Companies/Delete/5
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                _repo.DeleteCompany(id);
                TempData["SuccessMessage"] = "Company Deleted Successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting record: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // NEW METHODS FOR DETAILS & CONTACTS
        // ==========================================

        // GET: /Companies/Details/5
        public IActionResult Details(int id)
        {
            var company = _repo.GetCompanyById(id);
            if (company == null)
            {
                return NotFound();
            }
            return View(company);
        }

        // GET: /Companies/GetContacts?companyId=5 (For Popup)
        [HttpGet]
        public IActionResult GetContacts(int companyId)
        {
            var contacts = _repo.GetContactsByCompanyId(companyId);
            return PartialView("_ContactList", contacts);
        }
    }
}