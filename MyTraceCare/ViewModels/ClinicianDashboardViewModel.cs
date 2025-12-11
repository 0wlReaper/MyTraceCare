using System;
using System.Collections.Generic;

namespace MyTraceCare.Models
{
   

    public class ClinicianPatientCardViewModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;

        public double LatestPeakPressureIndex { get; set; }
        public double LatestContactAreaPercent { get; set; }
        public string LatestRiskLevel { get; set; } = "Low";
        public int AlertCount { get; set; }

        public List<PatientMetricsPoint> History { get; set; } = new();
    }

    public class ClinicianDashboardViewModel
    {
        public List<ClinicianPatientCardViewModel> Patients { get; set; } = new();
    }
}
