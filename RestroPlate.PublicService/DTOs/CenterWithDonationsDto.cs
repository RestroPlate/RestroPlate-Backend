using System.Collections.Generic;

namespace RestroPlate.PublicService.DTOs;

public class CenterWithDonationsDto
{
    public int CenterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<PublishedDonationDto> PublishedDonations { get; set; } = new();
}