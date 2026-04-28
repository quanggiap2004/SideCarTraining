using SideCar.Business.DTOs.Params;
using SideCar.Business.Entities;

namespace SideCar.Business.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<Users>> GetPagedUsersAsync(QueryUserParams userParams);
        Task<int> CountUsersAsync(QueryUserParams userParams);
        Task<Users?> FindUserByIdAsync(Guid id);
        Task<int> SaveChangesAsync();
        Task<int> UpdatePassword(string hashPassword, Users userEntity);
    }
}
