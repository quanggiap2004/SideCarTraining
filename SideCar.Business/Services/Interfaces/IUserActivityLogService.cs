using SideCar.Business.DTOs;
using SideCar.Business.DTOs.Params;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Services.Interfaces
{
    public interface IUserActivityLogService
    {
        Task<Pagination<UserActivityLogDto>> GetPagedUserActivityLogAsync(QueryUserActivityLogParams userActivityLogParams);
    }
}
