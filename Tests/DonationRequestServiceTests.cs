using Moq;
using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using RestroPlate.Services;

namespace Tests;

public class DonationRequestServiceTests
{
    [Fact]
    public async Task CreateDonationRequest_WithValidRequest_ReturnsPendingRequestAndCallsRepository()
    {
        var createdRequest = new DonationRequest
        {
            DonationRequestId = 15,
            DistributionCenterUserId = 12,
            RequestedQuantity = 50,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            FoodType = "Rice",
            Unit = "kg"
        };

        var requestRepo = new Mock<IDonationRequestRepository>();
        requestRepo
            .Setup(r => r.CreateAsync(It.IsAny<DonationRequest>()))
            .ReturnsAsync(createdRequest);

        var service = new DonationRequestService(requestRepo.Object);

        var result = await service.CreateDonationRequestAsync(12, new CreateDonationRequestDto
        {
            FoodType = "Rice",
            RequestedQuantity = 50,
            Unit = "kg"
        });

        Assert.Equal(15, result.DonationRequestId);
        Assert.Equal("pending", result.Status);
        Assert.Equal(12, result.DistributionCenterUserId);
        Assert.Equal("Rice", result.FoodType);
        Assert.Equal(50, result.RequestedQuantity);

        requestRepo.Verify(r => r.CreateAsync(It.Is<DonationRequest>(dr =>
            dr.DistributionCenterUserId == 12 &&
            dr.RequestedQuantity == 50 &&
            dr.FoodType == "Rice" &&
            dr.Status == "pending")), Times.Once);
    }

    [Fact]
    public async Task CreateDonationRequest_WhenQuantityIsZeroOrLess_ThrowsArgumentException()
    {
        var requestRepo = new Mock<IDonationRequestRepository>();
        var service = new DonationRequestService(requestRepo.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateDonationRequestAsync(4, new CreateDonationRequestDto
            {
                FoodType = "Rice",
                RequestedQuantity = 0,
                Unit = "kg"
            }));

        Assert.Equal("Requested quantity must be greater than zero.", exception.Message);
        requestRepo.Verify(r => r.CreateAsync(It.IsAny<DonationRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetAvailableRequests_WithValidStatus_NormalizesStatus()
    {
        var requestRepo = new Mock<IDonationRequestRepository>();
        requestRepo.Setup(r => r.GetAvailableAsync("pending")).ReturnsAsync(new List<DonationRequest>());

        var service = new DonationRequestService(requestRepo.Object);
        var result = await service.GetAvailableRequestsAsync(" Pending ");

        Assert.Empty(result);
        requestRepo.Verify(r => r.GetAvailableAsync("pending"), Times.Once);
    }

    [Fact]
    public async Task GetOutgoingRequests_WithInvalidStatus_ThrowsArgumentException()
    {
        var requestRepo = new Mock<IDonationRequestRepository>();
        var service = new DonationRequestService(requestRepo.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetOutgoingRequestsAsync(7, "open"));

        Assert.Equal("Status must be one of: pending, completed.", exception.Message);
        requestRepo.Verify(r => r.GetByDistributionCenterUserIdAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    [Theory]
    [InlineData(10, 5, 5, "pending")]
    [InlineData(10, 10, 10, "completed")]
    [InlineData(10, 12, 12, "completed")]
    [InlineData(10, -5, 0, "pending")]
    public async Task UpdateDonationRequestQuantityAsync_CalculatesQuantityAndStatusCorrecty(decimal requested, decimal increment, decimal expectedDonated, string expectedStatus)
    {
        var existingRequest = new DonationRequest
        {
            DonationRequestId = 1,
            RequestedQuantity = requested,
            DonatedQuantity = 0,
            Status = "pending"
        };

        var requestRepo = new Mock<IDonationRequestRepository>();
        requestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRequest);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<DonationRequest>())).ReturnsAsync(true);

        var service = new DonationRequestService(requestRepo.Object);
        var result = await service.UpdateDonationRequestQuantityAsync(1, increment);

        Assert.Equal(expectedDonated, result.DonatedQuantity);
        Assert.Equal(expectedStatus, result.Status);
        requestRepo.Verify(r => r.UpdateAsync(It.Is<DonationRequest>(dr => 
            dr.DonatedQuantity == expectedDonated && 
            dr.Status == expectedStatus)), Times.Once);
    }

    [Fact]
    public async Task UpdateDonationRequestQuantityAsync_WhenDecreasingBelowRequested_ChangesCompletedToPending()
    {
        var existingRequest = new DonationRequest
        {
            DonationRequestId = 1,
            RequestedQuantity = 10,
            DonatedQuantity = 10,
            Status = "completed"
        };

        var requestRepo = new Mock<IDonationRequestRepository>();
        requestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRequest);
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<DonationRequest>())).ReturnsAsync(true);

        var service = new DonationRequestService(requestRepo.Object);
        var result = await service.UpdateDonationRequestQuantityAsync(1, -2);

        Assert.Equal(8, result.DonatedQuantity);
        Assert.Equal("pending", result.Status);
    }
}
