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
            var allData = _consumerPlatformRepository.GetConsumerPlatform(""); // get all consumer platforms

            // Set EPPlus license for non-commercial use
            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Consumer Platforms");

                // Header row
                ws.Cells[1, 1].Value = "Application Name";
                ws.Cells[1, 2].Value = "Developed By";
                ws.Cells[1, 3].Value = "SDLC Phase";
                ws.Cells[1, 4].Value = "Start Date";
                ws.Cells[1, 5].Value = "Target Date";
                ws.Cells[1, 6].Value = "End User Type";
                ws.Cells[1, 7].Value = "Solution Value (LKR)";

                int row = 2;
                foreach (var item in allData)
                {
                    ws.Cells[row, 1].Value = item.AppName ?? "";
                    ws.Cells[row, 2].Value = item.DevelopedBy?.EmpName ?? "";
                    ws.Cells[row, 3].Value = item.SDLCPhase?.Phase ?? "";

                    ws.Cells[row, 4].Value = "";
                    ws.Cells[row, 5].Value = "";

                    ws.Cells[row, 6].Value = item.EndUserType?.EndUserType ?? "";

                    ws.Cells[row, 7].Value = item.Price?.ToString("N2") ?? "";

                    row++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"Consumer_Platforms_{DateTime.Now:yyyy-MM-dd}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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