using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyTraceCare.Data;
using MyTraceCare.Models;
using MyTraceCare.Models.ViewModels;

namespace MyTraceCare.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminDashboardController(
            AppDbContext db,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /AdminDashboard/Index
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalPatients = await _db.Users.CountAsync(u => u.Role == UserRole.Patient),
                TotalClinicians = await _db.Users.CountAsync(u => u.Role == UserRole.Clinician),
                TotalAdmins = await _db.Users.CountAsync(u => u.Role == UserRole.Admin),
                TotalAlerts = await _db.Alerts.CountAsync()
            };

            return View("~/Views/Admin/Index.cshtml", model);
        }

        // GET: /AdminDashboard/CreateUser
        [HttpGet]
        public IActionResult CreateUser()
        {
            var vm = new AdminUserViewModel
            {
                DOB = new System.DateTime(1990, 1, 1)
            };

            return View("~/Views/Admin/CreateUser.cshtml", vm);
        }

        // POST: /AdminDashboard/CreateUser
        [HttpPost]
        public async Task<IActionResult> CreateUser(AdminUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Admin/CreateUser.cshtml", model);

            // check if email already exists
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(string.Empty, "A user with this email already exists.");
                return View("~/Views/Admin/CreateUser.cshtml", model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Gender = model.Gender,
                DOB = model.DOB,
                Role = model.Role,
                EmailConfirmed = true,
                CreatedAt = System.DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                return View("~/Views/Admin/CreateUser.cshtml", model);
            }

            // ensure Identity role exists & add
            var roleName = model.Role.ToString(); // "Patient", "Clinician", "Admin"
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            await _userManager.AddToRoleAsync(user, roleName);

            TempData["AdminMessage"] = $"User '{model.FullName}' created as {roleName}.";
            return RedirectToAction("Index");
        }

        // GET: /AdminDashboard/Alerts
        public async Task<IActionResult> Alerts()
        {
            var alerts = await _db.Alerts
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View("~/Views/Admin/Alerts.cshtml", alerts);
        }
    }
}
