using Microsoft.EntityFrameworkCore;
using finalSE.Models;
using finalSE.Repository.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace finalSE.Repository.Application
{
    public class StudentRepository : IStudentRepository
    {
        private readonly MyDBContext _context;

        public StudentRepository(MyDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StudentModel>> GetAllAsync()
        {
            return await _context.Students.Include(s => s.Department).ToListAsync();
        }

        public async Task<StudentModel?> GetByIdAsync(int id)
        {
            return await _context.Students.Include(s => s.Department).FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(StudentModel student)
        {
            await _context.Students.AddAsync(student);
        }

        public void Update(StudentModel student)
        {
            _context.Students.Update(student);
        }

        public void Delete(StudentModel student)
        {
            _context.Students.Remove(student);
        }

        public async Task<(IEnumerable<StudentModel> Students, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            var query = _context.Students.Include(s => s.Department).AsNoTracking();
            var totalCount = await query.CountAsync();
            var students = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (students, totalCount);
        }

        public async Task<StudentModel?> GetByEmailAsync(string email)
        {
            return await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Email == email);
        }
    }
}
