using HealthcareAppointmentManager.UI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthcareAppointmentManager.UI.Controllers
{
    public class LoginController : Controller
    {
        private string apiURL = "https://localhost:7187/";
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginController> _logger;
        private readonly string _cookieName = "AuthToken";
        private readonly string _usernameCookieName = "Username";
        private readonly string _patientIdCookieName = "PatientId"; // Add this for Patient ID

        public LoginController(HttpClient httpClient, IConfiguration configuration, ILogger<LoginController> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            _logger.LogInformation("Login GET action called.");

            // Check for existing cookies
            var token = Request.Cookies[_cookieName];
            var username = Request.Cookies[_usernameCookieName];

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
            {
                _logger.LogInformation("Existing token and username found in cookies. Redirecting to role-based home.");
                return RedirectToRoleBasedHome(token);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                // Send the request to the API for login
                var response = await _httpClient.PostAsJsonAsync(apiURL + "login", model);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        _logger.LogInformation("Login successful. Setting auth token and username in cookies.");

                        // Set the token in a cookie with a 30-minute expiration
                        var cookieOptions = new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                            HttpOnly = true, // Prevent client-side access
                            Secure = true,   // Only send over HTTPS
                            SameSite = SameSiteMode.Strict // Prevent cross-site request forgery
                        };
                        Response.Cookies.Append(_cookieName, result.Token, cookieOptions);
                        Response.Cookies.Append(_usernameCookieName, model.Username, cookieOptions);

                        // Set the authorization header for subsequent requests
                        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);

                        // Retrieve patient details and set patient ID cookie if role is Patient
                        if (model.Username != null)
                        {
                            var patientResponse = await _httpClient.GetAsync(apiURL + $"api/GetPatientByUsername/{model.Username}");

                            if (patientResponse.IsSuccessStatusCode)
                            {
                                var patient = await patientResponse.Content.ReadFromJsonAsync<Patient>();
                                if (patient != null)
                                {
                                    Response.Cookies.Append(_patientIdCookieName, patient.PatientID.ToString(), cookieOptions);
                                    _logger.LogInformation("Patient ID stored in cookie.");
                                }
                                else
                                {
                                    _logger.LogWarning("Patient data is null for username: {Username}", model.Username);
                                    ViewBag.Message = "Unable to retrieve patient data.";
                                }
                            }
                            else
                            {
                                var errorMessage = await patientResponse.Content.ReadAsStringAsync();
                                _logger.LogError("Failed to retrieve patient data. Status Code: {StatusCode}, Reason: {Reason}, Error: {Error}",
                                    patientResponse.StatusCode, patientResponse.ReasonPhrase, errorMessage);
                                ViewBag.Message = "Unable to retrieve patient data.";
                            }
                        }

                        // Redirect to role-based home
                        return RedirectToRoleBasedHome(result.Token);
                    }
                    else
                    {
                        _logger.LogWarning("Login failed: Invalid token.");
                        ViewBag.Message = "Login failed: Invalid token.";
                    }
                }
                else
                {
                    _logger.LogWarning("Login failed: Incorrect credentials.");
                    ViewBag.Message = "Login failed: Incorrect credentials.";
                }
            }

            // Return the view with the model if validation fails or if there are errors
            return View(model);
        }


        [HttpGet]
        public IActionResult Logout()
        {
            _logger.LogInformation("Logout action called. Clearing cookies.");

            // Clear the cookies
            Response.Cookies.Delete(_cookieName);
            Response.Cookies.Delete(_usernameCookieName);
            Response.Cookies.Delete(_patientIdCookieName); // Clear Patient ID cookie

            // Redirect to the login page
            return RedirectToAction("Login");
        }

        private IActionResult RedirectToRoleBasedHome(string token)
        {
            _logger.LogInformation("Decoding JWT token to determine role.");

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Log all claims for debugging
            _logger.LogInformation("JWT Token Claims: {Claims}", string.Join(", ", jwtToken.Claims.Select(c => $"{c.Type}: {c.Value}")));

            // Directly check for the "role" claim type as it appears in the JWT token
            var role = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            if (string.IsNullOrEmpty(role))
            {
                _logger.LogWarning("No role claim found in token. Redirecting to login.");
                return RedirectToAction("Login");
            }

            _logger.LogInformation("Role claim found: {Role}. Redirecting to appropriate dashboard.", role);

            if (role == UserRole.Doctor.ToString())
            {
                return RedirectToAction("DoctorDashboard", "Doctor");
            }
            else if (role == UserRole.Patient.ToString())
            {
                return RedirectToAction("PatientDashboard", "Patient");
            }

            _logger.LogWarning("Unexpected role: {Role}. Redirecting to login.", role);
            return RedirectToAction("Login");
        }
    }
}
