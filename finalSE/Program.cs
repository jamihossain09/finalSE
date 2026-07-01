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

        // Seed Departments
        if (!context.Departments.Any())
        {
            context.Departments.AddRange(
                new DepartmentModel { DepartmentName = "Computer Science" },
                new DepartmentModel { DepartmentName = "Electrical Engineering" },
                new DepartmentModel { DepartmentName = "Physics" },
                new DepartmentModel { DepartmentName = "Mathematics" }
            );
            context.SaveChanges();
        }

        // Seed Subjects (minimum 6 per department)
        if (!context.Subjects.Any())
        {
            var csDept  = context.Departments.First(d => d.DepartmentName == "Computer Science");
            var eeDept  = context.Departments.First(d => d.DepartmentName == "Electrical Engineering");
            var phyDept = context.Departments.First(d => d.DepartmentName == "Physics");
            var mathDept= context.Departments.First(d => d.DepartmentName == "Mathematics");

            context.Subjects.AddRange(
                // Computer Science
                new Subject { SubjectName = "Data Structures & Algorithms", SubjectCode = "CSE101", DepartmentId = csDept.Id },
                new Subject { SubjectName = "Object Oriented Programming",  SubjectCode = "CSE102", DepartmentId = csDept.Id },
                new Subject { SubjectName = "Database Management Systems",  SubjectCode = "CSE201", DepartmentId = csDept.Id },
                new Subject { SubjectName = "Operating Systems",            SubjectCode = "CSE202", DepartmentId = csDept.Id },
                new Subject { SubjectName = "Computer Networks",            SubjectCode = "CSE301", DepartmentId = csDept.Id },
                new Subject { SubjectName = "Software Engineering",         SubjectCode = "CSE302", DepartmentId = csDept.Id },
                new Subject { SubjectName = "Artificial Intelligence",      SubjectCode = "CSE401", DepartmentId = csDept.Id },

                // Electrical Engineering
                new Subject { SubjectName = "Circuit Analysis",             SubjectCode = "EEE101", DepartmentId = eeDept.Id },
                new Subject { SubjectName = "Digital Electronics",          SubjectCode = "EEE102", DepartmentId = eeDept.Id },
                new Subject { SubjectName = "Signals & Systems",            SubjectCode = "EEE201", DepartmentId = eeDept.Id },
                new Subject { SubjectName = "Electromagnetic Theory",       SubjectCode = "EEE202", DepartmentId = eeDept.Id },
                new Subject { SubjectName = "Power Systems",                SubjectCode = "EEE301", DepartmentId = eeDept.Id },
                new Subject { SubjectName = "Control Systems",              SubjectCode = "EEE302", DepartmentId = eeDept.Id },
                new Subject { SubjectName = "Microprocessors",              SubjectCode = "EEE401", DepartmentId = eeDept.Id },

                // Physics
                new Subject { SubjectName = "Classical Mechanics",          SubjectCode = "PHY101", DepartmentId = phyDept.Id },
                new Subject { SubjectName = "Thermodynamics",               SubjectCode = "PHY102", DepartmentId = phyDept.Id },
                new Subject { SubjectName = "Electromagnetism",             SubjectCode = "PHY201", DepartmentId = phyDept.Id },
                new Subject { SubjectName = "Quantum Mechanics",            SubjectCode = "PHY202", DepartmentId = phyDept.Id },
                new Subject { SubjectName = "Optics",                       SubjectCode = "PHY301", DepartmentId = phyDept.Id },
                new Subject { SubjectName = "Nuclear Physics",              SubjectCode = "PHY302", DepartmentId = phyDept.Id },

                // Mathematics
                new Subject { SubjectName = "Calculus",                     SubjectCode = "MAT101", DepartmentId = mathDept.Id },
                new Subject { SubjectName = "Linear Algebra",               SubjectCode = "MAT102", DepartmentId = mathDept.Id },
                new Subject { SubjectName = "Differential Equations",       SubjectCode = "MAT201", DepartmentId = mathDept.Id },
                new Subject { SubjectName = "Discrete Mathematics",         SubjectCode = "MAT202", DepartmentId = mathDept.Id },
                new Subject { SubjectName = "Numerical Methods",            SubjectCode = "MAT301", DepartmentId = mathDept.Id },
                new Subject { SubjectName = "Probability & Statistics",     SubjectCode = "MAT302", DepartmentId = mathDept.Id }
            );
            context.SaveChanges();
        }

        // Seed Admin User
        if (!context.Users.Any(u => u.UserName == "admin"))
        {
            var adminRole = context.Roles.First(r => r.RoleName == "Admin");
            context.Users.Add(new User
            {
                UserName = "admin",
                Email = "admin@system.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Address = "System Admin",
                RoleId = adminRole.Id,
                CreatedAt = DateTime.Now
            });
            context.SaveChanges();
        }

        // Seed Teacher User & Profile
        if (!context.Users.Any(u => u.UserName == "teacher"))
        {
            var csDept = context.Departments.First(d => d.DepartmentName == "Computer Science");
            var teacherRole = context.Roles.First(r => r.RoleName == "Teacher");

            var teacherUser = new User
            {
                UserName = "teacher",
                Email = "teacher@system.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher123"),
                Address = "CS Department Office 101",
                RoleId = teacherRole.Id,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(teacherUser);
            context.SaveChanges();

            var teacherProfile = new Teacher
            {
                Name = "Dr. John Doe",
                Email = "teacher@system.com",
                Phone = "01711122233",
                Address = "CS Department Office 101",
                DepartmentId = csDept.Id
            };
            context.Teachers.Add(teacherProfile);
            context.SaveChanges();
        }

        // Seed Student Users & Profiles
        if (!context.Users.Any(u => u.UserName == "student1"))
        {
            var csDept = context.Departments.First(d => d.DepartmentName == "Computer Science");
            var studentRole = context.Roles.First(r => r.RoleName == "Student");

            var studentUser1 = new User
            {
                UserName = "student1",
                Email = "student1@system.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student123"),
                Address = "Dormitory A, Room 10",
                RoleId = studentRole.Id,
                CreatedAt = DateTime.Now
            };
            var studentUser2 = new User
            {
                UserName = "student2",
                Email = "student2@system.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student123"),
                Address = "Dormitory A, Room 11",
                RoleId = studentRole.Id,
                CreatedAt = DateTime.Now
            };
            var studentUser3 = new User
            {
                UserName = "student3",
                Email = "student3@system.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("student123"),
                Address = "Dormitory B, Room 22",
                RoleId = studentRole.Id,
                CreatedAt = DateTime.Now
            };
            context.Users.AddRange(studentUser1, studentUser2, studentUser3);
            context.SaveChanges();

            var studentProfile1 = new StudentModel
            {
                Name = "Alice Smith",
                Email = "student1@system.com",
                Age = 20,
                Address = "Dormitory A, Room 10",
                DepartmentId = csDept.Id
            };
            var studentProfile2 = new StudentModel
            {
                Name = "Bob Johnson",
                Email = "student2@system.com",
                Age = 21,
                Address = "Dormitory A, Room 11",
                DepartmentId = csDept.Id
            };
            var studentProfile3 = new StudentModel
            {
                Name = "Charlie Brown",
                Email = "student3@system.com",
                Age = 22,
                Address = "Dormitory B, Room 22",
                DepartmentId = csDept.Id
            };
            context.Students.AddRange(studentProfile1, studentProfile2, studentProfile3);
            context.SaveChanges();
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
