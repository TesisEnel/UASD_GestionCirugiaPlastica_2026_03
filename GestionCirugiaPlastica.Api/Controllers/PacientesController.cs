using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PacientesController(
    IServiceResult<Paciente, int> pacientesService
) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Guardar(Paciente paciente)
    {
        var resultado = await pacientesService.Guardar(paciente);


        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var resultado = await pacientesService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);


        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var resultado = await pacientesService.GetList(p => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Pacientes/buscar?nombre=María
    // Busca por nombre, apellido o nombre completo ("María Reyes").
    [HttpGet("buscar")]
    public async Task<IActionResult> BuscarPorNombre([FromQuery] string nombre)
    {
        var resultado = await pacientesService.GetList(
            p => p.Nombre.Contains(nombre) ||
                 p.Apellido.Contains(nombre) ||
                 (p.Nombre + " " + p.Apellido).Contains(nombre)
        );

        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Pacientes/buscar/cedula?cedula=001
    // Coincidencia parcial: sirve con la cédula completa o solo una parte.
    [HttpGet("buscar/cedula")]
    public async Task<IActionResult> BuscarPorCedula([FromQuery] string cedula)
    {
        var resultado = await pacientesService.GetList(
            p => p.Cedula != null && p.Cedula.Contains(cedula)
        );

        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Pacientes/buscar/telefono?telefono=809
    [HttpGet("buscar/telefono")]
    public async Task<IActionResult> BuscarPorTelefono([FromQuery] string telefono)
    {
        var resultado = await pacientesService.GetList(
            p => p.Telefono != null && p.Telefono.Contains(telefono)
        );

        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Pacientes/buscar/email?email=gmail
    [HttpGet("buscar/email")]
    public async Task<IActionResult> BuscarPorEmail([FromQuery] string email)
    {
        var resultado = await pacientesService.GetList(
            p => p.Email != null && p.Email.Contains(email)
        );

        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await pacientesService.Eliminar(id);


        return resultado.IsSuccess ? Ok(resultado) : Conflict(resultado);
    }
}
