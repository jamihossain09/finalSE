using finalSE.Repository.Interface;
using finalSE.UnitOfWork.Interface;
using finalSE.Repository.Interface;
using finalSE.UnitOfWork.Interface;

public class UnitOfWork : IUnitOfWork
{
    private readonly MyDBContext _context;

    public UnitOfWork(
        MyDBContext context,
        IDepartmentRepository departmentRepository,
        IStudentRepository studentRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        ITeacherRepository teacherRepository
    )
    {
        _context = context;

        Department = departmentRepository;
        Student = studentRepository;
        Role = roleRepository;
        User = userRepository;
        Teacher = teacherRepository; // ✅ IMPORTANT
    }

    public IDepartmentRepository Department { get; }
    public IStudentRepository Student { get; }
    public IRoleRepository Role { get; }
    public IUserRepository User { get; }
    public ITeacherRepository Teacher { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}