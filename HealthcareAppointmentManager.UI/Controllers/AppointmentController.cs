using HealthcareAppointmentManager.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthcareAppointmentManager.UI.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ILogger<AppointmentController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly string apiURL = "https://localhost:7187/api/";
        private readonly string _cookieName = "AuthToken";

        public AppointmentController(ILogger<AppointmentController> logger, IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
        }

        private HttpClient CreateHttpClientWithToken()
        {
            var token = _httpContextAccessor.HttpContext.Request.Cookies[_cookieName];
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Using token: {Token}", token);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _logger.LogWarning("No token found in cookies.");
            }
            return _httpClient;
        }

        public async Task<IActionResult> GetAllAppointments()
        {
            _logger.LogInformation("Attempting to get all appointments.");
            var client = CreateHttpClientWithToken();

            var response = await client.GetAsync(apiURL + "GetAllAppointments");
            List<Appointment> appointments = new List<Appointment>();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                appointments = JsonConvert.DeserializeObject<List<Appointment>>(content);
                _logger.LogInformation("Successfully retrieved {Count} appointments.", appointments.Count);
            }
            else
            {
                _logger.LogError("Failed to retrieve appointments. HTTP Status Code: {StatusCode}", response.StatusCode);
                _logger.LogError("Response content: {Content}", await response.Content.ReadAsStringAsync());
            }

            return View(appointments);
        }

        [HttpGet]
        public async Task<IActionResult> ManageAppointment()
        {
            _logger.LogInformation("Navigated to ManageAppointment view.");

            try
            {
                // Create HttpClient with token
                var client = CreateHttpClientWithToken();

                // Log the request URL
                var requestUrl = apiURL + "GetAllDoctors";
                _logger.LogInformation("Sending GET request to: {RequestUrl}", requestUrl);

                // Send the request
                var doctorsResponse = await client.GetAsync(requestUrl);

                // Log the response status code
                _logger.LogInformation("Received response. Status Code: {StatusCode}", doctorsResponse.StatusCode);

                if (doctorsResponse.IsSuccessStatusCode)
                {
                    // Log response content
                    var doctorsContent = await doctorsResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Response content: {Content}", doctorsContent);

                    // Deserialize and log the list of doctors
                    var doctorsList = JsonConvert.DeserializeObject<List<Doctor>>(doctorsContent);
                    _logger.LogInformation("Deserialized list of doctors. Count: {Count}", doctorsList.Count);

                    ViewBag.Doctors = new SelectList(doctorsList, "DoctorID", "DoctorSpecialization");

                    // Retrieve PatientID from cookie
                    var patientId = _httpContextAccessor.HttpContext.Request.Cookies["PatientID"];

                    if (string.IsNullOrEmpty(patientId))
                    {
                        _logger.LogWarning("PatientID cookie is not set or is empty.");
                        ViewBag.PatientID = string.Empty;
                    }
                    else
                    {
                        _logger.LogInformation("Retrieved PatientID from cookie: {PatientID}", patientId);
                        ViewBag.PatientID = patientId;
                    }
                }
                else
                {
                    // Log detailed error information
                    var errorMessage = await doctorsResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to retrieve doctors. HTTP Status Code: {StatusCode}, Reason: {Reason}, Error: {Error}",
                        doctorsResponse.StatusCode, doctorsResponse.ReasonPhrase, errorMessage);
                    ViewBag.Message = "Failed to retrieve doctors.";
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur
                _logger.LogError(ex, "An exception occurred while retrieving doctors or PatientID.");
                ViewBag.Message = "An error occurred while retrieving doctors.";
            }

            return View();
        }





        [HttpPost]
        public async Task<IActionResult> SaveAppointment(Appointment appointment)
        {
            _logger.LogInformation("Attempting to save appointment with ID: {AppointmentID}", appointment.AppointmentID);

            var client = CreateHttpClientWithToken();
            HttpResponseMessage response;

                try
                {
                    if (appointment.AppointmentID > 0)
                {
                    // Update the appointment data
                    response = await client.PostAsJsonAsync(apiURL + "UpdateAppointmentData", appointment);
                    if (response.IsSuccessStatusCode)
                    {
                        ViewBag.Message = "Saved Successfully";
                        _logger.LogInformation("Successfully updated appointment with ID: {AppointmentID}", appointment.AppointmentID);
                        return RedirectToAction("DoctorAppointments", "Doctor");
                    }
                    else
                    {
                        _logger.LogError("Failed to update appointment with ID: {AppointmentID}. HTTP Status Code: {StatusCode}", appointment.AppointmentID, response.StatusCode);
                    }
                }
                else
                {
                    // Insert new appointment
                    response = await client.PostAsJsonAsync(apiURL + "SaveAppointmentData", appointment);
                    if (response.IsSuccessStatusCode)
                    {
                        ViewBag.Message = "Saved Successfully";
                        _logger.LogInformation("Successfully created new appointment with ID: {AppointmentID}", appointment.AppointmentID);
                        return RedirectToAction("PatientAppointments", "Patient");
                    }
                    else
                    {
                        _logger.LogError("Failed to create new appointment. HTTP Status Code: {StatusCode}", response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while saving appointment with ID: {AppointmentID}", appointment.AppointmentID);
                return View("Error");
            }

            // In case of failure, return to the ManageAppointment view with the list of doctors
            return View("ManageAppointment", appointment);
        }


        public IActionResult Edit()
        {
            _logger.LogInformation("Navigated to Edit view.");

            return View();
        }

        public async Task<IActionResult> EditAppointmentById(int id)
        {
            _logger.LogInformation("Attempting to retrieve appointment with ID: {AppointmentID}", id);

            var client = CreateHttpClientWithToken();
            var response = await client.GetAsync(apiURL + "GetAppointmentById/" + id);
            Appointment appointment = new Appointment();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                appointment = JsonConvert.DeserializeObject<Appointment>(content);
                _logger.LogInformation("Successfully retrieved appointment with ID: {AppointmentID}", id);
            }
            else
            {
                _logger.LogError("Failed to retrieve appointment with ID: {AppointmentID}. HTTP Status Code: {StatusCode}", id, response.StatusCode);
            }

            return View("Edit", appointment);
        }
    }
}
