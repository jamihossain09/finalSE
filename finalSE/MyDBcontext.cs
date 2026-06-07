using Microsoft.EntityFrameworkCore;
using finalSE.Models;

public class MyDBContext : DbContext
{

    public MyDBContext(DbContextOptions<MyDBContext> options)
        : base(options)
    {
    }

    public DbSet<StudentModel> Students { get; set; }

    public DbSet<DepartmentModel> Departments { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<Teacher> Teachers { get; set; }

    public DbSet<Routine> Routines { get; set; }
    
    public DbSet<Invitation> Invitations { get; set; }

    public DbSet<ClassRecord> ClassRecords { get; set; }

    public DbSet<Tutorial> Tutorials { get; set; }

    public DbSet<AssignmentTask> AssignmentTasks { get; set; }

    public DbSet<Notice> Notices { get; set; }
    // 🔥 THIS PART YOU NEED TO ADD
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId);
    }
}