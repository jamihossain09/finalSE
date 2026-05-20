using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using finalSE.Models;
using System.Data;

[Authorize(Roles = "Admin")]
public class RoleController : Controller
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    // ================= LIST =================
    public IActionResult Index()
    {
        return View(_roleService.GetAll());
    }

    // ================= CREATE (GET) =================
    public IActionResult Create()
    {
        return View();
    }

    // ================= CREATE (POST) =================
    [HttpPost]
    public IActionResult Create(Role role)
    {
        if (string.IsNullOrEmpty(role.RoleName))
        {
            ViewBag.Error = "Role name required";
            return View(role);
        }

        _roleService.Add(role);
        return RedirectToAction("Index");
    }

    // ================= EDIT (GET) =================
    public IActionResult Edit(int id)
    {
        return View(_roleService.GetById(id));
    }

    // ================= EDIT (POST) =================
    [HttpPost]
    public IActionResult Edit(Role role)
    {
        _roleService.Update(role);
        return RedirectToAction("Index");
    }

    // ================= DELETE =================
    public IActionResult Delete(int id)
    {
        _roleService.Delete(id);
        return RedirectToAction("Index");
    }
}