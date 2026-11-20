using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Doctora,AsistenteClinico")]
public class RecetasController(
    RecetasServices recetasService,
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : ControllerBase
{
    // POST: api/Recetas  (registrar o actualizar una receta)
    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] Receta receta)
    {
        var resultado = await recetasService.Guardar(receta);
        if (resultado.IsSuccess) await AuditoriaClinica.RegistrarAsync(contextFactory, User, receta.PacienteId, "Receta", receta.RecetaId == 0 ? null : receta.RecetaId, receta.RecetaId == 0 ? "Creacion" : "Actualizacion", "Prescripción médica actualizada");
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Recetas/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var resultado = await recetasService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Recetas
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var resultado = await recetasService.GetList(r => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Recetas/paciente/1
    [HttpGet("paciente/{pacienteId:int}")]
    public async Task<IActionResult> BuscarPorPaciente(int pacienteId)
    {
        var resultado = await recetasService.BuscarPorPaciente(pacienteId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Recetas/consulta/1
    [HttpGet("consulta/{consultaId:int}")]
    public async Task<IActionResult> BuscarPorConsulta(int consultaId)
    {
        var resultado = await recetasService.BuscarPorConsulta(consultaId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Recetas/cirugia/1
    [HttpGet("cirugia/{cirugiaId:int}")]
    public async Task<IActionResult> BuscarPorCirugia(int cirugiaId)
    {
        var resultado = await recetasService.BuscarPorCirugia(cirugiaId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Recetas/usuario/1
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> BuscarPorUsuario(int usuarioId)
    {
        var resultado = await recetasService.BuscarPorUsuario(usuarioId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Recetas/rango-fechas?fechaDesde=2026-03-01&fechaHasta=2026-03-31
    [HttpGet("rango-fechas")]
    public async Task<IActionResult> BuscarPorFecha(
        [FromQuery] DateTime fechaDesde,
        [FromQuery] DateTime fechaHasta)
    {
        var resultado = await recetasService.BuscarPorFecha(fechaDesde, fechaHasta);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Recetas/medicamento?medicamento=Ibuprofeno
    [HttpGet("medicamento")]
    public async Task<IActionResult> BuscarPorMedicamento([FromQuery] string medicamento)
    {
        var resultado = await recetasService.BuscarPorMedicamento(medicamento);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}


