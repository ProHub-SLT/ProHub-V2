using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ProHub.Controllers
{
    public class OtherSystemIssuesController : Controller
    {
        private readonly string connectionString;

        public OtherSystemIssuesController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // =============================================
        // INDEX with Search + Sorting + Pagination
        // =============================================
        public IActionResult Index(
            string search = "",
            string sortColumn = "Issue_Start_Time",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10)
        {
            List<OtherSystemIssuesViewModel> issues = new List<OtherSystemIssuesViewModel>();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT
                            osi.OSystem_Issue_ID,
                            osi.Issue_Start_Time,
                            os.OSystem_Name AS System_Name,
                            osi.Description AS Incident_Description,
                            osi.Status,
                            osi.Issue_Closed_Time,
                            osi.Action_Taken,
                            osi.Entered_Time,
                            osi.Reporting_Person_ContactNo,
                            osi.Criticality
                        FROM Other_System_Issues osi
                        INNER JOIN Other_Systems os ON osi.OSystem_ID = os.OSystem_ID
                        WHERE os.OSystem_Name LIKE @search OR osi.Description LIKE @search
                        ORDER BY osi.Issue_Start_Time DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@search", "%" + search + "%");

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                issues.Add(new OtherSystemIssuesViewModel
                                {
                                    OSystem_Issue_ID = reader["OSystem_Issue_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["OSystem_Issue_ID"]),
                                    Issue_Start_Time = reader["Issue_Start_Time"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Issue_Start_Time"]),
                                    System_Name = reader["System_Name"]?.ToString(),
                                    Description = reader["Incident_Description"] == DBNull.Value ? "No description" : reader["Incident_Description"].ToString(),
                                    Status = reader["Status"] == DBNull.Value ? "Unknown" : reader["Status"].ToString(),
                                    Issue_Closed_Time = reader["Issue_Closed_Time"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Issue_Closed_Time"]),
                                    Action_Taken = reader["Action_Taken"] == DBNull.Value ? "No action taken" : reader["Action_Taken"].ToString(),
                                    Entered_Time = reader["Entered_Time"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Entered_Time"]),
                                    Reporting_Person_ContactNo = reader["Reporting_Person_ContactNo"] == DBNull.Value ? "N/A" : reader["Reporting_Person_ContactNo"].ToString(),
                                    Criticality = reader["Criticality"] == DBNull.Value ? "Medium" : reader["Criticality"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading system issues: " + ex.Message;
            }

            // =============================================
            // Sorting Logic
            // =============================================
            issues = sortColumn switch
            {
                "System_Name" => sortOrder == "asc"
                    ? issues.OrderBy(i => i.System_Name).ToList()
                    : issues.OrderByDescending(i => i.System_Name).ToList(),

                "Status" => sortOrder == "asc"
                    ? issues.OrderBy(i => i.Status).ToList()
                    : issues.OrderByDescending(i => i.Status).ToList(),

                "Criticality" => sortOrder == "asc"
                    ? issues.OrderBy(i => i.Criticality).ToList()
                    : issues.OrderByDescending(i => i.Criticality).ToList(),

                "Issue_Start_Time" or _ => sortOrder == "asc"
                    ? issues.OrderBy(i => i.Issue_Start_Time).ToList()
                    : issues.OrderByDescending(i => i.Issue_Start_Time).ToList(),
            };

            // =============================================
            // Pagination Logic
            // =============================================
            var totalRecords = issues.Count();
            var paginatedList = issues.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // =============================================
            // ViewBag Values Required by You
            // =============================================
            ViewBag.SearchTerm = search;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            ViewBag.StartRecord = totalRecords > 0 ? (page - 1) * pageSize + 1 : 0;
            ViewBag.EndRecord = Math.Min(page * pageSize, totalRecords);

            return View(paginatedList);
        }
    }
}
