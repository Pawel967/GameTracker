using Game_MVC.Models.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Game_MVC.Services;
using Game_MVC.Models.Admin;
using Microsoft.AspNetCore.Authorization;

namespace Game_MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var response = await _authService.LoginAsync(model);

                // Configure cookie expiration based on Remember Me
                var cookieExpiration = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(1);

                // JWT Token cookie
                var jwtCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = cookieExpiration
                };
                Response.Cookies.Append("JWTToken", response.Token, jwtCookieOptions);

                // Authentication cookie
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()),
            new Claim(ClaimTypes.Name, response.User.Username),
            new Claim(ClaimTypes.Email, response.User.Email)
        };

                claims.AddRange(response.User.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = cookieExpiration
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _authService.RegisterAsync(model);
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("JWTToken");
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public IActionResult Settings()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login");
                }

                var model = new UpdateMeViewModel
                {
                    Username = User.Identity?.Name,
                    Email = User.FindFirst(ClaimTypes.Email)?.Value
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user settings");
                TempData["Error"] = "Failed to load user settings.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(UpdateMeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var updatedUser = await _authService.UpdateCurrentUserAsync(model);

                // Update claims if username or email changed
                if (model.Username != User.Identity?.Name || model.Email != User.FindFirst(ClaimTypes.Email)?.Value)
                {
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", new { message = "Please login again with your updated credentials." });
                }

                TempData["Success"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }
    }
}
