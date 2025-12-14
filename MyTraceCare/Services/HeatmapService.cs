using System.Globalization;

namespace MyTraceCare.Models
{
    public class MetricsResult
    {
        public double PeakPressure { get; set; }
        public double PeakPressureIndex { get; set; }
        public double ContactAreaPercent { get; set; }
        public string RiskLevel { get; set; } = "Low";
    }

    public class HeatmapService
    {
        private sealed class FrameData
        {
            public double[,] Matrix { get; set; } = default!;
            public MetricsResult Metrics { get; set; } = default!;
        }

        private sealed class CachedFile
        {
            public string Path { get; set; } = string.Empty;
            public DateTime LastWriteUtc { get; set; }
            public FrameData[] Frames { get; set; } = Array.Empty<FrameData>();
            public int FrameCount => Frames.Length;
        }

        private readonly object _cacheLock = new();
        private readonly Dictionary<string, CachedFile> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        // ---------------- PUBLIC API ----------------

        public int GetTotalFrames(string path)
        {
            return GetOrLoadFile(path).FrameCount;
        }

        public double[,] LoadFrame(string path, int frameIndex)
        {
            var cf = GetOrLoadFile(path);
            frameIndex = Math.Clamp(frameIndex, 0, cf.FrameCount - 1);
            return cf.Frames[frameIndex].Matrix;
        }

        public MetricsResult GetFrameMetrics(string path, int frameIndex)
        {
            var cf = GetOrLoadFile(path);
            frameIndex = Math.Clamp(frameIndex, 0, cf.FrameCount - 1);
            return cf.Frames[frameIndex].Metrics;
        }

        public double[] GetPeakHistory(string path, int maxFrames)
        {
            var cf = GetOrLoadFile(path);
            int n = Math.Min(maxFrames, cf.FrameCount);
            var result = new double[n];

            for (int i = 0; i < n; i++)
                result[i] = cf.Frames[i].Metrics.PeakPressureIndex;

            return result;
        }

        // ⭐ NEW: highest risk up to a frame
        public (string riskLevel, int frameIndex, MetricsResult metrics)
            GetMaxRiskUpToFrame(string path, int frameIndex)
        {
            var cf = GetOrLoadFile(path);
            frameIndex = Math.Clamp(frameIndex, 0, cf.FrameCount - 1);

            int bestRank = -1;
            int bestFrame = 0;
            MetricsResult bestMetrics = cf.Frames[0].Metrics;

            for (int i = 0; i <= frameIndex; i++)
            {
                var m = cf.Frames[i].Metrics;
                int rank = RiskRank(m.RiskLevel);

                if (rank > bestRank)
                {
                    bestRank = rank;
                    bestFrame = i;
                    bestMetrics = m;

                    if (rank == 2) break;
                }
            }

            return (bestMetrics.RiskLevel, bestFrame, bestMetrics);
        }

        // ---------------- METRICS ----------------

        public MetricsResult GetMetrics(
            double[,] matrix,
            double lowerThreshold = 5.0,
            int minClusterSize = 10)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            bool[,] visited = new bool[rows, cols];

            double globalPpi = 0.0;
            int contactCount = 0;
            int total = rows * cols;

            int[] dr = { -1, 1, 0, 0 };
            int[] dc = { 0, 0, -1, 1 };

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double v = matrix[r, c];

                    if (v >= lowerThreshold)
                        contactCount++;

                    if (visited[r, c] || v < lowerThreshold)
                        continue;

                    int clusterSize = 0;
                    double clusterMax = 0.0;
                    var queue = new Queue<(int, int)>();

                    visited[r, c] = true;
                    queue.Enqueue((r, c));

                    while (queue.Count > 0)
                    {
                        var (cr, cc) = queue.Dequeue();
                        clusterSize++;

                        double cv = matrix[cr, cc];
                        if (cv > clusterMax) clusterMax = cv;

                        for (int k = 0; k < 4; k++)
                        {
                            int nr = cr + dr[k];
                            int nc = cc + dc[k];

                            if (nr < 0 || nr >= rows || nc < 0 || nc >= cols)
                                continue;
                            if (visited[nr, nc] || matrix[nr, nc] < lowerThreshold)
                                continue;

                            visited[nr, nc] = true;
                            queue.Enqueue((nr, nc));
                        }
                    }

                    if (clusterSize >= minClusterSize && clusterMax > globalPpi)
                        globalPpi = clusterMax;
                }
            }

            double contactAreaPercent =
                total > 0 ? (contactCount / (double)total) * 100 : 0;

            double rawPeak = GetPeak(matrix);

            return new MetricsResult
            {
                PeakPressure = rawPeak,
                PeakPressureIndex = globalPpi,
                ContactAreaPercent = contactAreaPercent,
                RiskLevel = RiskFromPpi(globalPpi)
            };
        }

        // ---------------- HELPERS ----------------

        private static string RiskFromPpi(double ppi)
        {
            if (ppi < 20) return "Low";
            if (ppi < 40) return "Medium";
            return "High";
        }

        private static int RiskRank(string risk) =>
            risk.ToLowerInvariant() switch
            {
                "high" => 2,
                "medium" => 1,
                _ => 0
            };

        private double GetPeak(double[,] matrix)
        {
            double max = double.MinValue;
            foreach (var v in matrix)
                if (v > max) max = v;
            return max;
        }

        private CachedFile GetOrLoadFile(string path)
        {
            DateTime lastWrite = File.GetLastWriteTimeUtc(path);

            lock (_cacheLock)
            {
                if (_cache.TryGetValue(path, out var cf) &&
                    cf.LastWriteUtc == lastWrite)
                    return cf;
            }

            var loaded = LoadFileFromDisk(path, lastWrite);

            lock (_cacheLock)
            {
                _cache[path] = loaded;
                return loaded;
            }
        }

        private CachedFile LoadFileFromDisk(string path, DateTime lastWriteUtc)
        {
            var lines = File.ReadAllLines(path);
            int frameCount = lines.Length / 32;
            var frames = new FrameData[frameCount];
            var fmt = CultureInfo.InvariantCulture;

            for (int f = 0; f < frameCount; f++)
            {
                var matrix = new double[32, 32];

                for (int r = 0; r < 32; r++)
                {
                    var parts = lines[f * 32 + r].Split(',');
                    for (int c = 0; c < 32 && c < parts.Length; c++)
                        double.TryParse(parts[c], NumberStyles.Float, fmt, out matrix[r, c]);
                }

                frames[f] = new FrameData
                {
                    Matrix = matrix,
                    Metrics = GetMetrics(matrix)
                };
            }

            return new CachedFile
            {
                Path = path,
                LastWriteUtc = lastWriteUtc,
                Frames = frames
            };
        }
    }
}
