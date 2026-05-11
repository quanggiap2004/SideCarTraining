using AutoMapper;
using SideCar.Business.DTOs;
using SideCar.Business.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideCar.Business.Helpers.Mappings
{
    public class UserLogActivityMappingProfile : Profile
    {
        public UserLogActivityMappingProfile()
        {
            CreateMap<UserActivityLog, UserActivityLogDto>()
                .ForMember(dest => dest.id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ActivityType, opt => opt.MapFrom(src => src.ActivityType))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => src.CreatedAt));
        }
    }
}
