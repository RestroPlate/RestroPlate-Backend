using System.Data;
using Microsoft.Data.SqlClient;
using RestroPlate.IdentityService.Models;
using RestroPlate.IdentityService.Models.Interfaces;

namespace RestroPlate.IdentityService.Repository
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
                using var connection = CreateConnection();
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.ExecuteScalar();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> RegisterAsync(User user)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO dbo.users (name, email, password_hash, phone_number, user_type, address)
                OUTPUT INSERTED.user_id
                VALUES (@Name, @Email, @PasswordHash, @PhoneNumber, @UserType, @Address)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", user.Name);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@PhoneNumber", (object?)user.PhoneNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@UserType", user.UserType);
            command.Parameters.AddWithValue("@Address", (object?)user.Address ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return result is int id ? id : Convert.ToInt32(result);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT user_id, name, email, password_hash, phone_number, user_type, address, created_at
                FROM dbo.users
                WHERE email = @Email";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapUser(reader);

            return null;
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT user_id, name, email, password_hash, phone_number, user_type, address, created_at
                FROM dbo.users
                WHERE user_id = @UserId";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapUser(reader);

            return null;
        }

        private static User MapUser(SqlDataReader reader) => new User
        {
            UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
            Name = reader.IsDBNull(reader.GetOrdinal("name")) ? string.Empty : reader.GetString(reader.GetOrdinal("name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("phone_number")) ? string.Empty : reader.GetString(reader.GetOrdinal("phone_number")),
            UserType = reader.GetString(reader.GetOrdinal("user_type")),
            Address = reader.IsDBNull(reader.GetOrdinal("address")) ? string.Empty : reader.GetString(reader.GetOrdinal("address")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }
}