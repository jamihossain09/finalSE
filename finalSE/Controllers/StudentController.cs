using finalSE.Models;
using finalSE.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalSE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IDepartmentService _departmentService;

        public StudentController(
            IStudentService studentService,
            IDepartmentService departmentService)
        {
            _studentService = studentService;
            _departmentService = departmentService;
        }

        // LIST + PAGINATION
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 5; // FIXED

            var result = await _studentService.GetPagedAsync(page, pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.PageSize = pageSize;

            return View(result.Students);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var student = await _studentService.GetByIdAsync(id);
            if (student == null) return NotFound();

            return View(student);
        }

        // CREATE (DISABLED - USE INVITATIONS)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return Content("❌ Direct student creation is disabled. Please use the invitation system instead.");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(StudentModel student)
        {
            return Content("❌ Direct student creation is disabled. Please use the invitation system instead.");
        }

        // EDIT
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _studentService.GetByIdAsync(id);
            if (student == null) return NotFound();

            ViewBag.Departments = new SelectList(
                await _departmentService.GetAllAsync(),
                "Id",
                "DepartmentName",
                student.DepartmentId
            );

            return View(student);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, StudentModel student)
        {
            if (id != student.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var success = await _studentService.UpdateAsync(id, student);
                if (success)
                {
                    TempData["SuccessMessage"] = "Student details updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Departments = new SelectList(
                await _departmentService.GetAllAsync(),
                "Id",
                "DepartmentName",
                student.DepartmentId
            );
            return View(student);
        }

        // DELETE
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _studentService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}