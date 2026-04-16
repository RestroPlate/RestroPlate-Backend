using System.Data;
using Microsoft.Data.SqlClient;
using RestroPlate.DonationService.Models.Interfaces;

namespace RestroPlate.DonationService.Repository.Database
{
    public class ConnectionFactory : IConnectionFactory
    {
        private readonly string _connectionString;

        public ConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DonationDb")
                ?? throw new InvalidOperationException("ConnectionStrings:DonationDb is not configured.");
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}