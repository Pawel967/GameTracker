using Game_MVC.Models;
using Game_MVC.Models.Game;
using Game_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game_MVC.Controllers
{
    [Authorize]
    public class GamesController : Controller
    {
        private readonly IGameService _gameService;
        private readonly ILibraryService _libraryService;
        private readonly ILogger<GamesController> _logger;

        public GamesController(
            IGameService gameService,
            ILibraryService libraryService,
            ILogger<GamesController> logger)
        {
            _gameService = gameService;
            _libraryService = libraryService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? query, string? genre, string? developer, int page = 1)
        {
            try
            {
                ViewBag.CurrentGenre = genre;
                ViewBag.CurrentDeveloper = developer;
                ViewBag.SearchQuery = query;
                ViewBag.Genres = (await _gameService.GetAllGenresAsync()).Genres;

                PaginatedResponse<GameViewModel> games;

                if (!string.IsNullOrEmpty(query))
                {
                    games = await _gameService.SearchGamesAsync(query, page);
                }
                else if (!string.IsNullOrEmpty(genre))
                {
                    games = await _gameService.GetGamesByGenreAsync(genre, page);
                }
                else if (!string.IsNullOrEmpty(developer))
                {
                    games = await _gameService.GetGamesByDeveloperAsync(developer, page);
                }
                else
                {
                    games = await _gameService.GetAllGamesAsync(page);
                }

                return View(games);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving games. Query: {Query}, Genre: {Genre}, Developer: {Developer}, Page: {Page}",
                    query, genre, developer, page);
                return View("Error");
            }
        }

        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var game = await _gameService.GetGameByIdAsync(id);
                if (game == null)
                {
                    return NotFound();
                }

                // Check if game is in user's library
                var userGame = await _libraryService.GetGameAsync(id);
                game.IsInUserLibrary = userGame != null;

                return View(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving game details for ID: {Id}", id);
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToLibrary(long id)
        {
            try
            {
                var result = await _libraryService.AddGameAsync(id);
                if (result)
                {
                    TempData["Success"] = "Game added to your library successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to add game to library. It might already be in your library.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game to library: {Id}", id);
                TempData["Error"] = "An error occurred while adding the game to your library.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
