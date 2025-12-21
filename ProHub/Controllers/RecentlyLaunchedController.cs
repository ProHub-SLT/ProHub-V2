using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProHub.Data.Repositories;
using ProHub.Models;
using PROHUB.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PROHUB.Controllers
{
    public class RecentlyLaunchedController : Controller
    {
        private readonly IRecentlyLaunchedService _dataAccess;
        private readonly ILogger<RecentlyLaunchedController> _logger;
        private readonly int[] _allowedPageSizes = new[] { 10, 25, 50, 100 };

        public RecentlyLaunchedController(IRecentlyLaunchedService dataAccess, ILogger<RecentlyLaunchedController> logger)
        {
            _dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ------------------ Index ------------------
        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            string sortColumn = "LaunchedDate", // Default sort
            string sortOrder = "desc",
            string search = ""
        )
        {
            try
            {
                if (!_allowedPageSizes.Contains(pageSize)) pageSize = 10;
                search = search?.Trim() ?? string.Empty;

                // 1. Getting data
                var all = await _dataAccess.GetRecentlyLaunchedAsync() ?? new List<InternalPlatform>();
                IEnumerable<InternalPlatform> filtered = all;

                // 2. Search Logic (Updated to match the Columns in the View)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.AppName) && x.AppName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.DevelopedByName) && x.DevelopedByName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.ParentProjectGroupName) && x.ParentProjectGroupName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.SDLCPhaseName) && x.SDLCPhaseName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.EndUserTypeName) && x.EndUserTypeName.Contains(search, StringComparison.OrdinalIgnoreCase)) // UD search
                    );
                }

                // 3. Sorting Logic (set to match the Headers in the View)
                bool ascending = sortOrder?.ToLower() == "asc";

                filtered = sortColumn switch
                {
                    "ParentProjectGroupName" => ascending ? filtered.OrderBy(x => x.ParentProjectGroupName) : filtered.OrderByDescending(x => x.ParentProjectGroupName),
                    "AppName" => ascending ? filtered.OrderBy(x => x.AppName) : filtered.OrderByDescending(x => x.AppName),
                    "DevelopedBy" => ascending ? filtered.OrderBy(x => x.DevelopedByName) : filtered.OrderByDescending(x => x.DevelopedByName),
                    "SDLCPhase" => ascending ? filtered.OrderBy(x => x.SDLCPhaseName) : filtered.OrderByDescending(x => x.SDLCPhaseName),
                    "Start" => ascending ? filtered.OrderBy(x => x.StartDate) : filtered.OrderByDescending(x => x.StartDate),
                    "Target" => ascending ? filtered.OrderBy(x => x.TargetDate) : filtered.OrderByDescending(x => x.TargetDate),
                    "UD" => ascending ? filtered.OrderBy(x => x.EndUserTypeName) : filtered.OrderByDescending(x => x.EndUserTypeName), // UD = EndUserType/Dept
                    "Price" => ascending ? filtered.OrderBy(x => x.Price) : filtered.OrderByDescending(x => x.Price),

                    // Default Case
                    _ => ascending ? filtered.OrderBy(x => x.LaunchedDate) : filtered.OrderByDescending(x => x.LaunchedDate)
                };

                // 4. Pagination
                var totalRecords = filtered.Count();
                var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalRecords / pageSize) : 1;
                if (totalPages == 0) totalPages = 1;
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedList = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // 5. View Data Passing
                ViewBag.PageSize = pageSize;
                ViewBag.CurrentPage = page;
                ViewBag.TotalRecords = totalRecords;
                ViewBag.TotalPages = totalPages;
                ViewBag.SortColumn = sortColumn;
                ViewBag.SortOrder = sortOrder;
                ViewBag.SearchTerm = search;

                return View(pagedList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Recently Launched list.");
                TempData["ErrorMessage"] = "Failed to load data.";
                return View(new List<InternalPlatform>());
            }
        }

        // ------------------ Export to Excel ------------------
        public async Task<IActionResult> ExportToExcel(string search = "")
        {
            try
            {
                // 1. Get projects (using your existing logic)
                var all = await _dataAccess.GetRecentlyLaunchedAsync();

                IEnumerable<InternalPlatform> filtered = all;

                // 2. Apply Search Filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.AppName) && x.AppName.Contains(search, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(x.ParentProjectGroupName) && x.ParentProjectGroupName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    );
                }

                var dataList = filtered.OrderByDescending(x => x.LaunchedDate).ToList();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("RecentlyLaunched");
                    int colIndex = 1;


                    // 3.HEADERS (14 Columns)

                    var headers = new List<string> {
                "AppGroup",
                "App_Name",
                "Developed_By",
                "App_URL",
                "App_IP",
                "SDLC_Stage",
                "Start_Date",
                "Target_Date",
                "VA_Date",
                "Percentage_Done",
                "Launched_Date",
                "Current_Status",
                "Price",
                "Launched_Year"
            };

                    // Set Header Styles
                    foreach (var h in headers)
                    {
                        var cell = worksheet.Cell(1, colIndex++);
                        cell.Value = h;
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#007BFF");
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    }


                    // 4. MAP DATA TO COLUMNS

                    int row = 2;
                    foreach (var item in dataList)
                    {
                        // 1. AppGroup
                        worksheet.Cell(row, 1).Value = item.ParentProjectGroupName;

                        // 2. App_Name
                        worksheet.Cell(row, 2).Value = item.AppName;

                        // 3. Developed_By
                        worksheet.Cell(row, 3).Value = item.DevelopedByName;

                        // 4. App_URL
                        worksheet.Cell(row, 4).Value = item.AppURL;

                        // 5. App_IP
                        worksheet.Cell(row, 5).Value = item.AppIP;

                        // 6. SDLC_Stage (Mapped to SDLCPhaseName)
                        worksheet.Cell(row, 6).Value = item.SDLCPhaseName;

                        // 7. Start_Date
                        worksheet.Cell(row, 7).Value = item.StartDate;
                        worksheet.Cell(row, 7).Style.DateFormat.Format = "yyyy-MM-dd";

                        // 8. Target_Date
                        worksheet.Cell(row, 8).Value = item.TargetDate;
                        worksheet.Cell(row, 8).Style.DateFormat.Format = "yyyy-MM-dd";

                        // 9. VA_Date
                        worksheet.Cell(row, 9).Value = item.VADate;
                        worksheet.Cell(row, 9).Style.DateFormat.Format = "yyyy-MM-dd";

                        // 10. Percentage_Done
                        worksheet.Cell(row, 10).Value = item.PercentageDone;

                        // 11. Launched_Date
                        worksheet.Cell(row, 11).Value = item.LaunchedDate;
                        worksheet.Cell(row, 11).Style.DateFormat.Format = "yyyy-MM-dd";

                        // 12. Current_Status
                        worksheet.Cell(row, 12).Value = item.Status;

                        // 13. Price
                        worksheet.Cell(row, 13).Value = item.Price;
                        worksheet.Cell(row, 13).Style.NumberFormat.Format = "#,##0.00";

                        // 14. Launched_Year (Extracted from LaunchedDate)
                        if (item.LaunchedDate.HasValue)
                        {
                            worksheet.Cell(row, 14).Value = item.LaunchedDate.Value.Year;
                        }
                        else
                        {
                            worksheet.Cell(row, 14).Value = "";
                        }

                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Internal Solutions - Recently Launched_{DateTime.Now:yyyyMMdd}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Export failed");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
