namespace SideCar.Business.Repositories.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IAuthenRepository Authen { get; }
        IUserActivityLogRepository ActivityLogs { get; }
        Task<int> CommitAsync();
    }
}
