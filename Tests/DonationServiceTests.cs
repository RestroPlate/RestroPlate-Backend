using Moq;
using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using RestroPlate.Services;

namespace Tests;

public class DonationServiceTests
{
    [Fact]
    public async Task CreateDonation_WithValidRequest_ReturnsCreatedDonationWithAvailableStatus()
    {
        // Arrange
        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Donation>())).ReturnsAsync(10);

        var service = new DonationService(mockRepo.Object);

        var request = new CreateDonationRequestDto
        {
            FoodType = "Cooked rice",
            Quantity = 5,
            Unit = "kg",
            ExpirationDate = DateTime.UtcNow.AddHours(3),
            PickupAddress = "123 Main St, Colombo",
            AvailabilityTime = "2:00 PM - 4:00 PM"
        };

        // Act
        var result = await service.CreateDonationAsync(2, request);

        // Assert
        Assert.Equal(10, result.DonationId);
        Assert.Equal(2, result.ProviderUserId);
        Assert.Equal("available", result.Status);
        mockRepo.Verify(r => r.CreateAsync(It.Is<Donation>(d => d.ProviderUserId == 2 && d.Status == "available")), Times.Once);
    }

    [Fact]
    public async Task CreateDonation_WithInvalidQuantity_ThrowsArgumentException()
    {
        // Arrange
        var mockRepo = new Mock<IDonationRepository>();
        var service = new DonationService(mockRepo.Object);

        var request = new CreateDonationRequestDto
        {
            FoodType = "Sandwich",
            Quantity = 0,
            Unit = "packs",
            ExpirationDate = DateTime.UtcNow.AddHours(4),
            PickupAddress = "45 Park Ave",
            AvailabilityTime = "10:00 AM - 11:00 AM"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDonationAsync(1, request));
        mockRepo.Verify(r => r.CreateAsync(It.IsAny<Donation>()), Times.Never);
    }

    [Fact]
    public async Task CreateDonation_WithPastExpiration_ThrowsArgumentException()
    {
        // Arrange
        var mockRepo = new Mock<IDonationRepository>();
        var service = new DonationService(mockRepo.Object);

        var request = new CreateDonationRequestDto
        {
            FoodType = "Bread",
            Quantity = 1,
            Unit = "box",
            ExpirationDate = DateTime.UtcNow.AddMinutes(-30),
            PickupAddress = "78 Lake Rd",
            AvailabilityTime = "Now"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDonationAsync(1, request));
        mockRepo.Verify(r => r.CreateAsync(It.IsAny<Donation>()), Times.Never);
    }

    [Fact]
    public async Task GetUserDonations_ReturnsMappedDonationsForAuthenticatedUser()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddHours(-1);
        var donations = new List<Donation>
        {
            new()
            {
                DonationId = 7,
                ProviderUserId = 3,
                FoodType = "Vegetable curry",
                Quantity = 12,
                Unit = "meals",
                ExpirationDate = DateTime.UtcNow.AddHours(2),
                PickupAddress = "12 Temple Road",
                AvailabilityTime = "6:00 PM - 7:00 PM",
                Status = "available",
                CreatedAt = createdAt
            }
        };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByUserIdAsync(3)).ReturnsAsync(donations);

        var service = new DonationService(mockRepo.Object);

        // Act
        var result = await service.GetUserDonationsAsync(3);

        // Assert
        var donation = Assert.Single(result);
        Assert.Equal(7, donation.DonationId);
        Assert.Equal(3, donation.ProviderUserId);
        Assert.Equal("Vegetable curry", donation.FoodType);
        Assert.Equal("available", donation.Status);
        Assert.Equal(createdAt, donation.CreatedAt);
        mockRepo.Verify(r => r.GetByUserIdAsync(3), Times.Once);
    }
}
