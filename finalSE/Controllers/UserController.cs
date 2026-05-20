using finalSE.Models;
using finalSE.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;

    public UserController(IUserService userService, IRoleService roleService)
    {
        _userService = userService;
        _roleService = roleService;
    }

    public IActionResult Index()
    {
        return View(_userService.GetAll());
    }

    // ================= CREATE (GET) =================
    public IActionResult Create()
    {
        ViewBag.Roles = new SelectList(
            _roleService.GetAll(),
            "Id",
            "RoleName"
        );

        return View();
    }

    // ================= CREATE (POST FIXED) =================
    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        // 🔥 IMPORTANT FIX: ensure RoleId is not 0
        if (user.RoleId == 0)
        {
            ViewBag.Roles = new SelectList(
                _roleService.GetAll(),
                "Id",
                "RoleName"
            );

            ViewBag.Error = "Please select a role";
            return View(user);
        }

        bool success = await _userService.RegisterAsync(user);

        if (!success)
        {
            ViewBag.Roles = new SelectList(
                _roleService.GetAll(),
                "Id",
                "RoleName"
            );

            ViewBag.Error = "Username already exists";
            return View(user);
        }

        return RedirectToAction("Index");
    }

    // ================= EDIT =================
    public IActionResult Edit(int id)
    {
        ViewBag.Roles = new SelectList(
            _roleService.GetAll(),
            "Id",
            "RoleName"
        );

        return View(_userService.GetById(id));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(User user)
    {
        await _userService.UpdateAsync(user);
        return RedirectToAction("Index");
    }

    // ================= DELETE =================
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteAsync(id);
        return RedirectToAction("Index");
    }
}