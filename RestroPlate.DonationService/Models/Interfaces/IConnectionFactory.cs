using System.Data;

namespace RestroPlate.DonationService.Models.Interfaces
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}