// Controllers/ExternalDocumentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProHub.Constants;
using ProHub.Data;
using ProHub.Models;
using System.Security.Claims;

namespace ProHub.Controllers
{
    [Authorize]
    public class ExternalDocumentsController : Controller
    {
        private readonly DocumentRepository _docRepo;
        private readonly ExternalSolutionRepository _externalRepo;
        private readonly EmployeeRepository _empRepo;

        public ExternalDocumentsController(
            DocumentRepository docRepo,
            ExternalSolutionRepository externalRepo,
            EmployeeRepository empRepo)
        {
            _docRepo = docRepo;
            _externalRepo = externalRepo;
            _empRepo = empRepo;
        }

        // Controllers/ExternalDocumentsController.cs

        public IActionResult Index(string search = "", int page = 1, int pageSize = 10)
        {

            var docs = _docRepo.GetExternalDocuments(search, null, page, pageSize);
            int total = _docRepo.GetExternalDocumentCount(search, null);

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalCount = total;


            ViewBag.CurrentSolutionId = null;

            return View(docs);
        }


        public IActionResult Folder(int id, string search = "", int page = 1, int pageSize = 10)
        {

            var docs = _docRepo.GetExternalDocuments(search, id, page, pageSize);
            int total = _docRepo.GetExternalDocumentCount(search, id);


            var solution = _externalRepo.GetAll().FirstOrDefault(x => x.Id == id);
            ViewBag.SolutionName = solution?.PlatformName ?? "Unknown Solution";

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalCount = total;


            ViewBag.CurrentSolutionId = id;


            return View("Index", docs);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.ExternalPlatforms = _externalRepo.GetAll();
            ViewBag.CreatedByName = GetCurrentUserName();
            ViewBag.AuthenticatedEmployeeId = GetCurrentEmployeeId();

            var main = _externalRepo.GetAllMainPlatforms()
                .FirstOrDefault(p => p.Platforms?.ToLower() == "external");
            ViewBag.ExternalPlatformName = main?.Platforms ?? "External";
            ViewBag.ExternalPlatformId = main?.ID ?? 2;

            return View(new Document());
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Document model, IFormFile ImagePath)
        {
            model.Created_By = GetCurrentEmployeeId();
            model.Created_Time = DateTime.Now;

            var platformId = _externalRepo.GetExternalPlatformId();
            // Use platform ID from Main_Platforms table, fallback to 2 if not found
            model.Platform_ID = platformId ?? 2;

            if (ImagePath != null && ImagePath.Length > 0)
            {
                var fileName = Guid.NewGuid() + "_" + Path.GetFileName(ImagePath.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.Create);
                await ImagePath.CopyToAsync(stream);
                model.Doc_URL = "/uploads/documents/" + fileName;
            }

            if (ModelState.IsValid)
            {
                _docRepo.Insert(model);
                TempData["Success"] = "External document created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ExternalPlatforms = _externalRepo.GetAll();
            return View(model);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var doc = _docRepo.GetById(id);
            if (doc == null) return NotFound();
            if (!IsOwnerOrAdmin(doc.Created_By)) return Forbid();

            ViewBag.ExternalPlatforms = _externalRepo.GetAll();
            ViewBag.CreatedByName = _empRepo.GetNameById(doc.Created_By ?? 0);
            return View(doc);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormCollection form, IFormFile ImagePath)
        {
            var existing = _docRepo.GetById(id);
            if (existing == null) return NotFound();
            if (!IsOwnerOrAdmin(existing.Created_By)) return Forbid();

            var model = new Document
            {
                ID = id,
                Platform_ID = existing.Platform_ID,
                Solution_ID = int.TryParse(form["Solution_ID"], out int s) ? s : null,
                Doc_Name = form["Doc_Name"],
                Doc_Classification = form["Doc_Classification"],
                Tags = form["Tags"],
                Confidential = bool.TryParse(form["Confidential"], out bool c) && c,
                Created_By = existing.Created_By,
                Created_Time = existing.Created_Time,
                Doc_URL = existing.Doc_URL
            };

            if (ImagePath != null && ImagePath.Length > 0)
            {
                var fileName = Guid.NewGuid() + "_" + Path.GetFileName(ImagePath.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.Create);
                await ImagePath.CopyToAsync(stream);
                model.Doc_URL = "/uploads/documents/" + fileName;
            }

            _docRepo.Update(model);
            TempData["Success"] = "Document updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Details(int id)
        {
            var doc = _docRepo.GetById(id);
            return doc == null ? NotFound() : View(doc);
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var doc = _docRepo.GetById(id);
            if (doc == null) return Json(new { success = false, message = "Not found" });
            if (!IsOwnerOrAdmin(doc.Created_By)) return Forbid();

            _docRepo.Delete(id);
            return Json(new { success = true });
        }

        private string GetCurrentUserName() =>
            User.FindFirst("preferred_username")?.Value is { } email
                ? _empRepo.GetEmployeeNameByEmail(email) ?? "User"
                : "User";

        private int? GetCurrentEmployeeId()
        {
            var claim = User.FindFirst("EmployeeId")?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }

        private bool IsOwnerOrAdmin(int? ownerId) =>
            ownerId.HasValue && (User.IsInRole(AppRoles.Admin) || ownerId.Value == GetCurrentEmployeeId());
    }
}