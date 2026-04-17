using Microsoft.Data.SqlClient;

namespace RestroPlate.PublicService.Models;

public class ConnectionFactory
{
    private readonly string _connectionString;

    public ConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PublicDb")
            ?? throw new InvalidOperationException("Connection string 'PublicDb' is not configured.");
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}