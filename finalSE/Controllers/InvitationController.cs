using finalSE.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace finalSE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InvitationController : Controller
    {
        private readonly IInvitationService _invitationService;
        private readonly IRoleService _roleService;

        public InvitationController(
            IInvitationService invitationService,
            IRoleService roleService)
        {
            _invitationService = invitationService;
            _roleService = roleService;
        }

        // ================= LIST =================
        public async Task<IActionResult> Index()
        {
            var invitations = await _invitationService.GetAllAsync();
            return View(invitations);
        }

        // ================= CREATE (GET) =================
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(
                _roleService.GetAll(),
                "Id",
                "RoleName"
            );

            return View();
        }

        // ================= CREATE (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string email, int roleId)
        {
            if (string.IsNullOrWhiteSpace(email) || roleId <= 0)
            {
                ViewBag.Error = "Email and Role required";

                ViewBag.Roles = new SelectList(
                    _roleService.GetAll(),
                    "Id",
                    "RoleName"
                );

                return View();
            }

            try
            {
                await _invitationService.SendInvitationAsync(email, roleId);
                TempData["SuccessMessage"] = $"Invitation email successfully sent to {email}!";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send invitation email: {ex.Message}. Make sure your SMTP settings in appsettings.json are configured.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ================= RESEND / SEND TO EMAIL =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmail(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Token and email are required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _invitationService.ResendInvitationEmailAsync(token, email);
                TempData["SuccessMessage"] = $"Invitation email successfully sent to {email}!";
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send email: {ex.Message}. Make sure your SMTP settings in appsettings.json are configured.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}