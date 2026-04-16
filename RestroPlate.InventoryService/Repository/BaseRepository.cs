using System.Data;
using RestroPlate.InventoryService.Models.Interfaces;

namespace RestroPlate.InventoryService.Repository
{
    public abstract class BaseRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        protected BaseRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        protected IDbConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
        }
    }
}