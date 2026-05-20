using finalSE.Repository.Interface;

namespace finalSE.UnitOfWork.Interface
{
    public interface IUnitOfWork : IDisposable
    {
        IDepartmentRepository Department { get; }
        IStudentRepository Student { get; }
        IRoleRepository Role { get; }
        IUserRepository User { get; }

        // 🔥 ADD THIS
        ITeacherRepository Teacher { get; }

        Task<int> SaveChangesAsync();
    }
}