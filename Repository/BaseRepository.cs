using System.Data;
using RestroPlate.Models.Interfaces;

namespace RestroPlate.Repository
{
    public abstract class BaseRepository
    {
        private readonly IConnectionFactory _connectionFactory;

        protected BaseRepository(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // Protected means only child repositories (like UserRepository) can use this
        protected IDbConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
        }
    }
}