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
        // ----- Internal cache types -----
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

        // ----- Cache -----
        private readonly object _cacheLock = new();
        private readonly Dictionary<string, CachedFile> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        // PUBLIC API USED BY CONTROLLER
        // -----------------------------

        /// <summary>
        /// Total frames in this CSV (32 lines = 1 frame).
        /// </summary>
        public int GetTotalFrames(string path)
        {
            var cf = GetOrLoadFile(path);
            return cf.FrameCount;
        }

        /// <summary>
        /// Returns the 32x32 matrix for a given frame index (0-based).
        /// </summary>
        public double[,] LoadFrame(string path, int frameIndex)
        {
            var cf = GetOrLoadFile(path);
            if (frameIndex < 0) frameIndex = 0;
            if (frameIndex >= cf.FrameCount) frameIndex = cf.FrameCount - 1;
            return cf.Frames[frameIndex].Matrix;
        }

        /// <summary>
        /// Returns the precomputed metrics for a given frame.
        /// </summary>
        public MetricsResult GetFrameMetrics(string path, int frameIndex)
        {
            var cf = GetOrLoadFile(path);
            if (frameIndex < 0) frameIndex = 0;
            if (frameIndex >= cf.FrameCount) frameIndex = cf.FrameCount - 1;
            return cf.Frames[frameIndex].Metrics;
        }

        /// <summary>
        /// Returns PeakPressureIndex history for first N frames in this file.
        /// Used for the PPI graph.
        /// </summary>
        public double[] GetPeakHistory(string path, int maxFrames)
        {
            var cf = GetOrLoadFile(path);
            int n = Math.Min(maxFrames, cf.FrameCount);
            var result = new double[n];

            for (int i = 0; i < n; i++)
            {
                result[i] = cf.Frames[i].Metrics.PeakPressureIndex;
            }

            return result;
        }

        /// <summary>
        /// Computes metrics for an arbitrary matrix (kept for compatibility / reuse).
        /// </summary>
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

                    // contact area = any pixel above lowerThreshold
                    if (v >= lowerThreshold)
                        contactCount++;

                    if (visited[r, c] || v < lowerThreshold)
                        continue;

                    // BFS cluster
                    int clusterSize = 0;
                    double clusterMax = 0.0;
                    var queue = new Queue<(int rr, int cc)>();

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
                            if (visited[nr, nc])
                                continue;
                            if (matrix[nr, nc] < lowerThreshold)
                                continue;

                            visited[nr, nc] = true;
                            queue.Enqueue((nr, nc));
                        }
                    }

                    // Only count meaningful clusters
                    if (clusterSize >= minClusterSize && clusterMax > globalPpi)
                        globalPpi = clusterMax;
                }
            }

            double contactAreaPercent = total > 0
                ? (contactCount / (double)total) * 100
                : 0;

            double rawPeak = GetPeak(matrix);

            string risk = rawPeak switch
            {
                < 20 => "Low",
                < 40 => "Medium",
                _ => "High"
            };

            return new MetricsResult
            {
                PeakPressure = rawPeak,
                PeakPressureIndex = globalPpi,
                ContactAreaPercent = contactAreaPercent,
                RiskLevel = risk
            };
        }

        // ----- Internal helpers -----

        private double GetPeak(double[,] matrix)
        {
            double max = double.MinValue;
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (matrix[r, c] > max)
                        max = matrix[r, c];
                }
            }
            return max;
        }

        private CachedFile GetOrLoadFile(string path)
        {
            DateTime lastWrite = File.GetLastWriteTimeUtc(path);

            // Fast path: check cache
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(path, out var cf)
                    && cf.LastWriteUtc == lastWrite)
                {
                    return cf;
                }
            }

            // Load / reload outside lock (avoid blocking everything)
            var loaded = LoadFileFromDisk(path, lastWrite);

            lock (_cacheLock)
            {
                _cache[path] = loaded;
                return loaded;
            }
        }

        /// <summary>
        /// Reads the whole CSV once, builds all frames + metrics, and stores in memory.
        /// Assumptions:
        ///  - CSV has ONLY sensor values (no timestamps)
        ///  - 32 rows per frame
        ///  - each row has at least 32 comma-separated numeric values
        /// </summary>
        private CachedFile LoadFileFromDisk(string path, DateTime lastWriteUtc)
        {
            var lines = File.ReadAllLines(path);

            if (lines.Length < 32)
            {
                return new CachedFile
                {
                    Path = path,
                    LastWriteUtc = lastWriteUtc,
                    Frames = Array.Empty<FrameData>()
                };
            }

            int totalLines = lines.Length;
            int frameCount = totalLines / 32; // integer division, ignore partial
            var frames = new FrameData[frameCount];

            var numberFormat = CultureInfo.InvariantCulture;

            for (int f = 0; f < frameCount; f++)
            {
                var matrix = new double[32, 32];
                int baseLine = f * 32;

                for (int r = 0; r < 32; r++)
                {
                    string line = lines[baseLine + r];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    for (int c = 0; c < 32 && c < parts.Length; c++)
                    {
                        if (double.TryParse(parts[c], NumberStyles.Float, numberFormat, out double val))
                            matrix[r, c] = val;
                        else
                            matrix[r, c] = 0.0; // fallback
                    }
                }

                var metrics = GetMetrics(matrix);

                frames[f] = new FrameData
                {
                    Matrix = matrix,
                    Metrics = metrics
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
