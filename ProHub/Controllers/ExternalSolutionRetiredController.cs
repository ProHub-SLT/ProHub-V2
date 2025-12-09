using ProHub.Data;
using ProHub.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

namespace ProHub.Controllers
{
    // Controller for managing retired external solutions
    public class ExternalSolutionRetiredController : Controller
    {
        private readonly ExternalSolutionRepository _repo;
        private readonly IConfiguration _config;

        public ExternalSolutionRetiredController(ExternalSolutionRepository repo, IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        // List retired solutions with search, sorting, and pagination (table view)
        public IActionResult Index(string search = "", string sortColumn = "PlatformName", string sortOrder = "asc", int page = 1, int pageSize = 15)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 15 : pageSize;

            List<ExternalPlatform> retiredList = _repo.GetRetiredSolutions(search ?? "");

            sortColumn = sortColumn?.Trim() ?? "PlatformName";
            sortOrder = sortOrder?.ToLowerInvariant() == "desc" ? "desc" : "asc";

            IOrderedEnumerable<ExternalPlatform> sortedList = sortColumn.ToLowerInvariant() switch
            {
                "platformname" => sortOrder == "asc" ? retiredList.OrderBy(x => x.PlatformName ?? "") : retiredList.OrderByDescending(x => x.PlatformName ?? ""),
                "developedby" => sortOrder == "asc" ? retiredList.OrderBy(x => x.DevelopedBy?.EmpName ?? "") : retiredList.OrderByDescending(x => x.DevelopedBy?.EmpName ?? ""),
                "launcheddate" => sortOrder == "asc" ? retiredList.OrderBy(x => x.LaunchedDate ?? DateTime.MinValue) : retiredList.OrderByDescending(x => x.LaunchedDate ?? DateTime.MinValue),
                "platformotc" => sortOrder == "asc" ? retiredList.OrderBy(x => x.PlatformOTC ?? 0) : retiredList.OrderByDescending(x => x.PlatformOTC ?? 0),
                "contractperiod" => sortOrder == "asc" ? retiredList.OrderBy(x => x.ContractPeriod ?? "") : retiredList.OrderByDescending(x => x.ContractPeriod ?? ""),
                "salesteam" => sortOrder == "asc" ? retiredList.OrderBy(x => x.SalesAM ?? "") : retiredList.OrderByDescending(x => x.SalesAM ?? ""),
                "proposaluploaded" => sortOrder == "asc" ? retiredList.OrderBy(x => x.ProposalUploaded ?? "") : retiredList.OrderByDescending(x => x.ProposalUploaded ?? ""),
                _ => retiredList.OrderBy(x => x.PlatformName ?? "")
            };

            int totalRecords = sortedList.Count();
            List<ExternalPlatform> paginatedList = sortedList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.SearchTerm = search ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortOrder = sortOrder;

            return View(paginatedList);
        }

        // View details of a single retired external solution by ID
        public IActionResult ViewDetails(int id)
        {
            if (id <= 0) return BadRequest("Invalid ID.");
            ExternalPlatform? item = _repo.GetRetiredSolutionById(id);
            return item is null ? NotFound($"Retired external platform with ID {id} not found.") : View(item);
        }

        // Export all retired external platforms to Excel
        [HttpGet]
        public IActionResult ExportAllToExcel()
        {
            string cs = _config.GetConnectionString("DefaultConnection");
            using var conn = new MySqlConnection(cs);
            conn.Open();

            string sql = @"
                SELECT ep.*
                FROM external_platforms ep
                LEFT JOIN SDLCPhas sp ON ep.SDLCStage = sp.ID
                WHERE (LOWER(TRIM(sp.Phase)) = 'retired' OR LOWER(TRIM(ep.Status)) = 'retired')
                ORDER BY ep.Platform_Name";

            using var da = new MySqlDataAdapter(sql, conn);
            using var dt = new DataTable("external_platforms");
            da.Fill(dt);

            ExcelPackage.License.SetNonCommercialOrganization("ProHub");
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Retired (external_platforms)");
            ws.Cells[1, 1].LoadFromDataTable(dt, true);
            using (var rng = ws.Cells[1, 1, 1, dt.Columns.Count])
            {
                rng.Style.Font.Bold = true;
            }
            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            ws.View.FreezePanes(2, 1);

            byte[] fileBytes = package.GetAsByteArray();
            string fileName = $"Retired_external_platforms_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
