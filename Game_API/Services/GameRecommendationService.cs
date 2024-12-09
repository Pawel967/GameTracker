using Game_API.Data;
using Game_API.Dtos.Recommendation;
using Game_API.Models.IGDB;
using Game_API.Models.Library;
using Microsoft.EntityFrameworkCore;

namespace Game_API.Services
{
    public class GameRecommendationService : IGameRecommendationService
    {
        private readonly AppDbContext _context;
        private readonly IIGDBService _igdbService;
        private readonly ILogger<GameRecommendationService> _logger;

        // Algorithm configuration constants
        private const int MinimumGamesForRecommendation = 3;
        private const int DefaultRecommendationCount = 10;
        private const int MaxSimilarGamesPerFavorite = 3;
        private const int MaxFavoriteGames = 3;
        private const int MaxGenres = 3;
        private const int MaxGamesPerGenre = 3;

        // Scoring weights
        private const double RATING_WEIGHT = 0.35;
        private const double GENRE_WEIGHT = 0.4;
        private const double THEME_WEIGHT = 0.25;

        public GameRecommendationService(
            AppDbContext context,
            IIGDBService igdbService,
            ILogger<GameRecommendationService> logger)
        {
            _context = context;
            _igdbService = igdbService;
            _logger = logger;
        }

