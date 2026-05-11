using SideCar.Business.Data;
using SideCar.Business.DTOs.Params;
using SideCar.Business.Entities;
using SideCar.Business.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Repositories
{
    public class UserActivityLogRepository : GenericRepository<UserActivityLog>, IUserActivityLogRepository
    {
        public UserActivityLogRepository(ProjectDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<int> GetCountAsync(QueryUserActivityLogParams userActivityLogParams)
        {
            return await CountAsync(u => (userActivityLogParams.UserId == null || u.UserId == userActivityLogParams.UserId) &&
                     (userActivityLogParams.FromDate == null || u.CreatedAt >= userActivityLogParams.FromDate) &&
                     (userActivityLogParams.ToDate == null || u.CreatedAt <= userActivityLogParams.ToDate));
        }

        public async Task<IEnumerable<UserActivityLog>> GetPagedUserActivityLogAsync(QueryUserActivityLogParams userActivityLogParams)
        {
            Func<IQueryable<UserActivityLog>, IOrderedQueryable<UserActivityLog>> orderBy = q => userActivityLogParams.IsDescending ? 
            q.OrderByDescending(u => u.CreatedAt) : q.OrderBy(u => u.CreatedAt);

            return await GetPageAsync(userActivityLogParams.Page, userActivityLogParams.PageSize,
                u => (userActivityLogParams.UserId == null || u.UserId == userActivityLogParams.UserId) &&
                     (userActivityLogParams.FromDate == null || u.CreatedAt >= userActivityLogParams.FromDate) &&
                     (userActivityLogParams.ToDate == null || u.CreatedAt <= userActivityLogParams.ToDate),
                orderBy,
                u => u.User
                );
        }
    }
}
