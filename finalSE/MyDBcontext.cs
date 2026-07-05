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

    public DbSet<Subject> Subjects { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<Teacher> Teachers { get; set; }

    public DbSet<Routine> Routines { get; set; }
    
    public DbSet<Invitation> Invitations { get; set; }

    public DbSet<ClassRecord> ClassRecords { get; set; }

    public DbSet<Tutorial> Tutorials { get; set; }

    public DbSet<AssignmentTask> AssignmentTasks { get; set; }

    public DbSet<Notice> Notices { get; set; }

    public DbSet<StudentMark> StudentMarks { get; set; }

    public DbSet<CourseAssignment> CourseAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId);

        modelBuilder.Entity<StudentMark>()
            .HasOne(sm => sm.Student)
            .WithMany()
            .HasForeignKey(sm => sm.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentMark>()
            .HasOne(sm => sm.Teacher)
            .WithMany()
            .HasForeignKey(sm => sm.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StudentMark>()
            .HasOne(sm => sm.Subject)
            .WithMany()
            .HasForeignKey(sm => sm.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Each student can only have one mark record per subject (from any teacher)
        modelBuilder.Entity<StudentMark>()
            .HasIndex(sm => new { sm.StudentId, sm.SubjectId })
            .IsUnique();

        modelBuilder.Entity<CourseAssignment>()
            .HasOne(ca => ca.Subject)
            .WithMany()
            .HasForeignKey(ca => ca.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CourseAssignment>()
            .HasOne(ca => ca.Teacher)
            .WithMany()
            .HasForeignKey(ca => ca.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}