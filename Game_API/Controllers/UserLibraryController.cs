using Game_API.Dtos.Profile;
using Game_API.Dtos.UserLibrary;
using Game_API.Models.Library;
using Game_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Game_API.Controllers
{
    [ApiController]
    [Route("api/library")]
    [Authorize]
    public class UserLibraryController : ControllerBase
    {
        private readonly IUserLibraryService _libraryService;
        private readonly ILogger<UserLibraryController> _logger;

        public UserLibraryController(
            IUserLibraryService libraryService,
            ILogger<UserLibraryController> logger)
        {
            _libraryService = libraryService;
            _logger = logger;
        }

        [HttpGet("my")]
        public async Task<ActionResult<PaginatedResponse<UserGameLibraryDto>>> GetMyLibrary(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] GameStatus? status = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool ascending = true)
        {
            try
            {
                var userId = GetUserId();
                var result = await _libraryService.GetUserLibraryAsync(
                    targetUserId: userId,
                    requestingUserId: userId as Guid?,
                    pageNumber,
                    pageSize,
                    status,
                    sortBy,
                    ascending);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user library");
                return StatusCode(500, "An error occurred while retrieving your library.");
            }
        }

        [HttpGet("users/{userId}")]
        [AllowAnonymous]
        public async Task<ActionResult<PaginatedResponse<UserGameLibraryDto>>> GetUserLibrary(
            Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] GameStatus? status = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool ascending = true)
        {
            try
            {
                Guid? requestingUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    requestingUserId = GetUserId();
                }

                var result = await _libraryService.GetUserLibraryAsync(
                    targetUserId: userId,
                    requestingUserId: requestingUserId,
                    pageNumber,
                    pageSize,
                    status,
                    sortBy,
                    ascending);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user library");
                return StatusCode(500, "An error occurred while retrieving the library.");
            }
        }

        [HttpPost("games/{igdbGameId}")]
        public async Task<ActionResult> AddGame(long igdbGameId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _libraryService.AddGameToLibraryAsync(userId, igdbGameId);
                if (!result)
                {
                    return BadRequest("Game could not be added to library. It might already exist in your library.");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game {GameId} to library", igdbGameId);
                return StatusCode(500, "An error occurred while adding the game to your library.");
            }
        }

        [HttpDelete("games/{igdbGameId}")]
        public async Task<ActionResult> RemoveGame(long igdbGameId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _libraryService.RemoveGameFromLibraryAsync(userId, igdbGameId);
                if (!result)
                {
                    return NotFound("Game not found in your library.");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game", igdbGameId);
                return StatusCode(500, "An error occurred while removing the game.");
            }
        }

        [HttpGet("games/{igdbGameId}")]
        public async Task<ActionResult<UserGameLibraryDto>> GetGame(long igdbGameId)
        {
            try
            {
                var userId = GetUserId();
                var game = await _libraryService.GetUserGameAsync(userId, igdbGameId);
                if (game == null)
                {
                    return NotFound("Game not found in your library.");
                }
                return Ok(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving game {GameId}", igdbGameId);
                return StatusCode(500, "An error occurred while retrieving the game.");
            }
        }

        [HttpPatch("games/{igdbGameId}/status")]
        public async Task<ActionResult> UpdateGameStatus(long igdbGameId, [FromBody] GameStatus status)
        {
            try
            {
                var userId = GetUserId();
                var result = await _libraryService.UpdateGameStatusAsync(userId, igdbGameId, status);
                if (!result)
                {
                    return NotFound("Game not found in your library.");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game status", igdbGameId);
                return StatusCode(500, "An error occurred while updating the game status.");
            }
        }

        [HttpPatch("games/{igdbGameId}/favorite")]
        public async Task<ActionResult> ToggleFavorite(long igdbGameId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _libraryService.ToggleGameFavoriteAsync(userId, igdbGameId);
                if (!result)
                {
                    return NotFound("Game not found in your library.");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite status", igdbGameId);
                return StatusCode(500, "An error occurred while updating the favorite status.");
            }
        }

        [HttpPut("games/{igdbGameId}/rating")]
        public async Task<ActionResult> UpdateGameRating(long igdbGameId, [FromBody] int rating)
        {
            try
            {
                if (rating < 1 || rating > 10)
                {
                    return BadRequest("Rating must be between 1 and 10.");
                }

                var userId = GetUserId();
                var result = await _libraryService.UpdateGameRatingAsync(userId, igdbGameId, rating);
                if (!result)
                {
                    return NotFound("Game not found in your library.");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game rating", igdbGameId);
                return StatusCode(500, "An error occurred while updating the game rating.");
            }
        }

        [HttpGet("genres")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<GenreDto>>> GetAllGenres()
        {
            try
            {
                var genres = await _libraryService.GetAllGenresAsync();
                return Ok(genres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genres");
                return StatusCode(500, "An error occurred while retrieving genres.");
            }
        }

        [HttpGet("my/genres")]
        public async Task<ActionResult<IEnumerable<UserGenreStatsDto>>> GetMyGenreStatistics()
        {
            try
            {
                var userId = GetUserId();
                var stats = await _libraryService.GetUserGenreStatisticsAsync(
                    targetUserId: userId,
                    requestingUserId: userId as Guid?);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genre statistics");
                return StatusCode(500, "An error occurred while retrieving genre statistics.");
            }
        }

        [HttpGet("users/{userId}/genres")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<UserGenreStatsDto>>> GetUserGenreStatistics(Guid userId)
        {
            try
            {
                Guid? requestingUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    requestingUserId = GetUserId();
                }

                var stats = await _libraryService.GetUserGenreStatisticsAsync(
                    targetUserId: userId,
                    requestingUserId: requestingUserId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genre statistics");
                return StatusCode(500, "An error occurred while retrieving genre statistics.");
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