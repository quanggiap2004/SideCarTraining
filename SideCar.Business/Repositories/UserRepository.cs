using SideCar.Business.Data;
using SideCar.Business.DTOs.Params;
using SideCar.Business.Entities;
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

        public async Task<int> UpdatePassword(string hashPassword, Users userEntity)
        {
            userEntity.PasswordHash = hashPassword;
            return await SaveChangesAsync();
        }
    }
}
