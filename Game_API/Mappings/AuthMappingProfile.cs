using AutoMapper;
using Game_API.Dtos.Auth;
using Game_API.Models.Auth;

namespace Game_API.Mappings
{
    public class AuthMappingProfile : Profile
    {
        public AuthMappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles,
                    opt => opt.MapFrom(src => src.Roles.Select(r => r.Name)));
        }
    }
}
