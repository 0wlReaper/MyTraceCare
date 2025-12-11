namespace MyTraceCare.Models.ViewModels
{
    public class ClinicianPatientDetailsViewModel
    {
        public User Patient { get; set; } = null!;
        public DateTime SelectedDate { get; set; }
        public double[,] Matrix { get; set; } = new double[32, 32];

        public double PeakPressure { get; set; }
        public double PeakPressureIndex { get; set; }
        public double ContactAreaPercent { get; set; }
        public string RiskLevel { get; set; } = "Low";

        public int FrameIndex { get; set; }
        public int TotalFrames { get; set; }

        public List<DateTime> AvailableDates { get; set; } = new();
        public string PeakHistoryJson { get; set; } = "[]";

        public List<Alert> Alerts { get; set; } = new();
        public List<PatientComment> Comments { get; set; } = new();
    }
}
