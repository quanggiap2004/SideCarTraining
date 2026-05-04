using SideCar.Business.Data;
using SideCar.Business.Repositories.Interfaces;

namespace SideCar.Business.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ProjectDbContext _dbContext;
        private IUserRepository? _users;
        private IAuthenRepository? _authen;
        private IUserActivityLogRepository? _activityLogs;

        public UnitOfWork(ProjectDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IUserRepository Users => _users ??= new UserRepository(_dbContext);
        public IAuthenRepository Authen => _authen ??= new AuthenRepository(_dbContext);
        public IUserActivityLogRepository ActivityLogs => _activityLogs ??= new UserActivityLogRepository(_dbContext);

        public async Task<int> CommitAsync() => await _dbContext.SaveChangesAsync();

    }
}
