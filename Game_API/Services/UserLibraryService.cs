using AutoMapper;
using Game_API.Data;
using Game_API.Dtos.UserLibrary;
using Game_API.Models.Library;
using Microsoft.EntityFrameworkCore;

namespace Game_API.Services
{
    public class UserLibraryService : IUserLibraryService
    {
        private readonly AppDbContext _context;
        private readonly IIGDBService _igdbService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserLibraryService> _logger;

        public UserLibraryService(
            AppDbContext context,
            IIGDBService igdbService,
            IMapper mapper,
            ILogger<UserLibraryService> logger)
        {
            _context = context;
            _igdbService = igdbService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> AddGameToLibraryAsync(Guid userId, long igdbGameId)
        {
            try
            {
                var existingUserGame = await _context.UserGameLibraries
                    .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == igdbGameId);

                if (existingUserGame != null)
                    return false;

                var igdbGame = await _igdbService.GetGameByIdAsync(igdbGameId);
                if (igdbGame == null)
                    return false;

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // First, check if the game exists in our database
                    var game = await _context.Games.FindAsync(igdbGameId);
                    if (game == null)
                    {
                        // If not, create it
                        game = _mapper.Map<Game>(igdbGame);
                        await _context.Games.AddAsync(game);
                        await _context.SaveChangesAsync();

                        // Handle genres
                        foreach (var genreName in igdbGame.Genres)
                        {
                            var genre = await _context.Genres
                                .FirstOrDefaultAsync(g => g.Name == genreName);

                            if (genre == null)
                            {
                                genre = new Genre { Name = genreName };
                                await _context.Genres.AddAsync(genre);
                                await _context.SaveChangesAsync();
                            }

                            game.GameGenres.Add(new GameGenre
                            {
                                GameId = game.Id,
                                GenreId = genre.Id
                            });
                        }

                        // Handle themes
                        foreach (var themeName in igdbGame.Themes)
                        {
                            var theme = await _context.Themes
                                .FirstOrDefaultAsync(t => t.Name == themeName);

                            if (theme == null)
                            {
                                theme = new Theme { Name = themeName };
                                await _context.Themes.AddAsync(theme);
                                await _context.SaveChangesAsync();
                            }

                            game.GameThemes.Add(new GameTheme
                            {
                                GameId = game.Id,
                                ThemeId = theme.Id
                            });
                        }

                        await _context.SaveChangesAsync();
                    }

                    // Create user game library entry
                    var userGameLibrary = _mapper.Map<UserGameLibrary>(igdbGame);
                    userGameLibrary.UserId = userId;
                    userGameLibrary.GameId = game.Id;

                    await _context.UserGameLibraries.AddAsync(userGameLibrary);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game to library");
                throw;
            }
        }

        public async Task<bool> RemoveGameFromLibraryAsync(Guid userId, long igdbGameId)
        {
            var userGame = await _context.UserGameLibraries
                .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == igdbGameId);

            if (userGame == null)
                return false;

            _context.UserGameLibraries.Remove(userGame);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserGameLibraryDto?> GetUserGameAsync(Guid userId, long igdbGameId)
        {
            var userGame = await _context.UserGameLibraries
                .Include(g => g.Game)
                    .ThenInclude(g => g.GameGenres)
                        .ThenInclude(gg => gg.Genre)
                .Include(g => g.Game)
                    .ThenInclude(g => g.GameThemes)
                        .ThenInclude(gt => gt.Theme)
                .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == igdbGameId);

            return userGame == null ? null : _mapper.Map<UserGameLibraryDto>(userGame);
        }

