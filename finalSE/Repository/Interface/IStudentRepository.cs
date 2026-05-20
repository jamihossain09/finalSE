using finalSE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace finalSE.Repository.Interface
{
    public interface IStudentRepository
    {
        Task<IEnumerable<StudentModel>> GetAllAsync();
        Task<StudentModel?> GetByIdAsync(int id);
        Task AddAsync(StudentModel student);
        void Update(StudentModel student);
        void Delete(StudentModel student);
        Task<(IEnumerable<StudentModel> Students, int TotalCount)> GetPagedAsync(int page, int pageSize);
    }
}
