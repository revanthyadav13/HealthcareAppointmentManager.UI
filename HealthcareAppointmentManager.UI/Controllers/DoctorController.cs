using System.Net.Http.Headers;
using HealthcareAppointmentManager.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HealthcareAppointmentManager.UI.Controllers
{
    public class DoctorController : Controller
    {
        private string apiURL = "https://localhost:7187/api/";
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DoctorController> _logger;
        private readonly string _cookieName = "AuthToken";
        private readonly string _usernameCookieName = "Username";

        public DoctorController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<DoctorController> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        public IActionResult DoctorDashboard()
        {
            return View();
        }
        private HttpClient CreateHttpClientWithToken()
        {
            var token = _httpContextAccessor.HttpContext.Request.Cookies[_cookieName];
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return _httpClient;
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterDoctor(Doctor model)
        {
            // Initialize ViewBag.Message to an empty string or a default message
            ViewBag.Message = string.Empty;

            if (ModelState.IsValid)
            {
                // Send the request to the API
                var response = await _httpClient.PostAsJsonAsync(apiURL+"DoctorRegister", model);

                if (response.IsSuccessStatusCode)
                {
                    // Handle successful registration
                    ViewBag.Message = "Registration successful!";
                }
                else
                {
                    // Handle error
                    ViewBag.Message = "Registration failed.";
                }
            }
            else
            {
                // Handle validation errors
                ViewBag.Message = "Please correct the errors in the form.";
            }

            // Return the view with the ViewBag.Message
            return View("Register");
        }

        [HttpGet]
        public async Task<IActionResult> DoctorAppointments()
        {
            _logger.LogInformation("DoctorAppointments called");

            var client = CreateHttpClientWithToken();
            var username = _httpContextAccessor.HttpContext.Request.Cookies[_usernameCookieName];
            _logger.LogInformation("Retrieved username from cookies: {Username}", username);

            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Username is not found in cookies. Redirecting to login.");
                return RedirectToAction("Login", "Login");
            }

            // Get doctor ID based on username
            var doctorResponse = await client.GetAsync(apiURL + "GetDoctorByUsername/" + username);
            if (doctorResponse.IsSuccessStatusCode)
            {
                var doctorContent = await doctorResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("Doctor data retrieved: {DoctorContent}", doctorContent);

                var doctor = JsonConvert.DeserializeObject<Doctor>(doctorContent);
                if (doctor != null)
                {
                    // Retrieve appointments by doctor ID
                    var appointmentResponse = await client.GetAsync(apiURL + "GetAppointmentsByDoctorId/" + doctor.DoctorID);
                    if (appointmentResponse.IsSuccessStatusCode)
                    {
                        var appointmentContent = await appointmentResponse.Content.ReadAsStringAsync();
                        _logger.LogInformation("Appointments data retrieved: {AppointmentContent}", appointmentContent);

                        var appointments = JsonConvert.DeserializeObject<List<Appointment>>(appointmentContent);

                        if (appointments != null && appointments.Any())
                        {
                            return View("DoctorAppointments", appointments);
                        }
                        else
                        {
                            _logger.LogInformation("No appointments found for doctor ID: {DoctorID}", doctor.DoctorID);
                            ViewBag.Message = "No appointments found.";
                            return View("DoctorAppointments", new List<Appointment>());
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to retrieve appointments. Status code: {StatusCode}", appointmentResponse.StatusCode);
                        ViewBag.Message = $"Failed to retrieve appointments: {appointmentResponse.ReasonPhrase}";
                        return View("Error");
                    }
                }
                else
                {
                    _logger.LogWarning("No doctor data found for username: {Username}", username);
                    ViewBag.Message = "Failed to retrieve doctor data.";
                    return View("Error");
                }
            }
            else
            {
                _logger.LogError("Failed to retrieve doctor data. Status code: {StatusCode}", doctorResponse.StatusCode);
                ViewBag.Message = "Failed to retrieve doctor data.";
                return View("Error");
            }
        }
    }
}
