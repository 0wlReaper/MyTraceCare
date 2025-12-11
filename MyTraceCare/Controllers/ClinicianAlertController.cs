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
    public class ClinicianAlertsController : Controller
    {
        private readonly AppDbContext _db;

        public ClinicianAlertsController(AppDbContext db)
        {
            _db = db;
        }

        // -----------------------------
        // SHOW ALERT + THREAD
        // -----------------------------
        public async Task<IActionResult> Thread(int id)
        {
            var alert = await _db.Alerts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alert == null)
                return NotFound();

            var comments = await _db.PatientComments
                .Where(c => c.AlertId == id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var patientName = alert.User.FullName;
            var patientId = alert.UserId;

            var threaded = BuildThread(comments, patientId, patientName, alert.Id);

            var vm = new ClinicianAlertThreadViewModel
            {
                Alert = alert,
                Thread = threaded,
                PatientName = patientName,
                PatientId = patientId
            };

            return View("~/Views/Clinician/AlertThread.cshtml", vm);
        }

        // -----------------------------
        // ADD A COMMENT OR REPLY
        // -----------------------------
        [HttpPost]
        public async Task<IActionResult> AddReply(int alertId, string comment, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return RedirectToAction("Thread", new { id = alertId });

            string clinicianId = User.GetUserId();

            var reply = new PatientComment
            {
                AlertId = alertId,
                UserId = clinicianId,
                Text = comment.Trim(),
                ParentCommentId = parentId,
                CreatedAt = DateTime.UtcNow
            };

            _db.PatientComments.Add(reply);
            await _db.SaveChangesAsync();

            return RedirectToAction("Thread", new { id = alertId });
        }

        // -----------------------------
        // THREAD BUILDER
        // -----------------------------
        private List<CommentThreadItemViewModel> BuildThread(
            List<PatientComment> comments,
            string patientId,
            string patientName,
            int alertId)
        {
            // group children by ParentCommentId
            var childMap = comments
                .Where(c => c.ParentCommentId != null)
                .GroupBy(c => c.ParentCommentId)
                .ToDictionary(g => g.Key!.Value, g => g.ToList());

            List<CommentThreadItemViewModel> roots = new();

            foreach (var c in comments.Where(c => c.ParentCommentId == null))
                roots.Add(BuildNode(c, 0));

            CommentThreadItemViewModel BuildNode(PatientComment c, int level)
            {
                var node = new CommentThreadItemViewModel
                {
                    Comment = c,
                    Level = level,
                    AuthorName = c.UserId == patientId ? patientName : "Clinician",
                    AlertId = alertId
                };

                if (childMap.ContainsKey(c.Id))
                {
                    foreach (var child in childMap[c.Id])
                        node.Replies.Add(BuildNode(child, level + 1));
                }

                return node;
            }

            return roots;
        }
    }
}
