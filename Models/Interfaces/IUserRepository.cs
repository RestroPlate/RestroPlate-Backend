using RestroPlate.Models;

namespace RestroPlate.Models.Interfaces
{
    public interface IUserRepository
    {
        bool TestConnection();
        Task<int> RegisterAsync(User user);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int userId);
    }
}