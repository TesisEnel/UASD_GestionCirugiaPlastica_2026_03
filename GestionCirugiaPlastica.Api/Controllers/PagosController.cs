using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PagosController(
    PagosServices pagosService
) : ControllerBase
{
    // POST: api/Pagos  (registrar o actualizar un pago)
    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] Pago pago)
    {
        var resultado = await pagosService.Guardar(pago);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Pagos/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var resultado = await pagosService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Pagos
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var resultado = await pagosService.GetList(p => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Pagos/cirugia/1
    [HttpGet("cirugia/{cirugiaId:int}")]
    public async Task<IActionResult> BuscarPorCirugia(int cirugiaId)
    {
        var resultado = await pagosService.BuscarPorCirugia(cirugiaId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Pagos/cita/1
    [HttpGet("cita/{citaId:int}")]
    public async Task<IActionResult> BuscarPorCita(int citaId)
    {
        var resultado = await pagosService.BuscarPorCita(citaId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Pagos/fecha?fecha=2026-03-10
    [HttpGet("fecha")]
    public async Task<IActionResult> BuscarPorFecha([FromQuery] DateTime fecha)
    {
        var resultado = await pagosService.BuscarPorFecha(fecha);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Pagos/rango-fechas?fechaInicio=2026-03-01&fechaFin=2026-03-31
    [HttpGet("rango-fechas")]
    public async Task<IActionResult> BuscarPorRangoFechas(
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin)
    {
        var resultado = await pagosService.BuscarPorRangoFechas(fechaInicio, fechaFin);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/Pagos/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await pagosService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}