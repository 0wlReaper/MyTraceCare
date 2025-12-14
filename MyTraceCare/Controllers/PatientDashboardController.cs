using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTraceCare.Data;
using MyTraceCare.Models;

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

        public async Task<IActionResult> Index(DateTime? date, int frame = 0, int rangeMinutes = 60)
        {
            var userId = _userManager.GetUserId(User)!;

            var files = await _db.PatientDataFiles
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.Date)
                .ToListAsync();

            if (!files.Any())
                return View("~/Views/Patient/Dashboard.cshtml", new HeatmapData());

            var selectedDate = date ?? files.First().Date;
            var file = files.FirstOrDefault(f => f.Date == selectedDate) ?? files.First();

            string path = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(path))
                return View("~/Views/Patient/Dashboard.cshtml", new HeatmapData { Date = file.Date });

            int totalFrames = _heatmap.GetTotalFrames(path);
            int requestedFrames = rangeMinutes * 60;
            int effectiveFrames = Math.Min(totalFrames, requestedFrames);

            if (effectiveFrames < requestedFrames)
            {
                ViewBag.FrameWarning =
                    $"Only {(effectiveFrames / 60.0):0.0} minutes of data are available for this date.";
            }

            frame = Math.Clamp(frame, 0, effectiveFrames - 1);

            var matrix = _heatmap.LoadFrame(path, frame);
            var metrics = _heatmap.GetFrameMetrics(path, frame);
            var maxRisk = _heatmap.GetMaxRiskUpToFrame(path, frame);

            var model = new HeatmapData
            {
                Date = file.Date,
                Matrix = matrix,
                PeakPressure = metrics.PeakPressure,
                PeakPressureIndex = metrics.PeakPressureIndex,
                ContactAreaPercent = metrics.ContactAreaPercent,
                RiskLevel = maxRisk.riskLevel,
                FrameIndex = frame,
                TotalFrames = effectiveFrames
            };

            ViewBag.PeakHistoryJson =
                JsonSerializer.Serialize(_heatmap.GetPeakHistory(path, effectiveFrames));

            ViewBag.AvailableDates = files.Select(f => f.Date).ToList();
            ViewBag.SelectedDate = selectedDate;
            ViewBag.RangeMinutes = rangeMinutes;

            return View("~/Views/Patient/Dashboard.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetFrame(DateTime date, int frame, int rangeMinutes = 60)
        {
            var userId = _userManager.GetUserId(User)!;

            var file = _db.PatientDataFiles
                .FirstOrDefault(f => f.UserId == userId && f.Date == date);

            if (file == null) return Json(new { success = false });

            string path = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(path)) return Json(new { success = false });

            int totalFrames = _heatmap.GetTotalFrames(path);
            int effectiveFrames = Math.Min(totalFrames, rangeMinutes * 60);

            frame = Math.Clamp(frame, 0, effectiveFrames - 1);

            var matrix = _heatmap.LoadFrame(path, frame);
            var metrics = _heatmap.GetFrameMetrics(path, frame);

            var flat = new double[1024];
            int i = 0;
            for (int r = 0; r < 32; r++)
                for (int c = 0; c < 32; c++)
                    flat[i++] = matrix[r, c];

            return Json(new
            {
                success = true,
                frameIndex = frame,
                peakPressure = metrics.PeakPressure,
                peakPressureIndex = metrics.PeakPressureIndex,
                contactAreaPercent = metrics.ContactAreaPercent,
                riskLevel = metrics.RiskLevel,
                matrix = flat
            });
        }
    }
}
