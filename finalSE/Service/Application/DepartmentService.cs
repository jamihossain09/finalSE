using finalSE.Models;
using finalSE.Service.Interface;
using finalSE.UnitOfWork.Interface;
using finalSE.Models;
using finalSE.Service.Interface;
using finalSE.UnitOfWork.Interface;

namespace finalSE.Service.Application
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IUnitOfWork _uow;

        public DepartmentService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<DepartmentModel>> GetAllAsync()
        {
            return await _uow.Department.GetAllAsync();
        }

        public async Task<DepartmentModel?> GetByIdAsync(int id)
        {
            return await _uow.Department.GetByIdAsync(id);
        }

        public async Task CreateAsync(DepartmentModel department)
        {
            await _uow.Department.AddAsync(department);
            await _uow.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, DepartmentModel department)
        {
            var existing = await _uow.Department.GetByIdAsync(id);

            if (existing == null)
                return;

            existing.DepartmentName = department.DepartmentName;

            _uow.Department.Update(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task<DepartmentModel?> DeleteAsync(int id)
        {
            var department = await _uow.Department.GetByIdAsync(id);

            if (department == null)
                return null;

            _uow.Department.Delete(department);
            await _uow.SaveChangesAsync();

            return department;
        }
    }
}