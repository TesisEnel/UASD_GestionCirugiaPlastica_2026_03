using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class HistorialEstadosCitasServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<HistorialEstadosCitas, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error HistorialRequerido =
        new("HistorialEstadosCitas.Requerido", "Debe enviar los datos del cambio de estado.");

    private static readonly Error NoSeEdita =
        new("HistorialEstadosCitas.NoSeEdita", "El historial de estados solo permite registros nuevos.");

    private static readonly Error CitaRequerida =
        new("HistorialEstadosCitas.CitaRequerida", "Debe indicar una cita válida.");

    private static readonly Error EstadoNuevoRequerido =
        new("HistorialEstadosCitas.EstadoNuevoRequerido", "El estado nuevo es obligatorio.");

    private static readonly Error TextoMuyLargo =
        new("HistorialEstadosCitas.TextoMuyLargo", "El estado anterior, el estado nuevo y las observaciones no pueden pasar de 250 caracteres.");

    private static readonly Error CitaNoExiste =
        new("HistorialEstadosCitas.CitaNoExiste", "La cita indicada no existe.");

    private static readonly Error UsuarioInvalido =
        new("HistorialEstadosCitas.UsuarioInvalido", "El usuario indicado no existe o está inactivo.");

    private static readonly Error ErrorAlGuardar =
        new("HistorialEstadosCitas.ErrorAlGuardar", "No se pudo guardar el cambio de estado.");

    private static readonly Error EliminarNoPermitido =
        new("HistorialEstadosCitas.EliminarNoPermitido", "No se permite eliminar registros del historial de citas.");

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(HistorialEstadosCitas historial)
    {
        if (historial == null)
            return Result.Failure(HistorialRequerido);

        // El historial es un registro de auditoría: solo se insertan filas nuevas.
        if (historial.HistorialEstadoCitaId != 0)
            return Result.Failure(NoSeEdita);

        if (historial.CitaId <= 0)
            return Result.Failure(CitaRequerida);

        if (string.IsNullOrWhiteSpace(historial.EstadoNuevo))
            return Result.Failure(EstadoNuevoRequerido);

        if (historial.EstadoNuevo.Trim().Length > 250 ||
            historial.EstadoAnterior?.Trim().Length > 250 ||
            historial.Observaciones?.Trim().Length > 250)
            return Result.Failure(TextoMuyLargo);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        bool citaExiste = await contexto.Citas
            .AnyAsync(c => c.CitaId == historial.CitaId);

        if (!citaExiste)
            return Result.Failure(CitaNoExiste);

        if (historial.UsuarioId.HasValue)
        {
            bool usuarioValido = await contexto.Usuarios
                .AnyAsync(u => u.UsuarioId == historial.UsuarioId.Value && u.Activo);

            if (!usuarioValido)
                return Result.Failure(UsuarioInvalido);
        }

        try
        {
            // Se copian solo los campos propios, así no se guarda por accidente
            // la Cita o el Usuario que vengan anidados en el JSON.
            contexto.HistorialEstadosCita.Add(new HistorialEstadosCitas
            {
                // Si el cliente no manda fecha, la pone el servidor.
                FechaCambio = historial.FechaCambio == default ? DateTime.Now : historial.FechaCambio,
                CitaId = historial.CitaId,
                EstadoAnterior = historial.EstadoAnterior?.Trim(),
                EstadoNuevo = historial.EstadoNuevo.Trim(),
                UsuarioId = historial.UsuarioId,
                Observaciones = historial.Observaciones?.Trim()
            });

            await contexto.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // ── Consultas ──────────────────────────────────────────────────────────
    public async Task<Result<HistorialEstadosCitas?>> Buscar(int historialEstadoCitaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var historial = await contexto.HistorialEstadosCita
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HistorialEstadoCitaId == historialEstadoCitaId);

        return Result<HistorialEstadosCitas?>.Success(historial);
    }

    public async Task<Result<List<HistorialEstadosCitas>>> GetList(
        Expression<Func<HistorialEstadosCitas, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var historial = await contexto.HistorialEstadosCita
            .AsNoTracking()
            .Where(criterio)
            .OrderByDescending(h => h.FechaCambio)
            .ThenByDescending(h => h.HistorialEstadoCitaId)
            .ToListAsync();

        return Result<List<HistorialEstadosCitas>>.Success(historial);
    }

    public async Task<Result<List<HistorialEstadosCitas>>> BuscarPorCita(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var historial = await contexto.HistorialEstadosCita
            .AsNoTracking()
            .Where(h => h.CitaId == citaId)
            .OrderByDescending(h => h.FechaCambio)
            .ThenByDescending(h => h.HistorialEstadoCitaId)
            .ToListAsync();

        return Result<List<HistorialEstadosCitas>>.Success(historial);
    }

    public async Task<Result<List<HistorialEstadosCitas>>> BuscarPorUsuario(int usuarioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var historial = await contexto.HistorialEstadosCita
            .AsNoTracking()
            .Where(h => h.UsuarioId == usuarioId)
            .OrderByDescending(h => h.FechaCambio)
            .ThenByDescending(h => h.HistorialEstadoCitaId)
            .ToListAsync();

        return Result<List<HistorialEstadosCitas>>.Success(historial);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    // El historial es de auditoría: se devuelve un fallo en vez de lanzar excepción.
    public Task<Result> Eliminar(int historialEstadoCitaId)
        => Task.FromResult(Result.Failure(EliminarNoPermitido));
}