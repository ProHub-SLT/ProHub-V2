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
    public class RevenueCalController : Controller
    {
        private readonly IConfiguration _configuration;

        public RevenueCalController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // --------------------------------------------------
        // PROJECTED REVENUE
        // --------------------------------------------------
        public IActionResult ProjectedRevenue(string sortColumn = "Project_Year", string sortOrder = "asc")
        {
            var list = Calculate_Projected_Revenue();

            // SORTING
            list = SortYearlyRevenue(list, sortColumn, sortOrder);

            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(list);
        }

        public IActionResult ProjectedRevenue_Details(int selected_year, string sortColumn = "Project_Name", string sortOrder = "asc")
        {
            var list = Calculate_Projected_Revenue_Details(selected_year);

            // SORTING
            list = SortProjectRevenue(list, sortColumn, sortOrder);

            ViewBag.SelectedYear = selected_year;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(list);
        }

        // --------------------------------------------------
        // ACTUAL REVENUE
        // --------------------------------------------------
        public IActionResult ActualRevenue(string sortColumn = "Project_Year", string sortOrder = "asc")
        {
            var list = Calculate_Actual_Revenue();

            // SORTING
            list = SortYearlyRevenue(list, sortColumn, sortOrder);

            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(list);
        }

        public IActionResult ActualRevenue_Details(int selected_year, string sortColumn = "Project_Name", string sortOrder = "asc")
        {
            var list = Calculate_Actual_Revenue_Details(selected_year);

            // SORTING
            list = SortProjectRevenue(list, sortColumn, sortOrder);

            ViewBag.SelectedYear = selected_year;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(list);
        }

        // --------------------------------------------------
        // INTERNAL PROJECT VALUE
        // --------------------------------------------------
        public IActionResult InternalProjectValue(string sortColumn = "Project_Year", string sortOrder = "asc")
        {
            var list = Calculate_Internal_Project_Value();

            // SORTING
            list = SortYearlyRevenue(list, sortColumn, sortOrder);

            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(list);
        }


        public IActionResult InternalProjectValue_Details(int selected_year, string sortColumn = "Project_Name", string sortOrder = "asc")
        {
            var list = Calculate_Internal_Project_Value_Details(selected_year);

            // SORTING
            list = SortProjectRevenue(list, sortColumn, sortOrder);

            ViewBag.SelectedYear = selected_year;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(list);
        }





        // --------------------------------------------------
        // SORTING HELPERS
        // --------------------------------------------------
        private List<YearlyRevenue> SortYearlyRevenue(List<YearlyRevenue> list, string sortColumn, string sortOrder)
        {
            return sortColumn switch
            {
                "Project_Year" => Sort(list, x => x.Project_Year, sortOrder),
                "Total_OTC" => Sort(list, x => x.Total_OTC, sortOrder),
                "Total_MRC" => Sort(list, x => x.Total_MRC, sortOrder),
                "Grand_Total" => Sort(list, x => x.Grand_Total, sortOrder),
                _ => Sort(list, x => x.Project_Year, sortOrder)
            };
        }

        private List<ProjectRevenue> SortProjectRevenue(List<ProjectRevenue> list, string sortColumn, string sortOrder)
        {
            return sortColumn switch
            {
                "Project_Name" => Sort(list, x => x.Project_Name, sortOrder),
                "Start_Year" => Sort(list, x => x.Start_Year, sortOrder),
                "Months_To_Current" => Sort(list, x => x.Months_To_Current, sortOrder),
                "OTC" => Sort(list, x => x.OTC, sortOrder),
                "MRC" => Sort(list, x => x.MRC, sortOrder),
                "MRC_For_Year" => Sort(list, x => x.MRC_For_Year, sortOrder),
                "MRC_For_StartYear" => Sort(list, x => x.MRC_For_StartYear, sortOrder),
                _ => Sort(list, x => x.Project_Name, sortOrder)
            };
        }

        private List<T> Sort<T, TKey>(List<T> list, Func<T, TKey> keySelector, string sortOrder)
        {
            return sortOrder == "asc"
                ? list.OrderBy(keySelector).ToList()
                : list.OrderByDescending(keySelector).ToList();
        }

        // --------------------------------------------------
        // EXISTING METHODS — REMAIN EXACTLY SAME
        // --------------------------------------------------

        private List<YearlyRevenue> Calculate_Projected_Revenue()
        {
            // YOUR ORIGINAL FULL METHOD (NO CHANGE)
            // ---------------------------------------
            List<YearlyRevenue> result = new List<YearlyRevenue>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = @"SELECT Platform_Name, Platform_MRC, Platform_OTC, Contract_Period, LaunchedDate
                                 FROM external_platforms
                                 WHERE LaunchedDate IS NOT NULL
                                 ORDER BY LaunchedDate";

                DataTable dt = new DataTable();
                dt.Columns.Add("Project_Year", typeof(int));
                dt.Columns.Add("Total_OTC");
                dt.Columns.Add("Total_MRC");

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime launchedDate = reader.GetDateTime("LaunchedDate");
                        int year = launchedDate.Year;
                        decimal otc = reader["Platform_OTC"] == DBNull.Value ? 0 : reader.GetDecimal("Platform_OTC");
                        decimal mrc = reader["Platform_MRC"] == DBNull.Value ? 0 : reader.GetDecimal("Platform_MRC");
                        int period = reader["Contract_Period"] == DBNull.Value ? 0 : reader.GetInt32("Contract_Period");

                        decimal totalMRC = mrc * (period * 12);

                        DataRow[] drs = dt.Select("Project_Year=" + year);
                        if (drs.Length > 0)
                        {
                            drs[0]["Total_OTC"] = Convert.ToDecimal(drs[0]["Total_OTC"]) + otc;
                            drs[0]["Total_MRC"] = Convert.ToDecimal(drs[0]["Total_MRC"]) + totalMRC;
                        }
                        else
                        {
                            DataRow ndr = dt.NewRow();
                            ndr["Project_Year"] = year;
                            ndr["Total_OTC"] = otc;
                            ndr["Total_MRC"] = totalMRC;
                            dt.Rows.Add(ndr);
                        }
                    }
                }

                foreach (DataRow dr in dt.Rows)
                {
                    result.Add(new YearlyRevenue
                    {
                        Project_Year = Convert.ToInt32(dr["Project_Year"]),
                        Total_OTC = Convert.ToDecimal(dr["Total_OTC"]),
                        Total_MRC = Convert.ToDecimal(dr["Total_MRC"]),
                        Grand_Total = Convert.ToDecimal(dr["Total_OTC"]) + Convert.ToDecimal(dr["Total_MRC"])
                    });
                }
            }

            return result;
        }

        private List<ProjectRevenue> Calculate_Projected_Revenue_Details(int selected_year)
        {
            // ORIGINAL METHOD WITHOUT CHANGES
            List<ProjectRevenue> result = new List<ProjectRevenue>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = @"SELECT Platform_Name, Platform_MRC, Platform_OTC, Contract_Period, LaunchedDate
                                 FROM external_platforms
                                 WHERE LaunchedDate IS NOT NULL AND YEAR(LaunchedDate) = @year";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@year", selected_year);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime launchedDate = reader.GetDateTime("LaunchedDate");
                            decimal mrc = reader["Platform_MRC"] == DBNull.Value ? 0 : reader.GetDecimal("Platform_MRC");
                            decimal otc = reader["Platform_OTC"] == DBNull.Value ? 0 : reader.GetDecimal("Platform_OTC");
                            int period = reader["Contract_Period"] == DBNull.Value ? 0 : reader.GetInt32("Contract_Period");

                            result.Add(new ProjectRevenue
                            {
                                Project_Name = reader["Platform_Name"].ToString(),
                                Start_Year = launchedDate.Year,
                                MRC = mrc,
                                Contract_Period = period,
                                Months_To_Current = period * 12,
                                OTC = otc,
                                MRC_For_Year = mrc * period * 12
                            });
                        }
                    }
                }
            }

            result.Add(new ProjectRevenue
            {
                Project_Name = "Total",
                OTC = result.Sum(x => x.OTC),
                MRC = result.Sum(x => x.MRC),
                MRC_For_Year = result.Sum(x => x.MRC_For_Year)
            });

            return result;
        }

        private List<YearlyRevenue> Calculate_Actual_Revenue()
        {
            // ORIGINAL METHOD UNCHANGED
            List<YearlyRevenue> result = new List<YearlyRevenue>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = @"SELECT Platform_Name, Platform_MRC, Platform_OTC, Contract_Period, LaunchedDate
                                 FROM external_platforms
                                 WHERE LaunchedDate IS NOT NULL
                                 ORDER BY LaunchedDate";

                DataTable dt = new DataTable();
                dt.Columns.Add("Project_Year", typeof(int));
                dt.Columns.Add("Total_OTC");
                dt.Columns.Add("MRC_For_StartYear");
                dt.Columns.Add("Total_MRC");

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime startDate = reader.GetDateTime("LaunchedDate");
                        int startYear = startDate.Year;
                        int startMonth = startDate.Month;
                        int period = reader["Contract_Period"] == DBNull.Value ? 0 : reader.GetInt32("Contract_Period");
                        decimal otc = reader["Platform_OTC"] == DBNull.Value ? 0 : reader.GetDecimal("Platform_OTC");
                        decimal mrc = reader["Platform_MRC"] == DBNull.Value ? 0 : reader.GetDecimal("Platform_MRC");

                        int endYear = startYear + period;

                        for (int y = startYear; y <= endYear; y++)
                        {
                            DataRow[] drs = dt.Select("Project_Year=" + y);
                            if (drs.Length > 0)
                            {
                                if (y == startYear)
                                    drs[0]["MRC_For_StartYear"] = Convert.ToDecimal(drs[0]["MRC_For_StartYear"]) + mrc * (12 - startMonth);
                                else if (y == endYear)
                                    drs[0]["Total_MRC"] = Convert.ToDecimal(drs[0]["Total_MRC"]) + mrc * startMonth;
                                else
                                    drs[0]["Total_MRC"] = Convert.ToDecimal(drs[0]["Total_MRC"]) + mrc * 12;

                                if (y == startYear)
                                    drs[0]["Total_OTC"] = Convert.ToDecimal(drs[0]["Total_OTC"]) + otc;
                            }
                            else
                            {
                                DataRow ndr = dt.NewRow();
                                ndr["Project_Year"] = y;

                                if (y == startYear)
                                {
                                    ndr["Total_OTC"] = otc;
                                    ndr["MRC_For_StartYear"] = mrc * (12 - startMonth);
                                    ndr["Total_MRC"] = 0;
                                }
                                else if (y == endYear)
                                {
                                    ndr["Total_OTC"] = 0;
                                    ndr["MRC_For_StartYear"] = 0;
                                    ndr["Total_MRC"] = mrc * startMonth;
                                }
                                else
                                {
                                    ndr["Total_OTC"] = 0;
                                    ndr["MRC_For_StartYear"] = 0;
                                    ndr["Total_MRC"] = mrc * 12;
                                }

                                dt.Rows.Add(ndr);
                            }
                        }
                    }
                }

                foreach (DataRow dr in dt.Rows)
                {
                    result.Add(new YearlyRevenue
                    {
                        Project_Year = Convert.ToInt32(dr["Project_Year"]),
                        Total_OTC = Convert.ToDecimal(dr["Total_OTC"]),
                        MRC_For_StartYear = Convert.ToDecimal(dr["MRC_For_StartYear"]),
                        Total_MRC = Convert.ToDecimal(dr["Total_MRC"]),
                        Grand_Total = Convert.ToDecimal(dr["Total_OTC"]) + Convert.ToDecimal(dr["Total_MRC"]) + Convert.ToDecimal(dr["MRC_For_StartYear"])
                    });
                }
            }

            return result;
        }

        private List<ProjectRevenue> Calculate_Actual_Revenue_Details(int selected_year)
        {
            // ORIGINAL METHOD NO CHANGE
            List<ProjectRevenue> result = new List<ProjectRevenue>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = @"SELECT Platform_Name, Platform_MRC, Platform_OTC, Contract_Period, LaunchedDate
                                 FROM external_platforms
                                 WHERE LaunchedDate IS NOT NULL 
                                 AND YEAR(LaunchedDate) <= @year 
                                 AND YEAR(LaunchedDate) + Contract_Period >= @year
                                 ORDER BY LaunchedDate";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@year", selected_year);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime startDate = reader.GetDateTime("LaunchedDate");
                            int startYear = startDate.Year;
                            int startMonth = startDate.Month;
                            int period = reader["Contract_Period"] == DBNull.Value ? 0 : reader.GetInt32("Contract_Period");
                            decimal mrc = reader["Platform_MRC"] == DBNull.Value ? 0 : reader.GetDecimal("Platform_MRC");
                            decimal otc = reader["Platform_OTC"] == DBNull.Value ? 0 : reader.GetDecimal("Platform_OTC");

                            int endYear = startYear + period;

                            ProjectRevenue proj = new ProjectRevenue
                            {
                                Project_Name = reader["Platform_Name"].ToString(),
                                Start_Year = startYear,
                                Start_Month = startMonth,
                                Contract_Period = period,
                                MRC = mrc,

                            };

                            if (startYear == selected_year)
                            {
                                proj.Months_To_Current = 12 - startMonth;
                                proj.OTC = otc;
                                proj.MRC_For_StartYear = proj.Months_To_Current * mrc;
                            }
                            else if (endYear == selected_year)
                            {
                                proj.Months_To_Current = startMonth;
                                proj.MRC_For_Year = proj.Months_To_Current * mrc;
                            }
                            else
                            {
                                proj.Months_To_Current = 12;
                                proj.MRC_For_Year = 12 * mrc;
                            }

                            result.Add(proj);
                        }
                    }
                }
            }

            result.Add(new ProjectRevenue
            {
                Project_Name = "Total",
                OTC = result.Sum(x => x.OTC),
                MRC_For_Year = result.Sum(x => x.MRC_For_Year),
                MRC_For_StartYear = result.Sum(x => x.MRC_For_StartYear)
            });

            return result;
        }

        private List<YearlyRevenue> Calculate_Internal_Project_Value()
        {
            List<YearlyRevenue> result = new List<YearlyRevenue>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                string query = @"
            SELECT ip.App_Name, ip.Price, ip.LaunchedDate, sp.Phase AS SDLCPhaseName
            FROM Internal_Platforms ip
            LEFT JOIN SDLCPhas sp ON ip.SDLCPhase = sp.ID
            WHERE ip.LaunchedDate IS NOT NULL AND ip.Price > 0
            ORDER BY ip.LaunchedDate;
        ";

                DataTable dt = new DataTable();
                dt.Columns.Add("Project_Year", typeof(int));
                dt.Columns.Add("Project_Value", typeof(decimal));
                dt.Columns.Add("Maintenance_Effort_Value", typeof(decimal));
                dt.Columns.Add("Grand_Total", typeof(decimal));

                int currentYear = DateTime.Today.Year;

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime launchedDate = reader.GetDateTime("LaunchedDate");
                        decimal price = reader["Price"] == DBNull.Value ? 0 : reader.GetDecimal("Price");
                        string phase = reader["SDLCPhaseName"]?.ToString() ?? "";
                        int startYear = launchedDate.Year;

                        for (int y = startYear; y <= currentYear; y++)
                        {
                            DataRow[] rows = dt.Select("Project_Year = " + y);

                            if (rows.Length > 0)
                            {
                                DataRow row = rows[0];

                                if (y == startYear)
                                {
                                    row["Project_Value"] = Convert.ToDecimal(row["Project_Value"]) + price;
                                }
                                else if (phase == "Maintenance")
                                {
                                    row["Maintenance_Effort_Value"] =
                                        Convert.ToDecimal(row["Maintenance_Effort_Value"]) + (0.1m * price);
                                }
                            }
                            else
                            {
                                DataRow newRow = dt.NewRow();
                                newRow["Project_Year"] = y;

                                if (y == startYear)
                                {
                                    newRow["Project_Value"] = price;
                                    newRow["Maintenance_Effort_Value"] = 0;
                                }
                                else if (phase == "Maintenance")
                                {
                                    newRow["Project_Value"] = 0;
                                    newRow["Maintenance_Effort_Value"] = 0.1m * price;
                                }
                                else
                                {
                                    newRow["Project_Value"] = 0;
                                    newRow["Maintenance_Effort_Value"] = 0;
                                }

                                dt.Rows.Add(newRow);
                            }
                        }
                    }
                }

                // Convert datatable → model list
                foreach (DataRow dr in dt.Rows)
                {
                    result.Add(new YearlyRevenue
                    {
                        Project_Year = Convert.ToInt32(dr["Project_Year"]),
                        Total_OTC = Convert.ToDecimal(dr["Project_Value"]),
                        Total_MRC = Convert.ToDecimal(dr["Maintenance_Effort_Value"]),
                        Grand_Total = Convert.ToDecimal(dr["Project_Value"]) +
                                      Convert.ToDecimal(dr["Maintenance_Effort_Value"])
                    });
                }
            }

            return result.OrderBy(e => e.Project_Year).ToList();
        }



        private List<ProjectRevenue> Calculate_Internal_Project_Value_Details(int selected_year)
        {
            List<ProjectRevenue> list = new List<ProjectRevenue>();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                string query = @"
                                SELECT 
                                    ip.App_Name,
                                    ip.Price,
                                    ip.LaunchedDate,
                                    sp.Phase AS SDLCPhase,
                                    emp.Emp_Name AS DevelopedBy
                                FROM Internal_Platforms ip
                                LEFT JOIN SDLCPhas sp ON ip.SDLCPhase = sp.ID
                                LEFT JOIN employee emp ON ip.Developed_By = emp.Emp_ID
                                WHERE ip.LaunchedDate IS NOT NULL
                                  AND ip.Price > 0
                                  AND YEAR(ip.LaunchedDate) <= @year
                                ORDER BY ip.LaunchedDate;
                            ";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@year", selected_year);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ProjectRevenue pr = new ProjectRevenue();

                            string empName = reader["DevelopedBy"] == DBNull.Value
                                ? "N/A"
                                : reader["DevelopedBy"].ToString();

                            pr.Project_Name = reader["App_Name"].ToString();
                            pr.Developed_By = empName;

                            DateTime launch = reader.GetDateTime("LaunchedDate");
                            pr.Start_Year = launch.Year;

                            decimal price = reader["Price"] == DBNull.Value ? 0 : reader.GetDecimal("Price");
                            string sdlc = reader["SDLCPhase"] == DBNull.Value ? "" : reader["SDLCPhase"].ToString();

                            if (pr.Start_Year == selected_year)
                            {
                                pr.OTC = price;
                                pr.MRC_For_StartYear = price;
                                pr.MRC = 0;
                            }
                            else if (sdlc == "Maintenance")
                            {
                                pr.OTC = price;
                                pr.MRC_For_StartYear = 0;
                                pr.MRC = price * 0.1m;
                            }
                            else
                            {
                                pr.OTC = price;
                                pr.MRC_For_StartYear = 0;
                                pr.MRC = 0;
                            }

                            list.Add(pr);
                        }
                    }
                }
            }

            // SUMMARY ROW
            list.Add(new ProjectRevenue
            {
                Project_Name = "Total",
                MRC = list.Sum(e => e.MRC),
                MRC_For_StartYear = list.Sum(e => e.MRC_For_StartYear)
            });

            return list;
        }


    }
}
