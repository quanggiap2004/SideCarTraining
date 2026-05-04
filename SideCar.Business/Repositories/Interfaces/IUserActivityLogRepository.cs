using SideCar.Business.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Repositories.Interfaces
{
    public interface IUserActivityLogRepository
    {
        Task AddAsync(UserActivityLog entity);
    }
}
