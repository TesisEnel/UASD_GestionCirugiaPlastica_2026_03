using Aplicada1.Core;
using GestionCirugiaPlastica.Application.Services;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Doctora,Secretaria")]
public class PagosController(
    PagosServices pagosService,
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] Pago pago)
    {
        var resultado = await pagosService.Guardar(pago);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    [HttpGet("opciones-cirugias")]
    public async Task<IActionResult> OpcionesCirugias()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var opciones = await db.Cirugias.AsNoTracking()
            .OrderByDescending(c => c.FechaCirugia)
            .Select(c => new
            {
                c.CirugiaId,
                c.PacienteId,
                c.FechaCirugia,
                Paciente = c.Paciente.Nombre + " " + c.Paciente.Apellido
            })
            .ToListAsync();

        // Este endpoint entrega solo datos necesarios para vincular un cobro;
        // no expone diagnósticos ni detalles clínicos a recepción.
        return Ok(opciones);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Buscar(int id) => Respuesta(await pagosService.Buscar(id));

    [HttpGet]
    public async Task<IActionResult> GetList() => Respuesta(await pagosService.GetList(p => true));

    [HttpGet("cirugia/{cirugiaId:int}")]
    public async Task<IActionResult> BuscarPorCirugia(int cirugiaId) => Respuesta(await pagosService.BuscarPorCirugia(cirugiaId));

    [HttpGet("cita/{citaId:int}")]
    public async Task<IActionResult> BuscarPorCita(int citaId) => Respuesta(await pagosService.BuscarPorCita(citaId));

    [HttpGet("fecha")]
    public async Task<IActionResult> BuscarPorFecha([FromQuery] DateTime fecha) => Respuesta(await pagosService.BuscarPorFecha(fecha));

    [HttpGet("rango-fechas")]
    public async Task<IActionResult> BuscarPorRangoFechas([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin) =>
        Respuesta(await pagosService.BuscarPorRangoFechas(fechaInicio, fechaFin));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await pagosService.Eliminar(id);
        return resultado.IsSuccess ? Ok(resultado) : BadRequest(resultado);
    }

    private IActionResult Respuesta(Result<Pago?> resultado)
    {
        if (resultado.IsFailure) return BadRequest(resultado);
        return Ok(new { resultado.IsSuccess, Value = resultado.Value is null ? null : Resumen(resultado.Value) });
    }

    private IActionResult Respuesta(Result<List<Pago>> resultado)
    {
        if (resultado.IsFailure) return BadRequest(resultado);
        return Ok(new { resultado.IsSuccess, Value = (resultado.Value ?? new()).Select(Resumen).ToList() });
    }

    private static PagoResumen Resumen(Pago pago)
    {
        var paciente = pago.Paciente ?? pago.Cita?.Paciente ?? pago.Cirugia?.Paciente;
        return new PagoResumen(
            pago.PagoId,
            pago.Fecha,
            pago.PacienteId ?? paciente?.PacienteId,
            paciente is null ? "Paciente" : $"{paciente.Nombre} {paciente.Apellido}",
            pago.CitaId,
            pago.Cita?.Fecha,
            pago.Cita?.TipoCita,
            pago.CirugiaId,
            pago.Cirugia?.FechaCirugia,
            pago.Monto,
            pago.MontoSeguro,
            pago.Seguro?.Nombre,
            pago.MetodoPago,
            pago.Concepto,
            pago.Estado);
    }

    private sealed record PagoResumen(
        int PagoId,
        DateTime Fecha,
        int? PacienteId,
        string Paciente,
        int? CitaId,
        DateTime? FechaCita,
        string? TipoCita,
        int? CirugiaId,
        DateTime? FechaCirugia,
        decimal Monto,
        decimal MontoSeguro,
        string? Aseguradora,
        string? MetodoPago,
        string? Concepto,
        string Estado);
}
