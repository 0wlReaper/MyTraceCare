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
            var userId = _userManager.GetUserId(User);

            var alerts = await _db.Alerts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Include(a => a.User)
                .Include(a => a.Comments)       // load comments
                .ToListAsync();

            return View("~/Views/Patient/Alerts.cshtml", alerts);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int alertId, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return RedirectToAction("Index");

            var userId = _userManager.GetUserId(User);

            var newComment = new PatientComment
            {
                AlertId = alertId,
                UserId = userId,
                Text = comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.PatientComments.Add(newComment);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
