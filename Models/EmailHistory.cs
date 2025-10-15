using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Examen_NET.Models
{
    public class EmailHistory
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Cita")]
        public int AppointmentId { get; set; }

        [Required]
        [Display(Name = "Fecha de Envío")]
        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Status { get; set; } = "Sent"; // Sent, Failed

        [StringLength(500)]
        [Display(Name = "Mensaje de Error")]
        public string? ErrorMessage { get; set; }

        // Navigation property - Relación 1:1
        [ForeignKey("AppointmentId")]
        public Appointment? Appointment { get; set; }
    }
}