using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTraceCare.Data;
using MyTraceCare.Models;
using MyTraceCare.Extensions;


namespace MyTraceCare.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientDashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly HeatmapService _heatmap;
        private readonly IWebHostEnvironment _env;

        public PatientDashboardController(
            AppDbContext db,
            UserManager<User> userManager,
            HeatmapService heatmap,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _heatmap = heatmap;
            _env = env;
        }

        // Main page
        public async Task<IActionResult> Index(DateTime? date, int frame = 0, int rangeMinutes = 60)
        {
            var userId = _userManager.GetUserId(User)?? string.Empty;

            var files = await _db.PatientDataFiles
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.Date)
                .ToListAsync();

            if (!files.Any())
            {
                ViewBag.AvailableDates = new List<DateTime>();
                return View("~/Views/Patient/Dashboard.cshtml", null);
            }

            var selectedDate = date ?? files.First().Date;
            ViewBag.SelectedDate = selectedDate;
            ViewBag.AvailableDates = files.Select(f => f.Date).ToList();
            ViewBag.RangeMinutes = rangeMinutes;

            var file = files.FirstOrDefault(f => f.Date == selectedDate);
            if (file == null)
                return View("~/Views/Patient/Dashboard.cshtml", null);

            string physicalPath = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(physicalPath))
                return View("~/Views/Patient/Dashboard.cshtml", null);

            // total frames for this CSV
            int totalFramesInFile = _heatmap.GetTotalFrames(physicalPath);
            if (totalFramesInFile <= 0)
            {
                ViewBag.FrameWarning = "No frames found in this dataset.";
                return View("~/Views/Patient/Dashboard.cshtml", null);
            }

            int requestedFrames = rangeMinutes * 60; // 1 fps
            int effectiveTotalFrames = Math.Min(totalFramesInFile, requestedFrames);

            if (requestedFrames > totalFramesInFile)
            {
                double minsAvail = totalFramesInFile / 60.0;
                ViewBag.FrameWarning =
                    $"Only {minsAvail:0.#} minutes of data are available for this date. " +
                    "The rest of the selected window has no data.";
            }

            if (effectiveTotalFrames <= 0)
            {
                ViewBag.FrameWarning = "No data frames within the selected window.";
                return View("~/Views/Patient/Dashboard.cshtml", null);
            }

            if (frame < 0) frame = 0;
            if (frame >= effectiveTotalFrames) frame = effectiveTotalFrames - 1;

            // current frame
            var matrix = _heatmap.LoadFrame(physicalPath, frame);
            var metrics = _heatmap.GetFrameMetrics(physicalPath, frame);

            var model = new HeatmapData
            {
                Date = selectedDate,
                Matrix = matrix,
                PeakPressure = metrics.PeakPressure,
                PeakPressureIndex = metrics.PeakPressureIndex,
                ContactAreaPercent = metrics.ContactAreaPercent,
                RiskLevel = metrics.RiskLevel,
                FrameIndex = frame,
                TotalFrames = effectiveTotalFrames
            };

            // 🔥 REQUIREMENT 3: auto-generate alert for high-pressure frames
            await GenerateAlertIfHighRiskAsync(userId, model);

            // PPI history for the selected window – uses cached metrics
            var peakHistory = _heatmap.GetPeakHistory(physicalPath, effectiveTotalFrames);
            ViewBag.PeakHistoryJson = JsonSerializer.Serialize(peakHistory);

            return View("~/Views/Patient/Dashboard.cshtml", model);
        }

        // AJAX endpoint for the player – returns one frame + metrics as JSON
        [HttpGet]
        public async Task<IActionResult> GetFrame(DateTime date, int frame, int rangeMinutes = 60)
        {
            var userId = _userManager.GetUserId(User);

            var file = await _db.PatientDataFiles
                .Where(f => f.UserId == userId && f.Date == date)
                .FirstOrDefaultAsync();

            if (file == null)
                return Json(new { success = false, message = "No data for this date." });

            string physicalPath = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(physicalPath))
                return Json(new { success = false, message = "File not found." });

            int totalFramesInFile = _heatmap.GetTotalFrames(physicalPath);
            if (totalFramesInFile <= 0)
                return Json(new { success = false, message = "No frames in file." });

            int requestedFrames = rangeMinutes * 60;
            int effectiveTotalFrames = Math.Min(totalFramesInFile, requestedFrames);
            if (effectiveTotalFrames <= 0)
                return Json(new { success = false, message = "No frames in selected window." });

            if (frame < 0) frame = 0;
            if (frame >= effectiveTotalFrames) frame = effectiveTotalFrames - 1;

            var matrix = _heatmap.LoadFrame(physicalPath, frame);
            var metrics = _heatmap.GetFrameMetrics(physicalPath, frame);

            // Flatten matrix for JSON
            var flat = new double[32 * 32];
            int idx = 0;
            for (int r = 0; r < 32; r++)
            {
                for (int c = 0; c < 32; c++)
                {
                    flat[idx++] = matrix[r, c];
                }
            }

            return Json(new
            {
                success = true,
                frameIndex = frame,
                totalFrames = effectiveTotalFrames,
                peakPressure = metrics.PeakPressure,
                peakPressureIndex = metrics.PeakPressureIndex,
                contactAreaPercent = metrics.ContactAreaPercent,
                riskLevel = metrics.RiskLevel,
                matrix = flat
            });
        }

        // ----------------- PRIVATE HELPERS -----------------

        /// <summary>
        /// If the current frame is "High" risk, create a DB Alert once, tied to that timestamp.
        /// This satisfies requirement 3 (auto alert + DB flag) and helps 7 (timestamp association).
        /// </summary>
        private async Task GenerateAlertIfHighRiskAsync(string userId, HeatmapData model)
        {
            if (!string.Equals(model.RiskLevel, "High", StringComparison.OrdinalIgnoreCase))
                return;

            // Represent time as "t = FrameIndex seconds"
            var timeText = $"{model.Date:dd MMM yyyy} at t={model.FrameIndex}s";

            // Unique tag to avoid duplicate alerts for the same frame
            var uniqueTag = $"[date={model.Date:yyyy-MM-dd};frame={model.FrameIndex}]";

            bool alreadyExists = await _db.Alerts
                .AnyAsync(a =>
                    a.UserId == userId &&
                    a.Message.Contains(uniqueTag));

            if (alreadyExists)
                return;

            var alert = new Alert
            {
                UserId = userId,
                Title = "High pressure detected",
                Message = $"High pressure period detected on {timeText}. {uniqueTag}",
                CreatedAt = model.Date.AddSeconds(model.FrameIndex)
            };

            _db.Alerts.Add(alert);
            await _db.SaveChangesAsync();
        }
    }
}
