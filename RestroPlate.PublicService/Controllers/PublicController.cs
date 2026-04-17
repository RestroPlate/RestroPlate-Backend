using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestroPlate.PublicService.DTOs;
using RestroPlate.PublicService.Models;

namespace RestroPlate.PublicService.Controllers;

[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly ConnectionFactory _connectionFactory;

    public PublicController(ConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [HttpGet("centers-with-donations")]
    [ProducesResponseType(typeof(IEnumerable<CenterWithDonationsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCentersWithDonations()
    {
        const string sql = @"
            SELECT
                DonationId,
                CenterId,
                CenterName,
                CenterAddress,
                FoodType,
                Quantity,
                Unit,
                CollectedAt
            FROM PublishedDonationsView
            ORDER BY CenterId, CollectedAt DESC;";

        var rows = new List<PublishedDonationReadModelRow>();

        await using (var connection = _connectionFactory.CreateConnection())
        {
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new PublishedDonationReadModelRow
                {
                    DonationId = reader.GetInt32(0),
                    CenterId = reader.GetInt32(1),
                    CenterName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    CenterAddress = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    FoodType = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Quantity = reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader.GetDouble(5)),
                    Unit = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    CollectedAt = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7)
                });
            }
        }

        var response = rows
            .GroupBy(row => row.CenterId)
            .Select(group =>
            {
                var first = group.First();

                return new CenterWithDonationsDto
                {
                    CenterId = first.CenterId,
                    Name = first.CenterName,
                    Address = first.CenterAddress,
                    PhoneNumber = string.Empty,
                    PublishedDonations = group.Select(donation => new PublishedDonationDto
                    {
                        DonationId = donation.DonationId,
                        FoodType = donation.FoodType,
                        Quantity = donation.Quantity,
                        Unit = donation.Unit,
                        CollectedAt = donation.CollectedAt,
                        ExpirationDate = DateTime.MinValue
                    }).ToList()
                };
            })
            .ToList();

        return Ok(response);
    }
}