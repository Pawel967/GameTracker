using AutoMapper;
using Game_API.Dtos.Profile;
using Game_API.Models.Auth;

namespace Game_API.Mappings
{
    public class ProfileMappingProfile : Profile
    {
        public ProfileMappingProfile()
        {
            CreateMap<User, UserProfileDto>()
                .ForMember(dest => dest.FollowersCount,
                    opt => opt.MapFrom(src => src.FollowedBy.Count))
                .ForMember(dest => dest.FollowingCount,
                    opt => opt.MapFrom(src => src.Following.Count))
                .ForMember(dest => dest.Games,
                    opt => opt.MapFrom(src => src.UserGameLibraries))
                .ForMember(dest => dest.GameStatusCounts,
                    opt => opt.MapFrom(src => src.UserGameLibraries
                        .GroupBy(g => g.Status)
                        .ToDictionary(g => g.Key, g => g.Count())))
                .ForMember(dest => dest.IsFollowedByCurrentUser,
                    opt => opt.Ignore());

            CreateMap<User, ProfileSearchDto>()
                .ForMember(dest => dest.GamesCount,
                    opt => opt.MapFrom(src => src.UserGameLibraries.Count))
                .ForMember(dest => dest.FollowersCount,
                    opt => opt.MapFrom(src => src.FollowedBy.Count))
                .ForMember(dest => dest.IsFollowedByCurrentUser,
                    opt => opt.Ignore());

            CreateMap<User, FollowedUserDto>()
                .ForMember(dest => dest.GamesCount,
                    opt => opt.MapFrom(src => src.UserGameLibraries.Count));
        }
    }
}
