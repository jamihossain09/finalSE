using finalSE.Service.Interface;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using finalSE.Repository.Application;
using finalSE.Repository.Interface;
using finalSE.Service.Application;
using finalSE.UnitOfWork.Interface;
using finalSE.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<MyDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Repositories
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
//Teacher
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<ITeacherService, TeacherService>();
var app = builder.Build();

// Automatically migrate and seed the database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MyDBContext>();
        try
        {
            context.Database.ExecuteSqlRaw(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
                BEGIN
                    IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20260624085913_AddDepartmentToRoutineAndNotice')
                    BEGIN
                        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
                        VALUES ('20260624085913_AddDepartmentToRoutineAndNotice', '8.0.0');
                    END
                    IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20260701014243_AddDepartmentToInvitation')
                    BEGIN
                        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
                        VALUES ('20260701014243_AddDepartmentToInvitation', '8.0.0');
                    END
                END
            ");
        }
        catch { }
        context.Database.Migrate();

        // Clean up orphan StudentMark rows from before subject system was added (SubjectId = 0)
        var orphanMarks = context.StudentMarks.Where(m => m.SubjectId == 0).ToList();
        if (orphanMarks.Any())
        {
            context.StudentMarks.RemoveRange(orphanMarks);
            context.SaveChanges();
        }

        // Seed Roles
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { RoleName = "Admin", RoleDescription = "Administrator Role" },
                new Role { RoleName = "User", RoleDescription = "Standard User Role" },
                new Role { RoleName = "Student", RoleDescription = "Student Role" },
                new Role { RoleName = "Teacher", RoleDescription = "Teacher Role" }
            );
            context.SaveChanges();
        }

        // Seed Departments & Subjects (Check if Mathematics exists and correct Physics subject is present, if not, reset and seed everything cleanly)
        if (!context.Departments.Any(d => d.DepartmentName == "Mathematics") || !context.Subjects.Any(s => s.SubjectName == "Waves, Oscillations and Advanced Mechanics"))
        {
            // Clear existing academic data first to avoid FK conflicts and start fresh
            context.StudentMarks.ExecuteDelete();
            context.ClassRecords.ExecuteDelete();
            context.Tutorials.ExecuteDelete();
            context.AssignmentTasks.ExecuteDelete();
            context.Routines.ExecuteDelete();
            context.Notices.ExecuteDelete();
            context.Invitations.ExecuteDelete();

            // Clear students and teachers profiles
            context.Students.ExecuteDelete();
            context.Teachers.ExecuteDelete();

            // Clear old subjects and departments
            context.Subjects.ExecuteDelete();
            context.Departments.ExecuteDelete();

            // Clear users except admin
            var usersToClear = context.Users.Where(u => u.UserName != "admin").ToList();
            context.Users.RemoveRange(usersToClear);
            context.SaveChanges();

            // Seed Departments
            var physics = new DepartmentModel { DepartmentName = "Physics" };
            var eee = new DepartmentModel { DepartmentName = "EEE" };
            var bba = new DepartmentModel { DepartmentName = "BBA" };
            var cse = new DepartmentModel { DepartmentName = "CSE" };
            var math = new DepartmentModel { DepartmentName = "Mathematics" };

            context.Departments.AddRange(physics, eee, bba, cse, math);
            context.SaveChanges();

            // Seed Subjects
            context.Subjects.AddRange(
                // Physics
                new Subject { SubjectName = "Mathematical Physics", SubjectCode = "PHY101", DepartmentId = physics.Id },
                new Subject { SubjectName = "Waves, Oscillations and Advanced Mechanics", SubjectCode = "PHY102", DepartmentId = physics.Id },
                new Subject { SubjectName = "Practical Laboratory", SubjectCode = "PHY103", DepartmentId = physics.Id },
                new Subject { SubjectName = "Optics", SubjectCode = "PHY104", DepartmentId = physics.Id },
                new Subject { SubjectName = "Electricity and Magnetism", SubjectCode = "PHY105", DepartmentId = physics.Id },

                // EEE
                new Subject { SubjectName = "Circuit Theory", SubjectCode = "EEE101", DepartmentId = eee.Id },
                new Subject { SubjectName = "Analog Electronics", SubjectCode = "EEE102", DepartmentId = eee.Id },
                new Subject { SubjectName = "Digital Signal Processing", SubjectCode = "EEE103", DepartmentId = eee.Id },
                new Subject { SubjectName = "Electrical Machines", SubjectCode = "EEE104", DepartmentId = eee.Id },
                new Subject { SubjectName = "Microprocessor", SubjectCode = "EEE105", DepartmentId = eee.Id },

                // BBA
                new Subject { SubjectName = "Principles of Management", SubjectCode = "BBA101", DepartmentId = bba.Id },
                new Subject { SubjectName = "Financial Accounting", SubjectCode = "BBA102", DepartmentId = bba.Id },
                new Subject { SubjectName = "Marketing Management", SubjectCode = "BBA103", DepartmentId = bba.Id },
                new Subject { SubjectName = "Human Resource Management", SubjectCode = "BBA104", DepartmentId = bba.Id },
                new Subject { SubjectName = "Corporate Finance", SubjectCode = "BBA105", DepartmentId = bba.Id },

                // CSE
                new Subject { SubjectName = "Introduction to Computer Science", SubjectCode = "CSE101", DepartmentId = cse.Id },
                new Subject { SubjectName = "Structured Programming Language", SubjectCode = "CSE102", DepartmentId = cse.Id },
                new Subject { SubjectName = "Data Structures", SubjectCode = "CSE103", DepartmentId = cse.Id },
                new Subject { SubjectName = "Algorithms", SubjectCode = "CSE104", DepartmentId = cse.Id },
                new Subject { SubjectName = "Database Management Systems", SubjectCode = "CSE105", DepartmentId = cse.Id },
                new Subject { SubjectName = "Operating Systems", SubjectCode = "CSE106", DepartmentId = cse.Id },

                // Mathematics
                new Subject { SubjectName = "Calculus and Analytical Geometry", SubjectCode = "MAT101", DepartmentId = math.Id },
                new Subject { SubjectName = "Differential Equations", SubjectCode = "MAT102", DepartmentId = math.Id },
                new Subject { SubjectName = "Linear Algebra", SubjectCode = "MAT103", DepartmentId = math.Id },
                new Subject { SubjectName = "Complex Variables", SubjectCode = "MAT104", DepartmentId = math.Id },
                new Subject { SubjectName = "Fourier Analysis", SubjectCode = "MAT105", DepartmentId = math.Id },
                new Subject { SubjectName = "Probability and Statistics", SubjectCode = "MAT106", DepartmentId = math.Id }
            );
            context.SaveChanges();

            // Seed Teacher & Student Users & Profiles
            var teacherRole = context.Roles.First(r => r.RoleName == "Teacher");
            var studentRole = context.Roles.First(r => r.RoleName == "Student");

            var depts = new List<(DepartmentModel Dept, string Prefix)>
            {
                (cse, "cse"), (eee, "eee"), (physics, "phy"), (math, "math"), (bba, "bba")
            };

            foreach (var (dept, prefix) in depts)
            {
                var teacherUser = new User
                {
                    UserName = $"{prefix}_teacher",
                    Email = $"{prefix}_teacher@system.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher123"),
                    Address = $"{dept.DepartmentName} Dept Office",
                    RoleId = teacherRole.Id,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(teacherUser);
                context.SaveChanges();

                var teacherProfile = new Teacher
                {
                    Name = $"Dr. {dept.DepartmentName} Teacher",
                    Email = $"{prefix}_teacher@system.com",
                    Phone = "01700000000",
                    Address = $"{dept.DepartmentName} Dept Office",
                    DepartmentId = dept.Id
                };
                context.Teachers.Add(teacherProfile);
                context.SaveChanges();

                // Seed Students
                var studentUser = new User
                {
                    UserName = $"{prefix}_student",
                    Email = $"{prefix}_student@system.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("student123"),
                    Address = $"{dept.DepartmentName} Student Dorm",
                    RoleId = studentRole.Id,
                    CreatedAt = DateTime.Now
                };
                context.Users.Add(studentUser);
                context.SaveChanges();

                var studentProfile = new StudentModel
                {
                    Name = $"Student {dept.DepartmentName}",
                    Email = $"{prefix}_student@system.com",
                    Age = 21,
                    Address = $"{dept.DepartmentName} Student Dorm",
                    DepartmentId = dept.Id
                };
                context.Students.Add(studentProfile);
                context.SaveChanges();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
