using finalSE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace finalSE.Service.Interface
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentModel>> GetAllAsync();
        Task<StudentModel?> GetByIdAsync(int id);
        Task<StudentModel> CreateAsync(StudentModel student);
        Task<bool> UpdateAsync(int id, StudentModel student);
        Task<bool> DeleteAsync(int id);
        Task<(IEnumerable<StudentModel> Students, int TotalCount, int TotalPages)> GetPagedAsync(int page, int pageSize);
    }
}
