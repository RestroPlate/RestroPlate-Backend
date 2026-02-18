using System;
using RestroPlate.Models.Interfaces;
using Microsoft.Data.SqlClient; // SQL Server Driver

namespace RestroPlate.Repository
{
    public class UserRepository : BaseRepository, IUserRepository
    {
        public UserRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public bool TestConnection()
        {
            try
            {
                // 1. Create and Open Connection
                using var connection = CreateConnection(); // From BaseRepository

                connection.Open();

                // 2. Execute a simple command (Standard ADO.NET)
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1"; // Fast query to check connectivity
                command.ExecuteScalar();

                return true; // If we get here, it worked!
            }
            catch (Exception ex)
            {
                // Log the error if you have a logger
                Console.WriteLine($"Connection Failed: {ex.Message}");
                return false;
            }
        }
    }
}