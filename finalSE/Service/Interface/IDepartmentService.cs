using System.Collections.Generic;
using System.Threading.Tasks;
using finalSE.Models;

namespace finalSE.Service.Interface
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentModel>> GetAllAsync();
        Task<DepartmentModel?> GetByIdAsync(int id);
        Task CreateAsync(DepartmentModel department);
        Task UpdateAsync(int id, DepartmentModel department);
        Task<DepartmentModel?> DeleteAsync(int id);
    }
}