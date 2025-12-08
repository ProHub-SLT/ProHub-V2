// Controllers/InternalDocumentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProHub.Constants;
using ProHub.Data;
using ProHub.Models;
using System.Security.Claims;

namespace ProHub.Controllers
{
    [Authorize]
    public class InternalDocumentsController : Controller
    {
        private readonly DocumentRepository _docRepo;
        private readonly ConsumerPlatformRepository _internalRepo;
        private readonly EmployeeRepository _empRepo;

        public InternalDocumentsController(
            DocumentRepository docRepo,
            ConsumerPlatformRepository internalRepo,
            EmployeeRepository empRepo)
        {
            _docRepo = docRepo;
            _internalRepo = internalRepo;
            _empRepo = empRepo;
        }

        // INDEX – Everyone can view
        public IActionResult Index(string search = "", int page = 1, int pageSize = 10)
        {
            var docs = _docRepo.GetInternalDocuments(search, page, pageSize);
            int total = _docRepo.GetInternalDocumentCount(search);

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalCount = total;

            return View(docs);
        }

        // CREATE – Only Admin + Developer
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.InternalPlatforms = _internalRepo.GetAll();
            ViewBag.CreatedByName = GetCurrentUserName();
            ViewBag.AuthenticatedEmployeeId = GetCurrentEmployeeId();

            var main = _internalRepo.GetAllMainPlatforms()
                .FirstOrDefault(p => p.Platforms?.ToLower() == "internal");
            ViewBag.InternalPlatformName = main?.Platforms ?? "Internal";
            ViewBag.InternalPlatformId = main?.ID ?? 1;

            return View(new Document());
        }

        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Document model, IFormFile ImagePath)
        {
            model.Created_By = GetCurrentEmployeeId();
            model.Created_Time = DateTime.Now;

            var platformId = _internalRepo.GetInternalPlatformId();
            model.Platform_ID = platformId ?? 1;

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
                TempData["Success"] = "Internal document created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.InternalPlatforms = _internalRepo.GetAll();
            return View(model);
        }

        // EDIT – Only Admin OR Owner
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var doc = _docRepo.GetById(id);
            if (doc == null) return NotFound();
            if (!IsOwnerOrAdmin(doc.Created_By)) return Forbid();

            ViewBag.InternalPlatforms = _internalRepo.GetAll();
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

        // DETAILS – Everyone
        public IActionResult Details(int id)
        {
            var doc = _docRepo.GetById(id);
            return doc == null ? NotFound() : View(doc);
        }

        // DELETE – Only Admin OR Owner
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Developer}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var doc = _docRepo.GetById(id);
            if (doc == null) return Json(new { success = false, message = "Document not found" });
            if (!IsOwnerOrAdmin(doc.Created_By)) return Forbid();

            _docRepo.Delete(id);
            return Json(new { success = true });
        }

        // HELPER METHODS
        private string GetCurrentUserName()
        {
            var email = User.FindFirst("preferred_username")?.Value ?? User.FindFirst("upn")?.Value;
            return !string.IsNullOrEmpty(email) ? _empRepo.GetEmployeeNameByEmail(email) ?? "User" : "User";
        }

        private int? GetCurrentEmployeeId()
        {
            var claim = User.FindFirst("EmployeeId")?.Value;
            return int.TryParse(claim, out int id) ? id : null;
        }

        private bool IsOwnerOrAdmin(int? ownerId)
        {
            if (!ownerId.HasValue) return false;
            if (User.IsInRole(AppRoles.Admin)) return true;
            return ownerId.Value == GetCurrentEmployeeId();
        }
    }
}