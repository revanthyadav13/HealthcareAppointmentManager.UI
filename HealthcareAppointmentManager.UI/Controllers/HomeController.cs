using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using HealthcareAppointmentManager.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HealthcareAppointmentManager.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string _cookieName = "AuthToken";

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }


        public IActionResult Index()
        {
            // Check for existing token in cookies
            var token = Request.Cookies[_cookieName];

            if (!string.IsNullOrEmpty(token))
            {
                // Redirect to role-based home
                return RedirectToRoleBasedHome(token);
            }

            // If no token, return the regular home page view or login page
            return View();
        }

        private IActionResult RedirectToRoleBasedHome(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Log all claims for debugging
            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Login");
            }

            if (role == UserRole.Doctor.ToString())
            {
                return RedirectToAction("DoctorDashboard", "Doctor");
            }
            else if (role == UserRole.Patient.ToString())
            {
                return RedirectToAction("PatientDashboard", "Patient");
            }

            return RedirectToAction("Login", "Login");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
