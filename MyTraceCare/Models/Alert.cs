using System;
using System.Collections.Generic;

namespace MyTraceCare.Models
{
    public class Alert
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        // NEW — required by your UI + logic
        public string RiskLevel { get; set; } = "Medium";  // Low, Medium, High

        // NEW — required for identifying frame in CSV dataset
        public int FrameIndex { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<PatientComment> Comments { get; set; } = new();
    }
}
