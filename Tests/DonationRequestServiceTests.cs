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
        var donation = new Donation
        {
            DonationId = 8,
            ProviderUserId = 3,
            FoodType = "Cooked rice",
            Quantity = 10,
            Unit = "kg",
            Status = "available"
        };

        var createdRequest = new DonationRequest
        {
            DonationRequestId = 15,
            DonationId = 8,
            ProviderUserId = 3,
            DistributionCenterUserId = 12,
            RequestedQuantity = 6,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            FoodType = "Cooked rice",
            Unit = "kg"
        };

        var donationRepo = new Mock<IDonationRepository>();
        donationRepo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(donation);

        var requestRepo = new Mock<IDonationRequestRepository>();
        requestRepo
            .Setup(r => r.CreateAsync(It.IsAny<DonationRequest>()))
            .ReturnsAsync(createdRequest);

        var service = new DonationRequestService(donationRepo.Object, requestRepo.Object);

        var result = await service.CreateDonationRequestAsync(12, new CreateDonationRequestRequestDto
        {
            DonationId = 8,
            RequestedQuantity = 6
        });

        Assert.Equal(15, result.DonationRequestId);
        Assert.Equal("pending", result.Status);
        Assert.Equal(3, result.ProviderUserId);
        Assert.Equal(12, result.DistributionCenterUserId);
        requestRepo.Verify(r => r.CreateAsync(It.Is<DonationRequest>(dr =>
            dr.DonationId == 8 &&
            dr.ProviderUserId == 3 &&
            dr.DistributionCenterUserId == 12 &&
            dr.RequestedQuantity == 6 &&
            dr.Status == "pending")), Times.Once);
    }

    [Fact]
    public async Task CreateDonationRequest_WhenDonationIsMissing_ThrowsKeyNotFoundException()
    {
        var donationRepo = new Mock<IDonationRepository>();
        donationRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Donation?)null);

        var requestRepo = new Mock<IDonationRequestRepository>();
        var service = new DonationRequestService(donationRepo.Object, requestRepo.Object);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateDonationRequestAsync(4, new CreateDonationRequestRequestDto
            {
                DonationId = 99,
                RequestedQuantity = 2
            }));

        Assert.Equal("Donation not found.", exception.Message);
        requestRepo.Verify(r => r.CreateAsync(It.IsAny<DonationRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateDonationRequest_WhenDonationIsNotAvailable_ThrowsInvalidOperationException()
    {
        var donationRepo = new Mock<IDonationRepository>();
        donationRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new Donation
        {
            DonationId = 9,
            ProviderUserId = 2,
            Quantity = 10,
            Status = "requested"
        });

        var requestRepo = new Mock<IDonationRequestRepository>();
        var service = new DonationRequestService(donationRepo.Object, requestRepo.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateDonationRequestAsync(4, new CreateDonationRequestRequestDto
            {
                DonationId = 9,
                RequestedQuantity = 2
            }));

        Assert.Equal("Only available donations can be requested.", exception.Message);
        requestRepo.Verify(r => r.CreateAsync(It.IsAny<DonationRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateDonationRequest_WhenRequestedQuantityExceedsDonation_ThrowsArgumentException()
    {
        var donationRepo = new Mock<IDonationRepository>();
        donationRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(new Donation
        {
            DonationId = 11,
            ProviderUserId = 2,
            Quantity = 5,
            Status = "available"
        });

        var requestRepo = new Mock<IDonationRequestRepository>();
        var service = new DonationRequestService(donationRepo.Object, requestRepo.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateDonationRequestAsync(4, new CreateDonationRequestRequestDto
            {
                DonationId = 11,
                RequestedQuantity = 6
            }));

        Assert.Equal("Requested quantity cannot exceed available donation quantity.", exception.Message);
        requestRepo.Verify(r => r.CreateAsync(It.IsAny<DonationRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetProviderRequests_WithValidStatus_NormalizesStatus()
    {
        var donationRepo = new Mock<IDonationRepository>();
        var requestRepo = new Mock<IDonationRequestRepository>();
        requestRepo.Setup(r => r.GetByProviderUserIdAsync(7, "pending")).ReturnsAsync(new List<DonationRequest>());

        var service = new DonationRequestService(donationRepo.Object, requestRepo.Object);
        var result = await service.GetProviderRequestsAsync(7, " Pending ");

        Assert.Empty(result);
        requestRepo.Verify(r => r.GetByProviderUserIdAsync(7, "pending"), Times.Once);
    }

    [Fact]
    public async Task GetOutgoingRequests_WithInvalidStatus_ThrowsArgumentException()
    {
        var donationRepo = new Mock<IDonationRepository>();
        var requestRepo = new Mock<IDonationRequestRepository>();
        var service = new DonationRequestService(donationRepo.Object, requestRepo.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetOutgoingRequestsAsync(7, "open"));

        Assert.Equal("Status must be one of: pending, approved, rejected.", exception.Message);
        requestRepo.Verify(r => r.GetByDistributionCenterUserIdAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }
}
