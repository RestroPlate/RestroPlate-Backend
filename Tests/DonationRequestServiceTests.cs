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
}
