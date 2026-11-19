using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.API.Controllers;

[ApiController]
[Authorize(Roles = "Doctora")]
[Route("api/[controller]")]
public class GastosController(GastosService gastosService) : ControllerBase
{
    // GET: api/Gastos
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await gastosService.GetList(g => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Gastos/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var resultado = await gastosService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Gastos/fecha/2026-03-10
    [HttpGet("fecha/{fecha}")]
    public async Task<IActionResult> BuscarPorFecha(DateTime fecha)
    {
        var resultado = await gastosService.BuscarPorFecha(fecha);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Gastos/rango/2026-03-01/2026-03-31
    [HttpGet("rango/{fechaInicio}/{fechaFin}")]
    public async Task<IActionResult> BuscarPorRangoFechas(DateTime fechaInicio, DateTime fechaFin)
    {
        var resultado = await gastosService.BuscarPorRangoFechas(fechaInicio, fechaFin);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // POST: api/Gastos
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Gasto gasto)
    {
        var resultado = await gastosService.Guardar(gasto);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/Gastos/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await gastosService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}
