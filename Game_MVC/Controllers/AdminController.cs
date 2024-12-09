using Game_MVC.Models.Admin;
using Game_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _adminService.GetAllUsersAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                TempData["Error"] = "Failed to retrieve users.";
                return View(Enumerable.Empty<UserListViewModel>());
            }
        }

        public async Task<IActionResult> EditUser(Guid id)
        {
            try
            {
                var user = await _adminService.GetUserAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = new UpdateUserViewModel
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId} for edit", id);
                TempData["Error"] = "Failed to retrieve user details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(Guid id, UpdateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _adminService.UpdateUserAsync(id, model);
                if (result)
                {
                    TempData["Success"] = "User updated successfully.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Failed to update user.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                ModelState.AddModelError("", "Failed to update user.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var result = await _adminService.DeleteUserAsync(id);
                if (result)
                {
                    TempData["Success"] = "User deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to delete user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                TempData["Error"] = "Failed to delete user.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ManageRole(Guid id)
        {
            try
            {
                var user = await _adminService.GetUserAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                var roles = await _adminService.GetAllRolesAsync();
                var model = new AssignRoleViewModel
                {
                    UserId = user.Id,
                    RoleName = user.Roles.FirstOrDefault() ?? "",
                    AvailableRoles = roles.ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for user {UserId}", id);
                TempData["Error"] = "Failed to retrieve roles.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRole(AssignRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    model.AvailableRoles = (await _adminService.GetAllRolesAsync()).ToList();
                    return View(model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving roles for model validation");
                    TempData["Error"] = "Failed to validate role assignment.";
                    return RedirectToAction(nameof(Index));
                }
            }

            try
            {
                var result = await _adminService.AssignRoleAsync(model.UserId, model.RoleName);
                if (result)
                {
                    TempData["Success"] = "Role assigned successfully.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Failed to assign role.");
                model.AvailableRoles = (await _adminService.GetAllRolesAsync()).ToList();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role for user {UserId}", model.UserId);
                ModelState.AddModelError("", "Failed to assign role.");
                model.AvailableRoles = (await _adminService.GetAllRolesAsync()).ToList();
                return View(model);
            }
        }
    }
}
