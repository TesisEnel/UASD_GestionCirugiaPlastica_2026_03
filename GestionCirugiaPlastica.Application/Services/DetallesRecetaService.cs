using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class DetallesRecetaService(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<DetallesReceta, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error RecetaRequerida =
        new("Detalle.RecetaRequerida", "Debe indicar una receta válida.");

    private static readonly Error RecetaNoExiste =
        new("Detalle.RecetaNoExiste", "La receta indicada no existe.");

    private static readonly Error MedicamentoRequerido =
        new("Detalle.MedicamentoRequerido", "El medicamento es obligatorio.");

    private static readonly Error TextoMuyLargo =
        new("Detalle.TextoMuyLargo", "Medicamento, dosis, frecuencia, duración e instrucciones no pueden pasar de 250 caracteres.");

    private static readonly Error DetalleNoExiste =
        new("Detalle.NoExiste", "El detalle indicado no existe.");

    private static readonly Error RecetaNoPuedeCambiar =
        new("Detalle.RecetaNoPuedeCambiar", "Un detalle no se puede mover a otra receta.");

    private static readonly Error UltimoDetalle =
        new("Detalle.UltimoDetalle", "No se puede eliminar: la receta debe conservar al menos un medicamento.");

    private static readonly Error ErrorAlGuardar =
        new("Detalle.ErrorAlGuardar", "No se pudo guardar el detalle.");

    private static readonly Error ErrorAlEliminar =
        new("Detalle.ErrorAlEliminar", "No se pudo eliminar el detalle.");

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(DetallesReceta detalle)
    {
        if (detalle == null || detalle.RecetaId <= 0)
            return Result.Failure(RecetaRequerida);

        if (string.IsNullOrWhiteSpace(detalle.Medicamento))
            return Result.Failure(MedicamentoRequerido);

        if (detalle.Medicamento.Trim().Length > 250 ||
            detalle.Dosis?.Length > 250 ||
            detalle.Frecuencia?.Length > 250 ||
            detalle.Duracion?.Length > 250 ||
            detalle.Instrucciones?.Length > 250)
        {
            return Result.Failure(TextoMuyLargo);
        }

        await using var contexto = await contextFactory.CreateDbContextAsync();

        // La receta debe existir.
        bool recetaExiste = await contexto.Recetas
            .AnyAsync(r => r.RecetaId == detalle.RecetaId);

        if (!recetaExiste)
            return Result.Failure(RecetaNoExiste);

        try
        {
            if (detalle.DetalleRecetaId == 0)
            {
                // Se copian solo los campos propios del detalle. Así no se guarda
                // por accidente la Receta que venga anidada en el JSON.
                var nuevo = new DetallesReceta
                {
                    RecetaId = detalle.RecetaId,
                    Medicamento = detalle.Medicamento.Trim(),
                    Dosis = detalle.Dosis?.Trim(),
                    Frecuencia = detalle.Frecuencia?.Trim(),
                    Duracion = detalle.Duracion?.Trim(),
                    Instrucciones = detalle.Instrucciones?.Trim()
                };

                contexto.DetallesReceta.Add(nuevo);
            }
            else
            {
                var existente = await contexto.DetallesReceta
                    .FirstOrDefaultAsync(d => d.DetalleRecetaId == detalle.DetalleRecetaId);

                if (existente == null)
                    return Result.Failure(DetalleNoExiste);

                if (existente.RecetaId != detalle.RecetaId)
                    return Result.Failure(RecetaNoPuedeCambiar);

                existente.Medicamento = detalle.Medicamento.Trim();
                existente.Dosis = detalle.Dosis?.Trim();
                existente.Frecuencia = detalle.Frecuencia?.Trim();
                existente.Duracion = detalle.Duracion?.Trim();
                existente.Instrucciones = detalle.Instrucciones?.Trim();
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
    // Buscar por ID
    public async Task<Result<DetallesReceta?>> Buscar(int detalleRecetaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var detalle = await contexto.DetallesReceta
            .AsNoTracking()
            .Include(d => d.Receta)
                .ThenInclude(r => r.Paciente)
            .FirstOrDefaultAsync(d => d.DetalleRecetaId == detalleRecetaId);

        return Result<DetallesReceta?>.Success(detalle);
    }

    // Buscar por nombre o apellido del paciente
    public async Task<Result<List<DetallesReceta>>> BuscarPorNombre(
        string nombre)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var detalles = await contexto.DetallesReceta
            .AsNoTracking()
            .Include(d => d.Receta)
                .ThenInclude(r => r.Paciente)
            .Where(d =>
                d.Receta.Paciente.Nombre.Contains(nombre) ||
                d.Receta.Paciente.Apellido.Contains(nombre))
            .ToListAsync();

        return Result<List<DetallesReceta>>.Success(detalles);
    }

    // Buscar por cédula del paciente
    public async Task<Result<List<DetallesReceta>>> BuscarPorCedula(
        string cedula)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var detalles = await contexto.DetallesReceta
            .AsNoTracking()
            .Include(d => d.Receta)
                .ThenInclude(r => r.Paciente)
            .Where(d => d.Receta.Paciente.Cedula == cedula)
            .ToListAsync();

        return Result<List<DetallesReceta>>.Success(detalles);
    }

    // Listar mediante un criterio personalizado
    public async Task<Result<List<DetallesReceta>>> GetList(
        Expression<Func<DetallesReceta, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var detalles = await contexto.DetallesReceta
            .AsNoTracking()
            .Include(d => d.Receta)
                .ThenInclude(r => r.Paciente)
            .Where(criterio)
            .ToListAsync();

        return Result<List<DetallesReceta>>.Success(detalles);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int detalleRecetaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        int? recetaId = await contexto.DetallesReceta
            .Where(d => d.DetalleRecetaId == detalleRecetaId)
            .Select(d => (int?)d.RecetaId)
            .FirstOrDefaultAsync();

        if (recetaId == null)
            return Result.Failure(DetalleNoExiste);

        // Una receta debe conservar al menos un medicamento.
        int cantidad = await contexto.DetallesReceta
            .CountAsync(d => d.RecetaId == recetaId.Value);

        if (cantidad <= 1)
            return Result.Failure(UltimoDetalle);

        try
        {
            bool eliminado = await contexto.DetallesReceta
                .Where(d => d.DetalleRecetaId == detalleRecetaId)
                .ExecuteDeleteAsync() > 0;

            return eliminado ? Result.Success() : Result.Failure(DetalleNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}