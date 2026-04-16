using System.Data;
using RestroPlate.DonationService.Models.Interfaces;

namespace RestroPlate.DonationService.Repository
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