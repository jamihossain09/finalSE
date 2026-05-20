using finalSE.Repository.Interface.finalSE.Reporsitory.Application;
using Microsoft.EntityFrameworkCore;


namespace finalSE.Repository.Application
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly MyDBContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(MyDBContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync() =>

             await _dbSet.ToListAsync();


        public async Task<T> GetByIdAsync(int id) =>

             await _dbSet.FindAsync(id);


        public async Task AddAsync(T entity) =>

            await _dbSet.AddAsync(entity);


        public void Update(T entity) =>

            _dbSet.Update(entity);


        public void Delete(T entity) =>

            _dbSet.Remove(entity);


    }
}

