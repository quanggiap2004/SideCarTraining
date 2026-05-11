using Microsoft.EntityFrameworkCore;
using SideCar.Business.Data;
using SideCar.Business.DTOs;
using SideCar.Business.DTOs.Params;
using SideCar.Business.Entities;
using SideCar.Business.Enums;
using SideCar.Business.Repositories.Interfaces;
using System.Linq.Expressions;

namespace SideCar.Business.Repositories
{
    public class UserRepository : GenericRepository<Users>, IUserRepository
    {
        public UserRepository(ProjectDbContext dbContext) : base(dbContext) { }

        public async Task<IEnumerable<Users>> GetPagedUsersAsync(QueryUserParams userParams)
        {
            Func<IQueryable<Users>, IOrderedQueryable<Users>> orderBy =
                q => userParams.IsDescending
                    ? q.OrderByDescending(u => u.CreatedAt)
                    : q.OrderBy(u => u.CreatedAt);

            return await GetPageAsync(userParams.Page, userParams.PageSize, BuildPredicate(userParams), orderBy);
        }

        public async Task<int> CountUsersAsync(QueryUserParams userParams)
        {
            return await CountAsync(BuildPredicate(userParams));
        }

        private static Expression<Func<Users, bool>> BuildPredicate(QueryUserParams userParams) =>
            u => (userParams.UserName == null || u.Username.Contains(userParams.UserName))
                 && (userParams.Email == null || u.Email.Contains(userParams.Email))
                 && (userParams.IsDeleted == null || u.IsDeleted == userParams.IsDeleted);

        public async Task<Users?> FindUserByIdAsync(Guid id)
        {
            return await FindByIdAsync(id);
        }

        public void UpdatePassword(string hashPassword, Users userEntity)
        {
            userEntity.PasswordHash = hashPassword;
        }

        public async Task<List<(Guid Id, string Email)>> GetInactiveUserCandidatesAsync(DateTime cutoffDate)
        {
            var result = await dbContext.Users
                .Where(u => (u.LastLoginAt ?? u.CreatedAt) <= cutoffDate
                         && u.Status == AccountStatus.Active
                         && !u.IsDeleted)
                .Select(u => new { u.Id, u.Email })
                .ToListAsync();

            return result.Select(u => (u.Id, u.Email)).ToList();
        }

        public async Task<int> BulkDeactivateAccountAsync(List<Guid> ids, DateTime cutoffDate)
        {
            return await dbContext.Users
                .Where(u => ids.Contains(u.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, AccountStatus.Deactivated));
        }

        public async Task<List<UserForWarning>> GetUsersForWarningAsync(DateTime cutoffDate)
        {
            var result = await dbContext.Users
                .Where(u => (u.LastLoginAt ?? u.CreatedAt) <= cutoffDate
                         && u.WarningSentAt == null
                         && u.Status == AccountStatus.Active
                         && !u.IsDeleted)
                .Select(u => new UserForWarning {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.Fullname 
                })
                .ToListAsync();

            return result;
        }

        public async Task<int> BulkMarkWarningSentAsync(List<Guid> ids)
        {
            return await dbContext.Users
                .Where(u => ids.Contains(u.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.WarningSentAt, DateTime.UtcNow));
        }

        public async Task<int> MarkWarningSentAsync(Guid id)
        {
            return await dbContext.Users.Where(u => u.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.WarningSentAt, DateTime.UtcNow));
        }
    }
}
