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
    public class FieldsOfSpecializationController : Controller
    {
        private readonly IConfiguration _configuration;

        public FieldsOfSpecializationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ✅ INDEX: View All + Search + Sort + Pagination
        public IActionResult Index(string search = "", string sortColumn = "FieldOfSpecName", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            var fields = new List<FieldsOfSpecialization>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string query = @"SELECT field_of_spec_id, field_of_spec_name, `desc`
                                 FROM fields_of_specialization
                                 WHERE (@search = '' OR field_of_spec_name LIKE CONCAT('%', @search, '%')
                                        OR `desc` LIKE CONCAT('%', @search, '%'))";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@search", search ?? "");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fields.Add(new FieldsOfSpecialization
                            {
                                FieldOfSpecId = reader.GetInt32("field_of_spec_id"),
                                FieldOfSpecName = reader["field_of_spec_name"].ToString(),
                                Desc = reader["desc"] == DBNull.Value ? "" : reader["desc"].ToString()
                            });
                        }
                    }
                }
            }

            // ✅ Sorting
            fields = sortColumn switch
            {
                "FieldOfSpecName" => sortOrder == "asc" ? fields.OrderBy(x => x.FieldOfSpecName).ToList() : fields.OrderByDescending(x => x.FieldOfSpecName).ToList(),
                "Desc" => sortOrder == "asc" ? fields.OrderBy(x => x.Desc).ToList() : fields.OrderByDescending(x => x.Desc).ToList(),
                _ => fields.OrderBy(x => x.FieldOfSpecName).ToList()
            };

            // ✅ Pagination
            var totalRecords = fields.Count();
            var paginatedList = fields
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

        // ✅ CREATE (GET)
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ✅ CREATE (POST)
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(FieldsOfSpecialization field)
        {
            if (!ModelState.IsValid)
            {
                return View(field);
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO fields_of_specialization (field_of_spec_name, `desc`) VALUES (@name, @desc)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", field.FieldOfSpecName);
                    cmd.Parameters.AddWithValue("@desc", field.Desc ?? "");
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "Field of Specialization Added Successfully!";
            return RedirectToAction("Create");
        }

        // ✅ EDIT (GET)
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpGet]
        public IActionResult Edit(int id)
        {
            FieldsOfSpecialization field = null;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM fields_of_specialization WHERE field_of_spec_id = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            field = new FieldsOfSpecialization
                            {
                                FieldOfSpecId = reader.GetInt32("field_of_spec_id"),
                                FieldOfSpecName = reader["field_of_spec_name"].ToString(),
                                Desc = reader["desc"] == DBNull.Value ? "" : reader["desc"].ToString()
                            };
                        }
                    }
                }
            }

            if (field == null)
                return NotFound();

            return View(field);
        }

        // ✅ EDIT (POST)
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(FieldsOfSpecialization field)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"UPDATE fields_of_specialization 
                                 SET field_of_spec_name=@name, `desc`=@desc 
                                 WHERE field_of_spec_id=@id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", field.FieldOfSpecName);
                    cmd.Parameters.AddWithValue("@desc", field.Desc ?? "");
                    cmd.Parameters.AddWithValue("@id", field.FieldOfSpecId);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "Field of Specialization Updated Successfully!";
            return RedirectToAction("Edit", new { id = field.FieldOfSpecId });
        }

        // ✅ DELETE
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        public IActionResult Delete(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM fields_of_specialization WHERE field_of_spec_id=@id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "Field of Specialization Deleted Successfully!";
            return RedirectToAction("Index");
        }
    }
}
