using System.Data;

namespace RestroPlate.IdentityService.Models.Interfaces
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}