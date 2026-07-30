using finalSE.Models;
using finalSE.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace finalSE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TeacherController : Controller
    {
        private readonly ITeacherService _teacherService;
        private readonly IDepartmentService _departmentService;
        private readonly MyDBContext _context;

        public TeacherController(
            ITeacherService teacherService,
            IDepartmentService departmentService,
            MyDBContext context)
        {
            _teacherService = teacherService;
            _departmentService = departmentService;
            _context = context;
        }

        // LIST + PAGINATION
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
        {
            var result = await _teacherService.GetPagedAsync(page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = result.TotalPages;

            return View(result.Teachers);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var teacher = await _teacherService.GetByIdAsync(id);
            if (teacher == null) return NotFound();

            return View(teacher);
        }

        // CREATE (DISABLED - USE INVITATIONS)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return Content("❌ Direct teacher creation is disabled. Please use the invitation system instead.");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Teacher teacher)
        {
            return Content("❌ Direct teacher creation is disabled. Please use the invitation system instead.");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var teacher = await _teacherService.GetByIdAsync(id);
            if (teacher == null) return NotFound();

            ViewBag.Departments = new SelectList(
                await _departmentService.GetAllAsync(),
                "Id",
                "DepartmentName",
                teacher.DepartmentId
            );

            return View(teacher);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Teacher teacher)
        {
            await _teacherService.UpdateAsync(id, teacher);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.Department)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                TempData["ErrorMessage"] = "Teacher not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // 1. Remove StudentMarks where this teacher was the grader (Restrict FK)
                var marks = _context.StudentMarks.Where(m => m.TeacherId == id);
                _context.StudentMarks.RemoveRange(marks);

                // 2. Remove CourseAssignments (Restrict FK)
                var assignments = _context.CourseAssignments.Where(ca => ca.TeacherId == id);
                _context.CourseAssignments.RemoveRange(assignments);

                // 3. Remove TeacherPayments (Restrict FK)
                var payments = _context.TeacherPayments.Where(p => p.TeacherID == id);
                _context.TeacherPayments.RemoveRange(payments);

                // 4. Remove ClassRecords uploaded by this teacher (cascades file cleanup)
                var classRecords = await _context.ClassRecords.Where(r => r.TeacherId == id).ToListAsync();
                _context.ClassRecords.RemoveRange(classRecords);

                // 5. Remove Tutorials uploaded by this teacher
                var tutorials = await _context.Tutorials.Where(t => t.TeacherId == id).ToListAsync();
                _context.Tutorials.RemoveRange(tutorials);

                // 6. Remove AssignmentTasks posted by this teacher
                var tasks = await _context.AssignmentTasks.Where(a => a.TeacherId == id).ToListAsync();
                _context.AssignmentTasks.RemoveRange(tasks);

                await _context.SaveChangesAsync();

                // 7. Delete the Teacher profile
                _context.Teachers.Remove(teacher);
                await _context.SaveChangesAsync();

                // 8. Also delete the linked User account (matched by email)
                var linkedUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == teacher.Email);
                if (linkedUser != null)
                {
                    _context.Users.Remove(linkedUser);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Teacher \"{teacher.Name}\" and all related records have been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to delete teacher: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}