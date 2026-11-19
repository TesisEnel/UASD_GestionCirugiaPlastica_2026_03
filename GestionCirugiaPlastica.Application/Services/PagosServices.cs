using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class PagosServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Pago, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error PagoRequerido =
        new("Pago.Requerido", "Debe enviar los datos del pago.");

    private static readonly Error MontoInvalido =
        new("Pago.MontoInvalido", "El monto del pago debe ser mayor que cero.");

    private static readonly Error DestinoRequerido =
        new("Pago.DestinoRequerido", "El pago debe estar asociado a un paciente, una cirugía o una cita.");

    private static readonly Error DestinoDuplicado =
        new("Pago.DestinoDuplicado", "El pago no puede estar asociado a una cirugía y a una cita a la vez.");

    private static readonly Error PacienteNoExiste =
        new("Pago.PacienteNoExiste", "El paciente indicado no existe.");

    private static readonly Error DestinoPacienteNoCoincide =
        new("Pago.DestinoPacienteNoCoincide", "El paciente debe coincidir con el paciente de la cita o cirugía seleccionada.");

    private static readonly Error SeguroPacienteNoCoincide =
        new("Pago.SeguroPacienteNoCoincide", "La aseguradora seleccionada no está vinculada a este paciente o no está activa.");

    private static readonly Error MontoSeguroInvalido =
        new("Pago.MontoSeguroInvalido", "La cobertura del seguro debe estar entre cero y el monto total.");

    private static readonly Error EstadoInvalido =
        new("Pago.EstadoInvalido", "El estado debe ser Pagado o Pendiente.");

    private static readonly Error TextoMuyLargo =
        new("Pago.TextoMuyLargo", "El método de pago y el concepto no pueden pasar de 250 caracteres.");

    private static readonly Error CirugiaNoExiste =
        new("Pago.CirugiaNoExiste", "La cirugía indicada no existe.");

    private static readonly Error CitaNoExiste =
        new("Pago.CitaNoExiste", "La cita indicada no existe.");

    private static readonly Error PagoNoExiste =
        new("Pago.NoExiste", "El pago indicado no existe.");

    private static readonly Error ErrorAlGuardar =
        new("Pago.ErrorAlGuardar", "No se pudo guardar el pago.");

    private static readonly Error ErrorAlEliminar =
        new("Pago.ErrorAlEliminar", "No se pudo eliminar el pago.");

    // ── Guardar ────────────────────────────────────────────────────────────
    // Registrar o modificar un pago
    public async Task<Result> Guardar(Pago pago)
    {
        if (pago == null)
            return Result.Failure(PagoRequerido);

        if (pago.Monto <= 0)
            return Result.Failure(MontoInvalido);

        if (pago.CirugiaId == null && pago.CitaId == null && pago.PacienteId == null)
            return Result.Failure(DestinoRequerido);

        if (pago.CirugiaId.HasValue && pago.CitaId.HasValue)
            return Result.Failure(DestinoDuplicado);

        if (pago.MontoSeguro < 0 || pago.MontoSeguro > pago.Monto)
            return Result.Failure(MontoSeguroInvalido);

        var estado = string.IsNullOrWhiteSpace(pago.Estado) ? "Pagado" : pago.Estado.Trim();
        if (estado is not ("Pagado" or "Pendiente"))
            return Result.Failure(EstadoInvalido);

        if (pago.MetodoPago?.Trim().Length > 250 ||
            pago.Concepto?.Trim().Length > 250)
            return Result.Failure(TextoMuyLargo);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        int? destinoPacienteId = pago.PacienteId;
        Cita? citaDestino = null;

        if (pago.PacienteId.HasValue && !await contexto.Pacientes.AnyAsync(p => p.PacienteId == pago.PacienteId.Value))
            return Result.Failure(PacienteNoExiste);

        if (pago.CirugiaId.HasValue)
        {
            var cirugiaPacienteId = await contexto.Cirugias
                .Where(c => c.CirugiaId == pago.CirugiaId.Value)
                .Select(c => (int?)c.PacienteId)
                .FirstOrDefaultAsync();

            if (cirugiaPacienteId is null)
                return Result.Failure(CirugiaNoExiste);

            if (destinoPacienteId.HasValue && destinoPacienteId != cirugiaPacienteId)
                return Result.Failure(DestinoPacienteNoCoincide);

            destinoPacienteId = cirugiaPacienteId;
        }

        if (pago.CitaId.HasValue)
        {
            citaDestino = await contexto.Citas
                .FirstOrDefaultAsync(c => c.CitaId == pago.CitaId.Value);

            if (citaDestino is null)
                return Result.Failure(CitaNoExiste);

            if (destinoPacienteId.HasValue && destinoPacienteId.Value != citaDestino.PacienteId)
                return Result.Failure(DestinoPacienteNoCoincide);

            destinoPacienteId = citaDestino.PacienteId;
        }

        if (!destinoPacienteId.HasValue)
            return Result.Failure(DestinoRequerido);

        if (pago.SeguroId.HasValue && !await contexto.PacienteSeguros.AnyAsync(ps =>
                ps.PacienteId == destinoPacienteId.Value && ps.SeguroId == pago.SeguroId.Value && ps.Seguro.Activo))
            return Result.Failure(SeguroPacienteNoCoincide);

        pago.PacienteId = destinoPacienteId;
        pago.Estado = estado;

        try
        {
            if (pago.PagoId == 0)
            {
                // Se copian solo los campos propios del pago, así no se guarda por
                // accidente la Cirugía o la Cita que vengan anidadas en el JSON.
                contexto.Pagos.Add(new Pago
                {
                    // La fecha del pago la pone el servidor.
                    Fecha = DateTime.Now,
                    CirugiaId = pago.CirugiaId,
                    CitaId = pago.CitaId,
                    PacienteId = destinoPacienteId,
                    SeguroId = pago.SeguroId,
                    Monto = pago.Monto,
                    MontoSeguro = pago.MontoSeguro,
                    Estado = estado,
                    MetodoPago = pago.MetodoPago?.Trim(),
                    Concepto = pago.Concepto?.Trim()
                });
            }
            else
            {
                var existente = await contexto.Pagos
                    .FirstOrDefaultAsync(p => p.PagoId == pago.PagoId);

                if (existente == null)
                    return Result.Failure(PagoNoExiste);

                // La fecha original del pago no se modifica.
                existente.CirugiaId = pago.CirugiaId;
                existente.CitaId = pago.CitaId;
                existente.PacienteId = destinoPacienteId;
                existente.SeguroId = pago.SeguroId;
                existente.Monto = pago.Monto;
                existente.MontoSeguro = pago.MontoSeguro;
                existente.Estado = estado;
                existente.MetodoPago = pago.MetodoPago?.Trim();
                existente.Concepto = pago.Concepto?.Trim();
            }

            if (citaDestino is not null)
                citaDestino.EstadoPago = estado;

            await contexto.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // ── Consultas ──────────────────────────────────────────────────────────
    // Buscar pago por ID
    public async Task<Result<Pago?>> Buscar(int pagoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pago = await ConRelaciones(contexto)
            .FirstOrDefaultAsync(p => p.PagoId == pagoId);

        return Result<Pago?>.Success(pago);
    }

    // Listar pagos mediante un criterio personalizado
    public async Task<Result<List<Pago>>> GetList(
        Expression<Func<Pago, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pagos = await ConRelaciones(contexto)
            .Where(criterio)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    // Buscar pagos por cirugía
    public async Task<Result<List<Pago>>> BuscarPorCirugia(int cirugiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pagos = await ConRelaciones(contexto)
            .Where(p => p.CirugiaId == cirugiaId)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    // Buscar pagos por cita
    public async Task<Result<List<Pago>>> BuscarPorCita(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pagos = await ConRelaciones(contexto)
            .Where(p => p.CitaId == citaId)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    // Buscar pagos de una fecha específica (todo el día, sin importar la hora)
    public async Task<Result<List<Pago>>> BuscarPorFecha(DateTime fecha)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        DateTime inicio = fecha.Date;
        DateTime fin = inicio.AddDays(1);

        var pagos = await ConRelaciones(contexto)
            .Where(p => p.Fecha >= inicio && p.Fecha < fin)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    // Buscar pagos entre dos fechas (ambas incluidas)
    public async Task<Result<List<Pago>>> BuscarPorRangoFechas(
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        if (fechaInicio.Date > fechaFin.Date)
            return Result<List<Pago>>.Success(new List<Pago>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        DateTime inicio = fechaInicio.Date;
        DateTime fin = fechaFin.Date.AddDays(1);

        var pagos = await ConRelaciones(contexto)
            .Where(p => p.Fecha >= inicio && p.Fecha < fin)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    private static IQueryable<Pago> ConRelaciones(GestionCirugiaPlasticaContext contexto) =>
        contexto.Pagos
            .AsNoTracking()
            .Include(p => p.Paciente)
            .Include(p => p.Seguro)
            .Include(p => p.Cirugia).ThenInclude(c => c!.Paciente)
            .Include(p => p.Cita).ThenInclude(c => c!.Paciente);

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int pagoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        try
        {
            bool eliminado = await contexto.Pagos
                .Where(p => p.PagoId == pagoId)
                .ExecuteDeleteAsync() > 0;

            return eliminado ? Result.Success() : Result.Failure(PagoNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}
