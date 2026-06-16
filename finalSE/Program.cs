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
