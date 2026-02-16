using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using ProHub.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProHub.Controllers
{
    public class InternalSolutionsAbandonedController : Controller
    {
        private readonly string connectionString;

        public InternalSolutionsAbandonedController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // ============================================================
        // INDEX WITH SEARCH + SORT + PAGINATION
        // ============================================================
        public IActionResult Index(string search = "", string sortColumn = "PlatformName",
                                  string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            
            List<InternalPlatform> abandonedList = GetAbandonedSolutions(search);

            // ======================
            //   APPLY SORTING
            // ======================
            abandonedList = sortColumn switch
            {
                "PlatformName" => sortOrder == "asc"
                    ? abandonedList.OrderBy(x => x.PlatformName).ToList()
                    : abandonedList.OrderByDescending(x => x.PlatformName).ToList(),

                "DevelopedBy" => sortOrder == "asc"
                    ? abandonedList.OrderBy(x => x.DevelopedBy?.EmpName).ToList()
                    : abandonedList.OrderByDescending(x => x.DevelopedBy?.EmpName).ToList(),

                "StartDate" => sortOrder == "asc"
                    ? abandonedList.OrderBy(x => x.StartDate).ToList()
                    : abandonedList.OrderByDescending(x => x.StartDate).ToList(),

                _ => abandonedList.OrderBy(x => x.PlatformName).ToList()
            };

            // Total record count
            var totalRecords = abandonedList.Count;

            // ======================
            //     PAGINATION
            // ======================
            var paginated = abandonedList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass values to View
            ViewBag.SearchTerm = search ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(paginated);
        }

        // ============================================================
        // LOAD ALL ABANDONED SOLUTIONS (UPDATED SQL QUERY)
        // ============================================================
        private List<InternalPlatform> GetAbandonedSolutions(string search)
        {
            List<InternalPlatform> list = new List<InternalPlatform>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                
                string query = @"
            SELECT
                p.ID AS Id,
                p.App_Name AS PlatformName,
                p.App_URL AS AppURL,
                p.App_IP AS AppIP,
                p.StartDate,
                p.TargetDate,
                p.VADate,
                p.LaunchedDate,
                p.PercentageDone,
                p.Status,
                p.Price,
                
                -- Joined Columns
                e.Emp_Name AS DevelopedByName,
                s.Phase AS SDLCPhaseName,
                pr.ParentProjectGroup,
                m.App_Name AS MainAppName,

                (
                    SELECT c.Comment
                    FROM Internal_Project_Comments c
                    WHERE c.Solution_ID = p.ID
                    ORDER BY c.Updated_Time DESC
                    LIMIT 1
                ) AS LastComment

            FROM internal_platforms p
            INNER JOIN SDLCPhas s ON p.SDLCPhase = s.ID
            LEFT JOIN employee e ON p.Developed_By = e.Emp_Id
            LEFT JOIN ParentProject pr ON p.ParentProjectID = pr.ParentProjectID
            LEFT JOIN internal_platforms m ON p.MainAppID = m.ID -- JOIN Self to get Main App Name
            
            WHERE s.Phase = 'Abandoned'
              AND (p.App_Name LIKE @search OR e.Emp_Name LIKE @search);
        ";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@search", "%" + search + "%");

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new InternalPlatform
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            PlatformName = reader["PlatformName"]?.ToString(),
                            MainAppName = reader["MainAppName"]?.ToString(),

                            AppURL = reader["AppURL"]?.ToString(),
                            AppIP = reader["AppIP"]?.ToString(),
                            Status = reader["Status"]?.ToString(),
                            PercentageDone = reader["PercentageDone"] as decimal?,
                            Price = reader["Price"] as decimal?,

                            StartDate = reader["StartDate"] as DateTime?,
                            TargetDate = reader["TargetDate"] as DateTime?,
                            VADate = reader["VADate"] as DateTime?,
                            LaunchedDate = reader["LaunchedDate"] as DateTime?,

                            DevelopedBy = new Employee
                            {
                                EmpName = reader["DevelopedByName"]?.ToString()
                            },

                            SDLCPhase = new SDLCPhase
                            {
                                Phase = reader["SDLCPhaseName"]?.ToString()
                            },

                            ParentProject = new ParentProject
                            {
                                ParentProjectGroup = reader["ParentProjectGroup"]?.ToString()
                            },

                            MainApp = new InternalPlatform
                            {
                                AppName = reader["MainAppName"]?.ToString()
                            },

                            ProjectComments = new List<InternalProjectComment>
                    {
                        new InternalProjectComment
                        {
                            Comment = reader["LastComment"]?.ToString()
                        }
                    }
                        });
                    }
                }
            }

            return list;
        }
        // ============================================================
        // EXPORT ALL TO EXCEL (15 Columns using ClosedXML)
        // ============================================================
        public IActionResult ExportAllToExcel()
        {

            var allData = GetAbandonedSolutions("");

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Abandoned Solutions");

                // 1. Headers List 
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
            "Comment"          // 15
        };

                // 2. Set Header Values
                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                int row = 2;
                foreach (var item in allData)
                {
                    // 1. AppGroup
                    worksheet.Cell(row, 1).Value = item.ParentProject?.ParentProjectGroup ?? "";

                    // 2. App_Name
                    worksheet.Cell(row, 2).Value = item.PlatformName ?? "";

                    // 3. Type Logic (Main vs CR) - Use MainAppName 
                    if (!string.IsNullOrEmpty(item.MainApp?.AppName))
                    {
                        worksheet.Cell(row, 3).Value = $"CR of {item.MainApp.AppName}";
                    }
                    else
                    {
                        worksheet.Cell(row, 3).Value = "Main Application";
                    }

                    // 4. Developed_By
                    worksheet.Cell(row, 4).Value = item.DevelopedBy?.EmpName ?? "";

                    // 5. App_URL
                    worksheet.Cell(row, 5).Value = item.AppURL ?? "";

                    // 6. App_IP
                    worksheet.Cell(row, 6).Value = item.AppIP ?? "";

                    // 7. SDLC_Stage
                    worksheet.Cell(row, 7).Value = item.SDLCPhase?.Phase ?? "";

                    // 8, 9, 10. Dates
                    SetDateCell(worksheet, row, 8, item.StartDate);
                    SetDateCell(worksheet, row, 9, item.TargetDate);
                    SetDateCell(worksheet, row, 10, item.VADate);

                    // 11. Percentage_Done
                    worksheet.Cell(row, 11).Value = item.PercentageDone.HasValue ? item.PercentageDone.Value : 0;

                    // 12. Launched_Date
                    SetDateCell(worksheet, row, 12, item.LaunchedDate);

                    // 13. Current_Status
                    worksheet.Cell(row, 13).Value = item.Status ?? "";

                    // 14. Price (Currency Formatting)
                    if (item.Price.HasValue)
                    {
                        worksheet.Cell(row, 14).Value = item.Price.Value;
                        worksheet.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";
                    }

                    // 15. Comment
                    worksheet.Cell(row, 15).Value = item.ProjectComments?.FirstOrDefault()?.Comment ?? "";

                    row++;
                }

                // 3. Styling Headers
                var headerRange = worksheet.Range(1, 1, 1, headers.Count);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#007BFF");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Auto Fit Columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"Internal Solutions - Abandoned_{DateTime.Now:yyyy-MM-dd}.xlsx";

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

        // ============================================================
        // DETAILS (UNCHANGED)
        // ============================================================
        public IActionResult Details(int id)
        {
            InternalPlatform details = null;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT
                            p.ID as Id,
                            p.App_Name as AppName,

                            -- Developer
                            p.Developed_By as DevelopedById,
                            d.Emp_Name AS DevelopedByName,

                            p.Developed_Team as DevelopedTeam,
                            p.StartDate,
                            p.TargetDate,
                            p.BitBucket,
                            p.BIT_bucket_repo as BitBucketRepo,

                            -- SDLC
                            p.SDLCPhase as SDLCPhaseId,
                            s.Phase AS SDLCPhaseName,

                            p.PercentageDone,
                            p.Status,
                            p.StatusDate,
                            p.Bus_Owner as BusOwner,
                            p.App_Category as AppCategory,
                            p.Scope,
                            p.App_IP as AppIP,
                            p.App_URL as AppURL,
                            p.App_Users as AppUsers,
                            p.UATDate,
                            p.Integrated_Apps as IntegratedApps,
                            p.DR,
                            p.LaunchedDate,
                            p.VADate,
                            p.WAF,
                            p.APP_OP_Owner as APPOwner,
                            p.App_Business_Owner as AppBusinessOwner,
                            p.Price,

                            -- End user type
                            p.EndUserType as EndUserTypeId,
                            u.EndUserType AS EndUserTypeName,

                            p.RequestNo,

                            -- Parent project
                            p.ParentProjectID,
                            pr.ParentProjectGroup AS ParentProjectName,

                            p.SLA,

                            -- Backup officers
                            p.BackupOfficer_1 as BackupOfficer1Id,
                            b1.Emp_Name AS BackupOfficer1Name,

                            p.BackupOfficer_2 as BackupOfficer2Id,
                            b2.Emp_Name AS BackupOfficer2Name,

                            -- Main App
                            p.MainAppID,
                            m.App_Name AS MainAppName,

                            p.SSLCertificateExpDate

                        FROM internal_platforms p
                        LEFT JOIN employee d ON p.Developed_By = d.Emp_Id
                        LEFT JOIN employee b1 ON p.BackupOfficer_1 = b1.Emp_Id
                        LEFT JOIN employee b2 ON p.BackupOfficer_2 = b2.Emp_Id
                        LEFT JOIN TargetEndUser u ON p.EndUserType = u.Id
                        LEFT JOIN ParentProject pr ON p.ParentProjectID = pr.ParentProjectID
                        LEFT JOIN SDLCPhas s ON p.SDLCPhase = s.ID
                        LEFT JOIN internal_platforms m ON p.MainAppID = m.ID

                        WHERE p.ID = @Id;
                    ";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            details = new InternalPlatform
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                AppName = reader["AppName"].ToString(),

                                DevelopedById = reader["DevelopedById"] as int?,
                                DevelopedBy = new Employee
                                {
                                    EmpName = reader["DevelopedByName"]?.ToString()
                                },

                                DevelopedTeam = reader["DevelopedTeam"]?.ToString(),
                                StartDate = reader["StartDate"] as DateTime?,
                                TargetDate = reader["TargetDate"] as DateTime?,
                                BitBucket = reader["BitBucket"]?.ToString(),
                                BitBucketRepo = reader["BitBucketRepo"]?.ToString(),

                                SDLCPhaseId = reader["SDLCPhaseId"] as int?,
                                SDLCPhase = new SDLCPhase
                                {
                                    Phase = reader["SDLCPhaseName"]?.ToString()
                                },

                                PercentageDone = reader["PercentageDone"] as decimal?,
                                Status = reader["Status"]?.ToString(),
                                StatusDate = reader["StatusDate"] as DateTime?,
                                BusOwner = reader["BusOwner"]?.ToString(),
                                AppCategory = reader["AppCategory"]?.ToString(),
                                Scope = reader["Scope"]?.ToString(),
                                AppIP = reader["AppIP"]?.ToString(),
                                AppURL = reader["AppURL"]?.ToString(),
                                AppUsers = reader["AppUsers"]?.ToString(),
                                UATDate = reader["UATDate"] as DateTime?,
                                IntegratedApps = reader["IntegratedApps"]?.ToString(),
                                DR = reader["DR"]?.ToString(),
                                LaunchedDate = reader["LaunchedDate"] as DateTime?,
                                VADate = reader["VADate"] as DateTime?,
                                WAF = reader["WAF"]?.ToString(),
                                APPOwner = reader["APPOwner"]?.ToString(),
                                AppBusinessOwner = reader["AppBusinessOwner"]?.ToString(),
                                Price = reader["Price"] as decimal?,

                                EndUserTypeId = reader["EndUserTypeId"] as int?,
                                EndUserType = new TargetEndUser
                                {
                                    EndUserType = reader["EndUserTypeName"]?.ToString()
                                },

                                RequestNo = reader["RequestNo"]?.ToString(),

                                ParentProjectID = reader["ParentProjectID"] as int?,
                                ParentProject = new ParentProject
                                {
                                    ParentProjectGroup = reader["ParentProjectName"]?.ToString()
                                },

                                SLA = reader["SLA"]?.ToString(),

                                BackupOfficer1Id = reader["BackupOfficer1Id"] as int?,
                                BackupOfficer1 = new Employee
                                {
                                    EmpName = reader["BackupOfficer1Name"]?.ToString()
                                },

                                BackupOfficer2Id = reader["BackupOfficer2Id"] as int?,
                                BackupOfficer2 = new Employee
                                {
                                    EmpName = reader["BackupOfficer2Name"]?.ToString()
                                },

                                MainAppID = reader["MainAppID"] as int?,
                                MainApp = new InternalPlatform
                                {
                                    AppName = reader["MainAppName"]?.ToString()
                                },

                                SSLCertificateExpDate = reader["SSLCertificateExpDate"] as DateTime?
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading details: {ex.Message}";
            }

            if (details == null)
            {
                return NotFound("Record not found.");
            }

            return View(details);
        }
    }
}



