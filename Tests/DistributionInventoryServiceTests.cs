using Moq;
using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using RestroPlate.Services;

namespace Tests;

public class DistributionInventoryServiceTests
{
    private readonly Mock<IDistributionInventoryRepository> _inventoryRepo;
    private readonly Mock<IDonationRequestRepository> _donationRequestRepo;
    private readonly DistributionInventoryService _service;

    public DistributionInventoryServiceTests()
    {
        _inventoryRepo = new Mock<IDistributionInventoryRepository>();
        _donationRequestRepo = new Mock<IDonationRequestRepository>();
        _service = new DistributionInventoryService(_inventoryRepo.Object, _donationRequestRepo.Object);
    }

    [Fact]
    public async Task CollectDonationAsync_WithValidRequest_ReturnsCollectedInventory()
    {
        var donationRequest = new DonationRequest
        {
            DonationRequestId = 1,
            DistributionCenterUserId = 10,
            RequestedQuantity = 50,
            DonatedQuantity = 50,
            Status = "completed",
            FoodType = "Rice",
            Unit = "kg"
        };

        var upsertedInventory = new DistributionInventory
        {
            InventoryId = 1,
            DonationRequestId = 1,
            CollectedQuantity = 45,
            CollectionDate = DateTime.UtcNow
        };

        _donationRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(donationRequest);
        _inventoryRepo.Setup(r => r.UpsertAsync(It.IsAny<DistributionInventory>())).ReturnsAsync(upsertedInventory);
        _donationRequestRepo.Setup(r => r.UpdateAsync(It.IsAny<DonationRequest>())).ReturnsAsync(true);

        var result = await _service.CollectDonationAsync(1, new UpdateCollectedQuantityDto { CollectedQuantity = 45 });

        Assert.Equal(1, result.InventoryId);
        Assert.Equal(1, result.DonationRequestId);
        Assert.Equal(45, result.CollectedQuantity);
        Assert.Equal("collected", result.Status);

        _donationRequestRepo.Verify(r => r.UpdateAsync(It.Is<DonationRequest>(dr => dr.Status == "collected")), Times.Once);
    }

    [Fact]
    public async Task CollectDonationAsync_WithQuantityDifferentFromRequested_AllowsDifference()
    {
        var donationRequest = new DonationRequest
        {
            DonationRequestId = 2,
            DistributionCenterUserId = 10,
            RequestedQuantity = 100,
            DonatedQuantity = 100,
            Status = "completed",
            FoodType = "Bread",
            Unit = "loaves"
        };

        var upsertedInventory = new DistributionInventory
        {
            InventoryId = 2,
            DonationRequestId = 2,
            CollectedQuantity = 80,
            CollectionDate = DateTime.UtcNow
        };

        _donationRequestRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(donationRequest);
        _inventoryRepo.Setup(r => r.UpsertAsync(It.IsAny<DistributionInventory>())).ReturnsAsync(upsertedInventory);
        _donationRequestRepo.Setup(r => r.UpdateAsync(It.IsAny<DonationRequest>())).ReturnsAsync(true);

        var result = await _service.CollectDonationAsync(2, new UpdateCollectedQuantityDto { CollectedQuantity = 80 });

        Assert.Equal(80, result.CollectedQuantity);
        Assert.Equal("collected", result.Status);
    }

    [Fact]
    public async Task CollectDonationAsync_WhenRequestNotFound_ThrowsKeyNotFoundException()
    {
        _donationRequestRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((DonationRequest?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CollectDonationAsync(999, new UpdateCollectedQuantityDto { CollectedQuantity = 10 }));

        _inventoryRepo.Verify(r => r.UpsertAsync(It.IsAny<DistributionInventory>()), Times.Never);
    }

    [Fact]
    public async Task CollectDonationAsync_WithZeroQuantity_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CollectDonationAsync(1, new UpdateCollectedQuantityDto { CollectedQuantity = 0 }));

        Assert.Equal("Collected quantity must be greater than zero.", exception.Message);
        _donationRequestRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CollectDonationAsync_WithNegativeQuantity_ThrowsArgumentException()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CollectDonationAsync(1, new UpdateCollectedQuantityDto { CollectedQuantity = -5 }));

        Assert.Equal("Collected quantity must be greater than zero.", exception.Message);
    }

    [Fact]
    public async Task GetInventoryByDistributionCenterAsync_ReturnsInventoryWithStatuses()
    {
        var inventoryList = new List<DistributionInventory>
        {
            new() { InventoryId = 1, DonationRequestId = 1, CollectedQuantity = 45, CollectionDate = DateTime.UtcNow },
            new() { InventoryId = 2, DonationRequestId = 2, CollectedQuantity = 80, CollectionDate = DateTime.UtcNow }
        };

        _inventoryRepo.Setup(r => r.GetByDistributionCenterUserIdAsync(10)).ReturnsAsync(inventoryList);
        _donationRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new DonationRequest { DonationRequestId = 1, Status = "collected" });
        _donationRequestRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new DonationRequest { DonationRequestId = 2, Status = "collected" });

        var result = await _service.GetInventoryByDistributionCenterAsync(10);

        Assert.Equal(2, result.Count);
        Assert.Equal(45, result[0].CollectedQuantity);
        Assert.Equal("collected", result[0].Status);
        Assert.Equal(80, result[1].CollectedQuantity);
        Assert.Equal("collected", result[1].Status);
    }

    [Fact]
    public async Task GetInventoryByDistributionCenterAsync_WhenEmpty_ReturnsEmptyList()
    {
        _inventoryRepo.Setup(r => r.GetByDistributionCenterUserIdAsync(99))
            .ReturnsAsync(new List<DistributionInventory>());

        var result = await _service.GetInventoryByDistributionCenterAsync(99);

        Assert.Empty(result);
    }
}
