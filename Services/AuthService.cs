using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        private static readonly HashSet<string> AllowedRoles =
            new(StringComparer.OrdinalIgnoreCase) { "DONOR", "DISTRIBUTION_CENTER" };

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration  = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            // Validate role before touching the DB
            var normalised = request.Role.Trim().ToUpperInvariant();
            if (!AllowedRoles.Contains(normalised))
                throw new ArgumentException($"Invalid role '{request.Role}'. Allowed: DONOR, DISTRIBUTION_CENTER.");

            // Ensure email is unique
            var existing = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
            if (existing is not null)
                throw new InvalidOperationException("email is already exists in the database");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Name         = request.Name.Trim(),
                Email        = request.Email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                UserType     = normalised,
                PhoneNumber  = request.PhoneNumber?.Trim() ?? string.Empty,
                Address      = request.Address?.Trim() ?? string.Empty
            };

            var userId = await _userRepository.RegisterAsync(user);

            return new AuthResponseDto
            {
                Token  = GenerateJwtToken(userId, user.Email, user.UserType),
                UserId = userId,
                Email  = user.Email,
                Role   = user.UserType
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user  = await _userRepository.GetByEmailAsync(email)
                        ?? throw new UnauthorizedAccessException("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            return new AuthResponseDto
            {
                Token  = GenerateJwtToken(user.UserId, user.Email, user.UserType),
                UserId = user.UserId,
                Email  = user.Email,
                Role   = user.UserType
            };
        }

        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new KeyNotFoundException($"User {userId} not found.");

            return new UserProfileDto
            {
                UserId      = user.UserId,
                Name        = user.Name,
                Email       = user.Email,
                Role        = user.UserType,
                PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber,
                Address     = string.IsNullOrWhiteSpace(user.Address)     ? null : user.Address,
                CreatedAt   = user.CreatedAt
            };
        }

        // ── Private helpers ────────────────────────────────────────────────────────

        private string GenerateJwtToken(int userId, string email, string role)
        {
            var secret   = _configuration["JWT:Secret"]
                           ?? throw new InvalidOperationException("JWT:Secret is not configured. Add JWT__Secret to .env for dev.");
            var issuer   = _configuration["JWT:Issuer"]   ?? "RestroPlate";
            var audience = _configuration["JWT:Audience"] ?? "RestroPlate";

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role,               role),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                expires:            DateTime.UtcNow.AddHours(24),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
