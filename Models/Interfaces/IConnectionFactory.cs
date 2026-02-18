using System.Data;

namespace RestroPlate.Models.Interfaces
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}