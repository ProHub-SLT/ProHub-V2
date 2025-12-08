using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using ProHub.Data;
using ProHub.Models;
using System;
using System.IO;
using System.Linq;

namespace ProHub.Controllers
{
    public class AbandonedController(ExternalSolutionRepository repo) : Controller
    {
        private readonly ExternalSolutionRepository _repo = repo;

        // Main View
        public IActionResult Index(string search = "", string sortColumn = "PlatformName", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            var abandonedList = _repo.GetAbandonedSolutions(search);

            // ✅ Sorting logic
            abandonedList = sortColumn switch
            {
                "PlatformName" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.PlatformName).ToList() : abandonedList.OrderByDescending(x => x.PlatformName).ToList(),
                "CompanyName" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.Company?.CompanyName).ToList() : abandonedList.OrderByDescending(x => x.Company?.CompanyName).ToList(),
                "DevelopedBy" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.DevelopedBy?.EmpName).ToList() : abandonedList.OrderByDescending(x => x.DevelopedBy?.EmpName).ToList(),
                "StartDate" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.StartDate).ToList() : abandonedList.OrderByDescending(x => x.StartDate).ToList(),
                "DPOHandoverDate" => sortOrder == "asc" ? abandonedList.OrderBy(x => x.DPOHandoverDate).ToList() : abandonedList.OrderByDescending(x => x.DPOHandoverDate).ToList(),
                _ => abandonedList.OrderBy(x => x.PlatformName).ToList()
            };

            var totalRecords = abandonedList.Count;

            // ✅ Pagination
            var paginatedList = abandonedList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ✅ Pass data to View
            ViewBag.SearchTerm = search ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
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

        // Export ALL abandoned data to Excel (server-side)
        public IActionResult ExportAllToExcel()
        {
            var allData = _repo.GetAbandonedSolutions(""); // get all abandoned items

            ExcelPackage.License.SetNonCommercialOrganization("ProHub");

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Abandoned Solutions");

                // Header
                ws.Cells[1, 1].Value = "Platform Name";
                ws.Cells[1, 2].Value = "Company Name";
                ws.Cells[1, 3].Value = "Developed By";
                ws.Cells[1, 4].Value = "SDLC Stage";
                ws.Cells[1, 5].Value = "Start Date";
                ws.Cells[1, 6].Value = "DPO Handover Date";

                int row = 2;
                foreach (var item in allData)
                {
                    ws.Cells[row, 1].Value = item.PlatformName ?? "";
                    ws.Cells[row, 2].Value = item.Company?.CompanyName ?? "";
                    ws.Cells[row, 3].Value = item.DevelopedBy?.EmpName ?? "";
                    ws.Cells[row, 4].Value = item.SDLCStage?.Phase ?? "";

                    // Safely handle nullable dates
                    ws.Cells[row, 5].Value = item.StartDate.HasValue
                        ? item.StartDate.Value.ToString("yyyy-MM-dd")
                        : "";

                    ws.Cells[row, 6].Value = item.DPOHandoverDate.HasValue
                        ? item.DPOHandoverDate.Value.ToString("yyyy-MM-dd")
                        : "";

                    row++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"Abandoned_Solutions_{DateTime.Now:yyyy-MM-dd}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}



