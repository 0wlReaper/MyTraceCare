using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTraceCare.Data;
using MyTraceCare.Models;

namespace MyTraceCare.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientAlertsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<User> _userManager;

        public PatientAlertsController(AppDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var alerts = await _db.Alerts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.SeverityRank)
                .ThenByDescending(a => a.CreatedAt)
                .Include(a => a.Comments)
                .ToListAsync();

            return View("~/Views/Patient/Alerts.cshtml", alerts);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int alertId, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return RedirectToAction(nameof(Index));

            _db.PatientComments.Add(new PatientComment
            {
                AlertId = alertId,
                UserId = _userManager.GetUserId(User)!,
                Text = comment.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
