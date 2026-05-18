using SideCar.Business.DTOs;
using SideCar.Business.DTOs.Params;
using SideCar.Business.Entities;

namespace SideCar.Business.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<Users>> GetPagedUsersAsync(QueryUserParams userParams);
        Task<int> CountUsersAsync(QueryUserParams userParams);
        Task<Users?> FindUserByIdAsync(Guid id);
        void UpdatePassword(string hashPassword, Users userEntity);
        Task<List<InactiveUserCandidate>> GetInactiveUserCandidatesAsync(DateTime cutoffDate);
        Task<int> BulkDeactivateAccountAsync(List<Guid> ids, DateTime cutoffDate);
        Task<List<UserForWarning>> GetUsersForWarningAsync(DateTime cutoffDate);
        Task<int> BulkMarkWarningSentAsync(List<Guid> ids);
        Task<int> MarkWarningSentAsync(Guid id);
    }
}
