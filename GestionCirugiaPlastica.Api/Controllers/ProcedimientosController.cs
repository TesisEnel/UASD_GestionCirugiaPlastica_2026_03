using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Doctora,AsistenteClinico")]
public class ProcedimientosController(
    ProcedimientosServices procedimientosService
) : ControllerBase
{
    // POST: api/Procedimientos  (registrar o actualizar un procedimiento)
    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] Procedimiento procedimiento)
    {
        var resultado = await procedimientosService.Guardar(procedimiento);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Procedimientos/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var resultado = await procedimientosService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Procedimientos
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var resultado = await procedimientosService.GetList(p => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Procedimientos/nombre?nombre=plastia
    [HttpGet("nombre")]
    public async Task<IActionResult> BuscarPorNombre([FromQuery] string nombre)
    {
        var resultado = await procedimientosService.BuscarPorNombre(nombre);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Procedimientos/rango-precio?precioMinimo=10000&precioMaximo=50000
    [HttpGet("rango-precio")]
    public async Task<IActionResult> BuscarPorRangoPrecio(
        [FromQuery] decimal precioMinimo,
        [FromQuery] decimal precioMaximo)
    {
        var resultado = await procedimientosService.BuscarPorRangoPrecio(precioMinimo, precioMaximo);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Procedimientos/cirugia/1  (procedimiento usado en esa cirugía)
    [HttpGet("cirugia/{cirugiaId:int}")]
    public async Task<IActionResult> BuscarPorCirugia(int cirugiaId)
    {
        var resultado = await procedimientosService.BuscarPorCirugia(cirugiaId);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // DELETE: api/Procedimientos/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await procedimientosService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}
