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

        public async Task<DepartmentModel> CreateAsync(DepartmentModel department)
        {
            await _uow.Department.AddAsync(department);
            await _uow.SaveChangesAsync();
            return department;
        }

        public async Task<bool> UpdateAsync(int id, DepartmentModel department)
        {
            var existing = await _uow.Department.GetByIdAsync(id);

            if (existing == null)
                return false;

            existing.DepartmentName = department.DepartmentName;

            _uow.Department.Update(existing);
            await _uow.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var department = await _uow.Department.GetByIdAsync(id);

            if (department == null)
                return false;

            _uow.Department.Delete(department);
            await _uow.SaveChangesAsync();

            return true;
        }

        Task IDepartmentService.CreateAsync(DepartmentModel department)
        {
            return CreateAsync(department);
        }

        Task IDepartmentService.UpdateAsync(int id, DepartmentModel department)
        {
            return UpdateAsync(id, department);
        }

        Task<DepartmentModel?> IDepartmentService.DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}