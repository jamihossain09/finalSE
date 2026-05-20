using finalSE.Models;

public interface ITeacherRepository
{
    Task<List<Teacher>> GetAllAsync();

    Task<Teacher?> GetByIdAsync(int id);

    Task AddAsync(Teacher teacher);

    void Update(Teacher teacher);

    void Delete(Teacher teacher);

    // PAGINATION
    Task<(IEnumerable<Teacher> Teachers, int TotalCount)>
    GetPagedAsync(int page, int pageSize);
}