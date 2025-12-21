using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using ProHub.Data;

namespace ProHub.Controllers
{
    public class ConsumerPlatformController : Controller
    {
        private ConsumerPlatformRepository _consumerPlatformRepository;

        public ConsumerPlatformController(ConsumerPlatformRepository consumerPlatformRepository)
        {
            _consumerPlatformRepository = consumerPlatformRepository;
        }

        // Main View
        public IActionResult Index(string search = "", string sortColumn = "AppName", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            var consumerList = _consumerPlatformRepository.GetConsumerPlatform(search);

            // ✅ Sorting logic
            consumerList = sortColumn switch
            {
                "AppName" => sortOrder == "asc" ? consumerList.OrderBy(x => x.AppName).ToList() : consumerList.OrderByDescending(x => x.AppName).ToList(),
                "DevelopedBy" => sortOrder == "asc" ? consumerList.OrderBy(x => x.DevelopedBy?.EmpName ?? "").ToList() : consumerList.OrderByDescending(x => x.DevelopedBy?.EmpName ?? "").ToList(),
                "SolutionValue" => sortOrder == "asc" ? consumerList.OrderBy(x => x.Price).ToList() : consumerList.OrderByDescending(x => x.Price).ToList(),
                "SDLCPhase" => sortOrder == "asc" ? consumerList.OrderBy(x => x.SDLCPhase?.Phase ?? "").ToList() : consumerList.OrderByDescending(x => x.SDLCPhase?.Phase ?? "").ToList(),
                _ => consumerList.OrderBy(x => x.AppName).ToList()
            };

            var totalRecords = consumerList.Count();

            // ✅ Pagination
            var paginatedList = consumerList
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

        // ✅ View details of a single record
        public IActionResult ViewDetails(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid ID.");

            var platform = _consumerPlatformRepository.GetConsumerPlatformById(id);

            if (platform == null)
                return NotFound();

            return View(platform);
        }

        public IActionResult ExportAllToExcel()
        {
            // Getiing Data 
            var allData = _consumerPlatformRepository.GetConsumerPlatform("");

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Consumer Platforms");

                // ✅ 1. Headers 
                string[] headers = {
            "App Group",        // 1
            "App Name",         // 2
            "Type",             // 3 
            "Developed By",     // 4
            "App URL",          // 5
            "App IP",           // 6
            "SDLC Stage",       // 7
            "Start Date",       // 8
            "Target Date",      // 9
            "VA Date",          // 10
            "Percentage Done",  // 11
            "Launched Date",    // 12
            "Current Status",   // 13
            "Price (LKR)",      // 14
            "Comment",          // 15
            "End User Type"     // 16
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                }

                // ✅ 2. Header Styling
                var headerRange = ws.Range(1, 1, 1, 16);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#007BFF");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // ✅ 3. Data Loop
                int row = 2;
                foreach (var item in allData)
                {
                    ws.Cell(row, 1).Value = item.ParentProject?.ParentProjectGroup ?? ""; 
                    ws.Cell(row, 2).Value = item.AppName ?? "";                           
                    if (!string.IsNullOrEmpty(item.MainApp?.AppName))
                    {
                        ws.Cell(row, 3).Value = $"CR of {item.MainApp.AppName}";
                    }
                    else
                    {
                        ws.Cell(row, 3).Value = "Main Application";
                    }
                    ws.Cell(row, 4).Value = item.DevelopedBy?.EmpName ?? "";              
                    ws.Cell(row, 5).Value = item.AppURL ?? "";                            
                    ws.Cell(row, 6).Value = item.AppIP ?? "";                             
                    ws.Cell(row, 7).Value = item.SDLCPhase?.Phase ?? "";                  

                    // Date Formatting
                    ws.Cell(row, 8).Value = item.StartDate;                               
                    ws.Cell(row, 9).Value = item.TargetDate;                              
                    ws.Cell(row, 10).Value = item.VADate;                                 

                    ws.Cell(row, 11).Value = item.PercentageDone.HasValue ? item.PercentageDone.Value / 100 : 0; 
                    ws.Cell(row, 11).Style.NumberFormat.Format = "0%";

                    ws.Cell(row, 12).Value = item.LaunchedDate;                           
                    ws.Cell(row, 13).Value = item.Status ?? "";                           

                    // Price Formatting
                    if (item.Price.HasValue)
                    {
                        ws.Cell(row, 14).Value = item.Price.Value;
                        ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
                    }
                    else
                    {
                        ws.Cell(row, 14).Value = 0;
                    }

                    ws.Cell(row, 15).Value = item.DPOHandoverComment ?? "";               
                    ws.Cell(row, 16).Value = item.EndUserType?.EndUserType ?? "";         

                    row++;
                }

                // ✅ 4. Auto fit columns
                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    string fileName = $"Consumer Service Platform_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx";

                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // New action method for downloading backup matrix
        public IActionResult DownloadBackupMatrix()
        {
            // Get all internal platforms with backup information
            var platforms = _consumerPlatformRepository.GetAllInternalPlatformsWithBackupInfo();

            // Set EPPlus license for non-commercial use
            ExcelPackage.License.SetNonCommercialPersonal("ProHub Application");

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Backup Matrix");

                // Add headers
                ws.Cells[1, 1].Value = "Application Name";
                ws.Cells[1, 2].Value = "Backup Person 1";
                ws.Cells[1, 3].Value = "Backup Person 2";

                // Add data
                int row = 2;
                foreach (var platform in platforms)
                {
                    ws.Cells[row, 1].Value = platform.AppName;

                    // Check if Backup Officer 1 exists
                    if (platform.BackupOfficer1 != null && !string.IsNullOrEmpty(platform.BackupOfficer1.EmpName))
                    {
                        ws.Cells[row, 2].Value = platform.BackupOfficer1.EmpName;
                    }

                    // Check if Backup Officer 2 exists
                    if (platform.BackupOfficer2 != null && !string.IsNullOrEmpty(platform.BackupOfficer2.EmpName))
                    {
                        ws.Cells[row, 3].Value = platform.BackupOfficer2.EmpName;
                    }

                    row++;
                }

                ws.Cells.AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"Backup_Matrix_{DateTime.Now:yyyy-MM-dd}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}