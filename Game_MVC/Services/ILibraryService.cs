using Game_MVC.Models;
using Game_MVC.Models.Library;
using Game_MVC.Models.Recommendations;

namespace Game_MVC.Services
{
    public interface ILibraryService
    {
        Task<PaginatedResponse<UserGameLibraryViewModel>> GetMyLibraryAsync(LibraryFilterViewModel filter);
        Task<UserGameLibraryViewModel?> GetGameAsync(long gameId);
        Task<bool> AddGameAsync(long gameId);
        Task<bool> RemoveGameAsync(long gameId);
        Task<bool> UpdateGameStatusAsync(long gameId, GameStatus status);
        Task<bool> ToggleFavoriteAsync(long gameId);
        Task<bool> UpdateRatingAsync(long gameId, int rating);
        Task<IEnumerable<UserGenreStatsViewModel>> GetMyGenreStatisticsAsync();
    }
}
