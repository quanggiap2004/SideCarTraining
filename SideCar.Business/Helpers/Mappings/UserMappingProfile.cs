using AutoMapper;
using SideCar.Business.DTOs;
using SideCar.Business.Entities;

namespace SideCar.Business.Helpers.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<Users, UserResponseDto>()
                .ForMember(dest => dest.Phonenumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

            CreateMap<Users, UserProfileDto>()
                .ForMember(dest => dest.Phonenumber, opt => opt.MapFrom(src => src.PhoneNumber));
        }
    }
}
