using Game_API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AutoMapper;
using Game_API.Configuration;
using Game_API.Dtos.Auth;
using Game_API.Models.Auth;

namespace Game_API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            IOptions<JwtSettings> jwtSettings,
            IMapper mapper,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new Exception("Invalid username or password");
            }

            var token = GenerateJwtToken(user);
            return new LoginResponseDto
            {
                User = _mapper.Map<UserDto>(user),
                Token = token
            };
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            ValidateRegistration(registerDto);

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                IsProfilePrivate = false
            };

            var roleName = await _context.Users.AnyAsync() ? Role.RoleNames.User : Role.RoleNames.Admin;
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName)
                ?? throw new Exception($"Role {roleName} not found");

            user.Roles.Add(role);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new Exception("User not found");

            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> UpdateCurrentUserAsync(Guid userId, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new Exception("User not found");

            await UpdateUserProperties(user, updateUserDto);
            await _context.SaveChangesAsync();

            return _mapper.Map<UserDto>(user);
        }

        private async Task UpdateUserProperties(User user, UpdateUserDto updateUserDto)
        {
            if (updateUserDto.Username != null)
            {
                if (await _context.Users.AnyAsync(u => u.Username == updateUserDto.Username && u.Id != user.Id))
                    throw new Exception("Username already exists");
                user.Username = updateUserDto.Username;
            }

            if (updateUserDto.Email != null)
            {
                if (!IsValidEmail(updateUserDto.Email))
                    throw new ArgumentException("Invalid email format");
                if (await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email && u.Id != user.Id))
                    throw new Exception("Email already exists");
                user.Email = updateUserDto.Email;
            }

            if (!string.IsNullOrEmpty(updateUserDto.Password))
            {
                if (!IsStrongPassword(updateUserDto.Password))
                    throw new ArgumentException("Password does not meet complexity requirements");
                user.PasswordHash = HashPassword(updateUserDto.Password);
            }
        }

        private void ValidateRegistration(RegisterDto registerDto)
        {
            if (!IsValidEmail(registerDto.Email))
                throw new ArgumentException("Invalid email format");

            if (!IsStrongPassword(registerDto.Password))
                throw new ArgumentException("Password does not meet complexity requirements");

            if (_context.Users.Any(u => u.Username == registerDto.Username || u.Email == registerDto.Email))
                throw new Exception("Username or email already exists");
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            return storedHash == HashPassword(password);
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
    }
}
