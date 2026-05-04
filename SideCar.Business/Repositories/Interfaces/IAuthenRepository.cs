using SideCar.Business.DTOs;
using SideCar.Business.Entities;
using System.Linq.Expressions;

namespace SideCar.Business.Repositories.Interfaces
{
    public interface IAuthenRepository
    {
        Task<Users?> GetByUsernameAsync(string username);
        Task<Users?> GetByRefreshTokenAsync(string hashedToken);
        Task<bool> ExistsAsync(string username, string email, string phoneNumber);
        Task AddUserAsync(Users user);
        Task<Users?> FindByAsync(Expression<Func<Users, bool>> predicate);
    }
}
