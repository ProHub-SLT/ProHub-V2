using ClosedXML.Excel;
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
            retiredList = sortColumn.Trim() switch
            {
                "AppName" => sortOrder == "asc"
                    ? retiredList.OrderBy(x => x.AppName).ToList()
                    : retiredList.OrderByDescending(x => x.AppName).ToList(),

                "DevelopedBy" => sortOrder == "asc"
                    ? retiredList.OrderBy(x => x.DevelopedBy?.EmpName).ToList()
                    : retiredList.OrderByDescending(x => x.DevelopedBy?.EmpName).ToList(),

                "LaunchedDate" => sortOrder == "asc"
                    ? retiredList.OrderBy(x => x.LaunchedDate).ToList()
                    : retiredList.OrderByDescending(x => x.LaunchedDate).ToList(),

                "Price" => sortOrder == "asc"
                    ? retiredList.OrderBy(x => x.Price).ToList()
                    : retiredList.OrderByDescending(x => x.Price).ToList(),

                "Comment" => sortOrder == "asc"
                    ? retiredList.OrderBy(x => x.DPOHandoverComment).ToList()
                    : retiredList.OrderByDescending(x => x.DPOHandoverComment).ToList(),

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


        public IActionResult ExportAllToExcel()
        {
            var allData = _repo.GetRetiredSolutions("");

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Retired Solutions");

                // Defined Headers (Added "Type" at index 2)
                var headers = new List<string>
        {
            "AppGroup",        // 1
            "App_Name",        // 2
            "Type",            // 3 
            "Developed_By",    // 4
            "App_URL",         // 5
            "App_IP",          // 6
            "SDLC_Stage",      // 7
            "Start_Date",      // 8
            "Target_Date",     // 9
            "VA_Date",         // 10
            "Percentage_Done", // 11
            "Launched_Date",   // 12
            "Current_Status",  // 13
            "Price",           // 14
           "Comment",           // 15
        };

                // Set Header Values
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                int row = 2;
                foreach (var item in allData)
                {
                    worksheet.Cell(row, 1).Value = item.ParentProject?.ParentProjectGroup ?? "";
                    worksheet.Cell(row, 2).Value = item.AppName ?? "";

                    // --- NEW TYPE LOGIC ---
                    if (!string.IsNullOrEmpty(item.MainAppName))
                    {
                        worksheet.Cell(row, 3).Value = $"CR of {item.MainAppName}";
                    }
                    else
                    {
                        worksheet.Cell(row, 3).Value = "Main Application";
                    }
                    // ----------------------

                    worksheet.Cell(row, 4).Value = item.DevelopedBy?.EmpName ?? "";
                    worksheet.Cell(row, 5).Value = item.AppURL ?? "";
                    worksheet.Cell(row, 6).Value = item.AppIP ?? "";
                    worksheet.Cell(row, 7).Value = item.SDLCPhase?.Phase ?? "";

                    SetDateCell(worksheet, row, 8, item.StartDate);
                    SetDateCell(worksheet, row, 9, item.TargetDate);
                    SetDateCell(worksheet, row, 10, item.VADate);

                    worksheet.Cell(row, 11).Value = item.PercentageDone.HasValue ? item.PercentageDone.Value : 0;

                    SetDateCell(worksheet, row, 12, item.LaunchedDate);

                    worksheet.Cell(row, 13).Value = item.Status ?? "";

                    if (item.Price.HasValue)
                    {
                        worksheet.Cell(row, 14).Value = item.Price.Value;
                        worksheet.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
                    }

                    worksheet.Cell(row, 15).Value = item.DPOHandoverComment ?? "";

                    row++;
                }

                // Styling
                var headerRange = worksheet.Range(1, 1, 1, headers.Count);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#007BFF");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"Internal Solutions - Retired_{DateTime.Now:yyyy-MM-dd}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // Helper method to handle null dates cleanly
        private void SetDateCell(IXLWorksheet ws, int row, int col, DateTime? dateValue)
        {
            if (dateValue.HasValue)
            {
                ws.Cell(row, col).Value = dateValue.Value;
                ws.Cell(row, col).Style.DateFormat.Format = "yyyy-MM-dd";
            }
        }
    }
}