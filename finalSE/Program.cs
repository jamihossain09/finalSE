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
        context.Database.Migrate();

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
