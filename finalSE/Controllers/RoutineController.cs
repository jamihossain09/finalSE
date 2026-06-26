using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class RoutineController : Controller
{
    private readonly MyDBContext _context;
    private readonly IWebHostEnvironment _environment;

    public RoutineController(MyDBContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ALL USERS CAN SEE
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var routines = await _context.Routines.ToListAsync();
        return View(routines);
    }

    // ONLY ADMIN CREATE
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _context.Departments.ToListAsync();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Routine routine, IFormFile file, int? departmentId)
    {
        if (file == null)
        {
            ViewBag.Departments = await _context.Departments.ToListAsync();
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
        routine.DepartmentId = departmentId;

        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
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