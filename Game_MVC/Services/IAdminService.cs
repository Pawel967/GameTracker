using Game_MVC.Models.Admin;

namespace Game_MVC.Services
{
    public interface IAdminService
    {
        Task<IEnumerable<UserListViewModel>> GetAllUsersAsync();
        Task<UserListViewModel?> GetUserAsync(Guid userId);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<bool> AssignRoleAsync(Guid userId, string roleName);
        Task<IEnumerable<string>> GetAllRolesAsync();
        Task<bool> UpdateUserAsync(Guid userId, UpdateUserViewModel model);
    }
}
