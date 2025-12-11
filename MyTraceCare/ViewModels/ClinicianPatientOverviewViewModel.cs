using MyTraceCare.Models;

namespace MyTraceCare.ViewModels
{
    public class ClinicianPatientOverviewViewModel
    {
        public User Patient { get; set; } = null!;
        public List<PatientDataFile> DataFiles { get; set; } = new();
        public HeatmapData? LatestHeatmap { get; set; }
        public MetricsResult? LatestMetrics { get; set; }
    }
}
