using Game_API.Dtos.Auth;

namespace Game_API.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto> GetCurrentUserAsync(Guid userId);
        Task<UserDto> UpdateCurrentUserAsync(Guid userId, UpdateUserDto updateUserDto);
    }
}