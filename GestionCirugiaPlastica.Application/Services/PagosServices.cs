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
        new("Pago.DestinoRequerido", "El pago debe estar asociado a una cirugía o a una cita.");

    private static readonly Error DestinoDuplicado =
        new("Pago.DestinoDuplicado", "El pago no puede estar asociado a una cirugía y a una cita a la vez.");

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

        if (pago.CirugiaId == null && pago.CitaId == null)
            return Result.Failure(DestinoRequerido);

        if (pago.CirugiaId.HasValue && pago.CitaId.HasValue)
            return Result.Failure(DestinoDuplicado);

        if (pago.MetodoPago?.Trim().Length > 250 ||
            pago.Concepto?.Trim().Length > 250)
            return Result.Failure(TextoMuyLargo);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        if (pago.CirugiaId.HasValue)
        {
            bool cirugiaExiste = await contexto.Cirugias
                .AnyAsync(c => c.CirugiaId == pago.CirugiaId.Value);

            if (!cirugiaExiste)
                return Result.Failure(CirugiaNoExiste);
        }

        if (pago.CitaId.HasValue)
        {
            bool citaExiste = await contexto.Citas
                .AnyAsync(c => c.CitaId == pago.CitaId.Value);

            if (!citaExiste)
                return Result.Failure(CitaNoExiste);
        }

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
                    Monto = pago.Monto,
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
                existente.Monto = pago.Monto;
                existente.MetodoPago = pago.MetodoPago?.Trim();
                existente.Concepto = pago.Concepto?.Trim();
            }

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

        var pago = await contexto.Pagos
            .AsNoTracking()
            .Include(p => p.Cirugia)
            .Include(p => p.Cita)
            .FirstOrDefaultAsync(p => p.PagoId == pagoId);

        return Result<Pago?>.Success(pago);
    }

    // Listar pagos mediante un criterio personalizado
    public async Task<Result<List<Pago>>> GetList(
        Expression<Func<Pago, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pagos = await contexto.Pagos
            .AsNoTracking()
            .Include(p => p.Cirugia)
            .Include(p => p.Cita)
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

        var pagos = await contexto.Pagos
            .AsNoTracking()
            .Include(p => p.Cirugia)
            .Include(p => p.Cita)
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

        var pagos = await contexto.Pagos
            .AsNoTracking()
            .Include(p => p.Cirugia)
            .Include(p => p.Cita)
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

        var pagos = await contexto.Pagos
            .AsNoTracking()
            .Include(p => p.Cirugia)
            .Include(p => p.Cita)
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

        var pagos = await contexto.Pagos
            .AsNoTracking()
            .Include(p => p.Cirugia)
            .Include(p => p.Cita)
            .Where(p => p.Fecha >= inicio && p.Fecha < fin)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

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