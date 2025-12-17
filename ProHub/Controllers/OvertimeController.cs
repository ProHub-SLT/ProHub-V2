// File: Controllers/OvertimeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProHub.Constants;
using ProHub.Data;
using ProHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProHub.Controllers
{
    [Authorize] // 🔐 All actions require login
    public class OvertimeController : Controller
    {
        private readonly OvertimeRepository _otRepo;
        private readonly EmployeeRepository _empRepo;

        public OvertimeController(
            OvertimeRepository otRepo,
            EmployeeRepository empRepo)
        {
            _otRepo = otRepo;
            _empRepo = empRepo;
        }

        // =============================================
        // 1️⃣ INDEX – Everyone can view
        // =============================================
        public IActionResult Index(string search = "", int page = 1, int pageSize = 10)
        {
            var data = _otRepo.GetAll(search, page, pageSize);
            int total = _otRepo.GetCount(search);

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalCount = total;

            return View(data);
        }

        // =============================================
        // 2️⃣ DETAILS – Everyone can view
        // =============================================
        public IActionResult Details(int id)
        {
            var overtime = _otRepo.GetById(id);
            if (overtime == null)
                return NotFound();

            return View(overtime);
        }

        // =============================================
        // 3️⃣ CREATE – Admin, Developer, NonDeveloper
        // =============================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        public IActionResult Create()
        {
            ViewBag.CreatedByName = GetAuthenticatedEmployeeName();
            ViewBag.AuthenticatedEmployeeId = GetAuthenticatedEmployeeId();
            LoadEmployeesDropdown();

            return View(new OvertimeRequest());
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OvertimeRequest model)
        {
            if (ModelState.IsValid)
            {
                model.Created_By = GetAuthenticatedEmployeeId();
                model.Created_Date = DateTime.Now;

                _otRepo.Insert(model);
                TempData["Success"] = "Overtime request created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CreatedByName = GetAuthenticatedEmployeeName();
            LoadEmployeesDropdown();
            return View(model);
        }

        // =============================================
        // 4️⃣ EDIT – Admin, Developer, NonDeveloper
        // =============================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        public IActionResult Edit(int id)
        {
            var overtime = _otRepo.GetById(id);
            if (overtime == null)
                return NotFound();

            LoadEmployeesDropdown();
            return View(overtime);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OvertimeRequest model)
        {
            var existing = _otRepo.GetById(model.ID);
            if (existing == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                _otRepo.Update(model);
                TempData["Success"] = "Overtime request updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            LoadEmployeesDropdown();
            return View(model);
        }

        // =============================================
        // 5️⃣ DELETE – Admin, Developer, NonDeveloper
        // =============================================
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer},{AppRoles.NonDeveloper}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var overtime = _otRepo.GetById(id);
            if (overtime == null)
                return Json(new { success = false, message = "Record not found" });

            _otRepo.Delete(id);
            return Json(new { success = true });
        }

        // =============================================
        // 🔧 HELPERS
        // =============================================
        private void LoadEmployeesDropdown()
        {
            var employeeIds = _empRepo.GetAllEmployeeIds();
            var employeeNames = _empRepo.GetNamesByIds(employeeIds);

            ViewBag.Employees = employeeNames
                .Select(e => new { Emp_ID = e.Key, Emp_Name = e.Value })
                .ToList();
        }

        private string GetAuthenticatedEmployeeName()
        {
            if (!User.Identity.IsAuthenticated)
                return "Unknown User";

            var email = User.FindFirst("preferred_username")?.Value
                        ?? User.FindFirst("upn")?.Value;

            return _empRepo.GetEmployeeNameByEmail(email) ?? "Unknown User";
        }

        private int? GetAuthenticatedEmployeeId()
        {
            if (!User.Identity.IsAuthenticated)
                return null;

            var email = User.FindFirst("preferred_username")?.Value
                        ?? User.FindFirst("upn")?.Value;

            int empId = _empRepo.GetEmployeeIdByEmail(email);
            return empId > 0 ? empId : (int?)null;
        }
    }
}
