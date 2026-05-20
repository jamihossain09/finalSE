using System.Collections.Generic;
using finalSE.Models;
public interface IRoleRepository
{
    List<Role> GetAll();
    Role GetById(int id);
    void Add(Role role);
    void Update(Role role);
    void Delete(int id);
}
