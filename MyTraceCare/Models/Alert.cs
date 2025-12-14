using MyTraceCare.Models;

namespace MyTraceCare.Models
{
    public class Alert
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string RiskLevel { get; set; } = "Low";
        public int SeverityRank { get; set; } = 0;

        // Frame index where alert occurred (for graph markers, playback jump)
        public int FrameIndex { get; set; }

        // Navigation properties
        public User User { get; set; } = default!;
        public ICollection<PatientComment> Comments { get; set; }
            = new List<PatientComment>();
    }
}
