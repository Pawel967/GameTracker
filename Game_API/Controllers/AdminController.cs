using Game_API.Dtos.Auth;
using Game_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Game_API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUserManagementService userManagementService,
            ILogger<AdminController> logger)
        {
            _userManagementService = userManagementService;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            try
            {
                var users = await _userManagementService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        [HttpGet("users/{userId}")]
        public async Task<ActionResult<UserDto>> GetUser(Guid userId)
        {
            try
            {
                var user = await _userManagementService.GetUserByIdAsync(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user");
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        [HttpPut("users/{userId}")]
        public async Task<ActionResult<UserDto>> UpdateUser(Guid userId, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var updatedUser = await _userManagementService.AdminUpdateUserAsync(userId, updateUserDto);
                return Ok(updatedUser);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User not found");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, "An error occurred while updating the user");
            }
        }

        [HttpDelete("users/{userId}")]
        public async Task<ActionResult> DeleteUser(Guid userId)
        {
            try
            {
                var result = await _userManagementService.DeleteUserAsync(userId);
                return result ? Ok() : NotFound("User not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        [HttpPost("users/{userId}/role")]
        public async Task<ActionResult> AssignRole(Guid userId, [FromBody] string roleName)
        {
            try
            {
                var result = await _userManagementService.AssignRoleAsync(userId, roleName);
                return result ? Ok() : NotFound("User or role not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role");
                return StatusCode(500, "An error occurred while assigning the role");
            }
        }

        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<string>>> GetAllRoles()
        {
            try
            {
                var roles = await _userManagementService.GetAllRolesAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, "An error occurred while retrieving roles");
            }
        }
    }
}
