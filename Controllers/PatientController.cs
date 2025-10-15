using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Examen_NET.Data;
using Examen_NET.Models;

namespace Examen_NET.Controllers
{
    public class PatientController : Controller
    {
        private readonly AppDbContext _context;

        public PatientController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET: Patient/Index - LISTAR TODOS LOS PACIENTES
        // =====================================================
        // Get the list of patients
        public async Task<IActionResult> Index(bool showInactive = false)
        {
            try
            {
                var patientsQuery = _context.Patients.AsQueryable();
        
                // ⭐ CORREGIDO: Filtrar por estado
                if (showInactive)
                {
                    // Mostrar SOLO inactivos
                    patientsQuery = patientsQuery.Where(p => !p.IsActive);
                }
                else
                {
                    // Mostrar SOLO activos
                    patientsQuery = patientsQuery.Where(p => p.IsActive);
                }
                var patients = await patientsQuery
                    .OrderBy(p => p.FullName)
                    .ToListAsync();

                ViewBag.ShowInactive = showInactive;
                return View(patients);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar los pacientes: {ex.Message}";
                return View(new List<Patient>());
            }
        }

        // =====================================================
        // GET: Patient/Details/5 - VER DETALLES DE UN PACIENTE
        // =====================================================
        // Get the patient by id
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de paciente no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var patient = await _context.Patients
                    .Include(p => p.Appointments)
                        .ThenInclude(a => a.Doctor)
                    .Include(p => p.Appointments)
                        .ThenInclude(a => a.EmailHistory)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                {
                    TempData["Error"] = "Paciente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(patient);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el paciente: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // GET: Patient/Create - MOSTRAR FORMULARIO DE CREACIÓN
        // =====================================================
        // Show creation form
        public IActionResult Create()
        {
            return View();
        }

        // =====================================================
        // POST: Patient/Create - CREAR NUEVO PACIENTE
        // =====================================================
        // Create new patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,DocumentNumber,Age,Phone,Email")] Patient patient)
        {
            // Validación de documento único
            var existingPatient = await _context.Patients
                .AnyAsync(p => p.DocumentNumber == patient.DocumentNumber);

            if (existingPatient)
            {
                ModelState.AddModelError("DocumentNumber", "Ya existe un paciente con este documento");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ⭐ Por defecto se crea activo
                    patient.IsActive = true;
                    
                    _context.Add(patient);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $"Paciente '{patient.FullName}' creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error al crear el paciente: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error inesperado: {ex.Message}";
                }
            }

            return View(patient);
        }

        // =====================================================
        // GET: Patient/Edit/5 - MOSTRAR FORMULARIO DE EDICIÓN
        // =====================================================
        // Show edit form
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de paciente no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var patient = await _context.Patients.FindAsync(id);
                
                if (patient == null)
                {
                    TempData["Error"] = "Paciente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(patient);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el paciente: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // POST: Patient/Edit/5 - ACTUALIZAR PACIENTE
        // =====================================================
        // Update patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,DocumentNumber,Age,Phone,Email,IsActive")] Patient patient)
        {
            if (id != patient.Id)
            {
                TempData["Error"] = "ID de paciente no coincide";
                return RedirectToAction(nameof(Index));
            }

            // Validación de documento único (excluyendo el paciente actual)
            var existingPatient = await _context.Patients
                .AnyAsync(p => p.DocumentNumber == patient.DocumentNumber && p.Id != patient.Id);

            if (existingPatient)
            {
                ModelState.AddModelError("DocumentNumber", "Ya existe otro paciente con este documento");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $"Paciente '{patient.FullName}' actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
                    {
                        TempData["Error"] = "El paciente ya no existe";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "Error de concurrencia al actualizar el paciente";
                    }
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error al actualizar el paciente: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error inesperado: {ex.Message}";
                }
            }

            return View(patient);
        }

        // =====================================================
        // GET: Patient/Deactivate/5 - CONFIRMAR DESACTIVACIÓN
        // =====================================================
        // Confirm deactivation
        public async Task<IActionResult> Deactivate(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de paciente no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var patient = await _context.Patients
                    .Include(p => p.Appointments)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                {
                    TempData["Error"] = "Paciente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si ya está inactivo
                if (!patient.IsActive)
                {
                    TempData["Error"] = "Este paciente ya está desactivado";
                    return RedirectToAction(nameof(Index));
                }

                return View(patient);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el paciente: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // POST: Patient/Deactivate/5 - DESACTIVAR PACIENTE
        // =====================================================
        // Deactivate patient
        [HttpPost, ActionName("Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateConfirmed(int id)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.Appointments)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                {
                    TempData["Error"] = "Paciente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si tiene citas programadas
                var hasActiveAppointments = patient.Appointments
                    .Any(a => a.Status == "Scheduled");

                if (hasActiveAppointments)
                {
                    TempData["Error"] = "No se puede desactivar el paciente porque tiene citas programadas. Debe cancelar o completar las citas primero.";
                    return View("Deactivate", patient);
                }

                // ⭐ BORRADO LÓGICO
                patient.IsActive = false;
                _context.Update(patient);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Paciente '{patient.FullName}' desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Error al desactivar el paciente: {ex.InnerException?.Message ?? ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error inesperado: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // POST: Patient/Reactivate/5 - REACTIVAR PACIENTE
        // =====================================================
        // Reactivate patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);

                if (patient == null)
                {
                    TempData["Error"] = "Paciente no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (patient.IsActive)
                {
                    TempData["Error"] = "Este paciente ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                patient.IsActive = true;
                _context.Update(patient);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Paciente '{patient.FullName}' reactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al reactivar el paciente: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // MÉTODO AUXILIAR - VERIFICAR SI EXISTE UN PACIENTE
        // =====================================================
        // Auxiliary method - check if a patient exists
        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }
    }
}