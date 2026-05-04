using SideCar.Business.Data;
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
    }
}
