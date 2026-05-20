using finalSE.UnitOfWork.Interface;
using finalSE.Models;
public class RoleService : IRoleService
{
    private readonly IUnitOfWork _uow;

    public RoleService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public List<Role> GetAll() => _uow.Role.GetAll();

    public Role GetById(int id) => _uow.Role.GetById(id);

    public void Add(Role role)
    {
        _uow.Role.Add(role);
        _uow.SaveChangesAsync().Wait();
    }

    public void Update(Role role)
    {
        _uow.Role.Update(role);
        _uow.SaveChangesAsync().Wait();
    }

    public void Delete(int id)
    {
        _uow.Role.Delete(id);
        _uow.SaveChangesAsync().Wait();
    }
}