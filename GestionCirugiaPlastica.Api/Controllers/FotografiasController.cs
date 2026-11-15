using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FotografiasController(FotografiasService fotografiasService) : ControllerBase
{
    // GET: api/Fotografias
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await fotografiasService.GetList(f => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Fotografias/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var resultado = await fotografiasService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Fotografias/buscar/nombre/María
    [HttpGet("buscar/nombre/{nombre}")]
    public async Task<IActionResult> BuscarPorNombre(string nombre)
    {
        var resultado = await fotografiasService.BuscarPorNombre(nombre);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Fotografias/buscar/cedula/00000000000
    [HttpGet("buscar/cedula/{cedula}")]
    public async Task<IActionResult> BuscarPorCedula(string cedula)
    {
        var resultado = await fotografiasService.BuscarPorCedula(cedula);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // POST: api/Fotografias
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Fotografia fotografia)
    {
        var resultado = await fotografiasService.Guardar(fotografia);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/Fotografias/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await fotografiasService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}