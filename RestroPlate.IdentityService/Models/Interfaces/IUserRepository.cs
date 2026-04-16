using RestroPlate.IdentityService.Models;

namespace RestroPlate.IdentityService.Models.Interfaces
{
    public interface IUserRepository
    {
        bool TestConnection();
        Task<int> RegisterAsync(User user);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int userId);
    }
}