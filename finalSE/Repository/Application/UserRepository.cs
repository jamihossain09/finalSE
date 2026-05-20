using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using finalSE.Models;

public class UserRepository : IUserRepository
{
    private readonly MyDBContext _context;

    public UserRepository(MyDBContext context)
    {
        _context = context;
    }

    public List<User> GetAll()
    {
        return _context.Users
            .Include(u => u.Role)
            .ToList();
    }

    public User GetByUserName(string userName)
    {
        return _context.Users
            .Include(u => u.Role)
            .FirstOrDefault(u => u.UserName == userName);
    }

    public User GetById(int id)
    {
        return _context.Users
            .Include(u => u.Role)
            .FirstOrDefault(u => u.Id == id);
    }

    public void Add(User user)
    {
        // optional: safe check
        user.CreatedAt = DateTime.Now;

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    public void Update(User user)
    {
        var existing = _context.Users.Find(user.Id);

        if (existing != null)
        {
            existing.UserName = user.UserName;
            existing.Email = user.Email;
            existing.Address = user.Address;
            existing.RoleId = user.RoleId;
            existing.PasswordHash = user.PasswordHash;
            existing.LastLogin = user.LastLogin;

            _context.SaveChanges();
        }
    }

    public void Delete(int id)
    {
        var user = _context.Users.Find(id);

        if (user != null)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
        }
    }
}