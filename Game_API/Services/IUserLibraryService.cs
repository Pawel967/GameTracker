using Game_API.Dtos.UserLibrary;
using Game_API.Models.Library;

namespace Game_API.Services
{
    public interface IUserLibraryService
    {
        Task<bool> AddGameToLibraryAsync(Guid userId, long igdbGameId);
        Task<bool> RemoveGameFromLibraryAsync(Guid userId, long igdbGameId);
        Task<UserGameLibraryDto?> GetUserGameAsync(Guid userId, long igdbGameId);

        Task<PaginatedResponse<UserGameLibraryDto>> GetUserLibraryAsync(
            Guid targetUserId,
            Guid? requestingUserId,
            int pageNumber = 1,
            int pageSize = 10,
            GameStatus? statusFilter = null,
            string? sortBy = null,
            bool ascending = true);

        Task<bool> UpdateGameStatusAsync(Guid userId, long igdbGameId, GameStatus status);
        Task<bool> UpdateGameRatingAsync(Guid userId, long igdbGameId, int rating);
        Task<bool> ToggleGameFavoriteAsync(Guid userId, long igdbGameId);
        Task<Dictionary<GameStatus, int>> GetUserGameStatusCountsAsync(Guid userId);
        Task<IEnumerable<UserGenreStatsDto>> GetUserGenreStatisticsAsync(Guid targetUserId, Guid? requestingUserId);
        Task<IEnumerable<GenreDto>> GetAllGenresAsync();
    }
}
