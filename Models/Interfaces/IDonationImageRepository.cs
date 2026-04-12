using RestroPlate.Models.DTOs;

namespace RestroPlate.Models.Interfaces
{
    public interface IDonationImageRepository
    {
        Task<DonationImageDto> AddImageAsync(int donationId, string imageUrl, string fileName);
        Task<IReadOnlyList<DonationImageDto>> GetByDonationIdAsync(int donationId);
        Task<DonationImageDto?> GetByImageIdAsync(int imageId);
        Task<bool> DeleteImageAsync(int imageId, int donationId);
        Task DeleteAllByDonationIdAsync(int donationId);
    }
}
