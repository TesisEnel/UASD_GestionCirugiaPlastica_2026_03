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
public class ConsultasController(ConsultasService consultasService, IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory) : ControllerBase
{
    // GET: api/Consultas
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await consultasService.GetList(c => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Consultas/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var resultado = await consultasService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Consultas/buscar/nombre/María
    [HttpGet("buscar/nombre/{nombre}")]
    public async Task<IActionResult> BuscarPorNombre(string nombre)
    {
        var resultado = await consultasService.BuscarPorNombre(nombre);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Consultas/buscar/cedula/00000000000
    [HttpGet("buscar/cedula/{cedula}")]
    public async Task<IActionResult> BuscarPorCedula(string cedula)
    {
        var resultado = await consultasService.BuscarPorCedula(cedula);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // POST: api/Consultas
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Consulta consulta)
    {
        var resultado = await consultasService.Guardar(consulta);
        if (resultado.IsSuccess) await AuditoriaClinica.RegistrarAsync(contextFactory, User, consulta.PacienteId, "Consulta", consulta.ConsultaId == 0 ? null : consulta.ConsultaId, consulta.ConsultaId == 0 ? "Creacion" : "Actualizacion", "Consulta o diagnóstico actualizado");
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/Consultas/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await consultasService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}
