using Game_MVC.Models;
using Game_MVC.Models.Library;
using Game_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game_MVC.Controllers
{
    [Authorize]
    public class LibraryController : Controller
    {
        private readonly ILibraryService _libraryService;
        private readonly ILogger<LibraryController> _logger;

        public LibraryController(ILibraryService libraryService, ILogger<LibraryController> logger)
        {
            _libraryService = libraryService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(LibraryFilterViewModel filter)
        {
            try
            {
                var library = await _libraryService.GetMyLibraryAsync(filter);
                ViewBag.CurrentFilter = filter;
                return View(library);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving library");
                TempData["Error"] = "Failed to retrieve your game library.";
                return View(new PaginatedResponse<UserGameLibraryViewModel>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddGame(long gameId)
        {
            try
            {
                var result = await _libraryService.AddGameAsync(gameId);
                if (result)
                    TempData["Success"] = "Game added to your library.";
                else
                    TempData["Error"] = "Failed to add game to library.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game");
                TempData["Error"] = "Failed to add game to library.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveGame(long gameId)
        {
            try
            {
                var result = await _libraryService.RemoveGameAsync(gameId);
                if (result)
                    TempData["Success"] = "Game removed from your library.";
                else
                    TempData["Error"] = "Failed to remove game from library.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game");
                TempData["Error"] = "Failed to remove game from library.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(long gameId, GameStatus status)
        {
            try
            {
                var result = await _libraryService.UpdateGameStatusAsync(gameId, status);
                if (!result)
                    TempData["Error"] = "Failed to update game status.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game status");
                TempData["Error"] = "Failed to update game status.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(long gameId)
        {
            try
            {
                var result = await _libraryService.ToggleFavoriteAsync(gameId);
                if (!result)
                    TempData["Error"] = "Failed to toggle favorite status.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite status");
                TempData["Error"] = "Failed to toggle favorite status.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRating(long gameId, int rating)
        {
            try
            {
                if (rating < 1 || rating > 10)
                {
                    TempData["Error"] = "Rating must be between 1 and 10.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _libraryService.UpdateRatingAsync(gameId, rating);
                if (!result)
                    TempData["Error"] = "Failed to update rating.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rating");
                TempData["Error"] = "Failed to update rating.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
