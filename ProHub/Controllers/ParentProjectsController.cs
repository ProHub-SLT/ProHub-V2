using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Constants;
using ProHub.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ProHub.Controllers
{
    public class ParentProjectsController : Controller
    {
        private readonly IConfiguration _configuration;

        public ParentProjectsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //  INDEX (List + Search + Sort + Pagination)
        public IActionResult Index(string search = "", string sortColumn = "ParentProjectGroup", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            var parentProjects = new List<ParentProject>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string query = @"SELECT ParentProjectID, ParentProjectGroup, OperationScope 
                                 FROM ParentProject
                                 WHERE (@search = '' OR ParentProjectGroup LIKE CONCAT('%', @search, '%') 
                                        OR OperationScope LIKE CONCAT('%', @search, '%'))";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@search", search ?? "");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            parentProjects.Add(new ParentProject
                            {
                                ParentProjectID = reader.GetInt32("ParentProjectID"),
                                ParentProjectGroup = reader["ParentProjectGroup"].ToString(),
                                OperationScope = reader["OperationScope"].ToString()
                            });
                        }
                    }
                }
            }

            //  Sorting
            parentProjects = sortColumn switch
            {
                "ParentProjectGroup" => sortOrder == "asc" ? parentProjects.OrderBy(x => x.ParentProjectGroup).ToList() : parentProjects.OrderByDescending(x => x.ParentProjectGroup).ToList(),
                "OperationScope" => sortOrder == "asc" ? parentProjects.OrderBy(x => x.OperationScope).ToList() : parentProjects.OrderByDescending(x => x.OperationScope).ToList(),
                _ => parentProjects.OrderBy(x => x.ParentProjectGroup).ToList()
            };

            //  Pagination
            var totalRecords = parentProjects.Count();
            var paginatedList = parentProjects
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchTerm = search ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(paginatedList);
        }

        //  GET: Create
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        //  POST: Create
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ParentProject project)
        {
            if (!ModelState.IsValid)
            {
                return View(project);
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO ParentProject (ParentProjectGroup, OperationScope)
                         VALUES (@group, @scope)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@group", project.ParentProjectGroup);
                    cmd.Parameters.AddWithValue("@scope", project.OperationScope);
                    cmd.ExecuteNonQuery();
                }
            }

            //  Set TempData message
            TempData["SuccessMessage"] = "Application Group Created Successfully!";

            //  Redirect back to Create page so popup can appear
            return RedirectToAction("Create");
        }


        //  GET: Edit
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpGet]
        public IActionResult Edit(int id)
        {
            ParentProject project = null;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM ParentProject WHERE ParentProjectID = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            project = new ParentProject
                            {
                                ParentProjectID = reader.GetInt32("ParentProjectID"),
                                ParentProjectGroup = reader["ParentProjectGroup"].ToString(),
                                OperationScope = reader["OperationScope"].ToString()
                            };
                        }
                    }
                }
            }

            if (project == null)
                return NotFound();

            return View(project);
        }

        //  POST: Edit
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpPost]
        public IActionResult Edit(ParentProject project)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE ParentProject 
                                 SET ParentProjectGroup=@group, OperationScope=@scope 
                                 WHERE ParentProjectID=@id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@group", project.ParentProjectGroup);
                    cmd.Parameters.AddWithValue("@scope", project.OperationScope);
                    cmd.Parameters.AddWithValue("@id", project.ParentProjectID);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "Application Group Updated Successfully!";
            return RedirectToAction("Edit", new { id = project.ParentProjectID });
        }
    }
}
