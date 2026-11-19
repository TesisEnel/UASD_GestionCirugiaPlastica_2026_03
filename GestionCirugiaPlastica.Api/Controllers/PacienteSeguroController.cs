using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Doctora,Secretaria")]
public class PacienteSeguroController(
    PacientesSegurosServices pacientesSegurosService
) : ControllerBase
{
    // POST: api/PacienteSeguro  (registrar o actualizar)
    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] PacienteSeguro pacienteSeguro)
    {
        var resultado = await pacientesSegurosService.Guardar(pacienteSeguro);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/PacienteSeguro/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var resultado = await pacientesSegurosService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/PacienteSeguro
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var resultado = await pacientesSegurosService.GetList(p => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/PacienteSeguro/paciente/1
    [HttpGet("paciente/{pacienteId:int}")]
    public async Task<IActionResult> BuscarPorPaciente(int pacienteId)
    {
        var resultado = await pacientesSegurosService.BuscarPorPaciente(pacienteId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/PacienteSeguro/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await pacientesSegurosService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}
