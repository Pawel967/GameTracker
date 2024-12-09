using Game_API.Dtos.Profile;
using Game_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Game_API.Controllers
{
    [ApiController]
    [Route("api/profiles")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _profileService;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(
            IUserProfileService profileService,
            ILogger<UserProfileController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile(Guid userId)
        {
            try
            {
                Guid? requestingUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    requestingUserId = GetUserId();
                }

                var profile = await _profileService.GetUserProfileAsync(userId, requestingUserId);
                return Ok(profile);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Profile not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile");
                return StatusCode(500, "An error occurred while retrieving the profile");
            }
        }

        [Authorize]
        [HttpPost("privacy")]
        public async Task<ActionResult> ToggleProfilePrivacy()
        {
            try
            {
                var userId = GetUserId();
                var result = await _profileService.ToggleProfilePrivacyAsync(userId);
                return result ? Ok() : NotFound("Profile not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling privacy");
                return StatusCode(500, "An error occurred while updating privacy settings");
            }
        }

        [Authorize]
        [HttpPost("follow/{userId}")]
        public async Task<ActionResult> FollowUser(Guid userId)
        {
            try
            {
                var followerId = GetUserId();
                var result = await _profileService.FollowUserAsync(followerId, userId);
                return result ? Ok() : BadRequest("Unable to follow user");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error following user");
                return StatusCode(500, "An error occurred while following the user");
            }
        }

        [Authorize]
        [HttpDelete("follow/{userId}")]
        public async Task<ActionResult> UnfollowUser(Guid userId)
        {
            try
            {
                var followerId = GetUserId();
                var result = await _profileService.UnfollowUserAsync(followerId, userId);
                return result ? Ok() : NotFound("Following relationship not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfollowing user");
                return StatusCode(500, "An error occurred while unfollowing the user");
            }
        }

        [HttpGet("{userId}/followers")]
        public async Task<ActionResult<IEnumerable<FollowedUserDto>>> GetFollowers(Guid userId)
        {
            try
            {
                var followers = await _profileService.GetFollowersAsync(userId);
                return Ok(followers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving followers");
                return StatusCode(500, "An error occurred while retrieving followers");
            }
        }

        [HttpGet("{userId}/following")]
        public async Task<ActionResult<IEnumerable<FollowedUserDto>>> GetFollowing(Guid userId)
        {
            try
            {
                var following = await _profileService.GetFollowingAsync(userId);
                return Ok(following);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving following users");
                return StatusCode(500, "An error occurred while retrieving following users");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProfileSearchDto>>> SearchProfiles(
            [FromQuery] string searchTerm,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest("Search term is required");

                Guid? requestingUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    requestingUserId = GetUserId();
                }

                var profiles = await _profileService.SearchProfilesAsync(searchTerm, requestingUserId, limit);
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching profiles");
                return StatusCode(500, "An error occurred while searching profiles");
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
