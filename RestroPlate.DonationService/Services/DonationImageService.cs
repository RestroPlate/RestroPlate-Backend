using Microsoft.AspNetCore.Hosting;
using RestroPlate.DonationService.Models.DTOs;
using RestroPlate.DonationService.Models.Interfaces;

namespace RestroPlate.DonationService.Services
{
    public class DonationImageService : IDonationImageService
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/jpg", "image/png" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private readonly IDonationImageRepository _imageRepository;
        private readonly IDonationRepository _donationRepository;
        private readonly IWebHostEnvironment _environment;

        public DonationImageService(
            IDonationImageRepository imageRepository,
            IDonationRepository donationRepository,
            IWebHostEnvironment environment)
        {
            _imageRepository = imageRepository;
            _donationRepository = donationRepository;
            _environment = environment;
        }

        public async Task<DonationImageDto> UploadImageAsync(
            int donationId,
            int providerUserId,
            Stream fileStream,
            string fileName,
            string contentType,
            long fileSize)
        {
            var donation = await _donationRepository.GetByIdAsync(donationId, providerUserId);
            if (donation is null)
                throw new KeyNotFoundException("Donation not found or you do not have permission to modify it.");

            if (!string.Equals(donation.Status, "available", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Images can only be added to donations in 'available' status.");

            if (fileStream == null || fileSize == 0)
                throw new ArgumentException("No file provided.");

            if (fileSize > MaxFileSizeBytes)
                throw new ArgumentException("File size exceeds the maximum allowed size of 5MB.");

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException("Unsupported file type. Only JPG, JPEG, and PNG are allowed.");

            if (!AllowedMimeTypes.Contains(contentType.ToLowerInvariant()))
                throw new ArgumentException("Unsupported file type. Only JPG, JPEG, and PNG are allowed.");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "donations");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(stream);
            }

            var imageUrl = $"/uploads/donations/{uniqueFileName}";
            return await _imageRepository.AddImageAsync(donationId, imageUrl, fileName);
        }

        public async Task<IReadOnlyList<DonationImageDto>> GetImagesAsync(int donationId)
        {
            return await _imageRepository.GetByDonationIdAsync(donationId);
        }

        public async Task<bool> DeleteImageAsync(int imageId, int donationId, int providerUserId)
        {
            var donation = await _donationRepository.GetByIdAsync(donationId, providerUserId);
            if (donation is null)
                throw new KeyNotFoundException("Donation not found or you do not have permission to modify it.");

            if (!string.Equals(donation.Status, "available", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Images can only be removed from donations in 'available' status.");

            var image = await _imageRepository.GetByImageIdAsync(imageId);
            if (image is null)
                throw new KeyNotFoundException("Image not found.");

            var deleted = await _imageRepository.DeleteImageAsync(imageId, donationId);

            if (deleted)
            {
                var filePath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            return deleted;
        }
    }
}