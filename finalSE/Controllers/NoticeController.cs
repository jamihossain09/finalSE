using finalSE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace finalSE.Controllers
{
    [Authorize]
    public class NoticeController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IWebHostEnvironment _environment;

        public NoticeController(MyDBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ================= INDEX (ALL USERS) =================
        public async Task<IActionResult> Index()
        {
            var notices = await _context.Notices.OrderByDescending(n => n.PublishedAt).ToListAsync();
            return View(notices);
        }

        // ================= CREATE (GET - ADMIN ONLY) =================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // ================= CREATE (POST - ADMIN ONLY) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Notice notice, IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                ModelState.AddModelError("FilePath", "Please upload a PDF notice file.");
                return View(notice);
            }

            if (Path.GetExtension(pdfFile.FileName).ToLower() != ".pdf")
            {
                ModelState.AddModelError("FilePath", "Only PDF files are allowed.");
                return View(notice);
            }

            // Create directories if they don't exist
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/notices");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Unique file name
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(pdfFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(fileStream);
            }

            notice.FilePath = "/uploads/notices/" + uniqueFileName;
            notice.PublishedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Notices.Add(notice);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notice published successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(notice);
        }

        // ================= DELETE (POST - ADMIN ONLY) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var notice = await _context.Notices.FindAsync(id);
            if (notice == null)
            {
                return NotFound();
            }

            // Delete physical file
            string fullPath = Path.Combine(_environment.WebRootPath, notice.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            _context.Notices.Remove(notice);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Notice deleted successfully!";

            return RedirectToAction(nameof(Index));
        }
    }
}
