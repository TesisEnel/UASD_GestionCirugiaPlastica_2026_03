using GestionCirugiaPlastica.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.Api.Controllers;

[ApiController]
[Route("api/Pacientes/expediente")]
[Authorize(Roles = "Doctora,AsistenteClinico")]
public sealed class ExpedienteController(IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory) : ControllerBase
{
    [HttpGet("selector")]
    public async Task<IActionResult> SelectorClinico()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var pacientes = await db.Pacientes.AsNoTracking()
            .OrderBy(p => p.Apellido).ThenBy(p => p.Nombre)
            .Select(p => new { p.PacienteId, p.Nombre, p.Apellido, p.Cedula })
            .ToListAsync();
        return Ok(new { Value = pacientes });
    }

    [HttpGet("{criterio}")]
    public async Task<IActionResult> Obtener(string criterio)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var clave = criterio.Trim();
        var query = db.Pacientes.AsNoTracking();

        if (int.TryParse(clave, out var id))
            query = query.Where(p => p.PacienteId == id);
        else if (clave.Contains('@'))
            query = query.Where(p => p.Cedula == clave);
        else
            query = query.Where(p => p.Cedula == clave || (p.Nombre + " " + p.Apellido).Contains(clave) || p.Nombre.Contains(clave) || p.Apellido.Contains(clave));

        var matches = await query.Select(p => p.PacienteId).Take(2).ToListAsync();
        if (matches.Count == 0) return NotFound(new { message = "No se encontró el paciente." });
        if (matches.Count > 1) return Conflict(new { message = "El nombre coincide con varios pacientes; busca por cédula o ID." });

        var pacienteId = matches[0];
        var paciente = await db.Pacientes.AsNoTracking()
            .Where(p => p.PacienteId == pacienteId)
            .Select(p => new { p.PacienteId, p.Nombre, p.Apellido, p.Cedula, p.FechaNacimiento, p.Sexo, p.Telefono, p.Email, p.Direccion, p.FechaRegistro, p.TieneSeguro })
            .SingleAsync();
        var polizas = await db.PacienteSeguros.AsNoTracking().Include(x => x.Seguro)
            .Where(x => x.PacienteId == pacienteId)
            .Select(x => new { x.PacienteSeguroId, x.SeguroId, Aseguradora = x.Seguro.Nombre, x.NumeroPoliza, x.Titular, x.FechaVencimiento, x.Observaciones })
            .ToListAsync();
        var citas = await db.Citas.AsNoTracking().Where(c => c.PacienteId == pacienteId)
            .OrderByDescending(c => c.Fecha)
            .Select(c => new { c.CitaId, c.Fecha, c.TipoCita, c.Estado, c.EstadoPago, c.Observaciones })
            .ToListAsync();
        var operaciones = await db.Cirugias.AsNoTracking().Include(c => c.Procedimiento)
            .Where(c => c.PacienteId == pacienteId)
            .OrderByDescending(c => c.FechaCirugia)
            .Select(c => new { c.CirugiaId, c.FechaCirugia, c.ProcedimientoId, Procedimiento = c.Procedimiento.Nombre, c.DiagnosticoPreoperatorio, c.Estado, c.Observaciones })
            .ToListAsync();
        var fotografias = await db.Fotografias.AsNoTracking()
            .Where(f => f.Cirugia.PacienteId == pacienteId)
            .OrderBy(f => f.Fecha)
            .Select(f => new { f.FotografiaId, f.CirugiaId, f.Fecha, f.Tipo, f.Descripcion })
            .ToListAsync();

        if (User.IsInRole("AsistenteClinico"))
        {
            var pacienteClinico = new { paciente.PacienteId, paciente.Nombre, paciente.Apellido, paciente.Cedula };
            return Ok(new
            {
                paciente = pacienteClinico,
                seguro = new { tieneSeguro = false, polizas = Array.Empty<object>() },
                citas = Array.Empty<object>(), operaciones, fotografias,
                historial = (object?)null,
                consultas = Array.Empty<object>(), preoperatorios = Array.Empty<object>(),
                controles = Array.Empty<object>(), recetas = Array.Empty<object>(),
                pagos = Array.Empty<object>(), documentos = Array.Empty<object>(), auditoria = Array.Empty<object>()
            });
        }

        var historial = await db.HistorialClinicos.AsNoTracking()
            .Where(h => h.PacienteId == pacienteId)
            .Select(h => new { h.HistoriaId, h.FechaActualizacion, h.Antecedentes, h.AntecedentesPersonales, h.AntecedentesFamiliares, h.Alergias, h.Medicamentos, h.NotasMedicas })
            .SingleOrDefaultAsync();
        var consultas = await db.Consultas.AsNoTracking().Where(c => c.PacienteId == pacienteId)
            .OrderByDescending(c => c.Fecha)
            .Select(c => new { c.ConsultaId, c.Fecha, c.CitaId, c.Motivo, c.Diagnostico, c.Observaciones })
            .ToListAsync();
        var preoperatorios = await db.Preoperatorios.AsNoTracking().Where(p => p.Cirugia.PacienteId == pacienteId)
            .OrderByDescending(p => p.FechaEvaluacion)
            .Select(p => new { p.PreoperatorioId, p.CirugiaId, p.FechaEvaluacion, p.AnalisisLaboratorio, p.EvaluacionCardiologica, p.EvaluacionAnestesica, p.Autorizado, p.Observaciones })
            .ToListAsync();
        var controles = await db.ControlesPostoperatorios.AsNoTracking().Where(c => c.Cirugia.PacienteId == pacienteId)
            .OrderByDescending(c => c.Fecha)
            .Select(c => new { c.ControlId, c.CirugiaId, c.Fecha, c.DiasPostoperatorio, c.Evolucion, c.Dolor, c.Inflamacion, c.Observaciones, c.ProximoControl })
            .ToListAsync();
        var recetas = await db.Recetas.AsNoTracking().Where(r => r.PacienteId == pacienteId)
            .OrderByDescending(r => r.Fecha)
            .Select(r => new { r.RecetaId, r.Fecha, r.ConsultaId, r.CirugiaId, r.Indicaciones, Detalles = r.DetallesReceta.Select(d => new { d.Medicamento, d.Dosis, d.Frecuencia, d.Duracion, d.Instrucciones }).ToList() })
            .ToListAsync();
        var pagos = await db.Pagos.AsNoTracking().Where(p => (p.Cita != null && p.Cita.PacienteId == pacienteId) || (p.Cirugia != null && p.Cirugia.PacienteId == pacienteId))
            .OrderByDescending(p => p.Fecha)
            .Select(p => new { p.PagoId, p.Fecha, p.Monto, p.MetodoPago, p.CitaId, p.CirugiaId, p.Concepto })
            .ToListAsync();
        var documentos = await db.DocumentosClinicos.AsNoTracking().Where(d => d.PacienteId == pacienteId)
            .OrderByDescending(d => d.FechaCarga)
            .Select(d => new { d.DocumentoClinicoId, d.Categoria, d.NombreArchivo, d.ContentType, d.Descripcion, d.FechaCarga, d.CirugiaId, d.ConsultaId })
            .ToListAsync();
        var auditoria = await db.AuditoriaExpedientes.AsNoTracking().Where(a => a.PacienteId == pacienteId)
            .OrderByDescending(a => a.Fecha).Take(100)
            .Select(a => new { a.Fecha, a.Entidad, a.RegistroId, a.Accion, a.Detalle, Usuario = a.UsuarioId })
            .ToListAsync();

        return Ok(new { paciente, seguro = new { tieneSeguro = paciente.TieneSeguro || polizas.Count > 0, polizas }, citas, operaciones, fotografias, historial, consultas, preoperatorios, controles, recetas, pagos, documentos, auditoria });
    }
}
