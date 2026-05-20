using finalSE.Models;
using finalSE.Service.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace finalSE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IUserService _userService;
        private readonly IStudentService _studentService;

        public HomeController(
            ILogger<HomeController> logger,
            IUserService userService,
            IStudentService studentService)
        {
            _logger = logger;
            _userService = userService;
            _studentService = studentService;
        }

        public async Task<IActionResult> Index()
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