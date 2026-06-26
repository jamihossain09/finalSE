using finalSE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace finalSE.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherPanelController : Controller
    {
        private readonly MyDBContext _context;
        private readonly IWebHostEnvironment _environment;

        public TeacherPanelController(MyDBContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // Helper method to get the current teacher profile based on logged-in user email
        private async Task<Teacher?> GetCurrentTeacherAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return null;

            if (int.TryParse(userIdString, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    return await _context.Teachers
                        .Include(t => t.Department)
                        .FirstOrDefaultAsync(t => t.Email == user.Email);
                }
            }
            return null;
        }

        // ================= DASHBOARD INDEX =================
        public async Task<IActionResult> Index()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return Content("❌ Teacher profile not found. Please make sure your registered email matches an assigned teacher email in the system, or contact the administrator.");
            }

            ViewBag.Teacher = teacher;
            ViewBag.ClassRecordsCount = await _context.ClassRecords.CountAsync(c => c.TeacherId == teacher.Id);
            ViewBag.TutorialsCount = await _context.Tutorials.CountAsync(t => t.TeacherId == teacher.Id);
            ViewBag.AssignmentsCount = await _context.AssignmentTasks.CountAsync(a => a.TeacherId == teacher.Id);

            return View();
        }

        // ================= ROUTINES =================
        public async Task<IActionResult> Routines()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var routines = await _context.Routines
                .Where(r => r.DepartmentId == teacher.DepartmentId || r.DepartmentId == null)
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();
            return View(routines);
        }

        // ================= CLASS RECORDS =================
        public async Task<IActionResult> ClassRecords()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var records = await _context.ClassRecords
                .Where(r => r.TeacherId == teacher.Id)
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            return View(records);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadClassRecord(string title, string? description, string uploadType, string? link, IFormFile? file)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var record = new ClassRecord
            {
                Title = title,
                Description = description,
                UploadType = uploadType,
                TeacherId = teacher.Id,
                UploadedAt = DateTime.Now,
                FilePathOrLink = ""
            };

            if (uploadType == "Link")
            {
                if (string.IsNullOrWhiteSpace(link))
                {
                    TempData["ErrorMessage"] = "Please provide a valid URL link.";
                    return RedirectToAction(nameof(ClassRecords));
                }
                record.FilePathOrLink = link;
            }
            else // File upload
            {
                if (file == null || file.Length == 0)
                {
                    TempData["ErrorMessage"] = "Please select a recording file to upload.";
                    return RedirectToAction(nameof(ClassRecords));
                }

                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/classrecords");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                record.FilePathOrLink = "/uploads/classrecords/" + uniqueFileName;
            }

            _context.ClassRecords.Add(record);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Class record added successfully!";

            return RedirectToAction(nameof(ClassRecords));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClassRecord(int id)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var record = await _context.ClassRecords.FirstOrDefaultAsync(r => r.Id == id && r.TeacherId == teacher.Id);
            if (record == null) return NotFound();

            if (record.UploadType == "File")
            {
                string fullPath = Path.Combine(_environment.WebRootPath, record.FilePathOrLink.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.ClassRecords.Remove(record);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Class record deleted successfully!";

            return RedirectToAction(nameof(ClassRecords));
        }

        // ================= TUTORIALS =================
        public async Task<IActionResult> Tutorials()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var tutorials = await _context.Tutorials
                .Where(t => t.TeacherId == teacher.Id)
                .OrderByDescending(t => t.UploadedAt)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            return View(tutorials);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTutorial(string title, string? description, string? videoLink, IFormFile? file)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var tutorial = new Tutorial
            {
                Title = title,
                Description = description,
                VideoLink = videoLink,
                TeacherId = teacher.Id,
                UploadedAt = DateTime.Now
            };

            if (file != null && file.Length > 0)
            {
                string extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".pdf" && extension != ".ppt" && extension != ".pptx")
                {
                    TempData["ErrorMessage"] = "Only PDF, PPT, and PPTX files are allowed for tutorials.";
                    return RedirectToAction(nameof(Tutorials));
                }

                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/tutorials");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                tutorial.FilePath = "/uploads/tutorials/" + uniqueFileName;
            }

            _context.Tutorials.Add(tutorial);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tutorial uploaded successfully!";

            return RedirectToAction(nameof(Tutorials));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTutorial(int id)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var tutorial = await _context.Tutorials.FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacher.Id);
            if (tutorial == null) return NotFound();

            if (!string.IsNullOrEmpty(tutorial.FilePath))
            {
                string fullPath = Path.Combine(_environment.WebRootPath, tutorial.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.Tutorials.Remove(tutorial);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tutorial deleted successfully!";

            return RedirectToAction(nameof(Tutorials));
        }

        // ================= ASSIGNMENTS =================
        public async Task<IActionResult> Assignments()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var assignments = await _context.AssignmentTasks
                .Where(a => a.TeacherId == teacher.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            return View(assignments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostAssignment(string title, string description, DateTime dueDate, IFormFile? file)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var assignment = new AssignmentTask
            {
                Title = title,
                Description = description,
                DueDate = dueDate,
                TeacherId = teacher.Id,
                CreatedAt = DateTime.Now
            };

            if (file != null && file.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/assignments");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                assignment.FilePath = "/uploads/assignments/" + uniqueFileName;
            }

            _context.AssignmentTasks.Add(assignment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Assignment task posted successfully!";

            return RedirectToAction(nameof(Assignments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound();

            var assignment = await _context.AssignmentTasks.FirstOrDefaultAsync(a => a.Id == id && a.TeacherId == teacher.Id);
            if (assignment == null) return NotFound();

            if (!string.IsNullOrEmpty(assignment.FilePath))
            {
                string fullPath = Path.Combine(_environment.WebRootPath, assignment.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.AssignmentTasks.Remove(assignment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Assignment task deleted successfully!";

            return RedirectToAction(nameof(Assignments));
        }

        // ================= STUDENT MARKS =================
        public async Task<IActionResult> StudentMarks(int? subjectId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return Content("❌ Teacher profile not found. Please contact the administrator.");
            }

            // Get all subjects in the teacher's department
            var subjects = await _context.Subjects
                .Where(s => s.DepartmentId == teacher.DepartmentId)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();

            ViewBag.Teacher  = teacher;
            ViewBag.Subjects = subjects;

            // If no subject selected yet, just show the subject picker
            if (subjectId == null)
            {
                ViewBag.SelectedSubject = null;
                return View(new List<StudentMark>());
            }

            // Verify the subject belongs to the teacher's department
            var subject = subjects.FirstOrDefault(s => s.Id == subjectId);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Invalid subject selected.";
                return RedirectToAction(nameof(StudentMarks));
            }

            ViewBag.SelectedSubject = subject;

            // Get all students in the teacher's department
            var students = await _context.Students
                .Include(s => s.Department)
                .Where(s => s.DepartmentId == teacher.DepartmentId)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Ensure every student has a mark row for this subject
            var existingMarks = await _context.StudentMarks
                .Where(sm => sm.SubjectId == subjectId)
                .ToListAsync();

            foreach (var student in students)
            {
                if (!existingMarks.Any(m => m.StudentId == student.Id))
                {
                    var newMark = new StudentMark
                    {
                        StudentId   = student.Id,
                        TeacherId   = teacher.Id,
                        SubjectId   = subjectId.Value,
                        Attendance  = 0,
                        ClassTest   = 0,
                        MidTerm     = 0,
                        FinalExam   = 0,
                        Total       = 0,
                        LetterGrade = "F",
                        GradePoint  = 0.00,
                        Remarks     = "Fail",
                        IsPublished = false,
                        LastUpdated = DateTime.Now
                    };
                    _context.StudentMarks.Add(newMark);
                    existingMarks.Add(newMark);
                }
            }

            if (_context.ChangeTracker.HasChanges())
                await _context.SaveChangesAsync();

            // Reload with full navigation props
            var finalMarks = await _context.StudentMarks
                .Include(sm => sm.Student)
                .Include(sm => sm.Subject)
                .Where(sm => sm.SubjectId == subjectId)
                .OrderBy(sm => sm.Student.Name)
                .ToListAsync();

            return View(finalMarks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMarks(int studentId, int subjectId, double attendance, double classTest, double midTerm, double finalExam)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return Json(new { success = false, message = "Teacher profile not found." });

            // Verify subject belongs to teacher's department
            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId && s.DepartmentId == teacher.DepartmentId);
            if (subject == null) return Json(new { success = false, message = "Subject not found or not in your department." });

            if (attendance < 0 || attendance > 10)  return Json(new { success = false, message = "Attendance marks must be between 0 and 10." });
            if (classTest  < 0 || classTest  > 20)  return Json(new { success = false, message = "Class Test marks must be between 0 and 20." });
            if (midTerm    < 0 || midTerm    > 30)  return Json(new { success = false, message = "Mid-term marks must be between 0 and 30." });
            if (finalExam  < 0 || finalExam  > 40)  return Json(new { success = false, message = "Final exam marks must be between 0 and 40." });

            var total = attendance + classTest + midTerm + finalExam;

            var mark = await _context.StudentMarks
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.SubjectId == subjectId);
            if (mark == null) return Json(new { success = false, message = "Student mark record not found." });

            mark.TeacherId  = teacher.Id;   // record which teacher last saved
            mark.Attendance = attendance;
            mark.ClassTest  = classTest;
            mark.MidTerm    = midTerm;
            mark.FinalExam  = finalExam;
            mark.Total      = total;

            var gradeInfo       = CalculateGrade(total);
            mark.LetterGrade    = gradeInfo.LetterGrade;
            mark.GradePoint     = gradeInfo.GradePoint;
            mark.Remarks        = gradeInfo.Remarks;
            mark.LastUpdated    = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new {
                success     = true,
                message     = "Marks updated successfully!",
                total       = total,
                letterGrade = gradeInfo.LetterGrade,
                gradePoint  = gradeInfo.GradePoint.ToString("0.00"),
                remarks     = gradeInfo.Remarks
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishResult(int studentId, int subjectId, bool publish)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return Json(new { success = false, message = "Teacher profile not found." });

            var mark = await _context.StudentMarks
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.SubjectId == subjectId);
            if (mark == null) return Json(new { success = false, message = "Student mark record not found." });

            mark.IsPublished = publish;
            await _context.SaveChangesAsync();

            string actionText = publish ? "published" : "drafted";
            return Json(new { success = true, message = $"Student marks set to {actionText} successfully!", isPublished = publish });
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentInfo(int studentId, int subjectId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return Json(new { success = false, message = "Teacher profile not found." });

            var student = await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return Json(new { success = false, message = "Student not found." });

            var mark = await _context.StudentMarks
                .Include(m => m.Subject)
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.SubjectId == subjectId);

            return Json(new {
                success = true,
                student = new {
                    name       = student.Name,
                    email      = student.Email,
                    age        = student.Age,
                    address    = student.Address,
                    department = student.Department?.DepartmentName ?? "N/A"
                },
                marks = mark != null ? new {
                    attendance  = mark.Attendance,
                    classTest   = mark.ClassTest,
                    midTerm     = mark.MidTerm,
                    finalExam   = mark.FinalExam,
                    total       = mark.Total,
                    letterGrade = mark.LetterGrade,
                    gradePoint  = mark.GradePoint.ToString("0.00"),
                    remarks     = mark.Remarks,
                    isPublished = mark.IsPublished,
                    lastUpdated = mark.LastUpdated.ToString("yyyy-MM-dd HH:mm")
                } : null
            });
        }

        // ================= DOWNLOAD EXCEL TEMPLATE =================
        [HttpGet]
        public async Task<IActionResult> DownloadMarksTemplate(int subjectId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound("Teacher profile not found.");

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId && s.DepartmentId == teacher.DepartmentId);
            if (subject == null) return NotFound("Subject not found or not in your department.");

            var students = await _context.Students
                .Where(s => s.DepartmentId == teacher.DepartmentId)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var marks = await _context.StudentMarks
                .Where(m => m.SubjectId == subjectId)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Student Marks");

                // Headers
                worksheet.Cell(1, 1).Value = "Student ID";
                worksheet.Cell(1, 2).Value = "Student Name";
                worksheet.Cell(1, 3).Value = "Student Email";
                worksheet.Cell(1, 4).Value = "Attendance (Max 10)";
                worksheet.Cell(1, 5).Value = "Class Test (Max 20)";
                worksheet.Cell(1, 6).Value = "Mid-Term (Max 30)";
                worksheet.Cell(1, 7).Value = "Final Exam (Max 40)";

                // Style headers
                var headerRange = worksheet.Range("A1:G1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.Indigo;
                headerRange.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var student in students)
                {
                    var mark = marks.FirstOrDefault(m => m.StudentId == student.Id);

                    worksheet.Cell(row, 1).Value = student.Id;
                    worksheet.Cell(row, 2).Value = student.Name;
                    worksheet.Cell(row, 3).Value = student.Email;
                    worksheet.Cell(row, 4).Value = mark?.Attendance ?? 0;
                    worksheet.Cell(row, 5).Value = mark?.ClassTest ?? 0;
                    worksheet.Cell(row, 6).Value = mark?.MidTerm ?? 0;
                    worksheet.Cell(row, 7).Value = mark?.FinalExam ?? 0;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"{subject.SubjectCode}_Marks_Template.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        // ================= UPLOAD MARKS EXCEL =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMarksExcel(int subjectId, IFormFile excelFile)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound("Teacher profile not found.");

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId && s.DepartmentId == teacher.DepartmentId);
            if (subject == null) return NotFound("Subject not found or not in your department.");

            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please upload a valid Excel file.";
                return RedirectToAction(nameof(StudentMarks), new { subjectId = subjectId });
            }

            var extension = Path.GetExtension(excelFile.FileName).ToLower();
            if (extension != ".xlsx")
            {
                TempData["ErrorMessage"] = "Only Excel (.xlsx) files are supported.";
                return RedirectToAction(nameof(StudentMarks), new { subjectId = subjectId });
            }

            try
            {
                int updatedCount = 0;
                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["ErrorMessage"] = "Excel file is empty.";
                            return RedirectToAction(nameof(StudentMarks), new { subjectId = subjectId });
                        }

                        var rows = worksheet.RowsUsed().Skip(1); // skip headers
                        foreach (var row in rows)
                        {
                            var studentIdCell = row.Cell(1).Value.ToString();
                            if (string.IsNullOrWhiteSpace(studentIdCell) || !int.TryParse(studentIdCell, out int studentId))
                            {
                                continue;
                            }

                            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == studentId && s.DepartmentId == teacher.DepartmentId);
                            if (student == null) continue;

                            double attendance = 0;
                            double classTest = 0;
                            double midTerm = 0;
                            double finalExam = 0;

                            double.TryParse(row.Cell(4).Value.ToString(), out attendance);
                            double.TryParse(row.Cell(5).Value.ToString(), out classTest);
                            double.TryParse(row.Cell(6).Value.ToString(), out midTerm);
                            double.TryParse(row.Cell(7).Value.ToString(), out finalExam);

                            attendance = Math.Clamp(attendance, 0, 10);
                            classTest = Math.Clamp(classTest, 0, 20);
                            midTerm = Math.Clamp(midTerm, 0, 30);
                            finalExam = Math.Clamp(finalExam, 0, 40);

                            double total = attendance + classTest + midTerm + finalExam;
                            var gradeInfo = CalculateGrade(total);

                            var mark = await _context.StudentMarks.FirstOrDefaultAsync(m => m.StudentId == studentId && m.SubjectId == subjectId);
                            if (mark == null)
                            {
                                mark = new StudentMark
                                {
                                    StudentId = studentId,
                                    SubjectId = subjectId,
                                    TeacherId = teacher.Id
                                };
                                _context.StudentMarks.Add(mark);
                            }

                            mark.TeacherId = teacher.Id;
                            mark.Attendance = attendance;
                            mark.ClassTest = classTest;
                            mark.MidTerm = midTerm;
                            mark.FinalExam = finalExam;
                            mark.Total = total;
                            mark.LetterGrade = gradeInfo.LetterGrade;
                            mark.GradePoint = gradeInfo.GradePoint;
                            mark.Remarks = gradeInfo.Remarks;
                            mark.IsPublished = true;
                            mark.LastUpdated = DateTime.Now;

                            updatedCount++;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Successfully imported and published marks for {updatedCount} students!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to parse Excel file: {ex.Message}";
            }

            return RedirectToAction(nameof(StudentMarks), new { subjectId = subjectId });
        }

        private (string LetterGrade, double GradePoint, string Remarks) CalculateGrade(double total)
        {
            if (total >= 80) return ("A+", 4.00, "Excellent");
            if (total >= 75) return ("A",  3.75, "Very Good");
            if (total >= 70) return ("A-", 3.50, "Very Good");
            if (total >= 65) return ("B+", 3.25, "Good");
            if (total >= 60) return ("B",  3.00, "Good");
            if (total >= 55) return ("B-", 2.75, "Satisfactory");
            if (total >= 50) return ("C+", 2.50, "Satisfactory");
            if (total >= 45) return ("C",  2.25, "Pass");
            if (total >= 40) return ("D",  2.00, "Pass");
            return ("F", 0.00, "Fail");
        }
    }
}
