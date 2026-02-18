using System.Data;
using Microsoft.Data.SqlClient; // This is crucial for SQL Server
using Microsoft.Extensions.Configuration;
using RestroPlate.Models.Interfaces; // Assuming you have IConnectionFactory here

namespace RestroPlate.Repository.Database
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        public ConnectionFactory(IConfiguration configuration)
        {
            // We will configure this in appsettings.json later
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}