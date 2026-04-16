using System.Data;
using RestroPlate.IdentityService.Models.Interfaces;

namespace RestroPlate.IdentityService.Repository
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