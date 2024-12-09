using AutoMapper;
using Game_API.Dtos.UserLibrary;
using Game_API.Models.IGDB;
using Game_API.Models.Library;

namespace Game_API.Mappings
{
    public class LibraryMappingProfile : Profile
    {
        public LibraryMappingProfile()
        {
            // Map from IGDB to Game entity
            CreateMap<IGDBGame, Game>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(src => src.Summary))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.RatingCount, opt => opt.MapFrom(src => src.RatingCount))
                .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate))
                .ForMember(dest => dest.CoverUrl, opt => opt.MapFrom(src => src.CoverUrl))
                .ForMember(dest => dest.Developer, opt => opt.MapFrom(src => src.Developer))
                .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.Publisher))
                .ForMember(dest => dest.UserGameLibraries, opt => opt.Ignore())
                .ForMember(dest => dest.GameGenres, opt => opt.Ignore())
                .ForMember(dest => dest.GameThemes, opt => opt.Ignore());

            // Map from IGDB to UserGameLibrary
            CreateMap<IGDBGame, UserGameLibrary>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.GameId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.DateAdded, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => GameStatus.PlanToPlay))
                .ForMember(dest => dest.IsFavorite, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.UserRating, opt => opt.Ignore())
                .ForMember(dest => dest.Game, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            // Map from UserGameLibrary to UserGameLibraryDto
            CreateMap<UserGameLibrary, UserGameLibraryDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.GameId, opt => opt.MapFrom(src => src.GameId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Game.Name))
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(src => src.Game.Summary))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Game.Rating))
                .ForMember(dest => dest.RatingCount, opt => opt.MapFrom(src => src.Game.RatingCount))
                .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.Game.ReleaseDate))
                .ForMember(dest => dest.CoverUrl, opt => opt.MapFrom(src => src.Game.CoverUrl))
                .ForMember(dest => dest.Developer, opt => opt.MapFrom(src => src.Game.Developer))
                .ForMember(dest => dest.Publisher, opt => opt.MapFrom(src => src.Game.Publisher))
                .ForMember(dest => dest.Genres, opt => opt.MapFrom(src =>
                    src.Game.GameGenres.Select(gg => gg.Genre.Name)))
                .ForMember(dest => dest.Themes, opt => opt.MapFrom(src =>
                    src.Game.GameThemes.Select(gt => gt.Theme.Name)));

            // Map Game entity to GameDto
            CreateMap<Game, GameDto>()
                .ForMember(dest => dest.Genres, opt => opt.MapFrom(src =>
                    src.GameGenres.Select(gg => gg.Genre.Name)))
                .ForMember(dest => dest.Themes, opt => opt.MapFrom(src =>
                    src.GameThemes.Select(gt => gt.Theme.Name)));

            CreateMap<Genre, GenreDto>();
            CreateMap<Theme, ThemeDto>();
        }
    }
}
