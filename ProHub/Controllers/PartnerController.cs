using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Data;
using ProHub.Models;
using System.Collections.Generic;
using System.Linq;

namespace ProHub.Controllers
{
    public class PartnerController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly PartnerRepository _partnerRepository;

        public PartnerController(IConfiguration configuration)
        {
            _configuration = configuration;
            _partnerRepository = new PartnerRepository(configuration);
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // ✅ INDEX: Search, Sort, Pagination
        public IActionResult Index(string search = "", string sortColumn = "Partner_Name", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            var partners = _partnerRepository.GetAllPartners(search);

            // Sorting
            partners = sortColumn switch
            {
                "Partner_Organization" => sortOrder == "asc" ? partners.OrderBy(p => p.Partner_Organization).ToList() : partners.OrderByDescending(p => p.Partner_Organization).ToList(),
                "Partner_Title" => sortOrder == "asc" ? partners.OrderBy(p => p.Partner_Title).ToList() : partners.OrderByDescending(p => p.Partner_Title).ToList(),
                "Partner_Name" => sortOrder == "asc" ? partners.OrderBy(p => p.Partner_Name).ToList() : partners.OrderByDescending(p => p.Partner_Name).ToList(),
                _ => partners.OrderBy(p => p.Partner_Name).ToList()
            };

            // Pagination
            var totalRecords = partners.Count();
            var paginatedList = partners.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.SearchTerm = search;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(paginatedList);
        }

        // ✅ CREATE: GET
        public IActionResult Create()
        {
            ViewBag.Titles = new List<string> { "Mr.", "Mrs.", "Ms.", "Dr.", "Prof." };
            return View();
        }

        // ✅ CREATE: POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Partner partner)
        {
            if (ModelState.IsValid)
            {
                _partnerRepository.AddPartner(partner);
                TempData["SuccessMessage"] = "Partner added successfully!";
                return RedirectToAction("Create");
            }

            ViewBag.Titles = new List<string> { "Mr.", "Mrs.", "Ms.", "Dr.", "Prof." };

            return View(partner);
        }
    }
}
