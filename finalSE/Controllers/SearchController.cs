using finalSE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace finalSE.Controllers
{
    public class SearchResultItem
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-search";
        public string BadgeColor { get; set; } = "bg-primary";
    }

    public class GlobalSearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<SearchResultItem> Results { get; set; } = new List<SearchResultItem>();
    }

    [Authorize]
    public class SearchController : Controller
    {
        private readonly MyDBContext _context;

        public SearchController(MyDBContext context)
        {
            _context = context;
        }

        // ---- Static navigation shortcuts -----------------------------------------------
        // Maps human-readable module aliases → (url generator, icon, badge colour)
        private void AddNavigationShortcuts(GlobalSearchViewModel vm, string term)
        {
            var shortcuts = new List<(string[] aliases, string label, string desc, Func<string> url, string icon, string badge)>
            {
                (new[]{"notice","notice board","notices","announcement","announcements"},
                 "Notice Board", "View all published notices and announcements",
                 () => User.IsInRole("Admin")  ? Url.Action("Index","Notice")  ?? "#"
                     : User.IsInRole("Teacher") ? Url.Action("Notices","TeacherPanel") ?? "#"
                     : Url.Action("Notices","StudentPortal") ?? "#",
                 "fas fa-bullhorn", "bg-warning text-dark"),

                (new[]{"routine","routines","class schedule","schedule","timetable"},
                 "Class Routines", "View class schedules and timetables",
                 () => User.IsInRole("Admin")  ? Url.Action("Index","Routine")  ?? "#"
                     : User.IsInRole("Teacher") ? Url.Action("Routines","TeacherPanel") ?? "#"
                     : Url.Action("Routines","StudentPortal") ?? "#",
                 "fas fa-calendar-days", "bg-success"),

                (new[]{"class record","class records","class video","class videos","lecture","lectures","recording","recordings"},
                 "Class Video Records", "Watch recorded class lectures",
                 () => User.IsInRole("Teacher") ? Url.Action("ClassRecords","TeacherPanel") ?? "#"
                     : Url.Action("ClassRecords","StudentPortal") ?? "#",
                 "fas fa-video", "bg-success"),

                (new[]{"tutorial","tutorials","slide","slides","material","materials"},
                 "Tutorials & Slides", "View study materials, PDFs and presentations",
                 () => User.IsInRole("Teacher") ? Url.Action("Tutorials","TeacherPanel") ?? "#"
                     : Url.Action("Tutorials","StudentPortal") ?? "#",
                 "fas fa-file-pdf", "bg-info"),

                (new[]{"assignment","assignments","task","tasks","homework"},
                 "Assignments", "View posted assignments and tasks",
                 () => User.IsInRole("Teacher") ? Url.Action("Assignments","TeacherPanel") ?? "#"
                     : Url.Action("Assignments","StudentPortal") ?? "#",
                 "fas fa-tasks", "bg-primary"),

                (new[]{"student","students","pupil","pupils"},
                 "Students", "Manage student profiles and records",
                 () => Url.Action("Index","Student") ?? "#",
                 "fas fa-user-graduate", "bg-primary"),

                (new[]{"teacher","teachers","faculty","instructor","instructors"},
                 "Teachers", "Manage teacher profiles and records",
                 () => Url.Action("Index","Teacher") ?? "#",
                 "fas fa-chalkboard-teacher", "bg-info"),

                (new[]{"result","results","marks","grade","grades","mark sheet","marksheet","gradesheet"},
                 "Student Marks & Results", "View or manage student academic results",
                 () => User.IsInRole("Teacher") ? Url.Action("StudentMarks","TeacherPanel") ?? "#"
                     : Url.Action("MyResults","StudentPortal") ?? "#",
                 "fas fa-chart-line", "bg-primary"),

                (new[]{"department","departments","dept"},
                 "Departments", "Manage academic departments",
                 () => Url.Action("Index","DepartmentModels") ?? "#",
                 "fas fa-building", "bg-secondary"),

                (new[]{"payment","payments","salary","bkash"},
                 "Teacher Payments", "View teacher salary payment history",
                 () => User.IsInRole("Admin") ? Url.Action("Index","TeacherPayment") ?? "#"
                     : Url.Action("PaymentHistory","TeacherPanel") ?? "#",
                 "fas fa-money-bill-wave", "bg-success"),
            };

            foreach (var (aliases, label, desc, urlFn, icon, badge) in shortcuts)
            {
                if (aliases.Any(a => a.Contains(term) || term.Contains(a)))
                {
                    vm.Results.Insert(0, new SearchResultItem
                    {
                        Category    = "Navigation",
                        Title       = label,
                        Subtitle    = "Quick Link — Module shortcut",
                        Description = desc,
                        Url         = urlFn(),
                        Icon        = icon,
                        BadgeColor  = badge
                    });
                }
            }
        }
        // ---- End navigation shortcuts ---------------------------------------------------

        public async Task<IActionResult> Index(string? q)
        {
            var viewModel = new GlobalSearchViewModel
            {
                Query = q?.Trim() ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(viewModel.Query))
            {
                return View(viewModel);
            }

            string term = viewModel.Query.ToLower();

            // Always add navigation shortcuts first
            AddNavigationShortcuts(viewModel, term);

            if (User.IsInRole("Admin"))
            {
                // Students
                var students = await _context.Students
                    .Include(s => s.Department)
                    .Where(s => s.Name.ToLower().Contains(term) ||
                                s.Email.ToLower().Contains(term) ||
                                (s.Address != null && s.Address.ToLower().Contains(term)) ||
                                (s.Department != null && s.Department.DepartmentName.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var s in students)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Student",
                        Title = s.Name,
                        Subtitle = $"{s.Department?.DepartmentName ?? "N/A"} Department • {s.Email}",
                        Description = $"Age: {s.Age} | Address: {s.Address}",
                        Url = Url.Action("Details", "Student", new { id = s.Id }) ?? "#",
                        Icon = "fas fa-user-graduate",
                        BadgeColor = "bg-primary"
                    });
                }

                // Teachers
                var teachers = await _context.Teachers
                    .Include(t => t.Department)
                    .Where(t => t.Name.ToLower().Contains(term) ||
                                t.Email.ToLower().Contains(term) ||
                                (t.Phone != null && t.Phone.ToLower().Contains(term)) ||
                                (t.Department != null && t.Department.DepartmentName.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var t in teachers)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Teacher",
                        Title = t.Name,
                        Subtitle = $"{t.Department?.DepartmentName ?? "N/A"} Department • {t.Email}",
                        Description = $"Phone: {t.Phone} | Office: {t.Address}",
                        Url = Url.Action("Index", "Teacher") ?? "#",
                        Icon = "fas fa-chalkboard-teacher",
                        BadgeColor = "bg-info"
                    });
                }

                // Routines
                var routines = await _context.Routines
                    .Include(r => r.Department)
                    .Where(r => r.Title.ToLower().Contains(term) ||
                                r.Type.ToLower().Contains(term) ||
                                (r.Department != null && r.Department.DepartmentName.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var r in routines)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Routine",
                        Title = r.Title,
                        Subtitle = $"Type: {r.Type} • Target: {(r.Department != null ? r.Department.DepartmentName : "Global")}",
                        Description = $"Uploaded: {r.UploadedAt:MMM dd, yyyy}",
                        Url = Url.Action("Index", "Routine") ?? "#",
                        Icon = "fas fa-calendar-days",
                        BadgeColor = "bg-success"
                    });
                }

                // Notices
                var notices = await _context.Notices
                    .Include(n => n.Department)
                    .Where(n => n.Title.ToLower().Contains(term) ||
                                (n.Description != null && n.Description.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var n in notices)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Notice",
                        Title = n.Title,
                        Subtitle = $"Published: {n.PublishedAt:MMM dd, yyyy} • Target: {(n.Department != null ? n.Department.DepartmentName : "Global")}",
                        Description = n.Description ?? "No description available",
                        Url = Url.Action("Index", "Notice") ?? "#",
                        Icon = "fas fa-bullhorn",
                        BadgeColor = "bg-warning text-dark"
                    });
                }

                // Subjects
                var subjects = await _context.Subjects
                    .Include(s => s.Department)
                    .Where(s => s.SubjectName.ToLower().Contains(term) ||
                                s.SubjectCode.ToLower().Contains(term))
                    .Take(10)
                    .ToListAsync();

                foreach (var sub in subjects)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Subject",
                        Title = $"{sub.SubjectCode} - {sub.SubjectName}",
                        Subtitle = $"{sub.Department?.DepartmentName ?? "N/A"} Department",
                        Description = $"Subject Code: {sub.SubjectCode}",
                        Url = Url.Action("Index", "CourseAssignment") ?? "#",
                        Icon = "fas fa-book",
                        BadgeColor = "bg-secondary"
                    });
                }
            }
            else if (User.IsInRole("Teacher"))
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Teacher? teacher = null;
                if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int uId))
                {
                    var user = await _context.Users.FindAsync(uId);
                    if (user != null)
                    {
                        teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Email == user.Email);
                    }
                }

                int? deptId = teacher?.DepartmentId;

                // Class Records
                var classRecords = await _context.ClassRecords
                    .Where(r => (teacher != null && r.TeacherId == teacher.Id) || (deptId != null && r.Teacher != null && r.Teacher.DepartmentId == deptId))
                    .Where(r => r.Title.ToLower().Contains(term) || (r.Description != null && r.Description.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var cr in classRecords)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Class Record",
                        Title = cr.Title,
                        Subtitle = $"Type: {cr.UploadType} • Uploaded: {cr.UploadedAt:MMM dd, yyyy}",
                        Description = cr.Description ?? "Class video recording",
                        Url = Url.Action("ClassRecords", "TeacherPanel") ?? "#",
                        Icon = "fas fa-video",
                        BadgeColor = "bg-success"
                    });
                }

                // Tutorials
                var tutorials = await _context.Tutorials
                    .Where(t => (teacher != null && t.TeacherId == teacher.Id) || (deptId != null && t.Teacher != null && t.Teacher.DepartmentId == deptId))
                    .Where(t => t.Title.ToLower().Contains(term) || (t.Description != null && t.Description.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var tut in tutorials)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Tutorial",
                        Title = tut.Title,
                        Subtitle = $"Uploaded: {tut.UploadedAt:MMM dd, yyyy}",
                        Description = tut.Description ?? "Tutorial material",
                        Url = Url.Action("Tutorials", "TeacherPanel") ?? "#",
                        Icon = "fas fa-file-pdf",
                        BadgeColor = "bg-info"
                    });
                }

                // Assignments
                var assignments = await _context.AssignmentTasks
                    .Where(a => (teacher != null && a.TeacherId == teacher.Id) || (deptId != null && a.Teacher != null && a.Teacher.DepartmentId == deptId))
                    .Where(a => a.Title.ToLower().Contains(term) || (a.Description != null && a.Description.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var ass in assignments)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Assignment",
                        Title = ass.Title,
                        Subtitle = $"Due Date: {ass.DueDate:MMM dd, yyyy}",
                        Description = ass.Description ?? "Course assignment task",
                        Url = Url.Action("Assignments", "TeacherPanel") ?? "#",
                        Icon = "fas fa-tasks",
                        BadgeColor = "bg-primary"
                    });
                }

                // Notices
                var notices = await _context.Notices
                    .Where(n => n.DepartmentId == null || n.DepartmentId == deptId)
                    .Where(n => n.Title.ToLower().Contains(term) || (n.Description != null && n.Description.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var n in notices)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Notice",
                        Title = n.Title,
                        Subtitle = $"Published: {n.PublishedAt:MMM dd, yyyy}",
                        Description = n.Description ?? "Official announcement",
                        Url = Url.Action("Index", "Notice") ?? "#",
                        Icon = "fas fa-bullhorn",
                        BadgeColor = "bg-warning text-dark"
                    });
                }
            }
            else if (User.IsInRole("Student"))
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                StudentModel? student = null;
                if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int uId))
                {
                    var user = await _context.Users.FindAsync(uId);
                    if (user != null)
                    {
                        student = await _context.Students.FirstOrDefaultAsync(s => s.Email == user.Email);
                    }
                }

                int? deptId = student?.DepartmentId;

                // Class Records
                var classRecords = await _context.ClassRecords
                    .Include(c => c.Teacher)
                    .Where(c => deptId == null || (c.Teacher != null && c.Teacher.DepartmentId == deptId))
                    .Where(c => c.Title.ToLower().Contains(term) || (c.Description != null && c.Description.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var cr in classRecords)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Class Video",
                        Title = cr.Title,
                        Subtitle = $"Teacher: {cr.Teacher?.Name ?? "Instructor"} • Recorded Lecture",
                        Description = cr.Description ?? "Class video recording",
                        Url = Url.Action("ClassRecords", "StudentPortal") ?? "#",
                        Icon = "fas fa-video",
                        BadgeColor = "bg-success"
                    });
                }

                // Tutorials
                var tutorials = await _context.Tutorials
                    .Include(t => t.Teacher)
                    .Where(t => deptId == null || (t.Teacher != null && t.Teacher.DepartmentId == deptId))
                    .Where(t => t.Title.ToLower().Contains(term) || (t.Description != null && t.Description.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var tut in tutorials)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Tutorial",
                        Title = tut.Title,
                        Subtitle = $"Teacher: {tut.Teacher?.Name ?? "Instructor"} • Slides & Materials",
                        Description = tut.Description ?? "Study tutorial material",
                        Url = Url.Action("Tutorials", "StudentPortal") ?? "#",
                        Icon = "fas fa-file-pdf",
                        BadgeColor = "bg-info"
                    });
                }

                // Assignments
                var assignments = await _context.AssignmentTasks
                    .Include(a => a.Teacher)
                    .Where(a => deptId == null || (a.Teacher != null && a.Teacher.DepartmentId == deptId))
                    .Where(a => a.Title.ToLower().Contains(term) || (a.Description != null && a.Description.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();


                foreach (var ass in assignments)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Assignment",
                        Title = ass.Title,
                        Subtitle = $"Due Date: {ass.DueDate:MMM dd, yyyy}",
                        Description = ass.Description ?? "Course assignment task",
                        Url = Url.Action("Assignments", "StudentPortal") ?? "#",
                        Icon = "fas fa-tasks",
                        BadgeColor = "bg-primary"
                    });
                }

                // Notices
                var notices = await _context.Notices
                    .Where(n => n.DepartmentId == null || n.DepartmentId == deptId)
                    .Where(n => n.Title.ToLower().Contains(term) || (n.Description != null && n.Description.ToLower().Contains(term)))
                    .Take(10)
                    .ToListAsync();

                foreach (var n in notices)
                {
                    viewModel.Results.Add(new SearchResultItem
                    {
                        Category = "Notice",
                        Title = n.Title,
                        Subtitle = $"Published: {n.PublishedAt:MMM dd, yyyy}",
                        Description = n.Description ?? "Official announcement",
                        Url = Url.Action("Notices", "StudentPortal") ?? "#",
                        Icon = "fas fa-bullhorn",
                        BadgeColor = "bg-warning text-dark"
                    });
                }
            }

            return View(viewModel);
        }
    }
}
