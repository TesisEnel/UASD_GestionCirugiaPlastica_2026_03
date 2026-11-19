using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.API.Controllers;

[ApiController]
[Authorize(Roles = "Doctora,AsistenteClinico")]
[Route("api/[controller]")]
public class CirugiasController : ControllerBase
{
    private readonly IServiceResult<Cirugia, int> _cirugiasService;

    public CirugiasController(
        IServiceResult<Cirugia, int> cirugiasService)
    {
        _cirugiasService = cirugiasService;
    }

    // GET: api/Cirugias
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await _cirugiasService.GetList(c => true);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return Ok(new { Value = resultado.Value?.Select(ResumenClinico) ?? Enumerable.Empty<object>() });
    }

    // GET: api/Cirugias/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var resultado = await _cirugiasService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        if (resultado.Value is null)
            return NotFound();

        return Ok(new { Value = resultado.Value is null ? null : ResumenClinico(resultado.Value) });
    }

    private static object ResumenClinico(Cirugia cirugia) => new
    {
        cirugia.CirugiaId,
        cirugia.FechaCirugia,
        cirugia.PacienteId,
        cirugia.ProcedimientoId,
        cirugia.DiagnosticoPreoperatorio,
        cirugia.Observaciones,
        cirugia.Estado,
        Paciente = cirugia.Paciente is null ? null : new
        {
            cirugia.Paciente.PacienteId,
            cirugia.Paciente.Nombre,
            cirugia.Paciente.Apellido
        },
        Procedimiento = cirugia.Procedimiento is null ? null : new
        {
            cirugia.Procedimiento.ProcedimientoId,
            cirugia.Procedimiento.Nombre
        }
    };

    // POST: api/Cirugias
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Cirugia cirugia)
    {
        var resultado = await _cirugiasService.Guardar(cirugia);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] Cirugia cirugia)
    {
        if (id != cirugia.CirugiaId)
            return BadRequest("El ID de la ruta no coincide con el ID de la operación.");

        var resultado = await _cirugiasService.Guardar(cirugia);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/Cirugias/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existente = await _cirugiasService.Buscar(id);

        if (existente.IsSuccess && existente.Value is null)
            return NotFound();

        var resultado = await _cirugiasService.Eliminar(id);

        if (resultado.IsFailure)
            return Conflict(resultado);

        return Ok(resultado);
    }
}
