using Game_MVC.Models.Admin;
using Game_MVC.Models.Auth;

namespace Game_MVC.Services
{
    public interface IAuthService
    {
        Task<LoginResponseViewModel> LoginAsync(LoginViewModel model);
        Task<UserViewModel> RegisterAsync(RegisterViewModel model);
        Task<UserViewModel> UpdateCurrentUserAsync(UpdateMeViewModel model);
    }
}
