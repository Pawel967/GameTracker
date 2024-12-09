
namespace Game_API.Dtos.Auth
{
    public class LoginResponseDto
    {
        public UserDto User { get; set; } = new();
        public string Token { get; set; } = string.Empty;
    }
}
