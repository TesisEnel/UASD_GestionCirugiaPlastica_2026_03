using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using GestionCirugiaPlastica.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestionCirugiaPlastica.API.Controllers;

[ApiController]
[Authorize(Roles = "Doctora,AsistenteClinico")]
[Route("api/[controller]")]
public class HistorialClinicoController(HistorialClinicoService historialService, IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory) : ControllerBase
{
    // GET: api/HistorialClinico
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await historialService.GetList(h => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/HistorialClinico/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var resultado = await historialService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/HistorialClinico/buscar/nombre/María
    [HttpGet("buscar/nombre/{nombre}")]
    public async Task<IActionResult> BuscarPorNombre(string nombre)
    {
        var resultado = await historialService.BuscarPorNombre(nombre);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/HistorialClinico/buscar/cedula/00000000000
    [HttpGet("buscar/cedula/{cedula}")]
    public async Task<IActionResult> BuscarPorCedula(string cedula)
    {
        var resultado = await historialService.BuscarPorCedula(cedula);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // POST: api/HistorialClinico
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] HistorialClinico historial)
    {
        if (historial is null || historial.PacienteId <= 0) return BadRequest(new { message = "Indica el paciente del historial." });
        await using var db = await contextFactory.CreateDbContextAsync();
        var existente = await db.HistorialClinicos.AsNoTracking().FirstOrDefaultAsync(h => h.PacienteId == historial.PacienteId);
        var resultado = await historialService.Guardar(historial);
        if (resultado.IsSuccess)
        {
            var guardado = await db.HistorialClinicos.AsNoTracking().FirstAsync(h => h.PacienteId == historial.PacienteId);
            db.AuditoriaExpedientes.Add(new AuditoriaExpediente
            {
                PacienteId = historial.PacienteId,
                UsuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                Fecha = DateTime.UtcNow,
                Entidad = "HistorialClinico",
                RegistroId = guardado.HistoriaId,
                Accion = existente is null ? "Creacion" : "Actualizacion",
                Detalle = "Antecedentes, alergias, medicamentos y/o notas clínicas"
            });
            await db.SaveChangesAsync();
        }
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/HistorialClinico/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        return Conflict(new { message = "El historial clínico forma parte del expediente permanente y no se elimina desde esta API." });
    }
}
