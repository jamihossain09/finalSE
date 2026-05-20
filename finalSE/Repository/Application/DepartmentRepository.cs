using finalSE.Models;
using finalSE.Repository.Interface;

namespace finalSE.Repository.Application
{
    public class DepartmentRepository : BaseRepository<DepartmentModel>, IDepartmentRepository
    {
        public DepartmentRepository(MyDBContext context) : base(context)
        {
        }
    }
}


