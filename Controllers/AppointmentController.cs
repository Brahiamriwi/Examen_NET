using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Examen_NET.Data;
using Examen_NET.Models;
using Examen_NET.Services;

namespace Examen_NET.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public AppointmentController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // =====================================================
        // GET: Appointment/Index - LISTAR TODAS LAS CITAS
        // =====================================================
        public async Task<IActionResult> Index(int? patientId, int? doctorId)
        {
            try
            {
                var appointmentsQuery = _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Include(a => a.EmailHistory)
                    .AsQueryable();

                // Filtrar por paciente si se especifica
                if (patientId.HasValue)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => a.PatientId == patientId.Value);
                    var patient = await _context.Patients.FindAsync(patientId.Value);
                    ViewBag.FilterTitle = $"Citas de {patient?.FullName}";
                }

                // Filtrar por médico si se especifica
                if (doctorId.HasValue)
                {
                    appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == doctorId.Value);
                    var doctor = await _context.Doctors.FindAsync(doctorId.Value);
                    ViewBag.FilterTitle = $"Citas del Dr. {doctor?.FullName}";
                }

                var appointments = await appointmentsQuery
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync();
                
                // ⭐ AGREGAR LISTAS PARA LOS SELECTORES
                ViewBag.Patients = await _context.Patients
                    .OrderBy(p => p.FullName)
                    .Select(p => new { p.Id, p.FullName })
                    .ToListAsync();
        
                ViewBag.Doctors = await _context.Doctors
                    .OrderBy(d => d.FullName)
                    .Select(d => new { d.Id, d.FullName })
                    .ToListAsync();
                
                return View(appointments);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar las citas: {ex.Message}";
                return View(new List<Appointment>());
            }
        }

        // =====================================================
        // GET: Appointment/Details/5 - VER DETALLES DE UNA CITA
        // =====================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de cita no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Include(a => a.EmailHistory)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    TempData["Error"] = "Cita no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                return View(appointment);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar la cita: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // GET: Appointment/Create - MOSTRAR FORMULARIO DE CREACIÓN
        // =====================================================
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewData["PatientId"] = new SelectList(
                    await _context.Patients.OrderBy(p => p.FullName).ToListAsync(), 
                    "Id", 
                    "FullName"
                );
                
                ViewData["DoctorId"] = new SelectList(
                    await _context.Doctors.OrderBy(d => d.FullName).ToListAsync(), 
                    "Id", 
                    "FullName"
                );

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el formulario: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // POST: Appointment/Create - CREAR NUEVA CITA
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,AppointmentDate,AppointmentTime")] Appointment appointment)
        {
            // ⭐ CONVERTIR A UTC ANTES DE VALIDAR
            var appointmentDateUtc = DateTime.SpecifyKind(appointment.AppointmentDate, DateTimeKind.Utc);
            
            // Validación 1: Fecha no puede ser en el pasado
            if (appointment.AppointmentDate < DateTime.Now.Date)
            {
                ModelState.AddModelError("AppointmentDate", "La fecha de la cita no puede ser en el pasado");
            }

            // ⭐ Validación 2: Conflicto de horario para el médico (con rango de 30 minutos)
            var minTime = appointment.AppointmentTime.Add(TimeSpan.FromMinutes(-30));
            var maxTime = appointment.AppointmentTime.Add(TimeSpan.FromMinutes(30));

            var doctorConflict = await _context.Appointments
                .AnyAsync(a => 
                    a.DoctorId == appointment.DoctorId &&
                    a.AppointmentDate == appointmentDateUtc &&
                    a.AppointmentTime >= minTime &&
                    a.AppointmentTime <= maxTime &&
                    a.Status == "Scheduled");

            if (doctorConflict)
            {
                ModelState.AddModelError("", "El médico ya tiene una cita programada en este horario o en los 30 minutos anteriores/posteriores");
            }

            // ⭐ Validación 3: Conflicto de horario para el paciente (con rango de 30 minutos)
            var patientConflict = await _context.Appointments
                .AnyAsync(a => 
                    a.PatientId == appointment.PatientId &&
                    a.AppointmentDate == appointmentDateUtc &&
                    a.AppointmentTime >= minTime &&
                    a.AppointmentTime <= maxTime &&
                    a.Status == "Scheduled");

            if (patientConflict)
            {
                ModelState.AddModelError("", "El paciente ya tiene una cita programada en este horario o en los 30 minutos anteriores/posteriores");
            }
            
            // Después de las validaciones de conflicto, agregar:

           // ⭐ Validar que el paciente esté activo
            var patient = await _context.Patients.FindAsync(appointment.PatientId);
            if (patient != null && !patient.IsActive)
            {
                ModelState.AddModelError("PatientId", "El paciente seleccionado está inactivo");
            }

            // ⭐ Validar que el médico esté activo
            var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);
            if (doctor != null && !doctor.IsActive)
            {
                ModelState.AddModelError("DoctorId", "El médico seleccionado está inactivo");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    // ⭐ Convertir fecha a UTC para guardar
                    appointment.AppointmentDate = appointmentDateUtc;
                    appointment.Status = "Scheduled";

                    // Guardar la cita
                    _context.Add(appointment);
                    await _context.SaveChangesAsync();

                    // Cargar relaciones para el envío del correo
                    await _context.Entry(appointment)
                        .Reference(a => a.Patient)
                        .LoadAsync();
                    
                    await _context.Entry(appointment)
                        .Reference(a => a.Doctor)
                        .LoadAsync();

                    // Enviar correo de confirmación
                    var emailResult = await _emailService.SendAppointmentConfirmation(appointment);

                    // Registrar en EmailHistory
                    var emailHistory = new EmailHistory
                    {
                        AppointmentId = appointment.Id,
                        SentDate = DateTime.UtcNow,
                        Status = emailResult.success ? "Sent" : "Failed",
                        ErrorMessage = emailResult.success ? null : emailResult.message
                    };

                    _context.EmailHistories.Add(emailHistory);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = emailResult.success 
                        ? $"Cita creada exitosamente. Correo de confirmación enviado a {appointment.Patient?.Email}"
                        : $"Cita creada exitosamente. Error al enviar correo: {emailResult.message}";

                    return RedirectToAction(nameof(Details), new { id = appointment.Id });
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error al crear la cita: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error inesperado: {ex.Message}";
                }
            }

            // Recargar listas si hay error
            ViewData["PatientId"] = new SelectList(
                await _context.Patients.OrderBy(p => p.FullName).ToListAsync(), 
                "Id", 
                "FullName", 
                appointment.PatientId
            );
            
            ViewData["DoctorId"] = new SelectList(
                await _context.Doctors.OrderBy(d => d.FullName).ToListAsync(), 
                "Id", 
                "FullName", 
                appointment.DoctorId
            );

            return View(appointment);
        }
        
        // =====================================================
        // GET: Appointment/Edit/5 - MOSTRAR FORMULARIO DE EDICIÓN
        // =====================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de cita no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                
                if (appointment == null)
                {
                    TempData["Error"] = "Cita no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                ViewData["PatientId"] = new SelectList(
                    await _context.Patients.OrderBy(p => p.FullName).ToListAsync(), 
                    "Id", 
                    "FullName", 
                    appointment.PatientId
                );
                
                ViewData["DoctorId"] = new SelectList(
                    await _context.Doctors.OrderBy(d => d.FullName).ToListAsync(), 
                    "Id", 
                    "FullName", 
                    appointment.DoctorId
                );

                return View(appointment);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar la cita: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // POST: Appointment/Edit/5 - ACTUALIZAR CITA
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,DoctorId,AppointmentDate,AppointmentTime,Status")] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                TempData["Error"] = "ID de cita no coincide";
                return RedirectToAction(nameof(Index));
            }

            // ⭐ CONVERTIR A UTC ANTES DE VALIDAR
            var appointmentDateUtc = DateTime.SpecifyKind(appointment.AppointmentDate, DateTimeKind.Utc);

            // Validación de fecha
            if (appointment.AppointmentDate < DateTime.Now.Date && appointment.Status == "Scheduled")
            {
                ModelState.AddModelError("AppointmentDate", "La fecha de la cita no puede ser en el pasado");
            }

            // ⭐ Validación con rango de 30 minutos
            var minTime = appointment.AppointmentTime.Add(TimeSpan.FromMinutes(-30));
            var maxTime = appointment.AppointmentTime.Add(TimeSpan.FromMinutes(30));

            // Validación de conflicto para el médico (excluyendo la cita actual)
            var doctorConflict = await _context.Appointments
                .AnyAsync(a => 
                    a.Id != appointment.Id &&
                    a.DoctorId == appointment.DoctorId &&
                    a.AppointmentDate == appointmentDateUtc &&
                    a.AppointmentTime >= minTime &&
                    a.AppointmentTime <= maxTime &&
                    a.Status == "Scheduled");

            if (doctorConflict)
            {
                ModelState.AddModelError("", "El médico ya tiene una cita programada en este horario o en los 30 minutos anteriores/posteriores");
            }

            // Validación de conflicto para el paciente (excluyendo la cita actual)
            var patientConflict = await _context.Appointments
                .AnyAsync(a => 
                    a.Id != appointment.Id &&
                    a.PatientId == appointment.PatientId &&
                    a.AppointmentDate == appointmentDateUtc &&
                    a.AppointmentTime >= minTime &&
                    a.AppointmentTime <= maxTime &&
                    a.Status == "Scheduled");

            if (patientConflict)
            {
                ModelState.AddModelError("", "El paciente ya tiene una cita programada en este horario o en los 30 minutos anteriores/posteriores");
            }
            
            // Después de las validaciones de conflicto, agregar:
            // ⭐ Validar que el paciente esté activo
            var patient = await _context.Patients.FindAsync(appointment.PatientId);
            if (patient != null && !patient.IsActive)
            {
                ModelState.AddModelError("PatientId", "El paciente seleccionado está inactivo");
            }

            // ⭐ Validar que el médico esté activo
            var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);
            if (doctor != null && !doctor.IsActive)
            {
                ModelState.AddModelError("DoctorId", "El médico seleccionado está inactivo");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ⭐ Convertir fecha a UTC para guardar
                    appointment.AppointmentDate = appointmentDateUtc;

                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "Cita actualizada exitosamente";
                    return RedirectToAction(nameof(Details), new { id = appointment.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                    {
                        TempData["Error"] = "La cita ya no existe";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "Error de concurrencia al actualizar la cita";
                    }
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error al actualizar la cita: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error inesperado: {ex.Message}";
                }
            }

            // Recargar listas
            ViewData["PatientId"] = new SelectList(
                await _context.Patients.OrderBy(p => p.FullName).ToListAsync(), 
                "Id", 
                "FullName", 
                appointment.PatientId
            );
            
            ViewData["DoctorId"] = new SelectList(
                await _context.Doctors.OrderBy(d => d.FullName).ToListAsync(), 
                "Id", 
                "FullName", 
                appointment.DoctorId
            );

            return View(appointment);
        }
        // =====================================================
        // POST: Appointment/Cancel/5 - CANCELAR CITA
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);

                if (appointment == null)
                {
                    TempData["Error"] = "Cita no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (appointment.Status != "Scheduled")
                {
                    TempData["Error"] = "Solo se pueden cancelar citas programadas";
                    return RedirectToAction(nameof(Details), new { id });
                }

                appointment.Status = "Cancelled";
                _context.Update(appointment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cita cancelada exitosamente";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cancelar la cita: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // POST: Appointment/Complete/5 - MARCAR CITA COMO ATENDIDA
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);

                if (appointment == null)
                {
                    TempData["Error"] = "Cita no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (appointment.Status != "Scheduled")
                {
                    TempData["Error"] = "Solo se pueden completar citas programadas";
                    return RedirectToAction(nameof(Details), new { id });
                }

                appointment.Status = "Completed";
                _context.Update(appointment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cita marcada como atendida";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al completar la cita: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        
        // =====================================================
        // MÉTODO AUXILIAR - VERIFICAR SI EXISTE UNA CITA
        // =====================================================
        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }
    }
}