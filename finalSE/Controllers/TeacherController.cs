using finalSE.Models;
using finalSE.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalSE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TeacherController : Controller
    {
        private readonly ITeacherService _teacherService;
        private readonly IDepartmentService _departmentService;

        public TeacherController(
            ITeacherService teacherService,
            IDepartmentService departmentService)
        {
            _teacherService = teacherService;
            _departmentService = departmentService;
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
            await _teacherService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}