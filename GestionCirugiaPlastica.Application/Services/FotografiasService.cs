using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class FotografiasService(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Fotografia, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error CirugiaRequerida =
        new("Fotografia.CirugiaRequerida", "Debe indicar una cirugía válida.");

    private static readonly Error CirugiaNoExiste =
        new("Fotografia.CirugiaNoExiste", "La cirugía indicada no existe.");

    private static readonly Error RutaRequerida =
        new("Fotografia.RutaRequerida", "La ruta del archivo es obligatoria.");

    private static readonly Error TextoMuyLargo =
        new("Fotografia.TextoMuyLargo", "Ruta, tipo y descripción no pueden pasar de 250 caracteres.");

    private static readonly Error FotografiaNoExiste =
        new("Fotografia.NoExiste", "La fotografía indicada no existe.");

    private static readonly Error ErrorAlGuardar =
        new("Fotografia.ErrorAlGuardar", "No se pudo guardar la fotografía.");

    private static readonly Error ErrorAlEliminar =
        new("Fotografia.ErrorAlEliminar", "No se pudo eliminar la fotografía.");

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(Fotografia fotografia)
    {
        if (fotografia == null || fotografia.CirugiaId <= 0)
            return Result.Failure(CirugiaRequerida);

        if (string.IsNullOrWhiteSpace(fotografia.RutaArchivo))
            return Result.Failure(RutaRequerida);

        if (fotografia.RutaArchivo.Trim().Length > 250 ||
            fotografia.Tipo?.Length > 250 ||
            fotografia.Descripcion?.Length > 250)
        {
            return Result.Failure(TextoMuyLargo);
        }

        await using var contexto = await contextFactory.CreateDbContextAsync();

        // La cirugía debe existir.
        bool cirugiaExiste = await contexto.Cirugias
            .AnyAsync(c => c.CirugiaId == fotografia.CirugiaId);

        if (!cirugiaExiste)
            return Result.Failure(CirugiaNoExiste);

        try
        {
            if (fotografia.FotografiaId == 0)
            {
                // Se copian solo los campos propios de la fotografía. Así no se guarda
                // por accidente la Cirugía que venga anidada en el JSON.
                // Si no viene fecha, se usa la actual (igual que el default de la base).
                var nueva = new Fotografia
                {
                    Fecha = fotografia.Fecha == default ? DateTime.Now : fotografia.Fecha,
                    CirugiaId = fotografia.CirugiaId,
                    Tipo = fotografia.Tipo?.Trim(),
                    RutaArchivo = fotografia.RutaArchivo.Trim(),
                    Descripcion = fotografia.Descripcion?.Trim()
                };

                contexto.Fotografias.Add(nueva);
            }
            else
            {
                var existente = await contexto.Fotografias
                    .FirstOrDefaultAsync(f => f.FotografiaId == fotografia.FotografiaId);

                if (existente == null)
                    return Result.Failure(FotografiaNoExiste);

                // Si no viene fecha se conserva la que ya tenía (no se pisa con 01/01/0001).
                if (fotografia.Fecha != default)
                    existente.Fecha = fotografia.Fecha;

                existente.CirugiaId = fotografia.CirugiaId;
                existente.Tipo = fotografia.Tipo?.Trim();
                existente.RutaArchivo = fotografia.RutaArchivo.Trim();
                existente.Descripcion = fotografia.Descripcion?.Trim();
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
    public async Task<Result<Fotografia?>> Buscar(int fotografiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var fotografia = await contexto.Fotografias
            .AsNoTracking()
            .Include(f => f.Cirugia)
                .ThenInclude(c => c.Paciente)
            .Include(f => f.Cirugia)
                .ThenInclude(c => c.Procedimiento)
            .FirstOrDefaultAsync(f => f.FotografiaId == fotografiaId);

        return Result<Fotografia?>.Success(fotografia);
    }

    // Buscar por nombre o apellido del paciente
    public async Task<Result<List<Fotografia>>> BuscarPorNombre(
        string nombre)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var fotografias = await contexto.Fotografias
            .AsNoTracking()
            .Include(f => f.Cirugia)
                .ThenInclude(c => c.Paciente)
            .Include(f => f.Cirugia)
                .ThenInclude(c => c.Procedimiento)
            .Where(f =>
                f.Cirugia.Paciente.Nombre.Contains(nombre) ||
                f.Cirugia.Paciente.Apellido.Contains(nombre))
            .ToListAsync();

        return Result<List<Fotografia>>.Success(fotografias);
    }

    // Buscar por cédula del paciente
    public async Task<Result<List<Fotografia>>> BuscarPorCedula(
        string cedula)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var fotografias = await contexto.Fotografias
            .AsNoTracking()
            .Include(f => f.Cirugia)
                .ThenInclude(c => c.Paciente)
            .Include(f => f.Cirugia)
                .ThenInclude(c => c.Procedimiento)
            .Where(f => f.Cirugia.Paciente.Cedula == cedula)
            .ToListAsync();

        return Result<List<Fotografia>>.Success(fotografias);
    }

    // Listar mediante un criterio personalizado
    public async Task<Result<List<Fotografia>>> GetList(
        Expression<Func<Fotografia, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var fotografias = await contexto.Fotografias
            .AsNoTracking()
            .Include(f => f.Cirugia)
                .ThenInclude(c => c.Paciente)
            .Include(f => f.Cirugia)
                .ThenInclude(c => c.Procedimiento)
            .Where(criterio)
            .ToListAsync();

        return Result<List<Fotografia>>.Success(fotografias);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    // OJO: solo borra el registro de la base de datos. El archivo físico en disco
    // (RutaArchivo) no se toca; si hay que borrarlo, hazlo en la capa que lo guarda.
    public async Task<Result> Eliminar(int fotografiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        try
        {
            bool eliminado = await contexto.Fotografias
                .Where(f => f.FotografiaId == fotografiaId)
                .ExecuteDeleteAsync() > 0;

            return eliminado ? Result.Success() : Result.Failure(FotografiaNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}