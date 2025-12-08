using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using ProHub.Data;
using ProHub.Models;
using System;
using System.IO;
using System.Linq;

namespace ProHub.Controllers
{
    public class InternalSolutionRetiredController : Controller
    {
        private readonly ConsumerPlatformRepository _repo;

        // Constructor
        public InternalSolutionRetiredController(ConsumerPlatformRepository repo)
        {
            _repo = repo;
        }

        // Main View
        public IActionResult Index(string search = "", string sortColumn = "AppName", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            var retiredList = _repo.GetRetiredSolutions(search);

            // Sorting
            retiredList = sortColumn switch
            {
                "AppName" => sortOrder == "asc" ? retiredList.OrderBy(x => x.AppName).ToList() : retiredList.OrderByDescending(x => x.AppName).ToList(),
                "DevelopedBy" => sortOrder == "asc" ? retiredList.OrderBy(x => x.DevelopedBy?.EmpName).ToList() : retiredList.OrderByDescending(x => x.DevelopedBy?.EmpName).ToList(),
                "LaunchedDate" => sortOrder == "asc" ? retiredList.OrderBy(x => x.LaunchedDate).ToList() : retiredList.OrderByDescending(x => x.LaunchedDate).ToList(),
                _ => retiredList.OrderBy(x => x.AppName).ToList()
            };

            var totalRecords = retiredList.Count;

            // Pagination
            var paginatedList = retiredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass data to View
            ViewBag.SearchTerm = search ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(paginatedList);
        }

        // View details of a single retired solution
        public IActionResult ViewDetails(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid ID.");

            var item = _repo.GetRetiredSolutionById(id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        // Export ALL retired solutions to Excel
        public IActionResult ExportAllToExcel()
        {
            var allData = _repo.GetRetiredSolutions(""); // get all retired solutions

            ExcelPackage.License.SetNonCommercialPersonal("ProHub");

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Retired Solutions");

                // Header
                ws.Cells[1, 1].Value = "Application Name";
                ws.Cells[1, 2].Value = "Developed By";
                ws.Cells[1, 3].Value = "Launched Date";
                ws.Cells[1, 4].Value = "SDLC Phase";
                ws.Cells[1, 5].Value = "Solution Value";
                ws.Cells[1, 6].Value = "Comment";

                int row = 2;
                foreach (var item in allData)
                {
                    ws.Cells[row, 1].Value = item.AppName ?? "";
                    ws.Cells[row, 2].Value = item.DevelopedBy?.EmpName ?? "";
                    ws.Cells[row, 3].Value = item.LaunchedDate.HasValue
                        ? item.LaunchedDate.Value.ToString("yyyy-MM-dd")
                        : "";
                    ws.Cells[row, 4].Value = item.SDLCPhase?.Phase ?? "";
                    ws.Cells[row, 5].Value = item.Price.HasValue ? item.Price.Value.ToString("N2") : "";
                    ws.Cells[row, 6].Value = item.DPOHandoverComment ?? "";

                    row++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"Retired_Internal_Solutions_{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}
