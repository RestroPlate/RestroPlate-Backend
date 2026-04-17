namespace RestroPlate.EventContracts;

public record InventoryPublishedEvent(
    int DonationId,
    int CenterId,
    string CenterName,
    string CenterAddress,
    string FoodType,
    double Quantity,
    string Unit,
    DateTime CollectedAt
);