using finalSE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace finalSE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CourseAssignmentController : Controller
    {
        private readonly MyDBContext _context;

        public CourseAssignmentController(MyDBContext context)
        {
            _context = context;
        }

        // GET: CourseAssignment
        public async Task<IActionResult> Index()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Department)
                .OrderBy(s => s.Department.DepartmentName)
                .ThenBy(s => s.SubjectCode)
                .ToListAsync();

            var assignments = await _context.CourseAssignments
                .Include(a => a.Teacher)
                .ToDictionaryAsync(a => a.SubjectId);

            var teachers = await _context.Teachers
                .Include(t => t.Department)
                .OrderBy(t => t.Department.DepartmentName)
                .ThenBy(t => t.Name)
                .ToListAsync();

            ViewBag.Assignments = assignments;
            ViewBag.Teachers = teachers;

            return View(subjects);
        }

        // POST: CourseAssignment/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int subjectId, int? teacherId)
        {
            var subject = await _context.Subjects.FindAsync(subjectId);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction(nameof(Index));
            }

            var existingAssignment = await _context.CourseAssignments
                .FirstOrDefaultAsync(a => a.SubjectId == subjectId);

            if (teacherId == null || teacherId == 0)
            {
                // Unassign
                if (existingAssignment != null)
                {
                    _context.CourseAssignments.Remove(existingAssignment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Unassigned teacher from subject {subject.SubjectName} successfully.";
                }
            }
            else
            {
                var teacher = await _context.Teachers.FindAsync(teacherId);
                if (teacher == null)
                {
                    TempData["ErrorMessage"] = "Teacher not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (existingAssignment != null)
                {
                    existingAssignment.TeacherId = teacherId.Value;
                    _context.CourseAssignments.Update(existingAssignment);
                }
                else
                {
                    var newAssignment = new CourseAssignment
                    {
                        SubjectId = subjectId,
                        TeacherId = teacherId.Value
                    };
                    _context.CourseAssignments.Add(newAssignment);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Assigned {teacher.Name} to {subject.SubjectName} successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