        public async Task<PaginatedResponse<UserGameLibraryDto>> GetUserLibraryAsync(
            Guid targetUserId,
            Guid? requestingUserId,
            int pageNumber = 1,
            int pageSize = 10,
            GameStatus? statusFilter = null,
            string? sortBy = null,
            bool ascending = true)
        {
            var user = await _context.Users.FindAsync(targetUserId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            if (user.IsProfilePrivate && requestingUserId != targetUserId)
            {
                return new PaginatedResponse<UserGameLibraryDto>
                {
                    Items = Enumerable.Empty<UserGameLibraryDto>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0
                };
            }

            var query = _context.UserGameLibraries
                .Include(g => g.Game)
                    .ThenInclude(g => g.GameGenres)
                        .ThenInclude(gg => gg.Genre)
                .Include(g => g.Game)
                    .ThenInclude(g => g.GameThemes)
                        .ThenInclude(gt => gt.Theme)
                .Where(g => g.UserId == targetUserId);

            if (statusFilter.HasValue)
            {
                query = query.Where(g => g.Status == statusFilter.Value);
            }

            query = ApplySorting(query, sortBy, ascending);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResponse<UserGameLibraryDto>
            {
                Items = _mapper.Map<IEnumerable<UserGameLibraryDto>>(items),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<bool> UpdateGameStatusAsync(Guid userId, long igdbGameId, GameStatus status)
        {
            var userGame = await _context.UserGameLibraries
                .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == igdbGameId);

            if (userGame == null)
                return false;

            userGame.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateGameRatingAsync(Guid userId, long igdbGameId, int rating)
        {
            if (rating < 1 || rating > 10)
                throw new ArgumentException("Rating must be between 1 and 10");

            var userGame = await _context.UserGameLibraries
                .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == igdbGameId);

            if (userGame == null)
                return false;

            userGame.UserRating = rating;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleGameFavoriteAsync(Guid userId, long igdbGameId)
        {
            var userGame = await _context.UserGameLibraries
                .FirstOrDefaultAsync(g => g.UserId == userId && g.GameId == igdbGameId);

            if (userGame == null)
                return false;

            userGame.IsFavorite = !userGame.IsFavorite;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<GameStatus, int>> GetUserGameStatusCountsAsync(Guid userId)
        {
            return await _context.UserGameLibraries
                .Where(g => g.UserId == userId)
                .GroupBy(g => g.Status)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Count()
                );
        }

        public async Task<IEnumerable<GenreDto>> GetAllGenresAsync()
        {
            var genres = await _context.Genres
                .OrderBy(g => g.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<GenreDto>>(genres);
        }

        public async Task<IEnumerable<UserGenreStatsDto>> GetUserGenreStatisticsAsync(
            Guid targetUserId,
            Guid? requestingUserId)
        {
            var user = await _context.Users.FindAsync(targetUserId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            if (user.IsProfilePrivate && requestingUserId != targetUserId)
            {
                return Enumerable.Empty<UserGenreStatsDto>();
            }

            var userGames = await _context.UserGameLibraries
                .Include(ugl => ugl.Game)
                    .ThenInclude(g => g.GameGenres)
                        .ThenInclude(gg => gg.Genre)
                .Where(g => g.UserId == targetUserId)
                .ToListAsync();

            if (!userGames.Any())
                return Enumerable.Empty<UserGenreStatsDto>();

            var totalGames = userGames.Count;

            return userGames
                .SelectMany(ug => ug.Game.GameGenres)
                .GroupBy(gg => gg.Genre.Name)
                .Select(group => new UserGenreStatsDto
                {
                    GenreName = group.Key,
                    GamesCount = group.Count(),
                    Percentage = Math.Round((double)group.Count() / totalGames * 100, 2),
                    Games = _mapper.Map<List<UserGameLibraryDto>>(
                        userGames.Where(ug =>
                            ug.Game.GameGenres.Any(gg =>
                                gg.Genre.Name == group.Key)))
                })
                .OrderByDescending(g => g.GamesCount)
                .ToList();
        }

        private IQueryable<UserGameLibrary> ApplySorting(
    IQueryable<UserGameLibrary> query,
    string? sortBy,
    bool ascending)
        {
            return sortBy?.ToLower() switch
            {
                "name" => ascending
                    ? query.OrderBy(g => g.Game.Name)
                    : query.OrderByDescending(g => g.Game.Name),

                "dateadded" => ascending
                    ? query.OrderBy(g => g.DateAdded)
                    : query.OrderByDescending(g => g.DateAdded),

                "globalrating" => ascending
                    ? query.OrderBy(g => g.Game.Rating)
                    : query.OrderByDescending(g => g.Game.Rating),

                "userrating" => ascending
                    ? query.OrderBy(g => g.UserRating)
                        .ThenBy(g => g.Game.Rating)
                    : query.OrderByDescending(g => g.UserRating)
                        .ThenByDescending(g => g.Game.Rating),

                _ => query.OrderByDescending(g => g.DateAdded)
            };
        }
    }
}