using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Examen_NET.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El paciente es obligatorio")]
        [Display(Name = "Paciente")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "El médico es obligatorio")]
        [Display(Name = "Médico")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "La fecha de la cita es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de la Cita")]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "La hora de la cita es obligatoria")]
        [DataType(DataType.Time)]
        [Display(Name = "Hora de la Cita")]
        public TimeSpan AppointmentTime { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Status { get; set; } = "Scheduled"; // Scheduled, Cancelled, Completed

        // Navigation properties
        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        [ForeignKey("DoctorId")]
        public Doctor? Doctor { get; set; }

        // Relación 1:1 con EmailHistory
        public EmailHistory? EmailHistory { get; set; }

        // Método de validación para fechas
        public bool IsValidAppointmentDate()
        {
            return AppointmentDate >= DateTime.Now.Date;
        }

        // Método para obtener fecha y hora combinadas
        public DateTime GetFullDateTime()
        {
            return AppointmentDate.Add(AppointmentTime);
        }
    }
}