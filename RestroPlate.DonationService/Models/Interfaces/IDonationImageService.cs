using RestroPlate.DonationService.Models.DTOs;

namespace RestroPlate.DonationService.Models.Interfaces
{
    public interface IDonationImageService
    {
        Task<DonationImageDto> UploadImageAsync(
            int donationId,
            int providerUserId,
            Stream fileStream,
            string fileName,
            string contentType,
            long fileSize);

        Task<IReadOnlyList<DonationImageDto>> GetImagesAsync(int donationId);
        Task<bool> DeleteImageAsync(int imageId, int donationId, int providerUserId);
    }
}