using HealthcareAppointmentManager.UI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class PatientController : Controller
{
    private string apiURL = "https://localhost:7187/api/";
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PatientController> _logger;
    private readonly string _cookieName = "AuthToken";
    private readonly string _usernameCookieName = "Username";

    public PatientController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<PatientController> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    public IActionResult PatientDashboard()
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
    public async Task<IActionResult> RegisterPatient(Patient model)
    {
        if (ModelState.IsValid)
        {
            _logger.LogInformation("RegisterPatient called with model: {@Model}", model);

            var response = await _httpClient.PostAsJsonAsync(apiURL + "PatientRegister", model);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Registration successful for patient: {@Model}", model);
                ViewBag.Message = "Registration successful!";
            }
            else
            {
                _logger.LogError("Registration failed. Status code: {StatusCode}", response.StatusCode);
                ViewBag.Message = "Registration failed.";
            }
        }

        return View("Register");
    }

    [HttpGet]
    public async Task<IActionResult> PatientAppointments()
    {
        _logger.LogInformation("PatientAppointments called");

        // Retrieve the token and patient ID from cookies
        var client = CreateHttpClientWithToken();
        var username = _httpContextAccessor.HttpContext.Request.Cookies[_usernameCookieName];
        _logger.LogInformation("Retrieved username from cookies: {Username}", username);

        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("Username is not found in cookies. Redirecting to login.");
            return RedirectToAction("Login", "Login");
        }

        // Get patient ID based on username
        var patientResponse = await client.GetAsync(apiURL + "GetPatientByUsername/" + username);
        if (patientResponse.IsSuccessStatusCode)
        {
            var patientContent = await patientResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Patient data retrieved: {PatientContent}", patientContent);

            var patient = JsonConvert.DeserializeObject<Patient>(patientContent);
            if (patient != null)
            {
                // Retrieve appointments by patient ID
                var appointmentResponse = await client.GetAsync(apiURL + "GetAppointmentsByPatientId/" + patient.PatientID);
                if (appointmentResponse.IsSuccessStatusCode)
                {
                    var appointmentContent = await appointmentResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Appointments data retrieved: {AppointmentContent}", appointmentContent);

                    var appointments = JsonConvert.DeserializeObject<List<Appointment>>(appointmentContent);

                    if (appointments != null && appointments.Any())
                    {
                        return View("PatientAppointments", appointments);
                    }
                    else
                    {
                        _logger.LogInformation("No appointments found for patient ID: {PatientID}", patient.PatientID);
                        ViewBag.Message = "No appointments found.";
                        return View("PatientAppointments", new List<Appointment>());
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
                _logger.LogWarning("No patient data found for username: {Username}", username);
                ViewBag.Message = "Failed to retrieve patient data.";
                return View("Error");
            }
        }
        else
        {
            _logger.LogError("Failed to retrieve patient data. Status code: {StatusCode}", patientResponse.StatusCode);
            ViewBag.Message = "Failed to retrieve patient data.";
            return View("Error");
        }
    }
}
