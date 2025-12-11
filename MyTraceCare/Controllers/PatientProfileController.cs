using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyTraceCare.Models;

namespace MyTraceCare.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientProfileController : Controller
    {
        private readonly UserManager<User> _userManager;

        public PatientProfileController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            return View("~/Views/Patient/Profile.cshtml", user);
        }
    }
}
