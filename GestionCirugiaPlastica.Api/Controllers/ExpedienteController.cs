using GestionCirugiaPlastica.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.Api.Controllers;

[ApiController]
[Route("api/Pacientes/expediente")]
[Authorize(Roles = "Doctor")]
public sealed class ExpedienteController(IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory) : ControllerBase
{
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

        return Ok(new { paciente, seguro = new { tieneSeguro = paciente.TieneSeguro || polizas.Count > 0, polizas }, citas, operaciones, fotografias });
    }
}
