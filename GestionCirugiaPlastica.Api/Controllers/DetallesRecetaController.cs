using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DetallesRecetaController(
    DetallesRecetaService detallesService
) : ControllerBase
{
    // GET: api/DetallesReceta
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await detallesService.GetList(d => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/DetallesReceta/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var resultado = await detallesService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/DetallesReceta/buscar/nombre/María
    [HttpGet("buscar/nombre/{nombre}")]
    public async Task<IActionResult> BuscarPorNombre(string nombre)
    {
        var resultado = await detallesService.BuscarPorNombre(nombre);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/DetallesReceta/buscar/cedula/00000000000
    [HttpGet("buscar/cedula/{cedula}")]
    public async Task<IActionResult> BuscarPorCedula(string cedula)
    {
        var resultado = await detallesService.BuscarPorCedula(cedula);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // POST: api/DetallesReceta  (registrar o actualizar un detalle)
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] DetallesReceta detalle)
    {
        var resultado = await detallesService.Guardar(detalle);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/DetallesReceta/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await detallesService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}