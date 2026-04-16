using System.Data;
using Microsoft.Data.SqlClient;
using RestroPlate.IdentityService.Models.Interfaces;

namespace RestroPlate.IdentityService.Repository.Database
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("IdentityDb")
                ?? _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string is not configured. Set ConnectionStrings:IdentityDb or ConnectionStrings:DefaultConnection.");
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}