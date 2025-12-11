using System;

namespace MyTraceCare.Models
{
    public class HeatmapData
    {
        public DateTime Date { get; set; }

        // 32x32 matrix for current frame
        public double[,] Matrix { get; set; } = new double[32, 32];

        // Case-study metrics
        public double PeakPressure { get; set; }
        public double PeakPressureIndex { get; set; }
        public double ContactAreaPercent { get; set; }
        public string RiskLevel { get; set; } = "Low";

        // For playback & slider
        public int FrameIndex { get; set; }
        public int TotalFrames { get; set; }
    }
}
