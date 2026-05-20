using finalSE.Models;
using finalSE.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalSE.Controllers
{
    [Authorize]
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

        // CREATE
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = new SelectList(
                await _departmentService.GetAllAsync(),
                "Id",
                "DepartmentName"
            );

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(StudentModel student)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(
                    await _departmentService.GetAllAsync(),
                    "Id",
                    "DepartmentName"
                );
                return View(student);
            }

            await _studentService.CreateAsync(student);
            return RedirectToAction(nameof(Index));
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

            await _studentService.UpdateAsync(id, student);
            return RedirectToAction(nameof(Index));
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