using Application.DTOs.Auth;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

public sealed class LaravelMappingProfile : Profile
{
    public LaravelMappingProfile()
    {
        CreateMap<User, LoginUserInfoDto>()
            .ForMember(dest => dest.ProfileImage, opt => opt.MapFrom(src => src.ProfileImage))
            .ForMember(dest => dest.LeaveBalance, opt => opt.MapFrom(src => src.LeaveBalance));

        CreateMap<User, SignupResponseDto>()
            .ForMember(dest => dest.Mobile, opt => opt.MapFrom(src => src.Mobile))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));
    }
}
