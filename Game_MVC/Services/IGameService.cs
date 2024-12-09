using Game_MVC.Models;
using Game_MVC.Models.Game;

namespace Game_MVC.Services
{
    public interface IGameService
    {
        Task<GameViewModel?> GetGameByIdAsync(long id);
        Task<PaginatedResponse<GameViewModel>> SearchGamesAsync(string? query, int pageNumber = 1, int pageSize = 9);
        Task<PaginatedResponse<GameViewModel>> GetGamesByGenreAsync(string genre, int pageNumber = 1, int pageSize = 9);
        Task<PaginatedResponse<GameViewModel>> GetGamesByDeveloperAsync(string developer, int pageNumber = 1, int pageSize = 9);
        Task<PaginatedResponse<GameViewModel>> GetAllGamesAsync(int pageNumber = 1, int pageSize = 9);
        Task<GenreListResponse> GetAllGenresAsync(int? limit = null);
    }
}
