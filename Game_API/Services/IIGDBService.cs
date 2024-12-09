using Game_API.Models.IGDB;

namespace Game_API.Services
{
    public interface IIGDBService
    {
        Task<IGDBGame?> GetGameByIdAsync(long id);
        Task<PaginatedResponse<IGDBGame>> SearchGamesAsync(string? query, int pageNumber, int pageSize);
        Task<PaginatedResponse<IGDBGame>> GetGamesByGenreAsync(string genre, int pageNumber, int pageSize);
        Task<PaginatedResponse<IGDBGame>> GetGamesByDeveloperAsync(string developer, int pageNumber, int pageSize);
        Task<PaginatedResponse<IGDBGame>> GetAllGamesAsync(int pageNumber, int pageSize);
        Task<GenreListResponse> GetAllGenresAsync(int? limit = null);
    }
}
