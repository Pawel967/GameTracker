using Game_MVC.Models.Profile;
using Game_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Game_MVC.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IProfileService profileService, ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(Guid userId)
        {
            try
            {
                var profile = await _profileService.GetUserProfileAsync(userId);
                if (profile == null)
                {
                    TempData["Error"] = "Profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Don't set error message if profile was found
                return View(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for user {UserId}", userId);
                TempData["Error"] = "Failed to load profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> TogglePrivacy()
        {
            try
            {
                var result = await _profileService.ToggleProfilePrivacyAsync();
                if (result)
                {
                    TempData["Success"] = "Privacy settings updated successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to update privacy settings.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling profile privacy");
                TempData["Error"] = "Failed to update privacy settings.";
            }

            return RedirectToAction(nameof(Index), new { userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Follow(Guid userId)
        {
            try
            {
                var result = await _profileService.FollowUserAsync(userId);
                if (result)
                {
                    TempData["Success"] = "User followed successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to follow user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error following user {UserId}", userId);
                TempData["Error"] = "Failed to follow user.";
            }

            return RedirectToAction(nameof(Index), new { userId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Unfollow(Guid userId)
        {
            try
            {
                var result = await _profileService.UnfollowUserAsync(userId);
                if (result)
                {
                    TempData["Success"] = "User unfollowed successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to unfollow user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfollowing user {UserId}", userId);
                TempData["Error"] = "Failed to unfollow user.";
            }

            return RedirectToAction(nameof(Index), new { userId });
        }

        [HttpGet]
        public async Task<IActionResult> Followers(Guid userId)
        {
            try
            {
                var followers = await _profileService.GetFollowersAsync(userId);
                return View(followers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving followers for user {UserId}", userId);
                TempData["Error"] = "Failed to load followers.";
                return RedirectToAction(nameof(Index), new { userId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Following(Guid userId)
        {
            try
            {
                var following = await _profileService.GetFollowingAsync(userId);
                return View(following);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving following for user {UserId}", userId);
                TempData["Error"] = "Failed to load following users.";
                return RedirectToAction(nameof(Index), new { userId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return View(Enumerable.Empty<ProfileSearchViewModel>());
            }

            try
            {
                var results = await _profileService.SearchProfilesAsync(searchTerm);
                return View(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching profiles with term: {SearchTerm}", searchTerm);
                TempData["Error"] = "Failed to search profiles.";
                return View(Enumerable.Empty<ProfileSearchViewModel>());
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult MyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guidUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            return RedirectToAction(nameof(Index), new { userId = guidUserId });
        }
    }
}
