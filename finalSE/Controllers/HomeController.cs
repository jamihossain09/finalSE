using finalSE.Models;
using finalSE.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace finalSE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;
        private readonly IStudentService _studentService;
        private readonly MyDBContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            IUserService userService,
            IStudentService studentService,
            MyDBContext context)
        {
            _logger = logger;
            _userService = userService;
            _studentService = studentService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch recent 5 notices
            var notices = await _context.Notices
                .Include(n => n.Department)
                .OrderByDescending(n => n.PublishedAt)
                .Take(5)
                .ToListAsync();

            // Fetch teachers grouped by department
            var teachers = await _context.Teachers
                .Include(t => t.Department)
                .OrderBy(t => t.Department.DepartmentName)
                .ThenBy(t => t.Name)
                .ToListAsync();

            // Counts
            var totalUsers = await _context.Users.CountAsync();
            var totalStudents = await _context.Students.CountAsync();
            var totalTeachers = await _context.Teachers.CountAsync();
            var totalDepartments = await _context.Departments.CountAsync();

            ViewBag.Notices = notices;
            ViewBag.Teachers = teachers;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalTeachers = totalTeachers;
            ViewBag.TotalDepartments = totalDepartments;

            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var users = _userService.GetAll();
            var students = await _studentService.GetAllAsync();

            ViewBag.TotalUsers = users?.Count ?? 0;
            ViewBag.TotalStudents = students?.Count() ?? 0;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}