using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using finalSE.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using System;

public class RoutineController : Controller
{
    private readonly MyDBContext _context;
    private readonly IWebHostEnvironment _environment;

    public RoutineController(MyDBContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ALL USERS CAN SEE - FILTERED BY DEPT
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId))
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                if (User.IsInRole("Student"))
                {
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == user.Email);
                    if (student != null)
                    {
                        var studentRoutines = await _context.Routines
                            .Include(r => r.Department)
                            .Where(r => r.DepartmentId == null || r.DepartmentId == student.DepartmentId)
                            .OrderByDescending(r => r.UploadedAt)
                            .ToListAsync();
                        return View(studentRoutines);
                    }
                }
                else if (User.IsInRole("Teacher"))
                {
                    var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == user.Email);
                    if (teacher != null)
                    {
                        var teacherRoutines = await _context.Routines
                            .Include(r => r.Department)
                            .Where(r => r.DepartmentId == null || r.DepartmentId == teacher.DepartmentId)
                            .OrderByDescending(r => r.UploadedAt)
                            .ToListAsync();
                        return View(teacherRoutines);
                    }
                }
            }
        }

        var routines = await _context.Routines
            .Include(r => r.Department)
            .OrderByDescending(r => r.UploadedAt)
            .ToListAsync();
        return View(routines);
    }

    // ONLY ADMIN CREATE
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Routine routine, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select a routine file to upload.");
            ViewBag.Departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
            return View(routine);
        }

        string folderPath = Path.Combine(_environment.WebRootPath, "uploads/routines");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        string fullPath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        routine.FilePath = "/uploads/routines/" + fileName;
        routine.UploadedAt = DateTime.Now;

        ModelState.Remove("FilePath");

        if (ModelState.IsValid)
        {
            _context.Routines.Add(routine);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Class routine uploaded successfully!";
            return RedirectToAction("Index");
        }

        ViewBag.Departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
        return View(routine);
    }

    // ONLY ADMIN DELETE
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var routine = await _context.Routines.FindAsync(id);
        if (routine == null) return NotFound();

        _context.Routines.Remove(routine);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }
}