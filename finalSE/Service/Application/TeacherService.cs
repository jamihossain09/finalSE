using finalSE.Models;
using finalSE.UnitOfWork.Interface;

public class TeacherService : ITeacherService
{
    private readonly IUnitOfWork _uow;

    public TeacherService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<Teacher>> GetAllAsync()
    {
        return await _uow.Teacher.GetAllAsync();
    }

    public async Task<Teacher?> GetByIdAsync(int id)
    {
        return await _uow.Teacher.GetByIdAsync(id);
    }

    public async Task<Teacher> CreateAsync(Teacher teacher)
    {
        await _uow.Teacher.AddAsync(teacher);

        await _uow.SaveChangesAsync();

        return teacher;
    }

    public async Task<bool> UpdateAsync(int id, Teacher teacher)
    {
        var existing =
            await _uow.Teacher.GetByIdAsync(id);

        if (existing == null)
            return false;

        existing.Name = teacher.Name;
        existing.Email = teacher.Email;
        existing.Phone = teacher.Phone;
        existing.Address = teacher.Address;
        existing.DepartmentId = teacher.DepartmentId;

        _uow.Teacher.Update(existing);

        await _uow.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var teacher =
            await _uow.Teacher.GetByIdAsync(id);

        if (teacher == null)
            return false;

        _uow.Teacher.Delete(teacher);

        await _uow.SaveChangesAsync();

        return true;
    }

    // PAGINATION
    public async Task<(IEnumerable<Teacher> Teachers,
                       int TotalCount,
                       int TotalPages)>
    GetPagedAsync(int page, int pageSize)
    {
        var result =
            await _uow.Teacher.GetPagedAsync(
                page,
                pageSize
            );

        int totalPages =
            (int)Math.Ceiling(
                (double)result.TotalCount / pageSize
            );

        return (
            result.Teachers,
            result.TotalCount,
            totalPages
        );
    }
}