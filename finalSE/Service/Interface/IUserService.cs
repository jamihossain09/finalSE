using System.Collections.Generic;
using System.Threading.Tasks;
using finalSE.Models;

namespace finalSE.Service.Interface
{
    public interface IUserService
    {
        List<User> GetAll();
        User GetById(int id);

        Task<bool> RegisterAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);

        Task<User> AuthenticateAsync(string username, string password);

        // ❌ REMOVE THIS (was wrong)
        // Task GetAllAsync();
    }
}