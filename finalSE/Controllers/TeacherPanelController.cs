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
using ExcelDataReader;

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
            var teacherIdClaim = User.FindFirstValue("TeacherId");
            if (!string.IsNullOrEmpty(teacherIdClaim) && int.TryParse(teacherIdClaim, out int teacherId))
            {
                return await _context.Teachers
                    .Include(t => t.Department)
                    .FirstOrDefaultAsync(t => t.Id == teacherId);
            }

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
                .Where(r => r.DepartmentId == null || r.DepartmentId == teacher.DepartmentId)
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
        [Microsoft.AspNetCore.Mvc.RequestSizeLimit(524_288_000)] // 500 MB
        [Microsoft.AspNetCore.Mvc.RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
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

            // Get assigned subject IDs for this teacher
            var assignedSubjectIds = await _context.CourseAssignments
                .Where(ca => ca.TeacherId == teacher.Id)
                .Select(ca => ca.SubjectId)
                .ToListAsync();

            // Get all subjects in the teacher's department that are assigned to the teacher
            var subjects = await _context.Subjects
                .Where(s => assignedSubjectIds.Contains(s.Id))
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

            // Verify the subject is assigned to this teacher
            var subject = subjects.FirstOrDefault(s => s.Id == subjectId);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Invalid or unassigned subject selected.";
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
                        AttendanceStatus = "Dis-collegiate",
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
                .Where(sm => sm.SubjectId == subjectId && sm.Student.DepartmentId == teacher.DepartmentId)
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

            // Verify subject belongs to teacher's department and is assigned to the teacher
            var isAssigned = await _context.CourseAssignments.AnyAsync(ca => ca.SubjectId == subjectId && ca.TeacherId == teacher.Id);
            if (!isAssigned) return Json(new { success = false, message = "Subject is not assigned to you." });

            // Verify student belongs to teacher's department
            var student = await _context.Students.FindAsync(studentId);
            if (student == null || student.DepartmentId != teacher.DepartmentId)
            {
                return Json(new { success = false, message = "Student not found or not in your department." });
            }

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

            var gradeInfo       = CalculateGrade(total, attendance);
            mark.LetterGrade    = gradeInfo.LetterGrade;
            mark.GradePoint     = gradeInfo.GradePoint;
            mark.Remarks        = gradeInfo.Remarks;
            mark.AttendanceStatus = gradeInfo.AttendanceStatus;
            mark.LastUpdated    = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new {
                success     = true,
                message     = "Marks updated successfully!",
                total       = total,
                letterGrade = gradeInfo.LetterGrade,
                gradePoint  = gradeInfo.GradePoint.ToString("0.00"),
                remarks     = gradeInfo.Remarks,
                attendanceStatus = gradeInfo.AttendanceStatus
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishResult(int studentId, int subjectId, bool publish)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return Json(new { success = false, message = "Teacher profile not found." });

            // Verify the subject is assigned to this teacher
            var isAssigned = await _context.CourseAssignments.AnyAsync(ca => ca.SubjectId == subjectId && ca.TeacherId == teacher.Id);
            if (!isAssigned)
                return Json(new { success = false, message = "You are not authorized to publish results for a subject not assigned to you." });

            var mark = await _context.StudentMarks
                .Include(m => m.Student)
                .FirstOrDefaultAsync(m => m.StudentId == studentId && m.SubjectId == subjectId);
            if (mark == null || mark.Student.DepartmentId != teacher.DepartmentId)
                return Json(new { success = false, message = "Student mark record not found or not authorized." });

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

            // Verify the subject is assigned to this teacher
            var isAssigned = await _context.CourseAssignments.AnyAsync(ca => ca.SubjectId == subjectId && ca.TeacherId == teacher.Id);
            if (!isAssigned)
                return Json(new { success = false, message = "You are not authorized for this subject." });

            var student = await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null || student.DepartmentId != teacher.DepartmentId)
                return Json(new { success = false, message = "Student not found or not authorized." });

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
                    attendanceStatus = mark.AttendanceStatus,
                    isPublished = mark.IsPublished,
                    lastUpdated = mark.LastUpdated.ToString("yyyy-MM-dd HH:mm")
                } : null
            });
        }

        // ================= BULK EXCEL/CSV MARKS MANAGEMENT =================
        [HttpGet]
        public async Task<IActionResult> DownloadMarksTemplate(int subjectId)
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null) return NotFound("Teacher profile not found.");

            // Verify the subject is assigned to this teacher
            var isAssigned = await _context.CourseAssignments.AnyAsync(ca => ca.SubjectId == subjectId && ca.TeacherId == teacher.Id);
            if (!isAssigned) return NotFound("Subject not found or not assigned to you.");

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId && s.DepartmentId == teacher.DepartmentId);
            if (subject == null) return NotFound("Subject not found.");

            var students = await _context.Students
                .Where(s => s.DepartmentId == teacher.DepartmentId)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var marks = await _context.StudentMarks
                .Where(sm => sm.SubjectId == subjectId)
                .ToListAsync();

            var csvBuilder = new System.Text.StringBuilder();
            csvBuilder.AppendLine("StudentId,Email,StudentName,Attendance_Max10,ClassTest_Max20,MidTerm_Max30,FinalExam_Max40");

            foreach (var student in students)
            {
                var mark = marks.FirstOrDefault(m => m.StudentId == student.Id);
                double att = mark?.Attendance ?? 0;
                double ct = mark?.ClassTest ?? 0;
                double mid = mark?.MidTerm ?? 0;
                double fin = mark?.FinalExam ?? 0;

                string name = student.Name.Contains(",") ? $"\"{student.Name}\"" : student.Name;
                csvBuilder.AppendLine($"{student.Id},{student.Email},{name},{att},{ct},{mid},{fin}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", $"MarksTemplate_{subject.SubjectCode}.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMarksExcel(int subjectId, IFormFile file)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                if (isAjax) return Json(new { success = false, message = "Teacher profile not found." });
                return NotFound("Teacher profile not found.");
            }

            // Verify the subject is assigned to this teacher
            var isAssigned = await _context.CourseAssignments.AnyAsync(ca => ca.SubjectId == subjectId && ca.TeacherId == teacher.Id);
            if (!isAssigned)
            {
                if (isAjax) return Json(new { success = false, message = "Subject not found or not assigned to you." });
                return NotFound("Subject not found or not assigned to you.");
            }

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId && s.DepartmentId == teacher.DepartmentId);
            if (subject == null)
            {
                if (isAjax) return Json(new { success = false, message = "Subject not found." });
                return NotFound("Subject not found.");
            }

            if (file == null || file.Length == 0)
            {
                if (isAjax) return Json(new { success = false, message = "Please select a valid file to upload." });
                TempData["ErrorMessage"] = "Please select a valid file to upload.";
                return RedirectToAction(nameof(StudentMarks), new { subjectId });
            }

            string extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".csv" && extension != ".xlsx" && extension != ".xls")
            {
                if (isAjax) return Json(new { success = false, message = "Only CSV and Excel files (.xlsx, .xls) are supported." });
                TempData["ErrorMessage"] = "Only CSV and Excel files (.xlsx, .xls) are supported.";
                return RedirectToAction(nameof(StudentMarks), new { subjectId });
            }

            int successCount = 0;
            int errorCount = 0;
            var errorList = new List<string>();

            try
            {
                if (extension == ".csv")
                {
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        string? headerLine = await reader.ReadLineAsync(); // Skip header
                        while (!reader.EndOfStream)
                        {
                            string? line = await reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var parts = ParseCsvLine(line);
                            if (parts.Count < 4) continue;

                            int studentId = 0;
                            int.TryParse(parts[0], out studentId);
                            string email = parts.Count > 1 ? parts[1] : "";

                            double attendance = parts.Count > 3 ? ParseDouble(parts[3]) : 0;
                            double classTest = parts.Count > 4 ? ParseDouble(parts[4]) : 0;
                            double midTerm = parts.Count > 5 ? ParseDouble(parts[5]) : 0;
                            double finalExam = parts.Count > 6 ? ParseDouble(parts[6]) : 0;

                            var result = await ProcessStudentMarkAsync(studentId, email, subjectId, teacher.Id, teacher.DepartmentId, attendance, classTest, midTerm, finalExam);
                            if (result.Success) successCount++;
                            else
                            {
                                errorCount++;
                                errorList.Add(result.Error);
                            }
                        }
                    }
                }
                else
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    using (var stream = file.OpenReadStream())
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            var resultDs = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                            });
                            var table = resultDs.Tables[0];
                            foreach (System.Data.DataRow row in table.Rows)
                            {
                                if (row.ItemArray.Length < 4) continue;

                                string sIdStr = row[0]?.ToString() ?? "";
                                int.TryParse(sIdStr, out int studentId);
                                string email = row[1]?.ToString() ?? "";

                                double attendance = ParseDouble(row[3]?.ToString() ?? "0");
                                double classTest = ParseDouble(row[4]?.ToString() ?? "0");
                                double midTerm = ParseDouble(row[5]?.ToString() ?? "0");
                                double finalExam = ParseDouble(row[6]?.ToString() ?? "0");

                                var result = await ProcessStudentMarkAsync(studentId, email, subjectId, teacher.Id, teacher.DepartmentId, attendance, classTest, midTerm, finalExam);
                                if (result.Success) successCount++;
                                else
                                {
                                    errorCount++;
                                    errorList.Add(result.Error);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (isAjax) return Json(new { success = false, message = $"Error parsing file: {ex.Message}" });
                TempData["ErrorMessage"] = $"Error parsing file: {ex.Message}";
                return RedirectToAction(nameof(StudentMarks), new { subjectId });
            }

            string resultMsg;
            if (errorCount > 0)
            {
                resultMsg = $"Upload complete: {successCount} succeeded, {errorCount} failed. Errors: {string.Join("; ", errorList.Take(5))}";
                if (isAjax) return Json(new { success = successCount > 0, message = resultMsg });
                TempData["ErrorMessage"] = resultMsg;
            }
            else
            {
                resultMsg = $"All {successCount} student marks successfully uploaded and published!";
                if (isAjax) return Json(new { success = true, message = resultMsg });
                TempData["SuccessMessage"] = resultMsg;
            }

            return RedirectToAction(nameof(StudentMarks), new { subjectId });
        }

        private List<string> ParseCsvLine(string line)
        {
            line = line.Trim('\uFEFF', '\r', '\n');
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(line)) return result;

            char delimiter = line.Contains(';') && !line.Contains(',') ? ';' : ',';
            bool inQuotes = false;
            var currentToken = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(currentToken.ToString().Trim(' ', '"', '\t'));
                    currentToken.Clear();
                }
                else
                {
                    currentToken.Append(c);
                }
            }
            result.Add(currentToken.ToString().Trim(' ', '"', '\t'));
            return result;
        }

        private double ParseDouble(string val)
        {
            val = val.Trim(' ', '"', '\t');
            if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double res))
                return res;
            if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out double res2))
                return res2;
            return 0;
        }

        private async Task<(bool Success, string Error)> ProcessStudentMarkAsync(int studentId, string email, int subjectId, int teacherId, int teacherDeptId, double attendance, double classTest, double midTerm, double finalExam)
        {
            string cleanEmail = email.Trim(' ', '"', '\t').ToLower();

            StudentModel? student = null;
            if (studentId > 0)
            {
                student = await _context.Students.FindAsync(studentId);
            }

            if (student == null && !string.IsNullOrEmpty(cleanEmail))
            {
                student = await _context.Students.FirstOrDefaultAsync(s => s.Email.ToLower() == cleanEmail);
            }

            if (student == null)
            {
                return (false, $"Student record with ID {studentId} or Email '{email}' not found.");
            }

            if (student.DepartmentId != teacherDeptId)
            {
                return (false, $"Student '{student.Name}' does not belong to your department.");
            }

            if (attendance < 0 || attendance > 10) return (false, $"Attendance marks for {student.Name} must be between 0 and 10.");
            if (classTest < 0 || classTest > 20) return (false, $"Class Test marks for {student.Name} must be between 0 and 20.");
            if (midTerm < 0 || midTerm > 30) return (false, $"Mid-term marks for {student.Name} must be between 0 and 30.");
            if (finalExam < 0 || finalExam > 40) return (false, $"Final exam marks for {student.Name} must be between 0 and 40.");

            var total = attendance + classTest + midTerm + finalExam;
            var gradeInfo = CalculateGrade(total, attendance);

            var mark = await _context.StudentMarks.FirstOrDefaultAsync(m => m.StudentId == student.Id && m.SubjectId == subjectId);
            if (mark == null)
            {
                mark = new StudentMark
                {
                    StudentId = student.Id,
                    SubjectId = subjectId,
                };
                _context.StudentMarks.Add(mark);
            }

            mark.TeacherId = teacherId;
            mark.Attendance = attendance;
            mark.ClassTest = classTest;
            mark.MidTerm = midTerm;
            mark.FinalExam = finalExam;
            mark.Total = total;
            mark.LetterGrade = gradeInfo.LetterGrade;
            mark.GradePoint = gradeInfo.GradePoint;
            mark.Remarks = gradeInfo.Remarks;
            mark.AttendanceStatus = gradeInfo.AttendanceStatus;
            mark.IsPublished = true; // Automatically publish results
            mark.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, "");
        }


        private (string LetterGrade, double GradePoint, string Remarks, string AttendanceStatus) CalculateGrade(double total, double attendance)
        {
            string attendanceStatus;
            double attendancePercentage = attendance * 10.0;
            if (attendancePercentage >= 70.0)
            {
                attendanceStatus = "Collegiate";
            }
            else if (attendancePercentage >= 60.0)
            {
                attendanceStatus = "Non-Collegiate";
            }
            else
            {
                attendanceStatus = "Dis-collegiate";
            }

            if (attendanceStatus == "Dis-collegiate")
            {
                return ("F", 0.00, "Fail (Dis-collegiate)", attendanceStatus);
            }

            if (total >= 80) return ("A+", 4.00, "Excellent", attendanceStatus);
            if (total >= 75) return ("A",  3.75, "Very Good", attendanceStatus);
            if (total >= 70) return ("A-", 3.50, "Very Good", attendanceStatus);
            if (total >= 65) return ("B+", 3.25, "Good", attendanceStatus);
            if (total >= 60) return ("B",  3.00, "Good", attendanceStatus);
            if (total >= 55) return ("B-", 2.75, "Satisfactory", attendanceStatus);
            if (total >= 50) return ("C+", 2.50, "Satisfactory", attendanceStatus);
            if (total >= 45) return ("C",  2.25, "Pass", attendanceStatus);
            if (total >= 40) return ("D",  2.00, "Pass", attendanceStatus);
            return ("F", 0.00, "Fail", attendanceStatus);
        }

        // ================= PAYMENT HISTORY (TEACHER VIEW) =================
        public async Task<IActionResult> PaymentHistory()
        {
            var teacher = await GetCurrentTeacherAsync();
            if (teacher == null)
            {
                return Content("❌ Teacher profile not found. Please contact the administrator.");
            }

            var payments = await _context.TeacherPayments
                .Where(p => p.TeacherID == teacher.Id)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            return View(payments);
        }
    }
}
