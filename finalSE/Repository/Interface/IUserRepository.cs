using System.Collections.Generic;
using finalSE.Models;

public interface IUserRepository
{
    List<User> GetAll();
    User GetById(int id);
    User GetByUserName(string userName);
    void Add(User user);
    void Update(User user);
    void Delete(int id);
}