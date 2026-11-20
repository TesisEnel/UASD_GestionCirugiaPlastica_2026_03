using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CitasController : ControllerBase
{
    private readonly CitasService _citasService;
    private readonly IDbContextFactory<GestionCirugiaPlasticaContext> _contextFactory;

    public CitasController(CitasService citasService, IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory)
    {
        _citasService = citasService;
        _contextFactory = contextFactory;
    }

    // GET: api/Citas
    [Authorize(Roles = "Doctora,AsistenteClinico,Secretaria")]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var resultado = await _citasService.GetList(c => true);
        return Ok(resultado);
    }

    // GET: api/Citas/5
    [Authorize(Roles = "Doctora,Secretaria")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var resultado = await _citasService.Buscar(id);
        return resultado.Value is null ? NotFound() : Ok(resultado);
    }

    // GET: api/Citas/buscar/nombre/María
    [Authorize(Roles = "Doctora,Secretaria")]
    [HttpGet("buscar/nombre/{nombre}")]
    public async Task<IActionResult> BuscarPorNombre(string nombre)
    {
        var resultado = await _citasService.BuscarPorNombre(nombre);
        return Ok(resultado);
    }

    // GET: api/Citas/buscar/cedula/00000000000
    [Authorize(Roles = "Doctora,Secretaria")]
    [HttpGet("buscar/cedula/{cedula}")]
    public async Task<IActionResult> BuscarPorCedula(string cedula)
    {
        var resultado = await _citasService.BuscarPorCedula(cedula);
        return Ok(resultado);
    }

    // POST: api/Citas
    [Authorize(Roles = "Doctora,Secretaria")]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Cita cita)
    {
        var resultado = await _citasService.Guardar(cita);
        if (!resultado.IsSuccess) return BadRequest(resultado);
        await RegistrarNotificacionAsync(cita.CitaId, "CitaAgendada", "Nueva cita registrada");
        return Ok(resultado);
    }

    // PUT: api/Citas/5
    [Authorize(Roles = "Doctora,Secretaria")]
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
        if (!resultado.IsSuccess) return BadRequest(resultado);

        var fueCancelada = !string.Equals(existente.Value.Estado, "Cancelada", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(cita.Estado, "Cancelada", StringComparison.OrdinalIgnoreCase);
        await RegistrarNotificacionAsync(
            id,
            fueCancelada ? "CitaCancelada" : "CitaModificada",
            fueCancelada ? "Cita cancelada" : "Cita modificada");
        return Ok(resultado);
    }

    // PATCH: api/Citas/5/estado
    [Authorize(Roles = "Doctora,Secretaria")]
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
        var estadoAnterior = cita.Estado;
        cita.Estado = estado.Trim();

        var actualizado = await _citasService.Guardar(cita);
        if (!actualizado.IsSuccess) return BadRequest(actualizado);

        var fueCancelada = !string.Equals(estadoAnterior, "Cancelada", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(cita.Estado, "Cancelada", StringComparison.OrdinalIgnoreCase);
        await RegistrarNotificacionAsync(
            id,
            fueCancelada ? "CitaCancelada" : "CitaModificada",
            fueCancelada ? "Cita cancelada" : "Estado de cita actualizado");
        return Ok(actualizado);
    }

    // DELETE: api/Citas/5
    [Authorize(Roles = "Doctora,Secretaria")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existente = await _citasService.Buscar(id);
        if (existente.Value is null) return NotFound("La cita no existe.");
        var resultado = await _citasService.Eliminar(id);
        if (!resultado.IsSuccess) return BadRequest(resultado);
        var mensaje = $"{existente.Value.Paciente?.Nombre} {existente.Value.Paciente?.Apellido} canceló/eliminó la cita del {existente.Value.Fecha:dd/MM/yyyy HH:mm}.";
        await RegistrarNotificacionAsync(null, "CitaCancelada", "Cita eliminada", mensaje);
        return Ok(resultado);
    }

    [AllowAnonymous]
    [HttpPost("publica")]
    public async Task<IActionResult> AgendarPublica([FromBody] SolicitudCitaPublica request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
            return BadRequest("Indica tu nombre y apellido.");
        if (request.Fecha <= DateTime.Now)
            return BadRequest("Selecciona una fecha y hora futuras.");
        if (string.IsNullOrWhiteSpace(request.TipoCita) || request.TipoCita.Trim().Length > 250)
            return BadRequest("Indica un tipo de cita válido.");

        await using var db = await _contextFactory.CreateDbContextAsync();
        Paciente? paciente = null;
        var cedula = string.IsNullOrWhiteSpace(request.Cedula) ? null : request.Cedula.Trim();
        var telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();

        if (cedula is not null)
        {
            paciente = await db.Pacientes.FirstOrDefaultAsync(p => p.Cedula == cedula);
            if (paciente is not null && !string.Equals(paciente.Telefono?.Trim(), telefono, StringComparison.OrdinalIgnoreCase))
                return Conflict("La cédula ya está registrada con otro teléfono. Contacta a recepción para validar tus datos.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        if (paciente is null)
        {
            paciente = new Paciente
            {
                FechaRegistro = DateTime.Now,
                Nombre = request.Nombre.Trim(),
                Apellido = request.Apellido.Trim(),
                Cedula = cedula,
                Telefono = telefono,
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                TieneSeguro = false
            };
            db.Pacientes.Add(paciente);
            await db.SaveChangesAsync();
        }

        var codigo = Guid.NewGuid().ToString("N");
        var cita = new Cita
        {
            Fecha = request.Fecha,
            PacienteId = paciente.PacienteId,
            TipoCita = request.TipoCita.Trim(),
            Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim(),
            Costo = 0,
            Estado = "Pendiente",
            EstadoPago = "Pendiente",
            CodigoPublico = codigo
        };
        db.Citas.Add(cita);
        await db.SaveChangesAsync();
        db.Notificaciones.Add(new Notificacion
        {
            Tipo = "CitaAgendada",
            Titulo = "Nueva cita solicitada",
            Mensaje = $"{paciente.Nombre} {paciente.Apellido} solicitó una cita para {cita.Fecha:dd/MM/yyyy HH:mm}.",
            CitaId = cita.CitaId,
            FechaCreacion = DateTime.Now
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { cita.CitaId, cita.Fecha, cita.Estado, CodigoGestion = codigo });
    }

    [AllowAnonymous]
    [HttpPost("publica/cancelar")]
    public async Task<IActionResult> CancelarPublica([FromBody] CancelacionPublica request)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        Cita? cita;
        if (!string.IsNullOrWhiteSpace(request.CodigoGestion))
        {
            cita = await db.Citas.Include(c => c.Paciente)
                .FirstOrDefaultAsync(c => c.CodigoPublico == request.CodigoGestion.Trim());
        }
        else if (!string.IsNullOrWhiteSpace(request.Cedula) && !string.IsNullOrWhiteSpace(request.Telefono))
        {
            var citas = await db.Citas.Include(c => c.Paciente)
                .Where(c => c.Fecha >= DateTime.Now &&
                    c.Paciente != null && c.Paciente.Cedula == request.Cedula.Trim() && c.Paciente.Telefono == request.Telefono.Trim() &&
                    c.Estado != "Cancelada")
                .OrderBy(c => c.Fecha).ToListAsync();
            if (citas.Count > 1)
                return Conflict("Hay varias citas asociadas. Usa el código de gestión de la cita que quieres cancelar.");
            cita = citas.SingleOrDefault();
        }
        else
        {
            return BadRequest("Indica el código de gestión o tu cédula y teléfono.");
        }

        if (cita is null) return NotFound("No encontramos una cita con esos datos.");
        if (string.Equals(cita.Estado, "Cancelada", StringComparison.OrdinalIgnoreCase))
            return BadRequest("La cita ya está cancelada.");

        cita.Estado = "Cancelada";
        db.Notificaciones.Add(new Notificacion
        {
            Tipo = "CitaCancelada",
            Titulo = "Cita cancelada",
            Mensaje = $"{cita.Paciente?.Nombre} {cita.Paciente?.Apellido} canceló la cita del {cita.Fecha:dd/MM/yyyy HH:mm}.",
            CitaId = cita.CitaId,
            FechaCreacion = DateTime.Now
        });
        await db.SaveChangesAsync();
        return Ok(new { Mensaje = "La cita fue cancelada." });
    }

    public sealed record SolicitudCitaPublica(
        string Nombre, string Apellido, string? Cedula, string? Telefono, string? Email,
        DateTime Fecha, string TipoCita, string? Observaciones);

    public sealed record CancelacionPublica(string? CodigoGestion, string? Cedula, string? Telefono);

    private async Task RegistrarNotificacionAsync(int? citaId, string tipo, string titulo, string? mensajeAlternativo = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var cita = citaId.HasValue
            ? await db.Citas.Include(c => c.Paciente).FirstOrDefaultAsync(c => c.CitaId == citaId.Value)
            : null;
        var mensaje = mensajeAlternativo ?? (cita is null
            ? "Se actualizó una cita."
            : $"{cita.Paciente?.Nombre} {cita.Paciente?.Apellido}: {cita.TipoCita ?? "cita"} para {cita.Fecha:dd/MM/yyyy HH:mm}.");
        db.Notificaciones.Add(new Notificacion
        {
            Tipo = tipo,
            Titulo = titulo,
            Mensaje = mensaje,
            CitaId = cita?.CitaId,
            FechaCreacion = DateTime.Now
        });
        await db.SaveChangesAsync();
    }
}

