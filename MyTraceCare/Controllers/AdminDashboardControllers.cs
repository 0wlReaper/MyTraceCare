using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTraceCare.Data;
using MyTraceCare.Models;
using MyTraceCare.ViewModels;

namespace MyTraceCare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _db;

        public AdminDashboardController(UserManager<User> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // =========================
        // INDEX → USER LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            var users = await _db.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View("~/Views/Admin/Index.cshtml", users);
        }

        // =========================
        // SYSTEM ALERTS
        // =========================
        public async Task<IActionResult> Alerts()
        {
            var alerts = await _db.Alerts
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View("~/Views/Admin/Alerts.cshtml", alerts);
        }

        // =========================
        // CREATE / EDIT USER (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> CreateUser(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return View("~/Views/Admin/CreateUser.cshtml", new AdminUserViewModel());

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View("~/Views/Admin/CreateUser.cshtml", new AdminUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Role = user.Role,
                Gender = user.Gender ?? Gender.Male,
                DOB = user.DOB ?? DateTime.UtcNow
            });
        }

        // =========================
        // CREATE / EDIT USER (POST)
        // =========================
        [HttpPost]
        public async Task<IActionResult> CreateUser(AdminUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/CreateUser.cshtml", model);

            // ---------- EDIT ----------
            if (!string.IsNullOrEmpty(model.Id))
            {
                var user = await _db.Users.FindAsync(model.Id);
                if (user == null) return NotFound();

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.Role = model.Role;
                user.Gender = model.Gender;
                user.DOB = model.DOB;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    await _userManager.RemovePasswordAsync(user);
                    await _userManager.AddPasswordAsync(user, model.Password);
                }

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // ---------- CREATE ----------
            var exists = await _userManager.FindByEmailAsync(model.Email);
            if (exists != null)
            {
                ModelState.AddModelError("", "Email already exists");
                return View("~/Views/Admin/CreateUser.cshtml", model);
            }

            var newUser = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = model.Role,
                Gender = model.Gender,
                DOB = model.DOB,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser, model.Password!);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                return View("~/Views/Admin/CreateUser.cshtml", model);
            }

            await _userManager.AddToRoleAsync(newUser, model.Role.ToString());
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE USER (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View("~/Views/Admin/DeleteUser.cshtml", user);
        }

        // =========================
        // DELETE USER (POST)
        // =========================
        [HttpPost]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
