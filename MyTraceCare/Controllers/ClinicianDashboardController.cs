using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTraceCare.Data;
using MyTraceCare.Models;
using MyTraceCare.Models.ViewModels;
using MyTraceCare.Extensions;

namespace MyTraceCare.Controllers
{
    [Authorize(Roles = "Clinician")]
    public class ClinicianDashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly HeatmapService _heatmap;
        private readonly IWebHostEnvironment _env;

        public ClinicianDashboardController(AppDbContext db, HeatmapService heatmap, IWebHostEnvironment env)
        {
            _db = db;
            _heatmap = heatmap;
            _env = env;
        }

        // ---------------------------
        // CLINICIAN HOME PAGE
        // ---------------------------
        public async Task<IActionResult> Index()
        {
            string clinicianId = User.GetUserId();

            var patients = await _db.ClinicianPatients
                .Where(cp => cp.ClinicianId == clinicianId)
                .Include(cp => cp.Patient)
                .ToListAsync();

            var model = new List<ClinicianPatientViewModel>();

            foreach (var cp in patients)
            {
                // ✔ correct deviceId lookup
                var deviceId = await _db.PatientDataFiles
                    .Where(f => f.UserId == cp.PatientId)
                    .Select(f => f.DeviceId)
                    .FirstOrDefaultAsync();

                // alert count
                int alerts = await _db.Alerts.CountAsync(a => a.UserId == cp.PatientId);

                model.Add(new ClinicianPatientViewModel
                {
                    PatientId = cp.PatientId,
                    FullName = cp.Patient.FullName,
                    Email = cp.Patient.Email,
                    DeviceId = deviceId ?? "Unknown",
                    AlertCount = alerts
                });
            }

            return View("~/Views/Clinician/Index.cshtml", model);
        }


        // ---------------------------
        // PATIENT DETAILS PAGE
        // ---------------------------
        public async Task<IActionResult> PatientDetails(string id, DateTime? date, int frame = 0, int rangeMinutes = 60)
        {
            var patient = await _db.Users.FindAsync(id);
            if (patient == null) return NotFound();

            var files = await _db.PatientDataFiles
                .Where(f => f.UserId == id)
                .OrderByDescending(f => f.Date)
                .ToListAsync();

            if (!files.Any())
                return View("~/Views/Clinician/PatientDetails.cshtml", null);

            var selectedDate = date ?? files.First().Date;

            var file = files.FirstOrDefault(f => f.Date == selectedDate);
            if (file == null) return View("~/Views/Clinician/PatientDetails.cshtml", null);

            string physical = Path.Combine(_env.WebRootPath, file.FilePath.TrimStart('/'));

            int totalFrames = _heatmap.GetTotalFrames(physical);
            int effective = Math.Min(totalFrames, rangeMinutes * 60);

            var matrix = _heatmap.LoadFrame(physical, frame);
            var metrics = _heatmap.GetFrameMetrics(physical, frame);

            var alerts = await _db.Alerts
                .Where(a => a.UserId == id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var model = new ClinicianPatientDetailsViewModel
            {
                Patient = patient,
                SelectedDate = selectedDate,
                Matrix = matrix,
                PeakPressure = metrics.PeakPressure,
                PeakPressureIndex = metrics.PeakPressureIndex,
                ContactAreaPercent = metrics.ContactAreaPercent,
                RiskLevel = metrics.RiskLevel,
                FrameIndex = frame,
                TotalFrames = effective,
                AvailableDates = files.Select(f => f.Date).ToList(),
                Alerts = alerts,
                PeakHistoryJson = System.Text.Json.JsonSerializer.Serialize(
                    _heatmap.GetPeakHistory(physical, effective)
                )
            };

            return View("~/Views/Clinician/PatientDetails.cshtml", model);
        }
    }
}
