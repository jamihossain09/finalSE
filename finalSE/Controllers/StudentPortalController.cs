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
            var studentIdClaim = User.FindFirstValue("StudentId");
            if (!string.IsNullOrEmpty(studentIdClaim) && int.TryParse(studentIdClaim, out int studentId))
            {
                return await _context.Students
                    .Include(s => s.Department)
                    .FirstOrDefaultAsync(s => s.Id == studentId);
            }

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
            ViewBag.ClassRecordsCount = await _context.ClassRecords.CountAsync(c => c.Teacher.DepartmentId == student.DepartmentId);
            ViewBag.TutorialsCount = await _context.Tutorials.CountAsync(t => t.Teacher.DepartmentId == student.DepartmentId);
            ViewBag.AssignmentsCount = await _context.AssignmentTasks.CountAsync(a => a.Teacher.DepartmentId == student.DepartmentId && a.DueDate >= DateTime.Now);
            ViewBag.NoticesCount = await _context.Notices.CountAsync(n => n.DepartmentId == null || n.DepartmentId == student.DepartmentId);

            // Get recent notices for their department
            var recentNotices = await _context.Notices
                .Where(n => n.DepartmentId == null || n.DepartmentId == student.DepartmentId)
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
                .Where(r => r.Teacher.DepartmentId == student.DepartmentId)
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
                .Where(t => t.Teacher.DepartmentId == student.DepartmentId)
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

            // Show active and past assignments filtered by student's department
            var assignments = await _context.AssignmentTasks
                .Include(a => a.Teacher)
                .Where(a => a.Teacher.DepartmentId == student.DepartmentId)
                .OrderByDescending(a => a.DueDate)
                .ToListAsync();

            return View(assignments);
        }

        // ================= ROUTINES =================
        public async Task<IActionResult> Routines()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return NotFound();

            var routines = await _context.Routines
                .Where(r => r.DepartmentId == null || r.DepartmentId == student.DepartmentId)
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();

            return View(routines);
        }

        // ================= NOTICES =================
        public async Task<IActionResult> Notices()
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return NotFound();

            var notices = await _context.Notices
                .Where(n => n.DepartmentId == null || n.DepartmentId == student.DepartmentId)
                .OrderByDescending(n => n.PublishedAt)
                .ToListAsync();

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
