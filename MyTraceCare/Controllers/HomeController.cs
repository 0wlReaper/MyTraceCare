using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyTraceCare.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            return Redirect("/Identity/Account/Login");
        }

        [Authorize(Roles = "Patient")]
        public IActionResult PatientDashboard()
        {
            return View();
        }

        [Authorize(Roles = "Clinician")]
        public IActionResult ClinicianDashboard()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return RedirectToAction("Index", "Admin");
        }

    }
}
