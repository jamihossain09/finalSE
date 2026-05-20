using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using finalSE.Models;
using System.Linq;

public class RoleRepository : IRoleRepository
{
    private readonly MyDBContext _context;

    public RoleRepository(MyDBContext context)
    {
        _context = context;
    }

    public List<Role> GetAll()
    {
        return _context.Roles.ToList();
    }

    public Role GetById(int id)
    {
        return _context.Roles.Find(id);
    }

    public void Add(Role role)
    {
        _context.Roles.Add(role);
        _context.SaveChanges();
    }

    public void Update(Role role)
    {
        _context.Roles.Update(role);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var role = _context.Roles.Find(id);
        _context.Roles.Remove(role);
        _context.SaveChanges();
    }
}