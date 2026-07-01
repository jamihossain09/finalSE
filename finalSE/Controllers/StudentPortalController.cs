using finalSE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace finalSE.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentPortalController : Controller
    {
        private readonly MyDBContext _context;

        public StudentPortalController(MyDBContext context)
        {
            _context = context;
        }

        // Helper method to get current Student profile
        private async Task<StudentModel?> GetCurrentStudentAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return null;

            if (int.TryParse(userIdString, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    return await _context.Students
                        .Include(s => s.Department)
                        .FirstOrDefaultAsync(s => s.Email == user.Email);
                }
            }
            return null;
        }

        // ================= DASHBOARD =================
        public async Task<IActionResult> Index()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                return Content("❌ Student profile not found. Please contact the administrator to sync your details.");
            }

            ViewBag.Student = student;
            ViewBag.ClassRecordsCount = await _context.ClassRecords.CountAsync();
            ViewBag.TutorialsCount = await _context.Tutorials.CountAsync();
            ViewBag.AssignmentsCount = await _context.AssignmentTasks.Where(a => a.DueDate >= DateTime.Now).CountAsync();
            ViewBag.NoticesCount = await _context.Notices.CountAsync();

            // Get recent notices
            var recentNotices = await _context.Notices
                .OrderByDescending(n => n.PublishedAt)
                .Take(3)
                .ToListAsync();
            ViewBag.RecentNotices = recentNotices;

            return View();
        }

        // ================= CLASS RECORDINGS =================
        public async Task<IActionResult> ClassRecords(string? search)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return NotFound();

            var query = _context.ClassRecords
                .Include(r => r.Teacher)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(q => q.Title.Contains(search) || q.Description.Contains(search) || q.Teacher.Name.Contains(search));
            }

            var records = await query.OrderByDescending(r => r.UploadedAt).ToListAsync();
            ViewBag.Search = search;
            return View(records);
        }

        // ================= TUTORIALS / SLIDES =================
        public async Task<IActionResult> Tutorials(string? search)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return NotFound();

            var query = _context.Tutorials
                .Include(t => t.Teacher)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(q => q.Title.Contains(search) || q.Description.Contains(search) || q.Teacher.Name.Contains(search));
            }

            var tutorials = await query.OrderByDescending(t => t.UploadedAt).ToListAsync();
            ViewBag.Search = search;
            return View(tutorials);
        }

        // ================= ASSIGNMENTS =================
        public async Task<IActionResult> Assignments()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return NotFound();

            // Show active and past assignments
            var assignments = await _context.AssignmentTasks
                .Include(a => a.Teacher)
                .OrderByDescending(a => a.DueDate)
                .ToListAsync();

            return View(assignments);
        }

        // ================= ROUTINES =================
        public async Task<IActionResult> Routines()
        {
            var routines = await _context.Routines.OrderByDescending(r => r.UploadedAt).ToListAsync();
            return View(routines);
        }

        // ================= NOTICES =================
        public async Task<IActionResult> Notices()
        {
            var notices = await _context.Notices.OrderByDescending(n => n.PublishedAt).ToListAsync();
            return View(notices);
        }

        // ================= MY RESULTS =================
        public async Task<IActionResult> MyResults()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null)
            {
                return Content("❌ Student profile not found. Please contact the administrator.");
            }

            var results = await _context.StudentMarks
                .Include(sm => sm.Teacher)
                .Include(sm => sm.Subject)
                .Where(sm => sm.StudentId == student.Id && sm.IsPublished)
                .OrderBy(sm => sm.Subject.SubjectCode)
                .ToListAsync();

            ViewBag.Student = student;
            return View(results);
        }
    }
}
