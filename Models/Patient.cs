using System.ComponentModel.DataAnnotations;

namespace Examen_NET.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El documento es obligatorio")]
        [StringLength(20, ErrorMessage = "El documento no puede exceder 20 caracteres")]
        [Display(Name = "Documento de Identidad")]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "La edad es obligatoria")]
        [Range(1, 120, ErrorMessage = "La edad debe estar entre 1 y 120 años")]
        [Display(Name = "Edad")]
        public int Age { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [Display(Name = "Teléfono")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = string.Empty;
        
        // ⭐ BORRADO LÓGICO
        public bool IsActive { get; set; } = true;

        // Navigation property - Relación 1:N
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}