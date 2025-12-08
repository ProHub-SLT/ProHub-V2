using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Models;
using PROHUB.Data;
using PROHUB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PROHUB.Controllers
{
    public class TraineeController : Controller
    {
        private readonly ITraineeService _traineeService;

        // Fix: Initialize with empty string so it's never null
        private readonly string _connectionString = "";

        public TraineeController(ITraineeService traineeService, IConfiguration configuration)
        {
            _traineeService = traineeService;

            // Fix: Use '?? ""' to handle potential nulls safely
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        // ... (Rest of your controller code) ...

        // 🔹 1. ALL TRAINEES
        public async Task<IActionResult> Index(string search = "", int page = 1, int pageSize = 10, string sortColumn = "Trainee_ID", string sortOrder = "desc")
        {
            // Get Data
            var allTrainees = (await _traineeService.GetAllAsync()).AsQueryable();

            // Search Logic
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                allTrainees = allTrainees.Where(t =>
                    (t.Trainee_Name != null && t.Trainee_Name.ToLower().Contains(search)) ||
                    (t.Trainee_NIC != null && t.Trainee_NIC.ToLower().Contains(search)) ||
                    (t.Institute != null && t.Institute.ToLower().Contains(search)) ||
                    (t.Trainee_ID.ToString().Contains(search)) ||
                    (t.FormattedTrainee_ID != null && t.FormattedTrainee_ID.ToLower().Contains(search))
                );
            }

            // Dynamic Sorting Logic
            // We check the sortOrder (asc/desc) and the sortColumn name
            bool isAsc = sortOrder.ToLower() == "asc";

            allTrainees = sortColumn.ToLower() switch
            {
                "trainee_name" => isAsc ? allTrainees.OrderBy(t => t.Trainee_Name) : allTrainees.OrderByDescending(t => t.Trainee_Name),
                "trainee_nic" => isAsc ? allTrainees.OrderBy(t => t.Trainee_NIC) : allTrainees.OrderByDescending(t => t.Trainee_NIC),
                "institute" => isAsc ? allTrainees.OrderBy(t => t.Institute) : allTrainees.OrderByDescending(t => t.Institute),
                "training_startdate" => isAsc ? allTrainees.OrderBy(t => t.Training_StartDate) : allTrainees.OrderByDescending(t => t.Training_StartDate),
                "training_enddate" => isAsc ? allTrainees.OrderBy(t => t.Training_EndDate) : allTrainees.OrderByDescending(t => t.Training_EndDate),
                "supervisor" => isAsc ? allTrainees.OrderBy(t => t.SupervisorName) : allTrainees.OrderByDescending(t => t.SupervisorName),
                // Default to ID if column not found
                _ => isAsc ? allTrainees.OrderBy(t => t.Trainee_ID) : allTrainees.OrderByDescending(t => t.Trainee_ID),
            };

            // Pagination Calculations
            var totalEntries = allTrainees.Count();
            var totalPages = (int)Math.Ceiling((double)totalEntries / pageSize);

            // Safety check: if page > totalPages, reset to last page
            if (page > totalPages && totalPages > 0)
            {
                page = totalPages;
            }

            // Apply Pagination
            var trainees = allTrainees
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            //  Set ViewData (Crucial for the View to maintain state)
            ViewData["TotalEntries"] = totalEntries;
            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["SearchTerm"] = search;

            // These two are required for your View's sort arrows to work
            ViewData["SortColumn"] = sortColumn;
            ViewData["SortOrder"] = sortOrder;

            ViewData["Title"] = "All Trainees";

            return View("AllTrainee", trainees);
        }

        // 🔹 2. ACTIVE TRAINEES
        public async Task<IActionResult> ActiveTrainee(string search = "", int page = 1, int pageSize = 10, string sortColumn = "Trainee_ID", string sortOrder = "desc")
        {
            // 1. Fetch Data
            var allTrainees = (await _traineeService.GetAllAsync()).AsQueryable();
            var today = DateTime.Now.Date;

            // 2. Filter: Active Logic (Not terminated AND End Date is in the future or today)
            var activeList = allTrainees.Where(t =>
                t.terminated_date == null &&
                (t.Training_EndDate.HasValue && t.Training_EndDate.Value.Date >= today)
            );

            // 3. Filter: Search Logic
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower().Trim();
                activeList = activeList.Where(t =>
                    (t.Trainee_Name != null && t.Trainee_Name.ToLower().Contains(search)) ||
                    (t.Trainee_NIC != null && t.Trainee_NIC.ToLower().Contains(search)) ||
                    (t.Institute != null && t.Institute.ToLower().Contains(search)) ||
                    (t.Trainee_ID.ToString().Contains(search)) ||
                    (t.FormattedTrainee_ID != null && t.FormattedTrainee_ID.ToLower().Contains(search))
                );
            }

            // 4. Sorting Logic (Matches the View's sortable columns)
            bool isAsc = sortOrder.ToLower() == "asc";

            activeList = sortColumn.ToLower() switch
            {
                "trainee_name" => isAsc ? activeList.OrderBy(t => t.Trainee_Name) : activeList.OrderByDescending(t => t.Trainee_Name),
                "trainee_nic" => isAsc ? activeList.OrderBy(t => t.Trainee_NIC) : activeList.OrderByDescending(t => t.Trainee_NIC),
                "training_startdate" => isAsc ? activeList.OrderBy(t => t.Training_StartDate) : activeList.OrderByDescending(t => t.Training_StartDate),
                "training_enddate" => isAsc ? activeList.OrderBy(t => t.Training_EndDate) : activeList.OrderByDescending(t => t.Training_EndDate),
                "institute" => isAsc ? activeList.OrderBy(t => t.Institute) : activeList.OrderByDescending(t => t.Institute),
                "supervisor" => isAsc ? activeList.OrderBy(t => t.SupervisorName) : activeList.OrderByDescending(t => t.SupervisorName),
                _ => isAsc ? activeList.OrderBy(t => t.Trainee_ID) : activeList.OrderByDescending(t => t.Trainee_ID), // Default
            };

            // 5. Pagination Logic
            var totalEntries = activeList.Count();
            var totalPages = (int)Math.Ceiling((double)totalEntries / pageSize);

            // Ensure page is within valid range
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var trainees = activeList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 6. Set ViewData for the View
            ViewData["TotalEntries"] = totalEntries;
            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["SearchTerm"] = search;

            // Critical for View sorting links to work:
            ViewData["SortColumn"] = sortColumn;
            ViewData["SortOrder"] = sortOrder;

            ViewData["Title"] = "Active Trainees";

            return View("ActiveTrainee", trainees);
        }

        // 🔹 3. INACTIVE TRAINEES
        public async Task<IActionResult> InactiveTrainee(string search = "", int page = 1, int pageSize = 10, string sortColumn = "Trainee_ID", string sortOrder = "desc")
        {
            // 1. Get Data
            var allTrainees = (await _traineeService.GetAllAsync()).AsQueryable();
            var today = DateTime.Now.Date;

            // 2. Filter for Inactive (Terminated OR End Date Passed)
            var inactiveList = allTrainees.Where(t =>
                t.terminated_date != null ||
                (t.Training_EndDate.HasValue && t.Training_EndDate.Value.Date < today)
            );

            // 3. Apply Search Filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower().Trim(); // Added Trim() to remove accidental spaces
                inactiveList = inactiveList.Where(t =>
                    (t.Trainee_Name != null && t.Trainee_Name.ToLower().Contains(search)) ||
                    (t.Trainee_NIC != null && t.Trainee_NIC.ToLower().Contains(search)) ||
                    (t.Institute != null && t.Institute.ToLower().Contains(search)) ||
                    (t.Trainee_ID.ToString().Contains(search)) ||
                    (t.FormattedTrainee_ID != null && t.FormattedTrainee_ID.ToLower().Contains(search))
                );
            }

            // 4. Apply Sorting (New Logic)
            // We default to descending ID if the switch fails
            inactiveList = sortColumn.ToLower() switch
            {
                "trainee_name" => sortOrder == "asc" ? inactiveList.OrderBy(t => t.Trainee_Name) : inactiveList.OrderByDescending(t => t.Trainee_Name),
                "trainee_nic" => sortOrder == "asc" ? inactiveList.OrderBy(t => t.Trainee_NIC) : inactiveList.OrderByDescending(t => t.Trainee_NIC),
                "training_startdate" => sortOrder == "asc" ? inactiveList.OrderBy(t => t.Training_StartDate) : inactiveList.OrderByDescending(t => t.Training_StartDate),
                "training_enddate" => sortOrder == "asc" ? inactiveList.OrderBy(t => t.Training_EndDate) : inactiveList.OrderByDescending(t => t.Training_EndDate),
                "institute" => sortOrder == "asc" ? inactiveList.OrderBy(t => t.Institute) : inactiveList.OrderByDescending(t => t.Institute),
                "supervisor" => sortOrder == "asc" ? inactiveList.OrderBy(t => t.SupervisorName) : inactiveList.OrderByDescending(t => t.SupervisorName),
                _ => sortOrder == "asc" ? inactiveList.OrderBy(t => t.Trainee_ID) : inactiveList.OrderByDescending(t => t.Trainee_ID),
            };

            // 5. Pagination
            var totalEntries = inactiveList.Count();
            var totalPages = (int)Math.Ceiling((double)totalEntries / pageSize);

            // Safety check: if page is out of bounds (e.g. user deleted records and stayed on page 5)
            if (page > totalPages && totalPages > 0) page = totalPages;

            var trainees = inactiveList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 6. Set ViewData (Must match what the View expects)
            ViewData["TotalEntries"] = totalEntries;
            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["SearchTerm"] = search;
            ViewData["SortColumn"] = sortColumn; // Pass this back so the View knows which column is active
            ViewData["SortOrder"] = sortOrder;   // Pass this back to toggle the arrow direction
            ViewData["Title"] = "Inactive Trainees";

            return View("InactiveTrainee", trainees);
        }

        // 🔹 4. PAID TRAINEES
        public async Task<IActionResult> PaidTrainee(string search = "", int page = 1, int pageSize = 10, string sortColumn = "Trainee_ID", string sortOrder = "desc")
        {
            // 1. Get Data
            var allTrainees = (await _traineeService.GetAllAsync()).AsQueryable();

            // 2. Filter for Paid Trainees (where requested_payment_date has a value)
            var paidList = allTrainees.Where(t => t.requested_payment_date.HasValue);

            // 3. Apply Search
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                paidList = paidList.Where(t =>
                    (t.Trainee_Name != null && t.Trainee_Name.ToLower().Contains(search)) ||
                    (t.Trainee_NIC != null && t.Trainee_NIC.ToLower().Contains(search)) ||
                    (t.Trainee_ID.ToString().Contains(search)) ||
                    (t.FormattedTrainee_ID != null && t.FormattedTrainee_ID.ToLower().Contains(search))
                );
            }

            // 4. Apply Sorting (Match these cases to your View's sort columns)
            paidList = sortColumn.ToLower() switch
            {
                "trainee_name" => sortOrder == "asc" ? paidList.OrderBy(t => t.Trainee_Name) : paidList.OrderByDescending(t => t.Trainee_Name),
                "trainee_nic" => sortOrder == "asc" ? paidList.OrderBy(t => t.Trainee_NIC) : paidList.OrderByDescending(t => t.Trainee_NIC),
                "training_enddate" => sortOrder == "asc" ? paidList.OrderBy(t => t.Training_EndDate) : paidList.OrderByDescending(t => t.Training_EndDate),
                "supervisor" => sortOrder == "asc" ? paidList.OrderBy(t => t.SupervisorName) : paidList.OrderByDescending(t => t.SupervisorName),
                "requested_payment_date" => sortOrder == "asc" ? paidList.OrderBy(t => t.requested_payment_date) : paidList.OrderByDescending(t => t.requested_payment_date),
                "payment_start_date" => sortOrder == "asc" ? paidList.OrderBy(t => t.payment_start_date) : paidList.OrderByDescending(t => t.payment_start_date),
                "payment_end_date" => sortOrder == "asc" ? paidList.OrderBy(t => t.payment_end_date) : paidList.OrderByDescending(t => t.payment_end_date),
                // Default Case (Trainee_ID)
                _ => sortOrder == "asc" ? paidList.OrderBy(t => t.Trainee_ID) : paidList.OrderByDescending(t => t.Trainee_ID),
            };

            // 5. Pagination
            var totalEntries = paidList.Count();
            var totalPages = (int)Math.Ceiling((double)totalEntries / pageSize);

            // Safety check: ensure page is not out of bounds
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var trainees = paidList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 6. Set ViewData
            ViewData["TotalEntries"] = totalEntries;
            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["SearchTerm"] = search;

            // Important: Pass these back so the View knows which arrow to show
            ViewData["SortColumn"] = sortColumn;
            ViewData["SortOrder"] = sortOrder;

            ViewData["Title"] = "Paid Trainees";

            return View("PaidTrainee", trainees);
        }

        // 🔹 5. EXPORT ACTIONS
        [HttpGet]
        public async Task<IActionResult> ExportAllToExcel()
        {
            var trainees = await _traineeService.GetAllAsync();
            return File(CreateExcelFile(trainees.ToList()), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AllTrainees.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportActiveToExcel()
        {
            var all = await _traineeService.GetAllAsync();
            var today = DateTime.Now.Date;
            var active = all.Where(t => t.terminated_date == null && (t.Training_EndDate.HasValue && t.Training_EndDate.Value.Date >= today)).ToList();
            return File(CreateExcelFile(active), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ActiveTrainees.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportTerminatedToExcel()
        {
            var all = await _traineeService.GetAllAsync();
            var terminatedList = all.Where(t => t.terminated_date != null).ToList();
            return File(CreateExcelFile(terminatedList), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TerminatedTrainees.xlsx");
        }

        private MemoryStream CreateExcelFile(List<Trainees> trainees)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Trainees");
                var currentRow = 1;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "ID";
                worksheet.Cell(currentRow, 2).Value = "Name";
                worksheet.Cell(currentRow, 3).Value = "NIC";
                worksheet.Cell(currentRow, 4).Value = "Phone";
                worksheet.Cell(currentRow, 5).Value = "Email";
                worksheet.Cell(currentRow, 6).Value = "Start Date";
                worksheet.Cell(currentRow, 7).Value = "End Date";
                worksheet.Cell(currentRow, 8).Value = "Institute";
                worksheet.Cell(currentRow, 9).Value = "Supervisor";
                worksheet.Cell(currentRow, 10).Value = "Specialization";
                worksheet.Cell(currentRow, 11).Value = "Terminated Date";
                worksheet.Cell(currentRow, 12).Value = "Requested Payment Date";

                var header = worksheet.Range("A1:L1");
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#007BFF");
                header.Style.Font.FontColor = XLColor.White;

                // Set Date Format for Date Columns
                var dateFormat = "yyyy/MM/dd";
                worksheet.Column(6).Style.DateFormat.Format = dateFormat;
                worksheet.Column(7).Style.DateFormat.Format = dateFormat;
                worksheet.Column(11).Style.DateFormat.Format = dateFormat;
                worksheet.Column(12).Style.DateFormat.Format = dateFormat;

                // FIX: Set Text Format for NIC and Phone Columns to prevent scientific notation
                worksheet.Column(3).Style.NumberFormat.Format = "@"; // NIC
                worksheet.Column(4).Style.NumberFormat.Format = "@"; // Phone

                foreach (var t in trainees)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = t.Trainee_ID;
                    worksheet.Cell(currentRow, 2).Value = t.Trainee_Name;

                    // FIX: Just assign the value. The column style above handles the text format.
                    worksheet.Cell(currentRow, 3).Value = t.Trainee_NIC;
                    worksheet.Cell(currentRow, 4).Value = t.Trainee_Phone;

                    worksheet.Cell(currentRow, 5).Value = t.Trainee_Email;
                    worksheet.Cell(currentRow, 6).Value = t.Training_StartDate;
                    worksheet.Cell(currentRow, 7).Value = t.Training_EndDate;
                    worksheet.Cell(currentRow, 8).Value = t.Institute;
                    worksheet.Cell(currentRow, 9).Value = t.SupervisorName;
                    worksheet.Cell(currentRow, 10).Value = t.FieldOfSpecName;
                    worksheet.Cell(currentRow, 11).Value = t.terminated_date;
                    worksheet.Cell(currentRow, 12).Value = t.requested_payment_date;
                }

                worksheet.Columns().AdjustToContents();

                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }
        }

        // 🔹 HELPER METHODS FOR DROPDOWNS (Corrected property names in SQL)
        private async Task<List<Employee>> GetEmployeesAsync()
        {
            var employees = new List<Employee>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // FIX: The SQL query must use the actual database column names (Emp_ID, Emp_Name)
                string query = "SELECT Emp_ID, Emp_Name FROM Employee ORDER BY Emp_Name";
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        employees.Add(new Employee
                        {
                            // FIX: We read the column by its exact DB name (Emp_ID)
                            EmpId = reader.GetInt32("Emp_ID"),
                            EmpName = reader.GetString("Emp_Name")
                        });
                    }
                }
            }
            return employees;
        }

        private async Task<List<FieldsOfSpecialization>> GetFieldsOfSpecializationAsync()
        {
            var fields = new List<FieldsOfSpecialization>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Ensure your DB table is actually 'Fields_of_Specialization' and columns are 'field_of_spec_id', 'field_of_spec_name'
                string query = "SELECT field_of_spec_id, field_of_spec_name FROM Fields_of_Specialization ORDER BY field_of_spec_name";
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        fields.Add(new FieldsOfSpecialization
                        {
                            FieldOfSpecId = reader.GetInt32(0),
                            FieldOfSpecName = reader.GetString(1)
                        });
                    }
                }
            }
            return fields;
        }

        // 🔹 CRUD ACTIONS

        public async Task<IActionResult> Details(int id)
        {
            var trainee = await _traineeService.GetByIdAsync(id);
            if (trainee == null) return NotFound();
            return View(trainee);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Employees = new SelectList(await GetEmployeesAsync(), "EmpId", "EmpName"); // FIX: Using EmpId/EmpName
            ViewBag.CustomerContacts = new SelectList(await GetFieldsOfSpecializationAsync(), "FieldOfSpecId", "FieldOfSpecName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Trainees trainee)
        {
            if (ModelState.IsValid)
            {
                await _traineeService.CreateAsync(trainee);
                TempData["SuccessMessage"] = "Trainee added successfully!";
                return RedirectToAction(nameof(Index));
            }


            ViewBag.Employees = new SelectList(await GetEmployeesAsync(), "EmpId", "EmpName", trainee.Supervisor);
            ViewBag.CustomerContacts = new SelectList(await GetFieldsOfSpecializationAsync(), "FieldOfSpecId", "FieldOfSpecName", trainee.field_of_spec_id);
            return View(trainee);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var trainee = await _traineeService.GetByIdAsync(id);
            if (trainee == null) return NotFound();

            ViewBag.Employees = new SelectList(await GetEmployeesAsync(), "EmpId", "EmpName", trainee.Supervisor);
            ViewBag.CustomerContacts = new SelectList(await GetFieldsOfSpecializationAsync(), "FieldOfSpecId", "FieldOfSpecName", trainee.field_of_spec_id);

            return View(trainee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Trainees trainee)
        {
            if (ModelState.IsValid)
            {
                var updated = await _traineeService.UpdateAsync(trainee);
                if (updated)
                {
                    TempData["SuccessMessage"] = "Trainee updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = "Update failed!";
            }


            ViewBag.Employees = new SelectList(await GetEmployeesAsync(), "EmpId", "EmpName", trainee.Supervisor);
            ViewBag.CustomerContacts = new SelectList(await GetFieldsOfSpecializationAsync(), "FieldOfSpecId", "FieldOfSpecName", trainee.field_of_spec_id);
            return View(trainee);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _traineeService.DeleteAsync(id);
            if (deleted) TempData["SuccessMessage"] = "Deleted successfully!";
            else TempData["ErrorMessage"] = "Delete failed!";
            return RedirectToAction(nameof(Index));
        }
    }
}