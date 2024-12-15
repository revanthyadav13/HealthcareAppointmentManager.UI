using System.ComponentModel.DataAnnotations;

namespace HealthcareAppointmentManager.UI.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentID { get; set; }

        [Required(ErrorMessage = "Patient ID is required.")]
        public int PatientID { get; set; }

        [Required(ErrorMessage = "Doctor ID is required.")]
        public int DoctorID { get; set; }

        [Required(ErrorMessage = "Appointment date is required.")]
        [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Appointment time is required.")]
        [DataType(DataType.Time, ErrorMessage = "Invalid time format.")]
        public TimeSpan AppointmentTime { get; set; }

        [Required(ErrorMessage = "Appointment status is required.")]
        [StringLength(20, ErrorMessage = "Appointment status cannot be longer than 20 characters.")]
        public string AppointmentStatus { get; set; }

        [StringLength(250, ErrorMessage = "Purpose description cannot be longer than 250 characters.")]
        public string PurposeDescription { get; set; }
    }
}
