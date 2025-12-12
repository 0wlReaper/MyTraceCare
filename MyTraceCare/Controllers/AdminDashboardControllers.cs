using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTraceCare.Data;
using MyTraceCare.Models;
using MyTraceCare.ViewModels;

namespace MyTraceCare.Controllers
{
    //“Dashboard is receiving only a number, not a list. We need to update the controller to send a list of users and fix the view to iterate through them.”
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

        // ------------------------------------------------------
        // DASHBOARD HOME
        // ------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardStatsViewModel
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalPatients = await _db.Users.CountAsync(u => u.Role == UserRole.Patient),
                TotalClinicians = await _db.Users.CountAsync(u => u.Role == UserRole.Clinician),
                TotalAdmins = await _db.Users.CountAsync(u => u.Role == UserRole.Admin),
                TotalAlerts = await _db.Alerts.CountAsync()
            };

            return View("~/Views/Admin/Index.cshtml", vm);
        }

        // ------------------------------------------------------
        // LIST USERS
        // ------------------------------------------------------
        public async Task<IActionResult> Users()
        {
            var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
            return View("~/Views/Admin/Users.cshtml", users);
        }

        // ------------------------------------------------------
        // SYSTEM ALERTS
        // ------------------------------------------------------
        public async Task<IActionResult> Alerts()
        {
            var alerts = await _db.Alerts
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View("~/Views/Admin/Alerts.cshtml", alerts);
        }

        // ------------------------------------------------------
        // CREATE USER (GET)
        // ------------------------------------------------------
        public IActionResult CreateUser()
        {
            return View("~/Views/Admin/CreateUser.cshtml", new AdminUserViewModel());
        }

        // ------------------------------------------------------
        // CREATE USER (POST)
        // ------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateUser(AdminUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/CreateUser.cshtml", model);

            var exists = await _userManager.FindByEmailAsync(model.Email);
            if (exists != null)
            {
                ModelState.AddModelError("", "Email already exists.");
                return View("~/Views/Admin/CreateUser.cshtml", model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = model.Role,
                Gender = model.Gender,
                DOB = model.DOB,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                return View("~/Views/Admin/CreateUser.cshtml", model);
            }

            await _userManager.AddToRoleAsync(user, model.Role.ToString());

            return RedirectToAction("Users");
        }

        // ------------------------------------------------------
        // EDIT USER (GET)
        // ------------------------------------------------------
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var vm = new AdminUserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Gender = user.Gender ?? Gender.Male,
                Role = user.Role,
                DOB = user.DOB ?? DateTime.UtcNow
            };

            return View("~/Views/Admin/EditUser.cshtml", vm);
        }

        // ------------------------------------------------------
        // EDIT USER (POST)
        // ------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> EditUser(AdminUserEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/EditUser.cshtml", model);

            var user = await _db.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Gender = model.Gender;
            user.Role = model.Role;
            user.DOB = model.DOB;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, model.NewPassword);
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Users");
        }

        // ------------------------------------------------------
        // DELETE USER CONFIRM
        // ------------------------------------------------------
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View("~/Views/Admin/DeleteUser.cshtml", user);
        }

        // ------------------------------------------------------
        // DELETE USER (POST)
        // ------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return RedirectToAction("Users");
        }
    }
}
