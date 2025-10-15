using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Examen_NET.Data;
using Examen_NET.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Examen_NET.Controllers
{
    public class DoctorController : Controller
    {
        private readonly AppDbContext _context;

        public DoctorController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET: Doctor/Index - LISTAR TODOS LOS MÉDICOS CON FILTRO
        // =====================================================
        public async Task<IActionResult> Index(string specialty, bool showInactive = false)
        {
            try
            {
                ViewBag.ShowInactive = showInactive;
                ViewBag.CurrentSpecialty = specialty;

                // Consulta base
                var doctorsQuery = _context.Doctors.AsQueryable();

                // ⭐ CORREGIDO: Filtrar por estado
                if (showInactive)
                {
                    // Mostrar SOLO inactivos
                    doctorsQuery = doctorsQuery.Where(d => !d.IsActive);
                }
                else
                {
                    // Mostrar SOLO activos
                    doctorsQuery = doctorsQuery.Where(d => d.IsActive);
                }

                // Aplicar filtro de especialidad
                if (!string.IsNullOrEmpty(specialty))
                {
                    doctorsQuery = doctorsQuery.Where(d => d.Specialty == specialty);
                }

                // ⭐ Obtener especialidades DESPUÉS de filtrar por estado (para que el dropdown sea correcto)
                var specialtiesQuery = _context.Doctors.AsQueryable();
                if (showInactive)
                {
                    specialtiesQuery = specialtiesQuery.Where(d => !d.IsActive);
                }
                else
                {
                    specialtiesQuery = specialtiesQuery.Where(d => d.IsActive);
                }
        
                var specialties = await specialtiesQuery
                    .Select(d => d.Specialty)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                ViewBag.Specialties = new SelectList(specialties);

                // Aplicar filtro si se seleccionó una especialidad
                if (!string.IsNullOrEmpty(specialty))
                {
                    doctorsQuery = doctorsQuery.Where(d => d.Specialty == specialty);
                }

                var doctors = await doctorsQuery
                    .OrderBy(d => d.FullName)
                    .ToListAsync();

                return View(doctors);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar los médicos: {ex.Message}";
                return View(new List<Doctor>());
            }
        }

        // =====================================================
        // GET: Doctor/Details/5 - VER DETALLES DE UN MÉDICO
        // =====================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de médico no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.Appointments)
                        .ThenInclude(a => a.Patient)
                    .Include(d => d.Appointments)
                        .ThenInclude(a => a.EmailHistory)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (doctor == null)
                {
                    TempData["Error"] = "Médico no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(doctor);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el médico: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // GET: Doctor/Create - MOSTRAR FORMULARIO DE CREACIÓN
        // =====================================================
        public IActionResult Create()
        {
            return View();
        }

        // =====================================================
        // POST: Doctor/Create - CREAR NUEVO MÉDICO
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,DocumentNumber,Specialty,Phone,Email")] Doctor doctor)
        {
            // Validación de documento único
            var existingDoctor = await _context.Doctors
                .AnyAsync(d => d.DocumentNumber == doctor.DocumentNumber);

            if (existingDoctor)
            {
                ModelState.AddModelError("DocumentNumber", "Ya existe un médico con este documento");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ⭐ Por defecto se crea activo
                    doctor.IsActive = true;
                    
                    _context.Add(doctor);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $"Médico '{doctor.FullName}' creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error al crear el médico: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error inesperado: {ex.Message}";
                }
            }

            return View(doctor);
        }

        // =====================================================
        // GET: Doctor/Edit/5 - MOSTRAR FORMULARIO DE EDICIÓN
        // =====================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de médico no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var doctor = await _context.Doctors.FindAsync(id);
                
                if (doctor == null)
                {
                    TempData["Error"] = "Médico no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                return View(doctor);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el médico: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // POST: Doctor/Edit/5 - ACTUALIZAR MÉDICO
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,DocumentNumber,Specialty,Phone,Email,IsActive")] Doctor doctor)
        {
            if (id != doctor.Id)
            {
                TempData["Error"] = "ID de médico no coincide";
                return RedirectToAction(nameof(Index));
            }

            // Validación de documento único (excluyendo el médico actual)
            var existingDoctor = await _context.Doctors
                .AnyAsync(d => d.DocumentNumber == doctor.DocumentNumber && d.Id != doctor.Id);

            if (existingDoctor)
            {
                ModelState.AddModelError("DocumentNumber", "Ya existe otro médico con este documento");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = $"Médico '{doctor.FullName}' actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.Id))
                    {
                        TempData["Error"] = "El médico ya no existe";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Error"] = "Error de concurrencia al actualizar el médico";
                    }
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error al actualizar el médico: {ex.InnerException?.Message ?? ex.Message}";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error inesperado: {ex.Message}";
                }
            }

            return View(doctor);
        }

        // =====================================================
        // GET: Doctor/Deactivate/5 - CONFIRMAR DESACTIVACIÓN
        // =====================================================
        public async Task<IActionResult> Deactivate(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de médico no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.Appointments)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (doctor == null)
                {
                    TempData["Error"] = "Médico no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si ya está inactivo
                if (!doctor.IsActive)
                {
                    TempData["Error"] = "Este médico ya está desactivado";
                    return RedirectToAction(nameof(Index));
                }

                return View(doctor);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar el médico: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // POST: Doctor/Deactivate/5 - DESACTIVAR MÉDICO
        // =====================================================
        [HttpPost, ActionName("Deactivate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateConfirmed(int id)
        {
            try
            {
                var doctor = await _context.Doctors
                    .Include(d => d.Appointments)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (doctor == null)
                {
                    TempData["Error"] = "Médico no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si tiene citas programadas
                var hasActiveAppointments = doctor.Appointments
                    .Any(a => a.Status == "Scheduled");

                if (hasActiveAppointments)
                {
                    TempData["Error"] = "No se puede desactivar el médico porque tiene citas programadas. Debe cancelar o completar las citas primero.";
                    return View("Deactivate", doctor);
                }

                // ⭐ BORRADO LÓGICO
                doctor.IsActive = false;
                _context.Update(doctor);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Médico '{doctor.FullName}' desactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Error al desactivar el médico: {ex.InnerException?.Message ?? ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error inesperado: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // POST: Doctor/Reactivate/5 - REACTIVAR MÉDICO
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            try
            {
                var doctor = await _context.Doctors.FindAsync(id);

                if (doctor == null)
                {
                    TempData["Error"] = "Médico no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (doctor.IsActive)
                {
                    TempData["Error"] = "Este médico ya está activo";
                    return RedirectToAction(nameof(Index));
                }

                doctor.IsActive = true;
                _context.Update(doctor);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Médico '{doctor.FullName}' reactivado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al reactivar el médico: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =====================================================
        // MÉTODO AUXILIAR - VERIFICAR SI EXISTE UN MÉDICO
        // =====================================================
        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.Id == id);
        }
    }
}