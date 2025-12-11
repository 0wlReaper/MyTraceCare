using System;

namespace MyTraceCare.Models
{
    public class PatientMetricsPoint
    {
        public DateTime Date { get; set; }
        public double PeakPressureIndex { get; set; }
        public double ContactAreaPercent { get; set; }
        public string RiskLevel { get; set; } = "Low";
    }
}
