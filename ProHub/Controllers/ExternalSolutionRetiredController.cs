using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using ProHub.Data;
using ProHub.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace ProHub.Controllers
{
    // Controller for managing retired external solutions
    public class ExternalSolutionRetiredController : Controller
    {
        private readonly ExternalSolutionRepository _repo;
        private readonly IConfiguration _config;

        public ExternalSolutionRetiredController(ExternalSolutionRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        // List retired solutions with search, sorting, and pagination (table view)
        public IActionResult Index(string search = "", string sortColumn = "PlatformName", string sortOrder = "asc", int page = 1, int pageSize = 15)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 15 : pageSize;

            List<ExternalPlatform> retiredList = _repo.GetRetiredSolutions(search ?? "");

            sortColumn = sortColumn?.Trim() ?? "PlatformName";
            sortOrder = sortOrder?.ToLowerInvariant() == "desc" ? "desc" : "asc";

            IOrderedEnumerable<ExternalPlatform> sortedList = sortColumn.ToLowerInvariant() switch
            {
                "platformname" => sortOrder == "asc" ? retiredList.OrderBy(x => x.PlatformName ?? "") : retiredList.OrderByDescending(x => x.PlatformName ?? ""),
                "developedby" => sortOrder == "asc" ? retiredList.OrderBy(x => x.DevelopedBy?.EmpName ?? "") : retiredList.OrderByDescending(x => x.DevelopedBy?.EmpName ?? ""),
                "launcheddate" => sortOrder == "asc" ? retiredList.OrderBy(x => x.LaunchedDate ?? DateTime.MinValue) : retiredList.OrderByDescending(x => x.LaunchedDate ?? DateTime.MinValue),
                "platformotc" => sortOrder == "asc" ? retiredList.OrderBy(x => x.PlatformOTC ?? 0) : retiredList.OrderByDescending(x => x.PlatformOTC ?? 0),
                "contractperiod" => sortOrder == "asc" ? retiredList.OrderBy(x => x.ContractPeriod ?? "") : retiredList.OrderByDescending(x => x.ContractPeriod ?? ""),

                
                "revenue" => sortOrder == "asc" ? retiredList.OrderBy(x => x.Revenue ?? 0) : retiredList.OrderByDescending(x => x.Revenue ?? 0),

                "salesteam" => sortOrder == "asc" ? retiredList.OrderBy(x => x.SalesAM ?? "") : retiredList.OrderByDescending(x => x.SalesAM ?? ""),
                "proposaluploaded" => sortOrder == "asc" ? retiredList.OrderBy(x => x.ProposalUploaded ?? "") : retiredList.OrderByDescending(x => x.ProposalUploaded ?? ""),
                _ => retiredList.OrderBy(x => x.PlatformName ?? "")
            };

            int totalRecords = sortedList.Count();
            List<ExternalPlatform> paginatedList = sortedList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.SearchTerm = search ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(paginatedList);
        }

        // View details of a single retired external solution by ID
        public IActionResult ViewDetails(int id)
        {
            if (id <= 0) return BadRequest("Invalid ID.");
            ExternalPlatform? item = _repo.GetRetiredSolutionByIdFull(id);
            return item is null ? NotFound($"Retired external platform with ID {id} not found.") : View(item);
        }

        // Export all retired external platforms to Excel
        [HttpGet]
        public IActionResult ExportAllToExcel()
        {
            string cs = _config.GetConnectionString("DefaultConnection");
            using var conn = new MySqlConnection(cs);
            conn.Open();


            string sql = @"
        SELECT 
            ep.Platform_Name,
            c.Company_Name,
            e1.Emp_Name AS Developed_By,
            ep.Developed_Team,
            st.Sales_Team_Name AS Sales_Team_Involved,
            sp.Phase AS SDLC_Stage,
            ep.LaunchedDate, 
            ep.Platform_OTC AS OneTime_Charge,
            ep.Platform_MRC AS Monthly_Charge,
            ep.Contract_Period,
            ep.Incentive_Earned,
            ep.Incentive_Share AS Incentive_Sharedwith,
            ep.Proposal_Upload AS Proposal_Uploaded,
            
            /* Calculate Revenue (SQL) */
            (
                COALESCE(ep.Platform_OTC, 0) + 
                (COALESCE(ep.Platform_MRC, 0) * 12 * COALESCE(ep.Contract_Period, 0))
            ) AS Revenue

        FROM external_platforms ep
        LEFT JOIN company c ON ep.Company_ID = c.ID
        LEFT JOIN employee e1 ON ep.Developed_By = e1.Emp_ID
        LEFT JOIN sales_team st ON ep.Sales_Team_ID = st.ID
        LEFT JOIN SDLCPhas sp ON ep.SDLCStage = sp.ID

        WHERE (LOWER(TRIM(sp.Phase)) = 'retired' OR LOWER(TRIM(ep.Status)) = 'retired')

        ORDER BY ep.Platform_Name";

            using var da = new MySqlDataAdapter(sql, conn);
            using var dt = new DataTable("ExternalSolutions");
            da.Fill(dt);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Retired Solutions");

                // 1. Defined Headers 
                var headers = new List<string>
        {
            "Platform Name",        // 1
            "Company Name",         // 2
            "Developed By",         // 3
            "Developed Team",       // 4
            "Sales Team Involved",  // 5
            "SDLC Stage",           // 6
            "Launched Date",        // 7
            "One Time Charge",      // 8
            "Monthly Charge",       // 9
            "Contract Period",      // 10
            "Incentive Earned",     // 11
            "Incentive Shared With",// 12
            "Proposal Uploaded",    // 13
            "Revenue"               // 14
        };

                // 2. Set Header Values 
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                // 3. Loop Rows 
                int row = 2;
                foreach (DataRow dr in dt.Rows)
                {
                    worksheet.Cell(row, 1).Value = dr["Platform_Name"]?.ToString();
                    worksheet.Cell(row, 2).Value = dr["Company_Name"]?.ToString();
                    worksheet.Cell(row, 3).Value = dr["Developed_By"]?.ToString();
                    worksheet.Cell(row, 4).Value = dr["Developed_Team"]?.ToString();
                    worksheet.Cell(row, 5).Value = dr["Sales_Team_Involved"]?.ToString();
                    worksheet.Cell(row, 6).Value = dr["SDLC_Stage"]?.ToString();

                    // Date formatting using Helper Method
                    SetDateCell(worksheet, row, 7, dr["LaunchedDate"]);

                    // Number/Currency Formatting using Helper Method
                    SetNumberCell(worksheet, row, 8, dr["OneTime_Charge"]);
                    SetNumberCell(worksheet, row, 9, dr["Monthly_Charge"]);

                    worksheet.Cell(row, 10).Value = dr["Contract_Period"]?.ToString();

                    SetNumberCell(worksheet, row, 11, dr["Incentive_Earned"]);
                    SetNumberCell(worksheet, row, 12, dr["Incentive_Sharedwith"]);

                    worksheet.Cell(row, 13).Value = dr["Proposal_Uploaded"]?.ToString();

                    SetNumberCell(worksheet, row, 14, dr["Revenue"]);

                    row++;
                }

                // 4. Styling 
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
                    string fileName = $"External Solutions - Retired_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // ======================= HELPER METHODS =======================

        // Helper to handle dates from DataRow
        private void SetDateCell(IXLWorksheet ws, int row, int col, object dateValue)
        {
            if (dateValue != null && dateValue != DBNull.Value && DateTime.TryParse(dateValue.ToString(), out DateTime date))
            {
                ws.Cell(row, col).Value = date;
                ws.Cell(row, col).Style.DateFormat.Format = "yyyy-MM-dd";
            }
        }

        // Helper to handle numbers/currency from DataRow
        private void SetNumberCell(IXLWorksheet ws, int row, int col, object value)
        {
            if (value != null && value != DBNull.Value && decimal.TryParse(value.ToString(), out decimal result))
            {
                ws.Cell(row, col).Value = result;
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
            }
            else
            {
            }
        }
    }
}
