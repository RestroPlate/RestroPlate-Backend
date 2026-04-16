using RestroPlate.IdentityService.DTOs;

namespace RestroPlate.IdentityService.Models.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<UserProfileDto> GetProfileAsync(int userId);
    }
}