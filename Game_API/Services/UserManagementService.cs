using AutoMapper;
using Game_API.Data;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Game_API.Dtos.Auth;

namespace Game_API.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            AppDbContext context,
            IMapper mapper,
            ILogger<UserManagementService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new KeyNotFoundException("User not found");

            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserDto> AdminUpdateUserAsync(Guid userId, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new KeyNotFoundException("User not found");

            if (updateUserDto.Username != null)
            {
                if (await _context.Users.AnyAsync(u => u.Username == updateUserDto.Username && u.Id != userId))
                    throw new InvalidOperationException("Username already exists");
                user.Username = updateUserDto.Username;
            }

            if (updateUserDto.Email != null)
            {
                if (!IsValidEmail(updateUserDto.Email))
                    throw new ArgumentException("Invalid email format");
                if (await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email && u.Id != userId))
                    throw new InvalidOperationException("Email already exists");
                user.Email = updateUserDto.Email;
            }

            if (!string.IsNullOrEmpty(updateUserDto.Password))
            {
                if (!IsStrongPassword(updateUserDto.Password))
                    throw new ArgumentException("Password does not meet complexity requirements");
                user.PasswordHash = HashPassword(updateUserDto.Password);
            }

            await _context.SaveChangesAsync();
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> AssignRoleAsync(Guid userId, string roleName)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            var newRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName);

            if (newRole == null)
                return false;

            user.Roles.Clear();
            user.Roles.Add(newRole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.Roles)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<IEnumerable<string>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Select(r => r.Name)
                .ToListAsync();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsStrongPassword(string password)
        {
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$");
            return regex.IsMatch(password);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
