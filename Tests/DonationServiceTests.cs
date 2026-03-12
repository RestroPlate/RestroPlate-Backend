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

    [Fact]
    public async Task GetUserDonations_WithValidStatusFilter_NormalizesStatusBeforeQueryingRepository()
    {
        // Arrange
        var mockRepo = new Mock<IDonationRepository>();
        mockRepo
            .Setup(r => r.GetByUserIdAsync(5, "requested"))
            .ReturnsAsync(new List<Donation>());

        var service = new DonationService(mockRepo.Object);

        // Act
        var result = await service.GetUserDonationsAsync(5, " Requested ");

        // Assert
        Assert.Empty(result);
        mockRepo.Verify(r => r.GetByUserIdAsync(5, "requested"), Times.Once);
    }

    [Fact]
    public async Task GetUserDonations_WithInvalidStatus_ThrowsArgumentException()
    {
        // Arrange
        var mockRepo = new Mock<IDonationRepository>();
        var service = new DonationService(mockRepo.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetUserDonationsAsync(5, "archived"));
        Assert.Equal("Status must be one of: available, requested, collected, completed.", exception.Message);
        mockRepo.Verify(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDonation_WithAvailableDonation_ReturnsUpdatedDonation()
    {
        // Arrange
        var existingDonation = new Donation
        {
            DonationId = 11,
            ProviderUserId = 4,
            FoodType = "Rice",
            Quantity = 5,
            Unit = "packs",
            ExpirationDate = DateTime.UtcNow.AddHours(2),
            PickupAddress = "Old address",
            AvailabilityTime = "1:00 PM - 2:00 PM",
            Status = "available",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var request = new UpdateDonationRequestDto
        {
            FoodType = "Cooked rice",
            Quantity = 8,
            Unit = "meals",
            ExpirationDate = DateTime.UtcNow.AddHours(4),
            PickupAddress = "New address",
            AvailabilityTime = "3:00 PM - 5:00 PM"
        };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(11, 4)).ReturnsAsync(existingDonation);
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Donation>())).ReturnsAsync(true);

        var service = new DonationService(mockRepo.Object);

        // Act
        var result = await service.UpdateDonationAsync(11, 4, request);

        // Assert
        Assert.Equal(11, result.DonationId);
        Assert.Equal("Cooked rice", result.FoodType);
        Assert.Equal(8, result.Quantity);
        Assert.Equal("meals", result.Unit);
        Assert.Equal("New address", result.PickupAddress);
        Assert.Equal("3:00 PM - 5:00 PM", result.AvailabilityTime);
        mockRepo.Verify(r => r.GetByIdAsync(11, 4), Times.Once);
        mockRepo.Verify(r => r.UpdateAsync(It.Is<Donation>(d =>
            d.DonationId == 11 &&
            d.ProviderUserId == 4 &&
            d.Status == "available" &&
            d.FoodType == "Cooked rice" &&
            d.Quantity == 8)), Times.Once);
    }

    [Fact]
    public async Task UpdateDonation_WithNonAvailableDonation_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingDonation = new Donation
        {
            DonationId = 12,
            ProviderUserId = 4,
            FoodType = "Bread",
            Quantity = 3,
            Unit = "boxes",
            ExpirationDate = DateTime.UtcNow.AddHours(2),
            PickupAddress = "Pickup point",
            AvailabilityTime = "2:00 PM - 4:00 PM",
            Status = "requested"
        };

        var request = new UpdateDonationRequestDto
        {
            FoodType = "Fresh bread",
            Quantity = 4,
            Unit = "boxes",
            ExpirationDate = DateTime.UtcNow.AddHours(3),
            PickupAddress = "Updated pickup point",
            AvailabilityTime = "3:00 PM - 4:00 PM"
        };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(12, 4)).ReturnsAsync(existingDonation);

        var service = new DonationService(mockRepo.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDonationAsync(12, 4, request));
        Assert.Equal("Only available donations can be updated.", exception.Message);
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Donation>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDonation_WhenDonationDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new UpdateDonationRequestDto
        {
            FoodType = "Soup",
            Quantity = 6,
            Unit = "cups",
            ExpirationDate = DateTime.UtcNow.AddHours(2),
            PickupAddress = "Center",
            AvailabilityTime = "5:00 PM - 6:00 PM"
        };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(99, 4)).ReturnsAsync((Donation?)null);

        var service = new DonationService(mockRepo.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateDonationAsync(99, 4, request));
        Assert.Equal("Donation not found.", exception.Message);
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Donation>()), Times.Never);
    }
}
