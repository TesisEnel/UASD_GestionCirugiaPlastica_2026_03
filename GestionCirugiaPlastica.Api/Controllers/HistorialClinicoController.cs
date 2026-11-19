using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.API.Controllers;

[ApiController]
[Authorize(Roles = "Doctora,AsistenteClinico")]
[Route("api/[controller]")]
public class HistorialClinicoController(HistorialClinicoService historialService) : ControllerBase
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
        var resultado = await historialService.Guardar(historial);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/HistorialClinico/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await historialService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}
