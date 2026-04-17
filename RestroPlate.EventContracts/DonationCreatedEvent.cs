namespace RestroPlate.EventContracts;

public record DonationCreatedEvent(
    int DonationId,
    int ProviderUserId,
    string FoodType,
    double Quantity,
    string Unit
);