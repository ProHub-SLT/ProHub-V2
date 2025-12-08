using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using ProHub.Data;

namespace ProHub.Controllers
{
    public class ExternalPlatformController : Controller
    {
        private ExternalSolutionRepository _externalSolutionRepository;

        public ExternalPlatformController(ExternalSolutionRepository externalSolutionRepository)
        {
            _externalSolutionRepository = externalSolutionRepository;
        }

        // Action method for downloading external backup matrix
        public IActionResult DownloadBackupMatrix()
        {
            // Get all external platforms with backup information
            var platforms = _externalSolutionRepository.GetAllExternalPlatformsWithBackupInfo();

            // Set EPPlus license for non-commercial use
            ExcelPackage.License.SetNonCommercialPersonal("ProHub Application");

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("External Backup Matrix");

                // Add headers
                ws.Cells[1, 1].Value = "Platform Name";
                ws.Cells[1, 2].Value = "Backup Person 1";
                ws.Cells[1, 3].Value = "Backup Person 2";

                // Add data
                int row = 2;
                foreach (var platform in platforms)
                {
                    ws.Cells[row, 1].Value = platform.PlatformName;

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
                string fileName = $"External_Backup_Matrix_{DateTime.Now:yyyy-MM-dd}.xlsx";

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}