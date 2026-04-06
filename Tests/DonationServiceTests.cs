using Moq;
using RestroPlate.Models;
using RestroPlate.Models.DTOs;
using RestroPlate.Models.Interfaces;
using RestroPlate.Services;

namespace Tests;

public class DonationServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    // modified: constructor now requires IInventoryLogRepository; updated all factory calls
    private static DonationService BuildService(
        Mock<IDonationRepository>? donationRepo = null,
        Mock<IDonationRequestRepository>? requestRepo = null,
        Mock<IInventoryLogRepository>? inventoryRepo = null,
        Mock<IUserRepository>? userRepo = null)
    {
        return new DonationService(
            (donationRepo ?? new Mock<IDonationRepository>()).Object,
            (requestRepo ?? new Mock<IDonationRequestRepository>()).Object,
            (inventoryRepo ?? new Mock<IInventoryLogRepository>()).Object,
            (userRepo ?? new Mock<IUserRepository>()).Object);
    }

    // ── Flow 1: CreateDonation (standalone, no DonationRequestId) ────────────

    [Fact]
    public async Task CreateDonation_Flow1_WithValidRequest_ReturnsAvailableStatus()
    {
        // exists & correct — updated constructor call
        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Donation>())).ReturnsAsync(10);

        var service = BuildService(donationRepo: mockRepo);

        var request = new CreateDonationDto
        {
            FoodType = "Cooked rice",
            Quantity = 5,
            Unit = "kg",
            ExpirationDate = DateTime.UtcNow.AddHours(3),
            PickupAddress = "123 Main St, Colombo",
            AvailabilityTime = "2:00 PM - 4:00 PM"
        };

        var result = await service.CreateDonationAsync(2, request);

        Assert.Equal(10, result.DonationId);
        Assert.Equal(2, result.ProviderUserId);
        Assert.Equal("available", result.Status);
        mockRepo.Verify(r => r.CreateAsync(It.Is<Donation>(d => d.ProviderUserId == 2 && d.Status == "available")), Times.Once);
    }

    [Fact]
    public async Task CreateDonation_WithInvalidQuantity_ThrowsArgumentException()
    {
        // exists & correct — updated constructor call
        var service = BuildService();

        var request = new CreateDonationDto
        {
            FoodType = "Sandwich",
            Quantity = 0,
            Unit = "packs",
            ExpirationDate = DateTime.UtcNow.AddHours(4),
            PickupAddress = "45 Park Ave",
            AvailabilityTime = "10:00 AM - 11:00 AM"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDonationAsync(1, request));
    }

    [Fact]
    public async Task CreateDonation_WithPastExpiration_ThrowsArgumentException()
    {
        // exists & correct — updated constructor call
        var service = BuildService();

        var request = new CreateDonationDto
        {
            FoodType = "Bread",
            Quantity = 1,
            Unit = "box",
            ExpirationDate = DateTime.UtcNow.AddMinutes(-30),
            PickupAddress = "78 Lake Rd",
            AvailabilityTime = "Now"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDonationAsync(1, request));
    }

    // ── Flow 2: CreateDonation (fulfil a pending DC request) ─────────────────

    [Fact]
    public async Task CreateDonation_Flow2_FulfilsPendingRequest_StatusIsRequested()
    {
        // new — Flow 2 happy path
        var donReq = new DonationRequest
        {
            DonationRequestId = 5,
            RequestedQuantity = 10,
            DonatedQuantity = 0,
            Status = "pending",
            FoodType = "Rice",
            Unit = "kg"
        };

        var mockDonRepo = new Mock<IDonationRepository>();
        mockDonRepo.Setup(r => r.CreateAsync(It.IsAny<Donation>())).ReturnsAsync(20);

        var mockReqRepo = new Mock<IDonationRequestRepository>();
        mockReqRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(donReq);
        mockReqRepo.Setup(r => r.UpdateAsync(It.IsAny<DonationRequest>())).ReturnsAsync(true);

        var service = BuildService(donationRepo: mockDonRepo, requestRepo: mockReqRepo);

        var request = new CreateDonationDto
        {
            DonationRequestId = 5,
            FoodType = "Rice",
            Quantity = 4,
            Unit = "kg",
            ExpirationDate = DateTime.UtcNow.AddHours(6),
            PickupAddress = "Main Rd",
            AvailabilityTime = "Morning"
        };

        var result = await service.CreateDonationAsync(7, request);

        Assert.Equal(20, result.DonationId);
        Assert.Equal("requested", result.Status); // modified: Flow 2 must be 'requested'
        // DonationRequest should be updated: DonatedQuantity += 4, still pending
        mockReqRepo.Verify(r => r.UpdateAsync(It.Is<DonationRequest>(dr =>
            dr.DonationRequestId == 5 &&
            dr.DonatedQuantity == 4 &&
            dr.Status == "pending")), Times.Once);
    }

    [Fact]
    public async Task CreateDonation_Flow2_FullyFulfilsRequest_SetsRequestCompleted()
    {
        // new — Flow 2: completing a request
        var donReq = new DonationRequest
        {
            DonationRequestId = 6,
            RequestedQuantity = 5,
            DonatedQuantity = 0,
            Status = "pending",
            FoodType = "Rice",
            Unit = "kg"
        };

        var mockDonRepo = new Mock<IDonationRepository>();
        mockDonRepo.Setup(r => r.CreateAsync(It.IsAny<Donation>())).ReturnsAsync(21);

        var mockReqRepo = new Mock<IDonationRequestRepository>();
        mockReqRepo.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(donReq);
        mockReqRepo.Setup(r => r.UpdateAsync(It.IsAny<DonationRequest>())).ReturnsAsync(true);

        var service = BuildService(donationRepo: mockDonRepo, requestRepo: mockReqRepo);

        var request = new CreateDonationDto
        {
            DonationRequestId = 6,
            FoodType = "Rice",
            Quantity = 5,
            Unit = "kg",
            ExpirationDate = DateTime.UtcNow.AddHours(6),
            PickupAddress = "Main Rd",
            AvailabilityTime = "Morning"
        };

        await service.CreateDonationAsync(8, request);

        mockReqRepo.Verify(r => r.UpdateAsync(It.Is<DonationRequest>(dr =>
            dr.DonationRequestId == 6 &&
            dr.DonatedQuantity == 5 &&
            dr.Status == "completed")), Times.Once);
    }

    [Fact]
    public async Task CreateDonation_Flow2_RequestNotFound_ThrowsKeyNotFoundException()
    {
        // new — error case: linked request doesn't exist
        var mockReqRepo = new Mock<IDonationRequestRepository>();
        mockReqRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((DonationRequest?)null);

        var service = BuildService(requestRepo: mockReqRepo);

        var request = new CreateDonationDto
        {
            DonationRequestId = 999,
            FoodType = "Rice",
            Quantity = 2,
            Unit = "kg",
            ExpirationDate = DateTime.UtcNow.AddHours(4),
            PickupAddress = "Main Rd",
            AvailabilityTime = "Morning"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateDonationAsync(1, request));
    }

    [Fact]
    public async Task CreateDonation_Flow2_RequestNotPending_ThrowsInvalidOperationException()
    {
        // new — error case: request already completed
        var donReq = new DonationRequest
        {
            DonationRequestId = 7,
            RequestedQuantity = 5,
            DonatedQuantity = 5,
            Status = "completed"
        };

        var mockReqRepo = new Mock<IDonationRequestRepository>();
        mockReqRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(donReq);

        var service = BuildService(requestRepo: mockReqRepo);

        var request = new CreateDonationDto
        {
            DonationRequestId = 7,
            FoodType = "Rice",
            Quantity = 2,
            Unit = "kg",
            ExpirationDate = DateTime.UtcNow.AddHours(4),
            PickupAddress = "Main Rd",
            AvailabilityTime = "Morning"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateDonationAsync(1, request));
    }

    // ── Flow 1: RequestDonation (available → requested) ──────────────────────

    [Fact]
    public async Task RequestDonation_WhenAvailable_TransitionsToRequested()
    {
        // new — happy path
        var donation = new Donation { DonationId = 30, Status = "available" };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(donation);
        mockRepo.Setup(r => r.UpdateStatusAsync(30, "requested")).ReturnsAsync(true);

        var service = BuildService(donationRepo: mockRepo);

        var result = await service.RequestDonationAsync(30, distributionCenterUserId: 5);

        Assert.Equal("requested", result.Status);
        mockRepo.Verify(r => r.UpdateStatusAsync(30, "requested"), Times.Once);
    }

    [Fact]
    public async Task RequestDonation_WhenAlreadyRequested_ThrowsInvalidOperationException()
    {
        // new — invalid transition (409)
        var donation = new Donation { DonationId = 31, Status = "requested" };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(31)).ReturnsAsync(donation);

        var service = BuildService(donationRepo: mockRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RequestDonationAsync(31, 5));
        mockRepo.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RequestDonation_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // new — error case
        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Donation?)null);

        var service = BuildService(donationRepo: mockRepo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RequestDonationAsync(999, 5));
    }

    // ── Shared: CollectDonation (requested → collected) ───────────────────────

    [Fact]
    public async Task CollectDonation_WhenRequested_CreatesInventoryLogAndTransitionsToCollected()
    {
        // new — happy path (shared Flow 1 + Flow 2)
        var donation = new Donation { DonationId = 50, Status = "requested", DonationRequestId = null };

        var mockDonRepo = new Mock<IDonationRepository>();
        mockDonRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(donation);
        mockDonRepo.Setup(r => r.UpdateStatusAsync(50, "collected")).ReturnsAsync(true);

        var expectedLog = new InventoryLogResponseDto
        {
            InventoryLogId = 1,
            DonationId = 50,
            DistributionCenterUserId = 9,
            CollectedAmount = 3.5m,
            CollectedAt = DateTime.UtcNow
        };

        var mockInvRepo = new Mock<IInventoryLogRepository>();
        mockInvRepo.Setup(r => r.CreateAsync(It.IsAny<InventoryLog>())).ReturnsAsync(expectedLog);

        var service = BuildService(donationRepo: mockDonRepo, inventoryRepo: mockInvRepo);

        var result = await service.CollectDonationAsync(50, 9, new CollectDonationDto { CollectedAmount = 3.5m });

        Assert.Equal(1, result.InventoryLogId);
        Assert.Equal(3.5m, result.CollectedAmount);
        mockDonRepo.Verify(r => r.UpdateStatusAsync(50, "collected"), Times.Once);
        mockInvRepo.Verify(r => r.CreateAsync(It.Is<InventoryLog>(l =>
            l.DonationId == 50 &&
            l.DistributionCenterUserId == 9 &&
            l.CollectedAmount == 3.5m)), Times.Once);
    }

    [Fact]
    public async Task CollectDonation_WhenNotRequested_ThrowsInvalidOperationException()
    {
        // new — invalid transition: can't collect an 'available' donation (409)
        var donation = new Donation { DonationId = 51, Status = "available" };

        var mockDonRepo = new Mock<IDonationRepository>();
        mockDonRepo.Setup(r => r.GetByIdAsync(51)).ReturnsAsync(donation);

        var service = BuildService(donationRepo: mockDonRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CollectDonationAsync(51, 9, new CollectDonationDto { CollectedAmount = 2m }));
        mockDonRepo.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CollectDonation_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // new — error case
        var mockDonRepo = new Mock<IDonationRepository>();
        mockDonRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Donation?)null);

        var service = BuildService(donationRepo: mockDonRepo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CollectDonationAsync(999, 9, new CollectDonationDto { CollectedAmount = 2m }));
    }

    [Fact]
    public async Task CollectDonation_WithZeroAmount_ThrowsArgumentException()
    {
        // new — validation error
        var service = BuildService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CollectDonationAsync(1, 9, new CollectDonationDto { CollectedAmount = 0 }));
    }

    // ── GetUserDonations ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserDonations_ReturnsMappedDonationsForAuthenticatedUser()
    {
        // exists & correct — updated constructor call
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
        mockRepo.Setup(r => r.GetByUserIdAsync(3, null)).ReturnsAsync(donations);
        var service = BuildService(donationRepo: mockRepo);

        var result = await service.GetUserDonationsAsync(3);

        var donation = Assert.Single(result);
        Assert.Equal(7, donation.DonationId);
        Assert.Equal("available", donation.Status);
        mockRepo.Verify(r => r.GetByUserIdAsync(3, null), Times.Once);
    }

    [Fact]
    public async Task GetUserDonations_WithValidStatusFilter_NormalizesStatus()
    {
        // exists & correct — updated constructor call
        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByUserIdAsync(5, "requested")).ReturnsAsync(new List<Donation>());
        var service = BuildService(donationRepo: mockRepo);

        var result = await service.GetUserDonationsAsync(5, " Requested ");

        Assert.Empty(result);
        mockRepo.Verify(r => r.GetByUserIdAsync(5, "requested"), Times.Once);
    }

    [Fact]
    public async Task GetUserDonations_WithInvalidStatus_ThrowsArgumentException()
    {
        // modified: error message updated to match new AllowedStatuses set
        var service = BuildService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetUserDonationsAsync(5, "archived"));
        Assert.Equal("Status must be one of: available, requested, collected, completed.", exception.Message);
    }

    // ── GetCenterInventory ───────────────────────────────────────────────────
    
    [Fact]
    public async Task GetCenterInventory_ReturnsMappedDonationsForCenter()
    {
        var createdAt = DateTime.UtcNow.AddHours(-1);
        var donations = new List<Donation>
        {
            new()
            {
                DonationId = 8,
                ProviderUserId = 3,
                ClaimedByCenterUserId = 12,
                FoodType = "Apples",
                Quantity = 50,
                Unit = "kg",
                ExpirationDate = DateTime.UtcNow.AddHours(48),
                PickupAddress = "Farm",
                AvailabilityTime = "Morning",
                Status = "requested",
                CreatedAt = createdAt
            }
        };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetCenterInventoryAsync(12)).ReturnsAsync(donations);
        var service = BuildService(donationRepo: mockRepo);

        var result = await service.GetCenterInventoryAsync(12);

        var donation = Assert.Single(result);
        Assert.Equal(8, donation.DonationId);
        Assert.Equal("requested", donation.Status);
        Assert.Equal(12, donation.ClaimedByCenterUserId);
        mockRepo.Verify(r => r.GetCenterInventoryAsync(12), Times.Once);
    }

    // ── UpdateDonation ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDonation_WithAvailableDonation_ReturnsUpdatedDonation()
    {
        // exists & correct — updated constructor call
        var existingDonation = new Donation
        {
            DonationId = 11, ProviderUserId = 4, FoodType = "Rice", Quantity = 5,
            Unit = "packs", ExpirationDate = DateTime.UtcNow.AddHours(2),
            PickupAddress = "Old address", AvailabilityTime = "1:00 PM - 2:00 PM",
            Status = "available", CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var request = new UpdateDonationRequestDto
        {
            FoodType = "Cooked rice", Quantity = 8, Unit = "meals",
            ExpirationDate = DateTime.UtcNow.AddHours(4),
            PickupAddress = "New address", AvailabilityTime = "3:00 PM - 5:00 PM"
        };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(11, 4)).ReturnsAsync(existingDonation);
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Donation>())).ReturnsAsync(true);
        var service = BuildService(donationRepo: mockRepo);

        var result = await service.UpdateDonationAsync(11, 4, request);

        Assert.Equal("Cooked rice", result.FoodType);
        Assert.Equal(8, result.Quantity);
    }

    [Fact]
    public async Task UpdateDonation_WithNonAvailableDonation_ThrowsInvalidOperationException()
    {
        // exists & correct — updated constructor call
        var existingDonation = new Donation { DonationId = 12, ProviderUserId = 4, Status = "requested", FoodType = "x", Unit = "y", PickupAddress = "z", AvailabilityTime = "a", Quantity = 1, ExpirationDate = DateTime.UtcNow.AddHours(2) };
        var request = new UpdateDonationRequestDto { FoodType = "Fresh bread", Quantity = 4, Unit = "boxes", ExpirationDate = DateTime.UtcNow.AddHours(3), PickupAddress = "Updated", AvailabilityTime = "3:00 PM - 4:00 PM" };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(12, 4)).ReturnsAsync(existingDonation);
        var service = BuildService(donationRepo: mockRepo);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDonationAsync(12, 4, request));
        Assert.Equal("Only available donations can be updated.", exception.Message);
    }

    [Fact]
    public async Task UpdateDonation_WhenDonationDoesNotExist_ThrowsKeyNotFoundException()
    {
        // exists & correct — updated constructor call
        var request = new UpdateDonationRequestDto { FoodType = "Soup", Quantity = 6, Unit = "cups", ExpirationDate = DateTime.UtcNow.AddHours(2), PickupAddress = "Center", AvailabilityTime = "5:00 PM - 6:00 PM" };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(99, 4)).ReturnsAsync((Donation?)null);
        var service = BuildService(donationRepo: mockRepo);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateDonationAsync(99, 4, request));
        Assert.Equal("Donation not found.", exception.Message);
    }

    // ── DeleteDonation ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteDonation_WithAvailableDonation_DeletesDonation()
    {
        // exists & correct — updated constructor call
        var existingDonation = new Donation { DonationId = 15, ProviderUserId = 6, Status = "available", FoodType = "Rice", Unit = "packs", PickupAddress = "pp", AvailabilityTime = "1PM", Quantity = 5, ExpirationDate = DateTime.UtcNow.AddHours(2) };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(15, 6)).ReturnsAsync(existingDonation);
        mockRepo.Setup(r => r.DeleteAsync(15, 6)).ReturnsAsync(true);
        var service = BuildService(donationRepo: mockRepo);

        await service.DeleteDonationAsync(15, 6);

        mockRepo.Verify(r => r.DeleteAsync(15, 6), Times.Once);
    }

    [Fact]
    public async Task DeleteDonation_WithNonAvailableDonation_ThrowsInvalidOperationException()
    {
        // exists & correct — updated constructor call
        var existingDonation = new Donation { DonationId = 16, ProviderUserId = 6, Status = "requested", FoodType = "Bread", Unit = "boxes", PickupAddress = "pp", AvailabilityTime = "4PM", Quantity = 2, ExpirationDate = DateTime.UtcNow.AddHours(2) };

        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(16, 6)).ReturnsAsync(existingDonation);
        var service = BuildService(donationRepo: mockRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteDonationAsync(16, 6));
    }

    [Fact]
    public async Task DeleteDonation_WhenDonationDoesNotExist_ThrowsKeyNotFoundException()
    {
        // exists & correct — updated constructor call
        var mockRepo = new Mock<IDonationRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(17, 6)).ReturnsAsync((Donation?)null);
        var service = BuildService(donationRepo: mockRepo);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteDonationAsync(17, 6));
        Assert.Equal("Donation not found.", exception.Message);
    }

    // ── UpdateDistributedQuantity ───────────────────────────────────────────

    [Fact]
    public async Task UpdateDistributedQuantity_WhenValid_UpdatesQuantityAndCallsRepository()
    {
        // new — happy path
        var logId = 100;
        var centerUserId = 5;
        var donationId = 50;
        var log = new InventoryLog
        {
            InventoryLogId = logId,
            DonationId = donationId,
            DistributionCenterUserId = centerUserId,
            CollectedAmount = 10m,
            DistributedQuantity = 2m
        };

        var mockInvRepo = new Mock<IInventoryLogRepository>();
        mockInvRepo.Setup(r => r.GetByIdAsync(logId)).ReturnsAsync(log);
        mockInvRepo.Setup(r => r.UpdateDistributedQuantityAsync(logId, 3m)).Returns(Task.CompletedTask);

        var mockDonRepo = new Mock<IDonationRepository>();

        var service = BuildService(inventoryRepo: mockInvRepo, donationRepo: mockDonRepo);

        var result = await service.UpdateDistributedQuantityAsync(logId, centerUserId, 3m);

        Assert.Equal(5m, result.DistributedQuantity);
        mockInvRepo.Verify(r => r.UpdateDistributedQuantityAsync(logId, 3m), Times.Once);
        mockDonRepo.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDistributedQuantity_WhenFullyDistributed_UpdatesDonationStatus()
    {
        // new — edge case: fully distributed
        var logId = 101;
        var centerUserId = 5;
        var donationId = 51;
        var log = new InventoryLog
        {
            InventoryLogId = logId,
            DonationId = donationId,
            DistributionCenterUserId = centerUserId,
            CollectedAmount = 10m,
            DistributedQuantity = 7m
        };

        var mockInvRepo = new Mock<IInventoryLogRepository>();
        mockInvRepo.Setup(r => r.GetByIdAsync(logId)).ReturnsAsync(log);
        mockInvRepo.Setup(r => r.UpdateDistributedQuantityAsync(logId, 3m)).Returns(Task.CompletedTask);

        var mockDonRepo = new Mock<IDonationRepository>();
        mockDonRepo.Setup(r => r.UpdateStatusAsync(donationId, "completed")).ReturnsAsync(true);

        var service = BuildService(inventoryRepo: mockInvRepo, donationRepo: mockDonRepo);

        var result = await service.UpdateDistributedQuantityAsync(logId, centerUserId, 3m);

        Assert.Equal(10m, result.DistributedQuantity);
        mockDonRepo.Verify(r => r.UpdateStatusAsync(donationId, "completed"), Times.Once);
    }

    [Fact]
    public async Task UpdateDistributedQuantity_WhenExceedsCollected_ThrowsInvalidOperationException()
    {
        // new — validation error
        var logId = 102;
        var centerUserId = 5;
        var log = new InventoryLog
        {
            InventoryLogId = logId,
            DistributionCenterUserId = centerUserId,
            CollectedAmount = 10m,
            DistributedQuantity = 8m
        };

        var mockInvRepo = new Mock<IInventoryLogRepository>();
        mockInvRepo.Setup(r => r.GetByIdAsync(logId)).ReturnsAsync(log);

        var service = BuildService(inventoryRepo: mockInvRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateDistributedQuantityAsync(logId, centerUserId, 3m));
    }

    [Fact]
    public async Task UpdateDistributedQuantity_WhenNotOwner_ThrowsUnauthorizedAccessException()
    {
        // new — security check
        var logId = 103;
        var log = new InventoryLog
        {
            InventoryLogId = logId,
            DistributionCenterUserId = 99 // different owner
        };

        var mockInvRepo = new Mock<IInventoryLogRepository>();
        mockInvRepo.Setup(r => r.GetByIdAsync(logId)).ReturnsAsync(log);

        var service = BuildService(inventoryRepo: mockInvRepo);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateDistributedQuantityAsync(logId, 5, 2m));
    }

    [Fact]
    public async Task GetPublicCentersWithDonations_CallsRepository()
    {
        // new — verification for the new public endpoint logic
        var mockInvRepo = new Mock<IInventoryLogRepository>();
        var expectedData = new List<CenterWithDonationsDto>
        {
            new() { CenterId = 1, Name = "Center A" }
        };
        mockInvRepo.Setup(r => r.GetCentersWithPublishedDonationsAsync()).ReturnsAsync(expectedData);

        var service = BuildService(inventoryRepo: mockInvRepo);

        var result = await service.GetPublicCentersWithDonationsAsync();

        Assert.Equal(expectedData, result);
        mockInvRepo.Verify(r => r.GetCentersWithPublishedDonationsAsync(), Times.Once);
    }
}
