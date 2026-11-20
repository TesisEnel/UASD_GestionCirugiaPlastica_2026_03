using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionCirugiaPlastica.Api.Controllers;

[ApiController]
[Route("api/PortalPaciente")]
[Authorize(Roles = "Paciente")]
public sealed class PortalPacienteController(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : ControllerBase
{
    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        var pacienteId = await ResolverPacienteIdAsync();
        if (pacienteId is null) return Ok(PerfilSinVinculo());

        await using var db = await contextFactory.CreateDbContextAsync();
        var paciente = await db.Pacientes.AsNoTracking()
            .Where(p => p.PacienteId == pacienteId.Value)
            .Select(p => new
            {
                Vinculado = true,
                p.PacienteId,
                p.Nombre,
                p.Apellido,
                p.Cedula,
                p.Telefono,
                p.Email,
                p.TieneSeguro
            })
            .SingleOrDefaultAsync();

        return paciente is null ? Ok(PerfilSinVinculo()) : Ok(paciente);
    }

    [HttpGet("citas")]
    public async Task<IActionResult> Citas()
    {
        var pacienteId = await ResolverPacienteIdAsync();
        if (pacienteId is null) return Ok(Array.Empty<object>());

        await using var db = await contextFactory.CreateDbContextAsync();
        var citas = await db.Citas.AsNoTracking()
            .Where(c => c.PacienteId == pacienteId.Value && c.Fecha >= DateTime.Now && c.Estado != "Cancelada")
            .OrderBy(c => c.Fecha)
            .Select(c => new { c.CitaId, c.Fecha, c.TipoCita, c.Estado, c.Observaciones })
            .ToListAsync();
        return Ok(citas);
    }

    [HttpPost("citas")]
    public async Task<IActionResult> Agendar([FromBody] SolicitudCita request)
    {
        var pacienteId = await ResolverPacienteIdAsync();
        if (pacienteId is null)
            return Conflict("Tu cuenta todavía no está vinculada a una ficha de paciente. Contacta a recepción para solicitar la vinculación.");
        if (request.Fecha <= DateTime.Now) return BadRequest("La cita debe tener una fecha y hora futura.");
        if (string.IsNullOrWhiteSpace(request.TipoCita)) return BadRequest("Selecciona el tipo de cita.");
        if (request.TipoCita.Trim().Length > 250) return BadRequest("El tipo de cita es demasiado largo.");

        await using var db = await contextFactory.CreateDbContextAsync();
        var paciente = await db.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.PacienteId == pacienteId.Value);
        if (paciente is null) return Conflict("No se encontró la ficha de paciente asociada a tu cuenta.");
        await using var transaction = await db.Database.BeginTransactionAsync();

        var cita = new Cita
        {
            Fecha = request.Fecha,
            PacienteId = pacienteId.Value,
            TipoCita = request.TipoCita.Trim(),
            Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim(),
            Costo = 0,
            Estado = "Pendiente",
            EstadoPago = "Pendiente"
        };
        db.Citas.Add(cita);
        await db.SaveChangesAsync();
        db.Notificaciones.Add(new Notificacion
        {
            Tipo = "CitaAgendada",
            Titulo = "Nueva cita solicitada",
            Mensaje = $"{paciente.Nombre} {paciente.Apellido} solicitó una cita para {cita.Fecha:dd/MM/yyyy HH:mm}.",
            CitaId = cita.CitaId,
            FechaCreacion = DateTime.Now
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { cita.CitaId, cita.Fecha, cita.Estado });
    }

    [HttpPut("citas/{citaId:int}")]
    public async Task<IActionResult> ModificarCita(int citaId, [FromBody] ModificacionCita request)
    {
        var pacienteId = await ResolverPacienteIdAsync();
        if (pacienteId is null) return Conflict("Tu cuenta todavía no está vinculada a una ficha de paciente.");
        if (request.Fecha <= DateTime.Now) return BadRequest("La cita debe tener una fecha y hora futura.");

        await using var db = await contextFactory.CreateDbContextAsync();
        var cita = await db.Citas.Include(c => c.Paciente)
            .FirstOrDefaultAsync(c => c.CitaId == citaId && c.PacienteId == pacienteId.Value);
        if (cita is null) return NotFound("No encontramos esa cita en tu perfil.");
        if (cita.Estado == "Cancelada" || cita.Fecha < DateTime.Now)
            return Conflict("Solo puedes reprogramar citas próximas que no estén canceladas.");

        cita.Fecha = request.Fecha;
        db.Notificaciones.Add(new Notificacion
        {
            Tipo = "CitaModificada",
            Titulo = "Cita reprogramada",
            Mensaje = $"{cita.Paciente?.Nombre} {cita.Paciente?.Apellido} reprogramó su cita para {cita.Fecha:dd/MM/yyyy HH:mm}.",
            CitaId = cita.CitaId,
            FechaCreacion = DateTime.Now
        });
        await db.SaveChangesAsync();
        return Ok(new { cita.CitaId, cita.Fecha, cita.Estado });
    }

    [HttpDelete("citas/{citaId:int}")]
    public async Task<IActionResult> CancelarCita(int citaId)
    {
        var pacienteId = await ResolverPacienteIdAsync();
        if (pacienteId is null) return Conflict("Tu cuenta todavía no está vinculada a una ficha de paciente.");

        await using var db = await contextFactory.CreateDbContextAsync();
        var cita = await db.Citas.Include(c => c.Paciente)
            .FirstOrDefaultAsync(c => c.CitaId == citaId && c.PacienteId == pacienteId.Value);
        if (cita is null) return NotFound("No encontramos esa cita en tu perfil.");
        if (cita.Fecha < DateTime.Now || cita.Estado == "Cancelada")
            return Conflict("Solo puedes cancelar citas próximas que no estén canceladas.");

        cita.Estado = "Cancelada";
        db.Notificaciones.Add(new Notificacion
        {
            Tipo = "CitaCancelada",
            Titulo = "Cita cancelada",
            Mensaje = $"{cita.Paciente?.Nombre} {cita.Paciente?.Apellido} canceló la cita del {cita.Fecha:dd/MM/yyyy HH:mm}.",
            CitaId = cita.CitaId,
            FechaCreacion = DateTime.Now
        });
        await db.SaveChangesAsync();
        return Ok(new { Mensaje = "La cita fue cancelada." });
    }

    [HttpGet("seguro")]
    public async Task<IActionResult> Seguro()
    {
        var pacienteId = await ResolverPacienteIdAsync();
        if (pacienteId is null) return Ok(Array.Empty<object>());

        await using var db = await contextFactory.CreateDbContextAsync();
        var polizas = await db.PacienteSeguros.AsNoTracking()
            .Where(p => p.PacienteId == pacienteId.Value)
            .OrderByDescending(p => p.FechaRegistro)
            .Select(p => new { p.PacienteSeguroId, Seguro = p.Seguro.Nombre, p.NumeroPoliza, p.FechaVencimiento, p.Observaciones })
            .ToListAsync();
        return Ok(polizas);
    }

    [HttpGet("recetas")]
    public async Task<IActionResult> Recetas()
    {
        var pacienteId = await ResolverPacienteIdAsync();
        if (pacienteId is null) return Ok(Array.Empty<object>());

        await using var db = await contextFactory.CreateDbContextAsync();
        var recetas = await db.Recetas.AsNoTracking()
            .Include(r => r.DetallesReceta)
            .Where(r => r.PacienteId == pacienteId.Value)
            .OrderByDescending(r => r.Fecha)
            .ToListAsync();

        return Ok(recetas.Select(r => new
        {
            r.RecetaId,
            r.Fecha,
            r.Indicaciones,
            Medicamentos = r.DetallesReceta.OrderBy(d => d.DetalleRecetaId).Select(d => new
            {
                d.Medicamento,
                d.Dosis,
                d.Frecuencia,
                d.Duracion,
                d.Instrucciones
            }).ToList()
        }));
    }

    private async Task<int?> ResolverPacienteIdAsync()
    {
        var usuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!int.TryParse(usuarioClaim, out var usuarioId)) return null;

        await using var db = await contextFactory.CreateDbContextAsync();
        var pacienteId = await db.Usuarios.AsNoTracking()
            .Where(u => u.UsuarioId == usuarioId && u.Activo)
            .Select(u => u.PacienteId)
            .SingleOrDefaultAsync();
        if (pacienteId is null) return null;

        return await db.Pacientes.AsNoTracking().AnyAsync(p => p.PacienteId == pacienteId.Value)
            ? pacienteId
            : null;
    }

    private static object PerfilSinVinculo() => new
    {
        Vinculado = false,
        PacienteId = (int?)null,
        Nombre = (string?)null,
        Apellido = (string?)null,
        Cedula = (string?)null,
        Telefono = (string?)null,
        Email = (string?)null,
        TieneSeguro = false
    };

    public sealed record SolicitudCita(DateTime Fecha, string? TipoCita, string? Observaciones);
    public sealed record ModificacionCita(DateTime Fecha);
}
