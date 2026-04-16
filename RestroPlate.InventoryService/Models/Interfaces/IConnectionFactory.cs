using System.Data;

namespace RestroPlate.InventoryService.Models.Interfaces
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}