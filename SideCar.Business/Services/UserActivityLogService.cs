using AutoMapper;
using SideCar.Business.DTOs;
using SideCar.Business.DTOs.Params;
using SideCar.Business.Helpers.Exceptions;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Services
{
    public class UserActivityLogService(IMapper mapper,  IUnitOfWork unitOfWork) : IUserActivityLogService
    {
        public async Task<Pagination<UserActivityLogDto>> GetPagedUserActivityLogAsync(QueryUserActivityLogParams userActivityLogParams)
        {
            if(userActivityLogParams.FromDate.HasValue && userActivityLogParams.ToDate.HasValue)
            {
                if(userActivityLogParams.FromDate.Value > userActivityLogParams.ToDate.Value)
                {
                    throw new ValidationException("From date cannot be greater than to date.");
                }
            }
            var userActivityLogs = await unitOfWork.ActivityLogs.GetPagedUserActivityLogAsync(userActivityLogParams);
            var userActivityLogDtosMapped = mapper.Map<IEnumerable<UserActivityLogDto>>(userActivityLogs).ToList();
            var count = await unitOfWork.ActivityLogs.GetCountAsync(userActivityLogParams);
            return new Pagination<UserActivityLogDto>(userActivityLogDtosMapped, count, userActivityLogParams.Page, userActivityLogParams.PageSize);
        }
    }
}
