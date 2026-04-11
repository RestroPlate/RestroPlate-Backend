namespace RestroPlate.Models.DTOs
{
    public class DonationImageDto
    {
        public int ImageId { get; set; }
        public int DonationId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
