using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using OfficeOpenXml; // Excel export
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
        // LOAD ALL ABANDONED SOLUTIONS (USED BY INDEX + EXPORT)
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
                        p.StartDate,
                        e.Emp_Name AS DevelopedByName,
                        s.Phase AS SDLCPhaseName,
                        (
                            SELECT c.Comment
                            FROM Internal_Project_Comments c
                            WHERE c.Solution_ID = p.ID
                            ORDER BY c.Updated_Time DESC
                            LIMIT 1
                        ) AS LastComment
                    FROM Internal_Platforms p
                    INNER JOIN SDLCPhas s ON p.SDLCPhase = s.ID
                    LEFT JOIN Employee e ON p.Developed_By = e.Emp_Id
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
                            StartDate = reader["StartDate"] as DateTime?,

                            DevelopedBy = new Employee
                            {
                                EmpName = reader["DevelopedByName"]?.ToString()
                            },

                            SDLCPhase = new SDLCPhase
                            {
                                Phase = reader["SDLCPhaseName"]?.ToString()
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
        // EXPORT ALL TO EXCEL
        // ============================================================
        public IActionResult ExportAllToExcel()
        {
            var allData = GetAbandonedSolutions("");

            ExcelPackage.License.SetNonCommercialOrganization("ProHub"); 

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Abandoned Solutions");

                // Header row
                ws.Cells[1, 1].Value = "Platform Name";
                ws.Cells[1, 2].Value = "Developed By";
                ws.Cells[1, 3].Value = "SDLC Phase";
                ws.Cells[1, 4].Value = "Start Date";
                ws.Cells[1, 5].Value = "Last Comment";

                int row = 2;

                foreach (var item in allData)
                {
                    ws.Cells[row, 1].Value = item.PlatformName ?? "";
                    ws.Cells[row, 2].Value = item.DevelopedBy?.EmpName ?? "";
                    ws.Cells[row, 3].Value = item.SDLCPhase?.Phase ?? "";
                    ws.Cells[row, 4].Value = item.StartDate?.ToString("yyyy-MM-dd") ?? "";
                    ws.Cells[row, 5].Value = item.ProjectComments?.FirstOrDefault()?.Comment ?? "";

                    row++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"Abandoned_Solutions_{DateTime.Now:yyyy-MM-dd}.xlsx";

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
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

                        FROM Internal_Platforms p
                        LEFT JOIN Employee d ON p.Developed_By = d.Emp_Id
                        LEFT JOIN Employee b1 ON p.BackupOfficer_1 = b1.Emp_Id
                        LEFT JOIN Employee b2 ON p.BackupOfficer_2 = b2.Emp_Id
                        LEFT JOIN TargetEndUser u ON p.EndUserType = u.Id
                        LEFT JOIN ParentProject pr ON p.ParentProjectID = pr.ParentProjectID
                        LEFT JOIN SDLCPhas s ON p.SDLCPhase = s.ID
                        LEFT JOIN Internal_Platforms m ON p.MainAppID = m.ID

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



