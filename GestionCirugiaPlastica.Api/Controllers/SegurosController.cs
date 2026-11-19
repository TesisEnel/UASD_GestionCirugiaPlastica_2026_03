using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Doctor,Secretaria")]
public class SegurosController(SegurosServices segurosService) : ControllerBase
{
    // Registrar o actualizar una aseguradora
    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] Seguro seguro)
    {
        var resultado = await segurosService.Guardar(seguro);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Buscar aseguradora por ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var resultado = await segurosService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // Obtener todas las aseguradoras
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var resultado = await segurosService.GetList(s => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Buscar aseguradoras por nombre
    [HttpGet("nombre")]
    public async Task<IActionResult> BuscarPorNombre([FromQuery] string nombre)
    {
        var resultado = await segurosService.BuscarPorNombre(nombre);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Obtener aseguradoras activas
    [HttpGet("activos")]
    public async Task<IActionResult> BuscarActivos()
    {
        var resultado = await segurosService.BuscarActivos();
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Buscar seguros asociados a un paciente
    [HttpGet("paciente/{pacienteId:int}")]
    public async Task<IActionResult> BuscarPorPaciente(int pacienteId)
    {
        var resultado = await segurosService.BuscarPorPaciente(pacienteId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Buscar pacientes asociados a una aseguradora
    [HttpGet("{seguroId:int}/pacientes")]
    public async Task<IActionResult> BuscarPacientes(int seguroId)
    {
        var resultado = await segurosService.BuscarPacientes(seguroId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Registrar una póliza para un paciente
    [HttpPost("polizas")]
    public async Task<IActionResult> RegistrarSeguroPaciente([FromBody] PacienteSeguro pacienteSeguro)
    {
        var resultado = await segurosService.RegistrarSeguroPaciente(pacienteSeguro);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Actualizar una póliza existente
    [HttpPut("polizas")]
    public async Task<IActionResult> ActualizarSeguroPaciente([FromBody] PacienteSeguro pacienteSeguro)
    {
        var resultado = await segurosService.ActualizarSeguroPaciente(pacienteSeguro);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Buscar póliza por ID
    [HttpGet("polizas/{pacienteSeguroId:int}")]
    public async Task<IActionResult> BuscarPoliza(int pacienteSeguroId)
    {
        var resultado = await segurosService.BuscarPoliza(pacienteSeguroId);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // Buscar pólizas vencidas
    [HttpGet("polizas/vencidas")]
    public async Task<IActionResult> BuscarPolizasVencidas()
    {
        var resultado = await segurosService.BuscarPolizasVencidas();
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Buscar pólizas próximas a vencer
    [HttpGet("polizas/por-vencer")]
    public async Task<IActionResult> BuscarPolizasPorVencer([FromQuery] int diasAnticipacion = 30)
    {
        var resultado = await segurosService.BuscarPolizasPorVencer(diasAnticipacion);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Desactivar una aseguradora
    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var resultado = await segurosService.Desactivar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // Reactivar una aseguradora
    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id)
    {
        var resultado = await segurosService.Activar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // El servicio no permite eliminar aseguradoras (siempre devuelve fallo)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await segurosService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}
