using Game_MVC.Models.Recommendations;
using Game_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game_MVC.Controllers
{
    [Authorize]
    public class RecommendationsController : Controller
    {
        private readonly IRecommendationService _recommendationService;
        private readonly ILibraryService _libraryService;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            IRecommendationService recommendationService,
            ILibraryService libraryService,
            ILogger<RecommendationsController> logger)
        {
            _recommendationService = recommendationService;
            _libraryService = libraryService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var stats = await _libraryService.GetMyGenreStatisticsAsync();
                var model = new RecommendationIndexViewModel
                {
                    GenreStats = stats,
                    TotalGames = stats.Sum(s => s.GamesCount)
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genre statistics");
                TempData["Error"] = "Failed to retrieve library statistics.";
                return View(new RecommendationIndexViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateRecommendations()
        {
            try
            {
                var recommendations = await _recommendationService.GetPersonalizedRecommendationsAsync();
                return PartialView("_RecommendationsList", recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendations");
                return PartialView("_Error", "Failed to generate recommendations.");
            }
        }
    }
}
