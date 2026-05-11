namespace SideCar.Business.Services.Interfaces
{
    public interface IQueueService
    {
        Task<int> RedriveDlqAsync();
    }
}
