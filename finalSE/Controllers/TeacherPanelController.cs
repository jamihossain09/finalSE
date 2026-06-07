using finalSE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace finalSE.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherPanelController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IWebHostEnvironment _environment;

        public TeacherPanelController(MyDBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Helper method to get the current teacher profile based on logged-in user email
        private async Task<Teacher?> GetCurrentTeacherAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return null;

            if (int.TryParse(userIdString, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    return await _context.Teachers
                        .Include(t => t.Department)
                        .FirstOrDefaultAsync(t => t.Email == user.Email);
                }
            }
            return null;
        }

        // ================= DASHBOARD INDEX =================
        public async Task<IActionResult> Index()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return Content("❌ Teacher profile not found. Please make sure your registered email matches an assigned teacher email in the system, or contact the administrator.");
            }

            ViewBag.Teacher = teacher;
            ViewBag.ClassRecordsCount = await _context.ClassRecords.CountAsync(c => c.TeacherId == teacher.Id);
            ViewBag.TutorialsCount = await _context.Tutorials.CountAsync(t => t.TeacherId == teacher.Id);
            ViewBag.AssignmentsCount = await _context.AssignmentTasks.CountAsync(a => a.TeacherId == teacher.Id);

            return View();
        }

        // ================= ROUTINES =================
        public async Task<IActionResult> Routines()
        {
            var routines = await _context.Routines.OrderByDescending(r => r.UploadedAt).ToListAsync();
            return View(routines);
        }

        // ================= CLASS RECORDS =================
        public async Task<IActionResult> ClassRecords()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var records = await _context.ClassRecords
                .Where(r => r.TeacherId == teacher.Id)
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            return View(records);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadClassRecord(string title, string? description, string uploadType, string? link, IFormFile? file)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var record = new ClassRecord
            {
                Title = title,
                Description = description,
                UploadType = uploadType,
                TeacherId = teacher.Id,
                UploadedAt = DateTime.Now,
                FilePathOrLink = ""
            };

            if (uploadType == "Link")
            {
                if (string.IsNullOrWhiteSpace(link))
                {
                    TempData["ErrorMessage"] = "Please provide a valid URL link.";
                    return RedirectToAction(nameof(ClassRecords));
                }
                record.FilePathOrLink = link;
            }
            else // File upload
            {
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a recording file to upload.";
                    return RedirectToAction(nameof(ClassRecords));
                }

                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/classrecords");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                record.FilePathOrLink = "/uploads/classrecords/" + uniqueFileName;
            }

            _context.ClassRecords.Add(record);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Class record added successfully!";

            return RedirectToAction(nameof(ClassRecords));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClassRecord(int id)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var record = await _context.ClassRecords.FirstOrDefaultAsync(r => r.Id == id && r.TeacherId == teacher.Id);
            if (record == null) return NotFound();

            if (record.UploadType == "File")
            {
                string fullPath = Path.Combine(_environment.WebRootPath, record.FilePathOrLink.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.ClassRecords.Remove(record);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Class record deleted successfully!";

            return RedirectToAction(nameof(ClassRecords));
        }

        // ================= TUTORIALS =================
        public async Task<IActionResult> Tutorials()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var tutorials = await _context.Tutorials
                .Where(t => t.TeacherId == teacher.Id)
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            return View(tutorials);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTutorial(string title, string? description, string? videoLink, IFormFile? file)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var tutorial = new Tutorial
            {
                Title = title,
                Description = description,
                VideoLink = videoLink,
                TeacherId = teacher.Id,
                UploadedAt = DateTime.Now
            };

            if (file != null && file.Length > 0)
            {
                string extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".pdf" && extension != ".ppt" && extension != ".pptx")
                {
                    TempData["ErrorMessage"] = "Only PDF, PPT, and PPTX files are allowed for tutorials.";
                    return RedirectToAction(nameof(Tutorials));
                }

                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/tutorials");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                tutorial.FilePath = "/uploads/tutorials/" + uniqueFileName;
            }

            _context.Tutorials.Add(tutorial);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tutorial uploaded successfully!";

            return RedirectToAction(nameof(Tutorials));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTutorial(int id)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var tutorial = await _context.Tutorials.FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacher.Id);
            if (tutorial == null) return NotFound();

            if (!string.IsNullOrEmpty(tutorial.FilePath))
            {
                string fullPath = Path.Combine(_environment.WebRootPath, tutorial.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.Tutorials.Remove(tutorial);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tutorial deleted successfully!";

            return RedirectToAction(nameof(Tutorials));
        }

        // ================= ASSIGNMENTS =================
        public async Task<IActionResult> Assignments()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var assignments = await _context.AssignmentTasks
                .Where(a => a.TeacherId == teacher.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            return View(assignments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostAssignment(string title, string description, DateTime dueDate, IFormFile? file)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var assignment = new AssignmentTask
            {
                Title = title,
                Description = description,
                DueDate = dueDate,
                TeacherId = teacher.Id,
                CreatedAt = DateTime.Now
            };

            if (file != null && file.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/assignments");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                assignment.FilePath = "/uploads/assignments/" + uniqueFileName;
            }

            _context.AssignmentTasks.Add(assignment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Assignment task posted successfully!";

            return RedirectToAction(nameof(Assignments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var assignment = await _context.AssignmentTasks.FirstOrDefaultAsync(a => a.Id == id && a.TeacherId == teacher.Id);
            if (assignment == null) return NotFound();

            if (!string.IsNullOrEmpty(assignment.FilePath))
            {
                string fullPath = Path.Combine(_environment.WebRootPath, assignment.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.AssignmentTasks.Remove(assignment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Assignment task deleted successfully!";

            return RedirectToAction(nameof(Assignments));
        }
    }
}
