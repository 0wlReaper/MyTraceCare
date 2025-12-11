namespace MyTraceCare.Models
{
    public class PatientDataFile
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }

        public DateTime Date { get; set; }

        public string FilePath { get; set; } = string.Empty;

        // NEW → required for mapping device → patient
        public string DeviceId { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string? UploadedBy { get; set; }
    }
}
