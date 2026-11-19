using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.API.Controllers;

[ApiController]
[Authorize(Roles = "Doctora,Secretaria")]
[Route("api/[controller]")]
public class CitasController : ControllerBase
{
    private readonly CitasService _citasService;

    public CitasController(CitasService citasService)
    {
        _citasService = citasService;
    }

    // GET: api/Citas
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await _citasService.GetList(c => true);
        return Ok(resultado);
    }

    // GET: api/Citas/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var resultado = await _citasService.Buscar(id);
        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Citas/buscar/nombre/María
    [HttpGet("buscar/nombre/{nombre}")]
    public async Task<IActionResult> BuscarPorNombre(string nombre)
    {
        var resultado = await _citasService.BuscarPorNombre(nombre);
        return Ok(resultado);
    }

    // GET: api/Citas/buscar/cedula/00000000000
    [HttpGet("buscar/cedula/{cedula}")]
    public async Task<IActionResult> BuscarPorCedula(string cedula)
    {
        var resultado = await _citasService.BuscarPorCedula(cedula);
        return Ok(resultado);
    }

    // POST: api/Citas
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Cita cita)
    {
        var resultado = await _citasService.Guardar(cita);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // PUT: api/Citas/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] Cita cita)
    {
        if (id != cita.CitaId)
            return BadRequest("El ID de la ruta no coincide con el ID de la cita.");

        var existente = await _citasService.Buscar(id);
        if (existente.Value is null) return NotFound("La cita no existe.");

        var estadosPermitidos = new[] { "Pendiente", "Confirmada", "Llegó", "Atendida", "Completada", "Cancelada" };
        if (string.IsNullOrWhiteSpace(cita.Estado) || !estadosPermitidos.Contains(cita.Estado.Trim(), StringComparer.OrdinalIgnoreCase))
            return BadRequest("El estado indicado no es válido.");

        var resultado = await _citasService.Guardar(cita);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // PATCH: api/Citas/5/estado
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> ActualizarEstado(
        int id,
        [FromBody] string estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
            return BadRequest("Debe indicar un estado.");

        var estadosPermitidos = new[]
        {
        "Pendiente",
        "Confirmada",
        "Llegó",
        "Atendida",
        "Completada",
        "Cancelada"
    };

        if (!estadosPermitidos.Contains(
            estado.Trim(),
            StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("El estado indicado no es válido.");
        }

        var resultado = await _citasService.Buscar(id);

        if (resultado.Value is null)
            return NotFound("La cita no existe.");

        var cita = resultado.Value;
        cita.Estado = estado.Trim();

        var actualizado = await _citasService.Guardar(cita);

        return actualizado.IsSuccess
            ? Ok(actualizado)
            : BadRequest(actualizado);
    }

    // DELETE: api/Citas/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resultado = await _citasService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}

