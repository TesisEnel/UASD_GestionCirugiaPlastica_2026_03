using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class GastosService(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Gasto, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error ConceptoRequerido =
        new("Gasto.ConceptoRequerido", "El concepto es obligatorio.");

    private static readonly Error MontoInvalido =
        new("Gasto.MontoInvalido", "El monto debe ser mayor que cero.");

    private static readonly Error TextoMuyLargo =
        new("Gasto.TextoMuyLargo", "Concepto, categoría y método de pago no pueden pasar de 250 caracteres.");

    private static readonly Error UsuarioNoExiste =
        new("Gasto.UsuarioNoExiste", "El usuario indicado no existe.");

    private static readonly Error GastoNoExiste =
        new("Gasto.NoExiste", "El gasto indicado no existe.");

    private static readonly Error ErrorAlGuardar =
        new("Gasto.ErrorAlGuardar", "No se pudo guardar el gasto.");

    private static readonly Error ErrorAlEliminar =
        new("Gasto.ErrorAlEliminar", "No se pudo eliminar el gasto.");

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(Gasto gasto)
    {
        if (gasto == null || string.IsNullOrWhiteSpace(gasto.Concepto))
            return Result.Failure(ConceptoRequerido);

        if (gasto.Monto <= 0)
            return Result.Failure(MontoInvalido);

        if (gasto.Concepto.Trim().Length > 250 ||
            gasto.Categoria?.Length > 250 ||
            gasto.MetodoPago?.Length > 250)
        {
            return Result.Failure(TextoMuyLargo);
        }

        await using var contexto = await contextFactory.CreateDbContextAsync();

        // El usuario es opcional, pero si viene debe existir.
        if (gasto.UsuarioId.HasValue)
        {
            bool usuarioExiste = await contexto.Usuarios
                .AnyAsync(u => u.UsuarioId == gasto.UsuarioId.Value);

            if (!usuarioExiste)
                return Result.Failure(UsuarioNoExiste);
        }

        try
        {
            if (gasto.GastoId == 0)
            {
                // Se copian solo los campos propios del gasto. Así no se guarda
                // por accidente el Usuario que venga anidado en el JSON.
                // Si no viene fecha, se usa la actual (igual que el default de la base).
                var nuevo = new Gasto
                {
                    Fecha = gasto.Fecha == default ? DateTime.Now : gasto.Fecha,
                    Concepto = gasto.Concepto.Trim(),
                    Categoria = gasto.Categoria?.Trim(),
                    Monto = gasto.Monto,
                    MetodoPago = gasto.MetodoPago?.Trim(),
                    Observaciones = gasto.Observaciones?.Trim(),
                    UsuarioId = gasto.UsuarioId
                };

                contexto.Gastos.Add(nuevo);
            }
            else
            {
                var existente = await contexto.Gastos
                    .FirstOrDefaultAsync(g => g.GastoId == gasto.GastoId);

                if (existente == null)
                    return Result.Failure(GastoNoExiste);

                // Si no viene fecha se conserva la que ya tenía (no se pisa con 01/01/0001).
                if (gasto.Fecha != default)
                    existente.Fecha = gasto.Fecha;

                existente.Concepto = gasto.Concepto.Trim();
                existente.Categoria = gasto.Categoria?.Trim();
                existente.Monto = gasto.Monto;
                existente.MetodoPago = gasto.MetodoPago?.Trim();
                existente.Observaciones = gasto.Observaciones?.Trim();
                existente.UsuarioId = gasto.UsuarioId;
            }

            await contexto.SaveChangesAsync();

            // Si no cambió nada, SaveChanges devuelve 0 pero no es un error.
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            // Red de seguridad por si la base rechaza algo que no se validó arriba.
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // ── Consultas ──────────────────────────────────────────────────────────
    // Buscar gasto por ID
    public async Task<Result<Gasto?>> Buscar(int gastoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var gasto = await contexto.Gastos
            .AsNoTracking()
            .Include(g => g.Usuario)
            .FirstOrDefaultAsync(g => g.GastoId == gastoId);

        return Result<Gasto?>.Success(gasto);
    }

    // Buscar gastos de una fecha específica
    public async Task<Result<List<Gasto>>> BuscarPorFecha(DateTime fecha)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        // Rango [inicio, fin) en vez de g.Fecha.Date: así la base puede usar índices
        // y se comporta igual en SQL Server y SQLite.
        DateTime inicio = fecha.Date;
        DateTime fin = inicio.AddDays(1);

        var gastos = await contexto.Gastos
            .AsNoTracking()
            .Include(g => g.Usuario)
            .Where(g => g.Fecha >= inicio && g.Fecha < fin)
            .ToListAsync();

        return Result<List<Gasto>>.Success(gastos);
    }

    // Buscar gastos entre dos fechas (el día final se incluye completo)
    public async Task<Result<List<Gasto>>> BuscarPorRangoFechas(
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        if (fechaInicio.Date > fechaFin.Date)
            return Result<List<Gasto>>.Success(new List<Gasto>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        DateTime inicio = fechaInicio.Date;
        DateTime fin = fechaFin.Date.AddDays(1);

        var gastos = await contexto.Gastos
            .AsNoTracking()
            .Include(g => g.Usuario)
            .Where(g => g.Fecha >= inicio && g.Fecha < fin)
            .ToListAsync();

        return Result<List<Gasto>>.Success(gastos);
    }

    // Listar gastos mediante un criterio personalizado
    public async Task<Result<List<Gasto>>> GetList(
        Expression<Func<Gasto, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var gastos = await contexto.Gastos
            .AsNoTracking()
            .Include(g => g.Usuario)
            .Where(criterio)
            .ToListAsync();

        return Result<List<Gasto>>.Success(gastos);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int gastoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        try
        {
            bool eliminado = await contexto.Gastos
                .Where(g => g.GastoId == gastoId)
                .ExecuteDeleteAsync() > 0;

            return eliminado ? Result.Success() : Result.Failure(GastoNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}