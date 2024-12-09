using Game_API.Dtos.Recommendation;
using Game_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Game_API.Controllers
{
    [ApiController]
    [Route("api/recommendations")]
    public class RecommendationController : ControllerBase
    {
        private readonly IGameRecommendationService _recommendationService;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(
            IGameRecommendationService recommendationService,
            ILogger<RecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("personalized")]
        public async Task<ActionResult<IEnumerable<GameRecommendationDto>>> GetPersonalizedRecommendations(
            [FromQuery] int count = 10)
        {
            try
            {
                var userId = GetUserId();
                var recommendations = await _recommendationService.GetPersonalizedRecommendationsAsync(userId, count);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations");
                return StatusCode(500, "An error occurred while getting recommendations.");
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found");
            return Guid.Parse(userIdClaim);
        }
    }
}