        public async Task<IEnumerable<GameRecommendationDto>> GetPersonalizedRecommendationsAsync(
            Guid userId,
            int count = DefaultRecommendationCount)
        {
            try
            {
                var userLibrary = await GetUserLibraryWithDetailsAsync(userId);

                if (userLibrary.Count < MinimumGamesForRecommendation)
                {
                    return await GetPopularGamesRecommendationsAsync(count);
                }

                var recommendations = new List<GameRecommendationDto>();
                var existingGameIds = userLibrary.Select(g => g.GameId).ToHashSet();

                var (genrePreferences, themePreferences) = AnalyzeUserPreferences(userLibrary);
                var favoriteGames = GetUserFavoriteGames(userLibrary);

                recommendations.AddRange(await GetSimilarGamesRecommendations(
                    favoriteGames, existingGameIds, genrePreferences, themePreferences));

                recommendations.AddRange(await GetGenreBasedRecommendations(
                    genrePreferences, existingGameIds, themePreferences));

                return ApplyDiversityFiltering(recommendations, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations for user {UserId}", userId);
                return await GetPopularGamesRecommendationsAsync(count);
            }
        }

        private async Task<List<UserGameLibrary>> GetUserLibraryWithDetailsAsync(Guid userId)
        {
            return await _context.UserGameLibraries
                .Include(ugl => ugl.Game)
                    .ThenInclude(g => g.GameGenres)
                        .ThenInclude(gg => gg.Genre)
                .Include(ugl => ugl.Game)
                    .ThenInclude(g => g.GameThemes)
                        .ThenInclude(gt => gt.Theme)
                .Where(ugl => ugl.UserId == userId)
                .ToListAsync();
        }

        private (Dictionary<string, double> genres, Dictionary<string, double> themes) AnalyzeUserPreferences(
            List<UserGameLibrary> userLibrary)
        {
            var genrePreferences = new Dictionary<string, double>();
            var themePreferences = new Dictionary<string, double>();

            foreach (var userGame in userLibrary)
            {
                double gameWeight = CalculateGameWeight(userGame);

                foreach (var genre in userGame.Game.GameGenres.Select(gg => gg.Genre.Name))
                {
                    if (!genrePreferences.ContainsKey(genre))
                        genrePreferences[genre] = 0;
                    genrePreferences[genre] += gameWeight;
                }

                foreach (var theme in userGame.Game.GameThemes.Select(gt => gt.Theme.Name))
                {
                    if (!themePreferences.ContainsKey(theme))
                        themePreferences[theme] = 0;
                    themePreferences[theme] += gameWeight;
                }
            }

            NormalizePreferences(genrePreferences);
            NormalizePreferences(themePreferences);

            return (genrePreferences, themePreferences);
        }

        private double CalculateGameWeight(UserGameLibrary userGame)
        {
            double weight = 1.0;

            if (userGame.IsFavorite)
                weight *= 1.5;

            if (userGame.UserRating.HasValue)
                weight *= (userGame.UserRating.Value / 10.0);

            return weight;
        }

        private void NormalizePreferences(Dictionary<string, double> preferences)
        {
            if (!preferences.Any()) return;

            double maxValue = preferences.Values.Max();
            if (maxValue > 0)
            {
                foreach (var key in preferences.Keys.ToList())
                {
                    preferences[key] /= maxValue;
                }
            }
        }

        private List<UserGameLibrary> GetUserFavoriteGames(List<UserGameLibrary> userLibrary)
        {
            return userLibrary
                .Where(g => g.IsFavorite || (g.UserRating ?? 0) >= 8)
                .OrderByDescending(g => g.UserRating ?? 0)
                .ThenBy(g => g.IsFavorite)
                .Take(MaxFavoriteGames)
                .ToList();
        }

        private async Task<List<GameRecommendationDto>> GetSimilarGamesRecommendations(
            List<UserGameLibrary> favoriteGames,
            HashSet<long> existingGameIds,
            Dictionary<string, double> genrePreferences,
            Dictionary<string, double> themePreferences)
        {
            var recommendations = new List<GameRecommendationDto>();

            foreach (var favoriteGame in favoriteGames)
            {
                var game = await _igdbService.GetGameByIdAsync(favoriteGame.GameId);
                if (game?.SimilarGames != null)
                {
                    foreach (var similarGameBasic in game.SimilarGames.Take(MaxSimilarGamesPerFavorite))
                    {
                        if (!existingGameIds.Contains(similarGameBasic.Id))
                        {
                            var similarGame = await _igdbService.GetGameByIdAsync(similarGameBasic.Id);
                            if (similarGame != null)
                            {
                                var score = CalculateRecommendationScore(
                                    similarGame,
                                    genrePreferences,
                                    themePreferences);

                                recommendations.Add(new GameRecommendationDto
                                {
                                    Game = similarGame,
                                    RecommendationReason = $"Because you enjoyed {game.Name}",
                                    Score = score
                                });
                            }
                        }
                    }
                }
            }

            return recommendations;
        }

        private async Task<List<GameRecommendationDto>> GetGenreBasedRecommendations(
            Dictionary<string, double> genrePreferences,
            HashSet<long> existingGameIds,
            Dictionary<string, double> themePreferences)
        {
            var recommendations = new List<GameRecommendationDto>();

            foreach (var genre in genrePreferences.OrderByDescending(g => g.Value).Take(MaxGenres))
            {
                var genreGames = await _igdbService.GetGamesByGenreAsync(genre.Key, 1, MaxGamesPerGenre);
                foreach (var game in genreGames.Items)
                {
                    if (!existingGameIds.Contains(game.Id))
                    {
                        var score = CalculateRecommendationScore(
                            game,
                            genrePreferences,
                            themePreferences);

                        recommendations.Add(new GameRecommendationDto
                        {
                            Game = game,
                            RecommendationReason = $"Based on your interest in {genre.Key} games",
                            Score = score
                        });
                    }
                }
            }

            return recommendations;
        }

        private double CalculateRecommendationScore(
            IGDBGame game,
            Dictionary<string, double> genrePreferences,
            Dictionary<string, double> themePreferences)
        {
            double ratingScore = game.Rating / 100;

            double genreScore = game.Genres
                .Where(g => genrePreferences.ContainsKey(g))
                .Select(g => genrePreferences[g])
                .DefaultIfEmpty(0)
                .Average();

            double themeScore = game.Themes
                .Where(t => themePreferences.ContainsKey(t))
                .Select(t => themePreferences[t])
                .DefaultIfEmpty(0)
                .Average();

            double score = (ratingScore * RATING_WEIGHT) +
                         (genreScore * GENRE_WEIGHT) +
                         (themeScore * THEME_WEIGHT);

            return Math.Min(score, 1.0);
        }

        private IEnumerable<GameRecommendationDto> ApplyDiversityFiltering(
            List<GameRecommendationDto> recommendations,
            int count)
        {
            var selectedGames = new List<GameRecommendationDto>();
            var selectedGenres = new HashSet<string>();

            var orderedRecommendations = recommendations
                .OrderByDescending(r => r.Score)
                .ToList();

            foreach (var recommendation in orderedRecommendations)
            {
                var gameGenres = recommendation.Game.Genres;

                if (gameGenres.Count(g => selectedGenres.Contains(g)) <= 2)
                {
                    selectedGames.Add(recommendation);

                    foreach (var genre in gameGenres)
                    {
                        selectedGenres.Add(genre);
                    }

                    if (selectedGames.Count >= count)
                        break;
                }
            }

            return selectedGames;
        }

        private async Task<IEnumerable<GameRecommendationDto>> GetPopularGamesRecommendationsAsync(int count)
        {
            var popularGames = await _igdbService.GetAllGamesAsync(1, count);

            return popularGames.Items.Select(game => new GameRecommendationDto
            {
                Game = game,
                RecommendationReason = "Popular among players",
                Score = game.Rating / 100
            });
        }
    }
}