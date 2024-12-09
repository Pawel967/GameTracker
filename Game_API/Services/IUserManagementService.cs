using Game_API.Dtos.Auth;

namespace Game_API.Services
{
    public interface IUserManagementService
    {
        Task<bool> DeleteUserAsync(Guid userId);
        Task<UserDto> AdminUpdateUserAsync(Guid userId, UpdateUserDto updateUserDto);
        Task<bool> AssignRoleAsync(Guid userId, string roleName);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<IEnumerable<string>> GetAllRolesAsync();
        Task<UserDto> GetUserByIdAsync(Guid userId);  // Added this method
    }
}
