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
    private int? PacienteId => int.TryParse(User.FindFirst("paciente_id")?.Value, out var id) ? id : null;

    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        if (PacienteId is not int id) return Forbid();
        await using var db = await contextFactory.CreateDbContextAsync();
        var paciente = await db.Pacientes.AsNoTracking()
            .Where(p => p.PacienteId == id)
            .Select(p => new { p.PacienteId, p.Nombre, p.Apellido, p.Cedula, p.Telefono, p.Email, p.TieneSeguro })
            .SingleOrDefaultAsync();
        return paciente is null ? NotFound() : Ok(paciente);
    }

    [HttpGet("citas")]
    public async Task<IActionResult> Citas()
    {
        if (PacienteId is not int id) return Forbid();
        await using var db = await contextFactory.CreateDbContextAsync();
        var citas = await db.Citas.AsNoTracking()
            .Where(c => c.PacienteId == id && c.Fecha >= DateTime.Now)
            .OrderBy(c => c.Fecha)
            .Select(c => new { c.CitaId, c.Fecha, c.TipoCita, c.Estado })
            .ToListAsync();
        return Ok(citas);
    }

    [HttpPost("citas")]
    public async Task<IActionResult> Agendar([FromBody] SolicitudCita request)
    {
        if (PacienteId is not int id) return Forbid();
        if (request.Fecha <= DateTime.Now) return BadRequest("La cita debe tener una fecha futura.");
        var cita = new Cita
        {
            Fecha = request.Fecha,
            PacienteId = id,
            TipoCita = request.TipoCita,
            Observaciones = request.Observaciones,
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
        if (PacienteId is not int id) return Forbid();
        await using var db = await contextFactory.CreateDbContextAsync();
        var polizas = await db.PacienteSeguros.AsNoTracking()
            .Include(p => p.Seguro)
            .Where(p => p.PacienteId == id)
            .Select(p => new { p.PacienteSeguroId, Seguro = p.Seguro.Nombre, p.NumeroPoliza, p.FechaVencimiento, p.Observaciones })
            .ToListAsync();
        return Ok(polizas);
    }

    public sealed record SolicitudCita(DateTime Fecha, string? TipoCita, string? Observaciones);
}
