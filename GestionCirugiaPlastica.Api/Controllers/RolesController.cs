using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Doctor")]
public class RolesController(
    RolesServices rolesService
) : ControllerBase
{
    // POST: api/Roles  (registrar o actualizar un rol)
    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] Role rol)
    {
        var resultado = await rolesService.Guardar(rol);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Roles/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var resultado = await rolesService.Buscar(id);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Roles
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var resultado = await rolesService.GetList(r => true);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Roles/nombre?nombre=admin
    [HttpGet("nombre")]
    public async Task<IActionResult> BuscarPorNombre([FromQuery] string nombre)
    {
        var resultado = await rolesService.BuscarPorNombre(nombre);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Roles/usuario/1  (rol asignado a ese usuario)
    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<IActionResult> BuscarPorUsuario(int usuarioId)
    {
        var resultado = await rolesService.BuscarPorUsuario(usuarioId);

        if (resultado.IsFailure)
            return BadRequest(resultado);

        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Roles/1/usuarios
    [HttpGet("{rolId:int}/usuarios")]
    public async Task<IActionResult> BuscarUsuarios(int rolId)
    {
        var resultado = await rolesService.BuscarUsuarios(rolId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Roles/1/tiene-usuarios
    [HttpGet("{rolId:int}/tiene-usuarios")]
    public async Task<IActionResult> TieneUsuarios(int rolId)
    {
        var resultado = await rolesService.TieneUsuarios(rolId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Roles/1/cantidad-usuarios
    [HttpGet("{rolId:int}/cantidad-usuarios")]
    public async Task<IActionResult> ContarUsuarios(int rolId)
    {
        var resultado = await rolesService.ContarUsuarios(rolId);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // GET: api/Roles/existe-administrador-activo
    [HttpGet("existe-administrador-activo")]
    public async Task<IActionResult> ExisteAdministradorActivo()
    {
        var resultado = await rolesService.ExisteAdministradorActivo();
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    // DELETE: api/Roles/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await rolesService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }
}
