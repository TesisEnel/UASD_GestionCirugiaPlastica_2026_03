using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class ProcedimientosServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Procedimiento, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error ProcedimientoRequerido =
        new("Procedimiento.Requerido", "Debe enviar los datos del procedimiento.");

    private static readonly Error NombreRequerido =
        new("Procedimiento.NombreRequerido", "El nombre del procedimiento es obligatorio.");

    private static readonly Error NombreMuyLargo =
        new("Procedimiento.NombreMuyLargo", "El nombre del procedimiento no puede pasar de 250 caracteres.");

    private static readonly Error PrecioInvalido =
        new("Procedimiento.PrecioInvalido", "El precio base no puede ser negativo.");

    private static readonly Error NombreDuplicado =
        new("Procedimiento.NombreDuplicado", "Ya existe un procedimiento con ese nombre.");

    private static readonly Error ProcedimientoNoExiste =
        new("Procedimiento.NoExiste", "El procedimiento indicado no existe.");

    private static readonly Error TieneCirugias =
        new("Procedimiento.TieneCirugias", "No se puede eliminar un procedimiento que tiene cirugías asociadas.");

    private static readonly Error ErrorAlGuardar =
        new("Procedimiento.ErrorAlGuardar", "No se pudo guardar el procedimiento.");

    private static readonly Error ErrorAlEliminar =
        new("Procedimiento.ErrorAlEliminar", "No se pudo eliminar el procedimiento.");

    // ── Guardar ────────────────────────────────────────────────────────────
    // Guardar o actualizar un procedimiento
    public async Task<Result> Guardar(Procedimiento procedimiento)
    {
        if (procedimiento == null)
            return Result.Failure(ProcedimientoRequerido);

        if (string.IsNullOrWhiteSpace(procedimiento.Nombre))
            return Result.Failure(NombreRequerido);

        if (procedimiento.Nombre.Trim().Length > 250)
            return Result.Failure(NombreMuyLargo);

        if (procedimiento.PrecioBase < 0)
            return Result.Failure(PrecioInvalido);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        string nombre = procedimiento.Nombre.Trim();

        // Evitar nombres duplicados (sin distinguir mayúsculas), excluyendo el propio registro.
        bool nombreExiste = await contexto.Procedimientos
            .AnyAsync(p =>
                p.Nombre.ToLower() == nombre.ToLower() &&
                p.ProcedimientoId != procedimiento.ProcedimientoId);

        if (nombreExiste)
            return Result.Failure(NombreDuplicado);

        string? descripcion = string.IsNullOrWhiteSpace(procedimiento.Descripcion)
            ? null
            : procedimiento.Descripcion.Trim();

        try
        {
            if (procedimiento.ProcedimientoId == 0)
            {
                // Se copian solo los campos propios, así no se guardan por accidente
                // las Cirugías que vengan anidadas en el JSON.
                contexto.Procedimientos.Add(new Procedimiento
                {
                    FechaRegistro = DateTime.Now,
                    Nombre = nombre,
                    Descripcion = descripcion,
                    PrecioBase = procedimiento.PrecioBase
                });
            }
            else
            {
                var existente = await contexto.Procedimientos
                    .FirstOrDefaultAsync(p => p.ProcedimientoId == procedimiento.ProcedimientoId);

                if (existente == null)
                    return Result.Failure(ProcedimientoNoExiste);

                existente.Nombre = nombre;
                existente.Descripcion = descripcion;
                existente.PrecioBase = procedimiento.PrecioBase;
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
    // Buscar procedimiento por ID
    public async Task<Result<Procedimiento?>> Buscar(int procedimientoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var procedimiento = await contexto.Procedimientos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProcedimientoId == procedimientoId);

        return Result<Procedimiento?>.Success(procedimiento);
    }

    // Obtener procedimientos según un criterio
    public async Task<Result<List<Procedimiento>>> GetList(
        Expression<Func<Procedimiento, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var procedimientos = await contexto.Procedimientos
            .AsNoTracking()
            .Where(criterio)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return Result<List<Procedimiento>>.Success(procedimientos);
    }

    // Buscar procedimientos por nombre
    public async Task<Result<List<Procedimiento>>> BuscarPorNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result<List<Procedimiento>>.Success(new List<Procedimiento>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        nombre = nombre.Trim();

        var procedimientos = await contexto.Procedimientos
            .AsNoTracking()
            .Where(p => p.Nombre.Contains(nombre))
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return Result<List<Procedimiento>>.Success(procedimientos);
    }

    // Buscar procedimientos por rango de precios (ambos extremos incluidos)
    public async Task<Result<List<Procedimiento>>> BuscarPorRangoPrecio(
        decimal precioMinimo,
        decimal precioMaximo)
    {
        if (precioMinimo < 0 || precioMaximo < precioMinimo)
            return Result<List<Procedimiento>>.Success(new List<Procedimiento>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var procedimientos = await contexto.Procedimientos
            .AsNoTracking()
            .Where(p => p.PrecioBase >= precioMinimo && p.PrecioBase <= precioMaximo)
            .OrderBy(p => p.PrecioBase)
            .ThenBy(p => p.Nombre)
            .ToListAsync();

        return Result<List<Procedimiento>>.Success(procedimientos);
    }

    // Buscar el procedimiento utilizado en una cirugía
    public async Task<Result<Procedimiento?>> BuscarPorCirugia(int cirugiaId)
    {
        if (cirugiaId <= 0)
            return Result<Procedimiento?>.Success((Procedimiento?)null);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var procedimiento = await contexto.Procedimientos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Cirugia.Any(c => c.CirugiaId == cirugiaId));

        return Result<Procedimiento?>.Success(procedimiento);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    // Solo se elimina si no tiene cirugías asociadas.
    public async Task<Result> Eliminar(int procedimientoId)
    {
        if (procedimientoId <= 0)
            return Result.Failure(ProcedimientoNoExiste);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        bool existe = await contexto.Procedimientos
            .AnyAsync(p => p.ProcedimientoId == procedimientoId);

        if (!existe)
            return Result.Failure(ProcedimientoNoExiste);

        bool tieneCirugias = await contexto.Cirugias
            .AnyAsync(c => c.ProcedimientoId == procedimientoId);

        if (tieneCirugias)
            return Result.Failure(TieneCirugias);

        try
        {
            bool eliminado = await contexto.Procedimientos
                .Where(p => p.ProcedimientoId == procedimientoId)
                .ExecuteDeleteAsync() > 0;

            return eliminado ? Result.Success() : Result.Failure(ProcedimientoNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}