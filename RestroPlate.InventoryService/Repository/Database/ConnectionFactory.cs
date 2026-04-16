using System.Data;
using Microsoft.Data.SqlClient;
using RestroPlate.InventoryService.Models.Interfaces;

namespace RestroPlate.InventoryService.Repository.Database
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        public ConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("InventoryDb")
                ?? throw new InvalidOperationException("ConnectionStrings:InventoryDb is not configured.");
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}