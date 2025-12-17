using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Constants;
using ProHub.Data;
using ProHub.Models;
using System.Collections.Generic;
using System.Linq;

namespace ProHub.Controllers
{
    public class CustomerContactsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly CustomerContactRepository _contactRepository;

        public CustomerContactsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _contactRepository = new CustomerContactRepository(configuration);
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // ✅ Index: Show table (Title, Contact Name, Phone, Company, Platform)
        public IActionResult Index(string search = "", string sortColumn = "Contact_Name", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            var contacts = _contactRepository.GetCustomerContacts(search);

            // Sorting
            contacts = sortColumn switch
            {
                "Contact_Name" => sortOrder == "asc" ? contacts.OrderBy(c => c.Contact_Name).ToList() : contacts.OrderByDescending(c => c.Contact_Name).ToList(),
                "Company" => sortOrder == "asc" ? contacts.OrderBy(c => c.Company.CompanyName).ToList() : contacts.OrderByDescending(c => c.Company.CompanyName).ToList(),
                "Platform" => sortOrder == "asc" ? contacts.OrderBy(c => c.Platform.PlatformName).ToList() : contacts.OrderByDescending(c => c.Platform.PlatformName).ToList(),
                _ => contacts.OrderBy(c => c.Contact_Name).ToList()
            };

            // Pagination
            var totalRecords = contacts.Count();
            var paginatedList = contacts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.SearchTerm = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(paginatedList);
        }

        // ✅ Details View
        public IActionResult Details(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid ID.");

            var contact = _contactRepository.GetCustomerContactById(id);

            if (contact == null)
                return NotFound();

            return View(contact);
        }

        // ✅ GET: Create Form
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        public IActionResult Create()
        {
            LoadDropdownData();
            return View();
        }

        // ✅ POST: Create
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CustomerContact contact)
        {
            if (ModelState.IsValid)
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    var query = @"INSERT INTO CustomerContacts 
                                (Platform_ID, Customer_Title, Contact_Name, Contact_Phone1, Contact_Phone2, 
                                 Contact_Email, Contact_Designation, Contact_Company)
                                 VALUES (@Platform_ID, @Customer_Title, @Contact_Name, @Contact_Phone1, @Contact_Phone2,
                                         @Contact_Email, @Contact_Designation, @Contact_Company)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Platform_ID", contact.Platform_ID);
                        cmd.Parameters.AddWithValue("@Customer_Title", contact.Customer_Title);
                        cmd.Parameters.AddWithValue("@Contact_Name", contact.Contact_Name);
                        cmd.Parameters.AddWithValue("@Contact_Phone1", contact.Contact_Phone1);
                        cmd.Parameters.AddWithValue("@Contact_Phone2", contact.Contact_Phone2);
                        cmd.Parameters.AddWithValue("@Contact_Email", contact.Contact_Email);
                        cmd.Parameters.AddWithValue("@Contact_Designation", contact.Contact_Designation);
                        cmd.Parameters.AddWithValue("@Contact_Company", contact.Contact_Company);
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Contact Added Successfully!";
                return RedirectToAction("Create");
            }

            LoadDropdownData();
            return View(contact);
        }

        // ✅ GET: Edit
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        public IActionResult Edit(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid ID.");

            var contact = _contactRepository.GetCustomerContactById(id);

            if (contact == null)
                return NotFound();

            LoadDropdownData();
            return View(contact);
        }

        // ✅ POST: Edit
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, CustomerContact contact)
        {
            if (id != contact.ID)
                return BadRequest("ID mismatch.");

            if (ModelState.IsValid)
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    var query = @"UPDATE CustomerContacts SET
                                    Platform_ID = @Platform_ID,
                                    Customer_Title = @Customer_Title,
                                    Contact_Name = @Contact_Name,
                                    Contact_Phone1 = @Contact_Phone1,
                                    Contact_Phone2 = @Contact_Phone2,
                                    Contact_Email = @Contact_Email,
                                    Contact_Designation = @Contact_Designation,
                                    Contact_Company = @Contact_Company
                                  WHERE ID = @ID";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", contact.ID);
                        cmd.Parameters.AddWithValue("@Platform_ID", contact.Platform_ID);
                        cmd.Parameters.AddWithValue("@Customer_Title", contact.Customer_Title);
                        cmd.Parameters.AddWithValue("@Contact_Name", contact.Contact_Name);
                        cmd.Parameters.AddWithValue("@Contact_Phone1", contact.Contact_Phone1);
                        cmd.Parameters.AddWithValue("@Contact_Phone2", contact.Contact_Phone2);
                        cmd.Parameters.AddWithValue("@Contact_Email", contact.Contact_Email);
                        cmd.Parameters.AddWithValue("@Contact_Designation", contact.Contact_Designation);
                        cmd.Parameters.AddWithValue("@Contact_Company", contact.Contact_Company);

                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Contact updated successfully!";

                return RedirectToAction("Edit", new { id = contact.Platform_ID });
            }

            LoadDropdownData();
            return View(contact);
        }

        // ✅ Helper method to load dropdown data
        private void LoadDropdownData()
        {
            ViewBag.Platforms = GetPlatforms();
            ViewBag.Companies = GetCompanies();
            ViewBag.Titles = new List<string> { "Mr.", "Mrs.", "Ms.", "Dr.", "Prof." };
        }

        // ✅ Fetch external platforms
        private List<ExternalPlatform> GetPlatforms()
        {
            var list = new List<ExternalPlatform>();
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT Id, Platform_Name FROM External_Platforms", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ExternalPlatform
                        {
                            Id = reader.GetInt32("Id"),
                            PlatformName = reader.GetString("Platform_Name")
                        });
                    }
                }
            }
            return list;
        }

        // ✅ Fetch companies
        private List<Company> GetCompanies()
        {
            var list = new List<Company>();
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT Id, Company_Name FROM Company", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Company
                        {
                            Id = reader.GetInt32("Id"),
                            CompanyName = reader.GetString("Company_Name")
                        });
                    }
                }
            }
            return list;
        }


        // ✅ DELETE
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]

        public IActionResult Delete(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM customercontacts WHERE ID=@ID";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "Customer Contact Deleted Successfully!";
            return RedirectToAction("Index");
        }
    }
}
