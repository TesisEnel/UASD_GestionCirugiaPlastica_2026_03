using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HistorialEstadosCitasController(
    HistorialEstadosCitasServices historialService
) : ControllerBase
{
    // POST: api/HistorialEstadosCitas  (registrar un cambio de estado)
    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] HistorialEstadosCitas historial)
    {
        var resultado = await historialService.Guardar(historial);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/HistorialEstadosCitas/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var resultado = await historialService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/HistorialEstadosCitas
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var resultado = await historialService.GetList(h => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/HistorialEstadosCitas/cita/1
    [HttpGet("cita/{citaId:int}")]
    public async Task<IActionResult> BuscarPorCita(int citaId)
    {
        var resultado = await historialService.BuscarPorCita(citaId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/HistorialEstadosCitas/usuario/1
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> BuscarPorUsuario(int usuarioId)
    {
        var resultado = await historialService.BuscarPorUsuario(usuarioId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}