using Microsoft.Data.SqlClient;
using RestroPlate.DonationService.Models.DTOs;
using RestroPlate.DonationService.Models.Interfaces;

namespace RestroPlate.DonationService.Repository
{
    public class DonationImageRepository : BaseRepository, IDonationImageRepository
    {
        public DonationImageRepository(IConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<DonationImageDto> AddImageAsync(int donationId, string imageUrl, string fileName)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO dbo.donation_images (donation_id, image_url, file_name)
                OUTPUT INSERTED.image_id, INSERTED.donation_id, INSERTED.image_url,
                       INSERTED.file_name, INSERTED.uploaded_at
                VALUES (@DonationId, @ImageUrl, @FileName);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", donationId);
            command.Parameters.AddWithValue("@ImageUrl", imageUrl);
            command.Parameters.AddWithValue("@FileName", fileName);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return MapImage(reader);
        }

        public async Task<IReadOnlyList<DonationImageDto>> GetByDonationIdAsync(int donationId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT image_id, donation_id, image_url, file_name, uploaded_at
                FROM dbo.donation_images
                WHERE donation_id = @DonationId
                ORDER BY uploaded_at ASC;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", donationId);

            var images = new List<DonationImageDto>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                images.Add(MapImage(reader));

            return images;
        }

        public async Task<DonationImageDto?> GetByImageIdAsync(int imageId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                SELECT image_id, donation_id, image_url, file_name, uploaded_at
                FROM dbo.donation_images
                WHERE image_id = @ImageId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ImageId", imageId);

            using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return MapImage(reader);
        }

        public async Task<bool> DeleteImageAsync(int imageId, int donationId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = @"
                DELETE FROM dbo.donation_images
                WHERE image_id = @ImageId AND donation_id = @DonationId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ImageId", imageId);
            command.Parameters.AddWithValue("@DonationId", donationId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task DeleteAllByDonationIdAsync(int donationId)
        {
            using var connection = (SqlConnection)CreateConnection();
            await connection.OpenAsync();

            const string sql = "DELETE FROM dbo.donation_images WHERE donation_id = @DonationId;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DonationId", donationId);
            await command.ExecuteNonQueryAsync();
        }

        private static DonationImageDto MapImage(SqlDataReader reader) => new()
        {
            ImageId = reader.GetInt32(reader.GetOrdinal("image_id")),
            DonationId = reader.GetInt32(reader.GetOrdinal("donation_id")),
            ImageUrl = reader.GetString(reader.GetOrdinal("image_url")),
            FileName = reader.GetString(reader.GetOrdinal("file_name")),
            UploadedAt = reader.GetDateTime(reader.GetOrdinal("uploaded_at"))
        };
    }
}