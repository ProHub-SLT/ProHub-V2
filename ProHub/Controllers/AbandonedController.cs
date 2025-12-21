using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using ProHub.Data;
using ProHub.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProHub.Controllers
{
    public class AbandonedController : Controller
    {
        private readonly ExternalSolutionRepository _repo;

        public AbandonedController(ExternalSolutionRepository repo)
        {
            _repo = repo;
        }

        // Main View
        public IActionResult Index(string search = "", string sortColumn = "PlatformName", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            // 1. Get Data
            var abandonedList = _repo.GetAbandonedSolutions(search ?? "");

            // 2. Sorting Logic (Updated to match View columns)
            sortColumn = sortColumn?.Trim() ?? "PlatformName";
            sortOrder = sortOrder?.ToLowerInvariant() == "desc" ? "desc" : "asc";

            abandonedList = sortColumn.ToLowerInvariant() switch
            {
                "platformname" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.PlatformName).ToList() : abandonedList.OrderByDescending(x => x.PlatformName).ToList(),
                "developedby" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.DevelopedBy?.EmpName).ToList() : abandonedList.OrderByDescending(x => x.DevelopedBy?.EmpName).ToList(),
                "launcheddate" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.LaunchedDate).ToList() : abandonedList.OrderByDescending(x => x.LaunchedDate).ToList(),
                "platformotc" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.PlatformOTC ?? 0).ToList() : abandonedList.OrderByDescending(x => x.PlatformOTC ?? 0).ToList(),
                "contractperiod" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.ContractPeriod).ToList() : abandonedList.OrderByDescending(x => x.ContractPeriod).ToList(),
                "revenue" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.Revenue ?? 0).ToList() : abandonedList.OrderByDescending(x => x.Revenue ?? 0).ToList(),
                "billed" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.BillingDate).ToList() : abandonedList.OrderByDescending(x => x.BillingDate).ToList(),
                "salesteam" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.SalesAM).ToList() : abandonedList.OrderByDescending(x => x.SalesAM).ToList(),
                "proposaluploaded" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.ProposalUploaded).ToList() : abandonedList.OrderByDescending(x => x.ProposalUploaded).ToList(),
                _ => abandonedList.OrderBy(x => x.PlatformName).ToList()
            };

            // 3. Pagination Logic
            int totalRecords = abandonedList.Count;
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // Validate page bounds
            page = page < 1 ? 1 : page;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var paginatedList = abandonedList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 4. Pass Data to View
            ViewBag.SearchTerm = search ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = totalPages; // Fixed: This was missing!
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(paginatedList);
        }

        // View details of a single abandoned record
        public IActionResult ViewDetails(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid ID.");

            var item = _repo.GetAbandonedSolutionById(id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        public IActionResult ExportAllToExcel()
        {
            // 1. Fetch Data
            var allData = _repo.GetAbandonedSolutions("");

            // 2. Create Workbook using ClosedXML
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Abandoned Solutions");

                // 3. Set Headers
                ws.Cell(1, 1).Value = "Platform Name";
                ws.Cell(1, 2).Value = "Company Name";
                ws.Cell(1, 3).Value = "Developed By";
                ws.Cell(1, 4).Value = "Developed Team";
                ws.Cell(1, 5).Value = "Sales Team Involved";
                ws.Cell(1, 6).Value = "SDLC Stage";
                ws.Cell(1, 7).Value = "Launched Date";
                ws.Cell(1, 8).Value = "Billing Date";
                ws.Cell(1, 9).Value = "One-Time Charge";
                ws.Cell(1, 10).Value = "Monthly Charge";
                ws.Cell(1, 11).Value = "Contract Period";
                ws.Cell(1, 12).Value = "Incentive Earned";
                ws.Cell(1, 13).Value = "Incentive Shared With";
                ws.Cell(1, 14).Value = "Proposal Uploaded";
                ws.Cell(1, 15).Value = "Revenue";

                // 4. Style Header (ClosedXML Syntax)
                var headerRange = ws.Range(1, 1, 1, 15);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#007BFF"); // Blue
                headerRange.Style.Font.FontColor = XLColor.White; // White Text

                // 5. Populate Data
                int row = 2;
                foreach (var item in allData)
                {
                    ws.Cell(row, 1).Value = item.PlatformName ?? "";
                    ws.Cell(row, 2).Value = item.Company?.CompanyName ?? "";
                    ws.Cell(row, 3).Value = item.DevelopedBy?.EmpName ?? "";
                    ws.Cell(row, 4).Value = item.DevelopedTeam ?? "";
                    ws.Cell(row, 5).Value = item.SalesTeam?.SalesTeamName ?? "";
                    ws.Cell(row, 6).Value = item.SDLCStage?.Phase ?? "";

                    // Handle Dates safely
                    ws.Cell(row, 7).Value = item.LaunchedDate.HasValue ? item.LaunchedDate.Value.ToString("yyyy-MM-dd") : "";
                    ws.Cell(row, 8).Value = item.BillingDate.HasValue ? item.BillingDate.Value.ToString("yyyy-MM-dd") : "";

                    ws.Cell(row, 9).Value = item.PlatformOTC ?? 0;
                    ws.Cell(row, 10).Value = item.PlatformMRC ?? 0;
                    ws.Cell(row, 11).Value = item.ContractPeriod ?? "";
                    ws.Cell(row, 12).Value = item.IncentiveEarned ?? 0;
                    ws.Cell(row, 13).Value = item.IncentiveShare ?? 0;
                    ws.Cell(row, 14).Value = item.ProposalUploaded ?? "";
                    ws.Cell(row, 15).Value = item.Revenue ?? 0;

                    row++;
                }

                // 6. Adjust Column Widths
                ws.Columns().AdjustToContents();

                // 7. Save and Return File
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"External Solutions - Abandoned_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}