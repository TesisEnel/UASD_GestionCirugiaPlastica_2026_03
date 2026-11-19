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
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory,
    CitasService citasService
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
            .Where(c => c.PacienteId == pacienteId.Value && c.Fecha >= DateTime.Now)
            .OrderBy(c => c.Fecha)
            .Select(c => new { c.CitaId, c.Fecha, c.TipoCita, c.Estado })
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
        var result = await citasService.Guardar(cita);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
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
}
