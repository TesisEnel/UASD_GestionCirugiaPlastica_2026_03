
using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.API.Controllers;

[ApiController]
[Authorize(Roles = "Doctora,AsistenteClinico")]
[Route("api/[controller]")]
public class ControlesPostoperatoriosController(
    ControlesPostoperatoriosService controlesService,
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : ControllerBase
{
    // GET: api/ControlesPostoperatorios
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await controlesService.GetList(c => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/ControlesPostoperatorios/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var resultado = await controlesService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/ControlesPostoperatorios/buscar/nombre/María
    [HttpGet("buscar/nombre/{nombre}")]
    public async Task<IActionResult> BuscarPorNombre(string nombre)
    {
        var resultado = await controlesService.BuscarPorNombre(nombre);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/ControlesPostoperatorios/buscar/cedula/00000000000
    [HttpGet("buscar/cedula/{cedula}")]
    public async Task<IActionResult> BuscarPorCedula(string cedula)
    {
        var resultado = await controlesService.BuscarPorCedula(cedula);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // POST: api/ControlesPostoperatorios
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ControlesPostoperatorio control)
    {
        var resultado = await controlesService.Guardar(control);
        if (resultado.IsSuccess && await AuditoriaClinica.PacienteDeCirugiaAsync(contextFactory, control.CirugiaId) is int pacienteId)
            await AuditoriaClinica.RegistrarAsync(contextFactory, User, pacienteId, "ControlPostoperatorio", control.ControlId == 0 ? null : control.ControlId, control.ControlId == 0 ? "Creacion" : "Actualizacion", "Evolución postoperatoria actualizada");
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/ControlesPostoperatorios/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await controlesService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}
