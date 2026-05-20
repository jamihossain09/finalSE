using BCrypt.Net;
using finalSE.Service.Interface;
using finalSE.UnitOfWork.Interface;
using finalSE.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;

    public UserService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public List<User> GetAll()
    {
        return _uow.User.GetAll();
    }

    public User GetById(int id)
    {
        return _uow.User.GetById(id);
    }

    // REGISTER
    public async Task<bool> RegisterAsync(User user)
    {
        var existingUser =
            _uow.User.GetByUserName(user.UserName);

        if (existingUser != null)
            return false;

        user.PasswordHash =
            BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

        user.CreatedAt = DateTime.Now;

        _uow.User.Add(user);

        await _uow.SaveChangesAsync();

        return true;
    }

    // LOGIN
    public async Task<User> AuthenticateAsync(string username, string password)
    {
        var user =
            _uow.User.GetByUserName(username);

        if (user == null)
            return null;

        bool isPasswordValid =
            BCrypt.Net.BCrypt.Verify(
                password,
                user.PasswordHash
            );

        if (!isPasswordValid)
            return null;

        user.LastLogin = DateTime.Now;

        _uow.User.Update(user);

        await _uow.SaveChangesAsync();

        return user;
    }

    // UPDATE
    public async Task UpdateAsync(User user)
    {
        _uow.User.Update(user);

        await _uow.SaveChangesAsync();
    }

    // DELETE
    public async Task DeleteAsync(int id)
    {
        _uow.User.Delete(id);

        await _uow.SaveChangesAsync();
    }
}