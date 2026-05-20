using finalSE.Models;
using finalSE.Service.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace finalSE.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IInvitationService _invitationService;
        private readonly IStudentService _studentService;
        private readonly ITeacherService _teacherService;
        private readonly IDepartmentService _departmentService;

        public AccountController(
            IUserService userService,
            IRoleService roleService,
            IInvitationService invitationService,
            IStudentService studentService,
            ITeacherService teacherService,
            IDepartmentService departmentService)
        {
            _userService = userService;
            _roleService = roleService;
            _invitationService = invitationService;
            _studentService = studentService;
            _teacherService = teacherService;
            _departmentService = departmentService;
        }

        // ================= LOGIN =================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _userService.AuthenticateAsync(username, password);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }

            var role = _roleService.GetById(user.RoleId);
            var roleName = role?.RoleName ?? "User";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, roleName)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            return RedirectToAction("Index", "Home");
        }

        // ================= NORMAL REGISTER =================
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Token = null;
            ViewBag.Email = null;
            ViewBag.RoleId = null;
            ViewBag.RoleName = null;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            var userRole = _roleService.GetAll().FirstOrDefault(r => r.RoleName == "User");
            user.RoleId = userRole?.Id ?? 2; // default User role

            var result = await _userService.RegisterAsync(user);

            if (!result)
            {
                ViewBag.Error = "User already exists";
                return View(user);
            }

            return RedirectToAction("Login");
        }

        // ================= INVITATION LINK (GET) =================
        [HttpGet]
        public async Task<IActionResult> RegisterWithToken(string token)
        {
            var invitation = await _invitationService.ValidateTokenAsync(token);

            if (invitation == null)
                return Content("❌ Invalid or expired invitation link");

            var role = _roleService.GetById(invitation.RoleId);

            ViewBag.Token = token;
            ViewBag.Email = invitation.Email;
            ViewBag.RoleId = invitation.RoleId;
            ViewBag.RoleName = role?.RoleName;

            var departments = await _departmentService.GetAllAsync();
            ViewBag.Departments = departments;

            return View("Register");
        }

        // ================= INVITATION REGISTER (POST) =================
        [HttpPost]
        public async Task<IActionResult> RegisterFromInvitation(User user, string token, int departmentId, int? age, string? phone)
        {
            var invitation = await _invitationService.ValidateTokenAsync(token);

            if (invitation == null)
            {
                ViewBag.Error = "Invalid or expired invitation";
                return View("Register", user);
            }

            user.Email = invitation.Email;
            user.RoleId = invitation.RoleId;

            var result = await _userService.RegisterAsync(user);

            var role = _roleService.GetById(invitation.RoleId);

            if (!result)
            {
                ViewBag.Error = "User already exists";
                ViewBag.Token = token;
                ViewBag.Email = invitation.Email;
                ViewBag.RoleId = invitation.RoleId;
                ViewBag.RoleName = role?.RoleName;
                ViewBag.Departments = await _departmentService.GetAllAsync();
                return View("Register", user);
            }

            // Create Student or Teacher based on Role
            if (role != null)
            {
                if (role.RoleName.ToLower() == "student")
                {
                    await _studentService.CreateAsync(new StudentModel
                    {
                        Name = user.UserName,
                        Email = user.Email,
                        Age = age ?? 0,
                        Address = user.Address ?? "",
                        DepartmentId = departmentId
                    });
                }
                else if (role.RoleName.ToLower() == "teacher")
                {
                    await _teacherService.CreateAsync(new Teacher
                    {
                        Name = user.UserName,
                        Email = user.Email,
                        Phone = phone ?? "",
                        Address = user.Address ?? "",
                        DepartmentId = departmentId
                    });
                }
            }

            await _invitationService.AcceptInvitationAsync(token);

            return RedirectToAction("Login");
        }

        // ================= LOGOUT =================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }

        // ================= SETUP =================
        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            var roles = _roleService.GetAll();

            if (!roles.Any(r => r.RoleName == "Admin"))
            {
                _roleService.Add(new Role
                {
                    RoleName = "Admin",
                    RoleDescription = "Administrator Role"
                });
            }

            if (!roles.Any(r => r.RoleName == "User"))
            {
                _roleService.Add(new Role
                {
                    RoleName = "User",
                    RoleDescription = "Standard User Role"
                });
            }

            if (!roles.Any(r => r.RoleName == "Student"))
            {
                _roleService.Add(new Role
                {
                    RoleName = "Student",
                    RoleDescription = "Student Role"
                });
            }

            if (!roles.Any(r => r.RoleName == "Teacher"))
            {
                _roleService.Add(new Role
                {
                    RoleName = "Teacher",
                    RoleDescription = "Teacher Role"
                });
            }

            roles = _roleService.GetAll();
            var adminRole = roles.First(r => r.RoleName == "Admin");

            var users = _userService.GetAll();

            if (!users.Any(u => u.UserName == "admin"))
            {
                await _userService.RegisterAsync(new User
                {
                    UserName = "admin",
                    Email = "admin@system.com",
                    PasswordHash = "admin123",
                    Address = "System Admin",
                    RoleId = adminRole.Id
                });
            }

            return Content("Setup Complete!");
        }
    }
}