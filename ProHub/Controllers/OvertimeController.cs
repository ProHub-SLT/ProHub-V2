﻿// File: Controllers/OvertimeController.cs
using Microsoft.AspNetCore.Mvc;
using ProHub.Data;
using ProHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProHub.Controllers
{
    public class OvertimeController : Controller
    {
        private readonly OvertimeRepository _otRepo;
        private readonly EmployeeRepository _empRepo;

        public OvertimeController(OvertimeRepository otRepo, EmployeeRepository empRepo)
        {
            _otRepo = otRepo;
            _empRepo = empRepo;
        }

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

        public IActionResult Create()
        {
            // Set the created by name
            ViewBag.CreatedByName = GetAuthenticatedEmployeeName();
            ViewBag.AuthenticatedEmployeeId = GetAuthenticatedEmployeeId();

            // Use existing methods to get all employees
            var employeeIds = _empRepo.GetAllEmployeeIds();
            var employeeNames = _empRepo.GetNamesByIds(employeeIds);

            // Convert to a format compatible with the view
            var employees = employeeNames.Select(kvp => new { Emp_ID = kvp.Key, Emp_Name = kvp.Value }).ToList();
            ViewBag.Employees = employees;

            return View(new OvertimeRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OvertimeRequest model)
        {
            if (ModelState.IsValid)
            {
                model.Created_By = GetAuthenticatedEmployeeId();
                model.Created_Date = DateTime.Now;

                _otRepo.Insert(model);
                TempData["Success"] = "Overtime request created!";
                return RedirectToAction("Index");
            }

            // Set the created by name
            ViewBag.CreatedByName = GetAuthenticatedEmployeeName();

            // Use existing methods to get all employees
            var employeeIds = _empRepo.GetAllEmployeeIds();
            var employeeNames = _empRepo.GetNamesByIds(employeeIds);

            // Convert to a format compatible with the view
            var employees = employeeNames.Select(kvp => new { Emp_ID = kvp.Key, Emp_Name = kvp.Value }).ToList();
            ViewBag.Employees = employees;

            return View(model);
        }

        public IActionResult Details(int id)
        {
            var overtime = _otRepo.GetById(id);
            if (overtime == null)
            {
                return NotFound();
            }
            return View(overtime);
        }

        public IActionResult Edit(int id)
        {
            var overtime = _otRepo.GetById(id);
            if (overtime == null)
            {
                return NotFound();
            }

            // Use existing methods to get all employees
            var employeeIds = _empRepo.GetAllEmployeeIds();
            var employeeNames = _empRepo.GetNamesByIds(employeeIds);

            // Convert to a format compatible with the view
            var employees = employeeNames.Select(kvp => new { Emp_ID = kvp.Key, Emp_Name = kvp.Value }).ToList();
            ViewBag.Employees = employees;

            return View(overtime);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OvertimeRequest model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _otRepo.Update(model);
                    TempData["Success"] = "Overtime request updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "An error occurred while updating the overtime request: " + ex.Message;
                }
            }

            // Repopulate employees list for the view
            var employeeIds = _empRepo.GetAllEmployeeIds();
            var employeeNames = _empRepo.GetNamesByIds(employeeIds);
            var employees = employeeNames.Select(kvp => new { Emp_ID = kvp.Key, Emp_Name = kvp.Value }).ToList();
            ViewBag.Employees = employees;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                _otRepo.Delete(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        /// <summary>
        /// Gets the authenticated employee name from the database using email from claims
        /// </summary>
        /// <returns>Employee name from database or fallback name</returns>
        private string GetAuthenticatedEmployeeName()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Get user email from claims
                var email = User.FindFirst("preferred_username")?.Value
                           ?? User.FindFirst("upn")?.Value
                           ?? "Unknown";
                
                // Get employee name from database using email
                try
                {
                    var empName = _empRepo.GetEmployeeNameByEmail(email);
                    return empName;
                }
                catch
                {
                    // Fallback to using name claim if database lookup fails
                    var displayName = User.FindFirst("name")?.Value ?? "Unknown";
                    return displayName;
                }
            }
            else
            {
                return "Unknown User";
            }
        }
        
        /// <summary>
        /// Gets the authenticated employee ID from the database using email from claims
        /// </summary>
        /// <returns>Employee ID from database or fallback ID</returns>
        private int? GetAuthenticatedEmployeeId()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Get user email from claims
                var email = User.FindFirst("preferred_username")?.Value
                           ?? User.FindFirst("upn")?.Value
                           ?? "Unknown";
                
                // Get employee ID from database using email
                try
                {
                    var empId = _empRepo.GetEmployeeIdByEmail(email);
                    return empId > 0 ? empId : (int?)null;
                }
                catch
                {
                    // Fallback to using name claim if database lookup fails
                    var displayName = User.FindFirst("name")?.Value ?? "Unknown";
                    try
                    {
                        var empId = _empRepo.GetIdByName(displayName);
                        return empId > 0 ? empId : (int?)null;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
        }
    }
}