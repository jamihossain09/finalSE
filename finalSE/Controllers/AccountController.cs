using finalSE.Models;
using finalSE.Service.Interface;
using finalSE.Service.Application;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System;
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
        private readonly IMemoryCache _memoryCache;
        private readonly EmailService _emailService;

        public AccountController(
            IUserService userService,
            IRoleService roleService,
            IInvitationService invitationService,
            IStudentService studentService,
            ITeacherService teacherService,
            IDepartmentService departmentService,
            IMemoryCache memoryCache,
            EmailService emailService)
        {
            _userService = userService;
            _roleService = roleService;
            _invitationService = invitationService;
            _studentService = studentService;
            _teacherService = teacherService;
            _departmentService = departmentService;
            _memoryCache = memoryCache;
            _emailService = emailService;
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

            // Check if username or email already exists
            var allUsers = _userService.GetAll();
            if (allUsers.Any(u => u.UserName.Equals(user.UserName, StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Error = "Username already exists";
                return View(user);
            }
            if (allUsers.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Error = "Email already exists";
                return View(user);
            }

            // Generate OTP
            var otp = Random.Shared.Next(100000, 999999).ToString();
            var pending = new PendingRegistration
            {
                User = user,
                Otp = otp,
                ExpiryTime = DateTime.Now.AddMinutes(10)
            };
            _memoryCache.Set("RegOTP_" + user.Email, pending, TimeSpan.FromMinutes(10));

            // Send Email
            string subject = "Register Verification OTP";
            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                <h2 style='color: #4f46e5; text-align: center;'>Email Verification</h2>
                <p>Hello,</p>
                <p>Thank you for registering. Your verification OTP code is:</p>
                <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 28px; font-weight: bold; letter-spacing: 5px; color: #4f46e5; border-radius: 4px; margin: 20px 0;'>
                    {otp}
                </div>
                <p>This code will expire in 10 minutes.</p>
                <p style='color: #999; font-size: 12px; text-align: center; margin-top: 30px;'>If you did not make this request, please ignore this email.</p>
            </div>";
            
            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to send verification email. " + ex.Message;
                return View(user);
            }

            return RedirectToAction("VerifyOtp", new { email = user.Email, purpose = "Register" });
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
            ViewBag.DepartmentId = invitation.DepartmentId;

            var departments = await _departmentService.GetAllAsync();
            ViewBag.Departments = departments;

            return View("Register");
        }

        // ================= INVITATION REGISTER (POST) =================
        [HttpPost]
        public async Task<IActionResult> RegisterFromInvitation(User user, string token, int departmentId, int? age, string? phone, string? fullName)
        {
            var invitation = await _invitationService.ValidateTokenAsync(token);

            if (invitation == null)
            {
                ViewBag.Error = "Invalid or expired invitation";
                return View("Register", user);
            }

            user.Email = invitation.Email;
            user.RoleId = invitation.RoleId;

            var role = _roleService.GetById(invitation.RoleId);
            int finalDeptId = invitation.DepartmentId ?? departmentId;

            // Check if username already exists
            var allUsers = _userService.GetAll();
            if (allUsers.Any(u => u.UserName.Equals(user.UserName, StringComparison.OrdinalIgnoreCase)))
            {
                ViewBag.Error = "Username already exists";
                ViewBag.Token = token;
                ViewBag.Email = invitation.Email;
                ViewBag.RoleId = invitation.RoleId;
                ViewBag.RoleName = role?.RoleName;
                ViewBag.DepartmentId = invitation.DepartmentId;
                ViewBag.Departments = await _departmentService.GetAllAsync();
                return View("Register", user);
            }

            // Generate OTP
            var otp = Random.Shared.Next(100000, 999999).ToString();
            var pending = new PendingRegistration
            {
                User = user,
                Otp = otp,
                Token = token,
                DepartmentId = finalDeptId,
                Age = age,
                Phone = phone,
                FullName = !string.IsNullOrWhiteSpace(fullName) ? fullName : user.UserName,
                ExpiryTime = DateTime.Now.AddMinutes(10)
            };
            _memoryCache.Set("RegOTP_" + user.Email, pending, TimeSpan.FromMinutes(10));

            // Send Email
            string subject = "Invitation Verification OTP";
            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                <h2 style='color: #4f46e5; text-align: center;'>Email Verification</h2>
                <p>Hello,</p>
                <p>You are registering using an invitation link. Your verification OTP code is:</p>
                <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 28px; font-weight: bold; letter-spacing: 5px; color: #4f46e5; border-radius: 4px; margin: 20px 0;'>
                    {otp}
                </div>
                <p>This code will expire in 10 minutes.</p>
                <p style='color: #999; font-size: 12px; text-align: center; margin-top: 30px;'>If you did not make this request, please ignore this email.</p>
            </div>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to send verification email. " + ex.Message;
                ViewBag.Token = token;
                ViewBag.Email = invitation.Email;
                ViewBag.RoleId = invitation.RoleId;
                ViewBag.RoleName = role?.RoleName;
                ViewBag.Departments = await _departmentService.GetAllAsync();
                return View("Register", user);
            }

            return RedirectToAction("VerifyOtp", new { email = user.Email, purpose = "RegisterInvitation" });
        }

        // ================= LOGOUT =================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }

        // ================= VERIFY OTP =================
        [HttpGet]
        public IActionResult VerifyOtp(string email, string purpose)
        {
            ViewBag.Email = email;
            ViewBag.Purpose = purpose;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string email, string otp, string purpose)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
            {
                ViewBag.Error = "Please enter the OTP.";
                ViewBag.Email = email;
                ViewBag.Purpose = purpose;
                return View();
            }

            if (purpose == "Register" || purpose == "RegisterInvitation")
            {
                if (_memoryCache.TryGetValue("RegOTP_" + email, out PendingRegistration pending))
                {
                    if (pending.Otp == otp)
                    {
                        if (DateTime.Now > pending.ExpiryTime)
                        {
                            ViewBag.Error = "OTP has expired. Please register again.";
                            ViewBag.Email = email;
                            ViewBag.Purpose = purpose;
                            return View();
                        }

                        // Register user
                        var result = await _userService.RegisterAsync(pending.User);
                        if (!result)
                        {
                            ViewBag.Error = "User registration failed (user might have been created meanwhile).";
                            ViewBag.Email = email;
                            ViewBag.Purpose = purpose;
                            return View();
                        }

                        // Create Student or Teacher based on Role if invitation
                        if (purpose == "RegisterInvitation")
                        {
                            var role = _roleService.GetById(pending.User.RoleId);
                            if (role != null)
                            {
                                if (role.RoleName.ToLower() == "student")
                                {
                                    await _studentService.CreateAsync(new StudentModel
                                    {
                                        Name = pending.FullName ?? pending.User.UserName,
                                        Email = pending.User.Email,
                                        Age = pending.Age ?? 0,
                                        Address = pending.User.Address ?? "",
                                        DepartmentId = pending.DepartmentId
                                    });
                                }
                                else if (role.RoleName.ToLower() == "teacher")
                                {
                                    await _teacherService.CreateAsync(new Teacher
                                    {
                                        Name = pending.FullName ?? pending.User.UserName,
                                        Email = pending.User.Email,
                                        Phone = pending.Phone ?? "",
                                        Address = pending.User.Address ?? "",
                                        DepartmentId = pending.DepartmentId
                                    });
                                }
                            }

                            await _invitationService.AcceptInvitationAsync(pending.Token);
                        }

                        // Remove from Cache
                        _memoryCache.Remove("RegOTP_" + email);

                        TempData["SuccessMessage"] = "Registration successful! You can now login.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ViewBag.Error = "Invalid OTP code.";
                    }
                }
                else
                {
                    ViewBag.Error = "No registration request found or OTP expired. Please try registering again.";
                }
            }
            else if (purpose == "ForgotPassword")
            {
                if (_memoryCache.TryGetValue("ResetOTP_" + email, out string cachedOtp))
                {
                    if (cachedOtp == otp)
                    {
                        _memoryCache.Set("ResetVerified_" + email, true, TimeSpan.FromMinutes(10));
                        return RedirectToAction("ResetPassword", new { email = email });
                    }
                    else
                    {
                        ViewBag.Error = "Invalid OTP code.";
                    }
                }
                else
                {
                    ViewBag.Error = "OTP expired or not found. Please request a new one.";
                }
            }

            ViewBag.Email = email;
            ViewBag.Purpose = purpose;
            return View();
        }

        // ================= FORGOT PASSWORD =================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            var user = _userService.GetAll().FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                ViewBag.Error = "No user found with this email address.";
                return View();
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            _memoryCache.Set("ResetOTP_" + email, otp, TimeSpan.FromMinutes(10));

            string subject = "Reset Password OTP";
            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                <h2 style='color: #dc3545; text-align: center;'>Reset Password Verification</h2>
                <p>Hello,</p>
                <p>You requested a password reset. Your verification OTP code is:</p>
                <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 28px; font-weight: bold; letter-spacing: 5px; color: #dc3545; border-radius: 4px; margin: 20px 0;'>
                    {otp}
                </div>
                <p>This code will expire in 10 minutes.</p>
                <p style='color: #999; font-size: 12px; text-align: center; margin-top: 30px;'>If you did not request this, please ignore this email.</p>
            </div>";

            try
            {
                await _emailService.SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Failed to send reset email. " + ex.Message;
                return View();
            }

            return RedirectToAction("VerifyOtp", new { email = email, purpose = "ForgotPassword" });
        }

        // ================= RESET PASSWORD =================
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (!_memoryCache.TryGetValue("ResetVerified_" + email, out bool verified) || !verified)
            {
                return RedirectToAction("ForgotPassword");
            }
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string password, string confirmPassword)
        {
            if (!_memoryCache.TryGetValue("ResetVerified_" + email, out bool verified) || !verified)
            {
                return RedirectToAction("ForgotPassword");
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                ViewBag.Email = email;
                return View();
            }

            var user = _userService.GetAll().FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                ViewBag.Error = "User not found.";
                ViewBag.Email = email;
                return View();
            }

            // Update password hash
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            await _userService.UpdateAsync(user);

            // Clean up cache
            _memoryCache.Remove("ResetVerified_" + email);
            _memoryCache.Remove("ResetOTP_" + email);

            TempData["SuccessMessage"] = "Password reset successful! You can now login.";
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

    public class PendingRegistration
    {
        public User User { get; set; }
        public string Otp { get; set; }
        public string Token { get; set; }       // for invitation
        public int DepartmentId { get; set; }   // for invitation
        public int? Age { get; set; }           // for invitation (student)
        public string Phone { get; set; }       // for invitation (teacher)
        public string? FullName { get; set; }   // full display name for Teacher/Student profile
        public DateTime ExpiryTime { get; set; }
    }
}