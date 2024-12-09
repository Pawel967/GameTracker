using Game_API.Models.IGDB;
using Game_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Game_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IIGDBService _igdbService;
        private readonly ILogger<GamesController> _logger;

        public GamesController(IIGDBService igdbService, ILogger<GamesController> logger)
        {
            _igdbService = igdbService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IGDBGame>> GetGame(long id)
        {
            var game = await _igdbService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound();
            }
            return Ok(game);
        }

        [HttpGet("search")]
        public async Task<ActionResult<PaginatedResponse<IGDBGame>>> SearchGames(
        [FromQuery] string? query,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        {
            var response = await _igdbService.SearchGamesAsync(query, pageNumber, pageSize);
            return Ok(response);
        }

        [HttpGet("by-genre")]
        public async Task<ActionResult<PaginatedResponse<IGDBGame>>> GetGamesByGenre(
            [FromQuery] string genre,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(genre))
            {
                return BadRequest("Genre parameter is required");
            }

            var response = await _igdbService.GetGamesByGenreAsync(genre, pageNumber, pageSize);
            return Ok(response);
        }

        [HttpGet("by-developer")]
        public async Task<ActionResult<PaginatedResponse<IGDBGame>>> GetGamesByDeveloper(
            [FromQuery] string developer,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(developer))
            {
                return BadRequest("Developer parameter is required");
            }

            var response = await _igdbService.GetGamesByDeveloperAsync(developer, pageNumber, pageSize);
            return Ok(response);
        }

        [HttpGet("all")]
        public async Task<ActionResult<PaginatedResponse<IGDBGame>>> GetAllGames(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var response = await _igdbService.GetAllGamesAsync(pageNumber, pageSize);
            return Ok(response);
        }

        [HttpGet("genres")]
        public async Task<ActionResult<GenreListResponse>> GetAllGenres(
    [FromQuery] int? limit = null)
        {
            var response = await _igdbService.GetAllGenresAsync(limit);
            return Ok(response);
        }
    }
}
