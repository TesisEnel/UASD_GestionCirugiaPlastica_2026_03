using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Doctor")]
public class PreoperatoriosController(
    PreoperatoriosServices preoperatoriosService
) : ControllerBase
{
    // POST: api/Preoperatorios  (registrar una nueva evaluación)
    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] Preoperatorio preoperatorio)
    {
        var resultado = await preoperatoriosService.Guardar(preoperatorio);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Preoperatorios/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var resultado = await preoperatoriosService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Preoperatorios
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var resultado = await preoperatoriosService.GetList(p => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Preoperatorios/historial/1
    [HttpGet("historial/{cirugiaId:int}")]
    public async Task<IActionResult> BuscarHistorial(int cirugiaId)
    {
        var resultado = await preoperatoriosService.BuscarHistorial(cirugiaId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Preoperatorios/cirugia/1
    [HttpGet("cirugia/{cirugiaId:int}")]
    public async Task<IActionResult> BuscarPorCirugia(int cirugiaId)
    {
        var resultado = await preoperatoriosService.BuscarPorCirugia(cirugiaId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Preoperatorios/autorizacion/true
    [HttpGet("autorizacion/{autorizado:bool}")]
    public async Task<IActionResult> BuscarPorAutorizacion(bool autorizado)
    {
        var resultado = await preoperatoriosService.BuscarPorAutorizacion(autorizado);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Preoperatorios/requisitos-pendientes
    [HttpGet("requisitos-pendientes")]
    public async Task<IActionResult> BuscarRequisitosPendientes()
    {
        var resultado = await preoperatoriosService.BuscarRequisitosPendientes();
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Preoperatorios/rango-fechas?fechaDesde=2026-03-01&fechaHasta=2026-03-31
    [HttpGet("rango-fechas")]
    public async Task<IActionResult> BuscarPorFecha(
        [FromQuery] DateTime fechaDesde,
        [FromQuery] DateTime fechaHasta)
    {
        var resultado = await preoperatoriosService.BuscarPorFecha(fechaDesde, fechaHasta);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/Preoperatorios/1  (siempre responde BadRequest: no se permite eliminar)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await preoperatoriosService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}
