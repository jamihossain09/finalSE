using Microsoft.EntityFrameworkCore;
using finalSE.Models;
using finalSE.Repository.Interface;

public class TeacherRepository : ITeacherRepository
{
    private readonly MyDBContext _context;

    public TeacherRepository(MyDBContext context)
    {
        _context = context;
    }

    // GET ALL
    public async Task<List<Teacher>> GetAllAsync()
    {
        return await _context.Teachers
            .Include(t => t.Department)
            .ToListAsync();
    }

    // GET BY ID
    public async Task<Teacher?> GetByIdAsync(int id)
    {
        return await _context.Teachers
            .Include(t => t.Department)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // ADD
    public async Task AddAsync(Teacher teacher)
    {
        await _context.Teachers.AddAsync(teacher);
    }

    // UPDATE
    public void Update(Teacher teacher)
    {
        _context.Teachers.Update(teacher);
    }

    // DELETE
    public void Delete(Teacher teacher)
    {
        _context.Teachers.Remove(teacher);
    }

    // PAGINATION
    public async Task<(IEnumerable<Teacher> Teachers, int TotalCount)>
    GetPagedAsync(int page, int pageSize)
    {
        var totalCount =
            await _context.Teachers.CountAsync();

        var teachers = await _context.Teachers
            .Include(t => t.Department)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (teachers, totalCount);
    }
}