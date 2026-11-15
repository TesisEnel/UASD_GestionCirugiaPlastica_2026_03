using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class CirugiasService(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Cirugia, int>
{
    private static readonly Error CirugiaNula =
        new("Cirugia.Nula", "La cirugía es obligatoria.");

    private static readonly Error PacienteRequerido =
        new("Cirugia.PacienteRequerido", "Debe seleccionar un paciente.");

    private static readonly Error ProcedimientoRequerido =
        new("Cirugia.ProcedimientoRequerido", "Debe seleccionar un procedimiento.");

    private static readonly Error FechaRequerida =
        new("Cirugia.FechaRequerida", "La fecha de la cirugía es obligatoria.");

    private static readonly Error EstadoRequerido =
        new("Cirugia.EstadoRequerido", "El estado es obligatorio al modificar una cirugía.");

    private static readonly Error PacienteNoExiste =
        new("Cirugia.PacienteNoExiste", "El paciente seleccionado no existe.");

    private static readonly Error ProcedimientoNoExiste =
        new("Cirugia.ProcedimientoNoExiste", "El procedimiento seleccionado no existe.");

    private static readonly Error ErrorAlGuardar =
        new("Cirugia.ErrorAlGuardar", "No se pudo guardar la cirugía.");

    private static readonly Error NoEncontrada =
        new("Cirugia.NoEncontrada", "La cirugía no existe.");

    private static readonly Error TieneRegistrosRelacionados =
        new("Cirugia.TieneRegistrosRelacionados",
            "No se puede eliminar: la cirugía tiene preoperatorios, controles postoperatorios, fotografías, pagos o recetas asociados.");

    private static readonly Error ErrorAlEliminar =
        new("Cirugia.ErrorAlEliminar", "No se pudo eliminar la cirugía.");

    // Mismo valor que HasDefaultValue("Pendiente") en el contexto.
    private const string EstadoPorDefecto = "Pendiente";

    private static Result Fallo(Error error) => Result.Failure(error);

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(Cirugia cirugia)
    {
        if (cirugia is null)
            return Fallo(CirugiaNula);

        if (cirugia.PacienteId <= 0)
            return Fallo(PacienteRequerido);

        if (cirugia.ProcedimientoId <= 0)
            return Fallo(ProcedimientoRequerido);

        if (cirugia.FechaCirugia == default)
            return Fallo(FechaRequerida);

        if (!await PacienteExiste(cirugia.PacienteId))
            return Fallo(PacienteNoExiste);

        if (!await ProcedimientoExiste(cirugia.ProcedimientoId))
            return Fallo(ProcedimientoNoExiste);

        try
        {
            bool existe = await Existe(cirugia.CirugiaId);

            // Al insertar, un Estado vacío toma el valor por defecto ("Pendiente").
            // Al modificar se exige, para no pisar el estado actual sin querer.
            if (existe && string.IsNullOrWhiteSpace(cirugia.Estado))
                return Fallo(EstadoRequerido);

            bool guardado = existe
                ? await Modificar(cirugia)
                : await Insertar(cirugia);

            return Result.Success(guardado);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            // Error de concurrencia o de base de datos.
            return Fallo(ErrorAlGuardar);
        }
    }

    private async Task<bool> PacienteExiste(int pacienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        return await contexto.Pacientes
            .AnyAsync(p => p.PacienteId == pacienteId);
    }

    private async Task<bool> ProcedimientoExiste(int procedimientoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        return await contexto.Procedimientos
            .AnyAsync(p => p.ProcedimientoId == procedimientoId);
    }

    private async Task<bool> Existe(int cirugiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        return await contexto.Cirugias
            .AnyAsync(c => c.CirugiaId == cirugiaId);
    }

    private async Task<bool> Insertar(Cirugia cirugia)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        // Se guarda una copia solo con los campos escalares: así EF nunca intenta
        // insertar Paciente/Procedimiento/colecciones y no se modifica el objeto recibido.
        var nueva = new Cirugia();
        contexto.Cirugias.Add(nueva);
        contexto.Entry(nueva).CurrentValues.SetValues(cirugia);

        if (string.IsNullOrWhiteSpace(nueva.Estado))
            nueva.Estado = EstadoPorDefecto;

        bool guardado = await contexto.SaveChangesAsync() > 0;

        // Devuelve el Id generado al objeto original.
        cirugia.CirugiaId = nueva.CirugiaId;

        return guardado;
    }

    private async Task<bool> Modificar(Cirugia cirugia)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        // Mismo criterio: solo campos escalares, sin arrastrar el grafo de navegación.
        var actual = new Cirugia { CirugiaId = cirugia.CirugiaId };
        contexto.Attach(actual);

        var entrada = contexto.Entry(actual);
        entrada.CurrentValues.SetValues(cirugia);
        entrada.State = EntityState.Modified; // fuerza actualizar todos los campos (incluye pasar a null)

        return await contexto.SaveChangesAsync() > 0;
    }

    // ── Consultas ──────────────────────────────────────────────────────────
    public async Task<Result<Cirugia?>> Buscar(int cirugiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var cirugia = await contexto.Cirugias
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Procedimiento)
            .FirstOrDefaultAsync(c => c.CirugiaId == cirugiaId);

        return Result<Cirugia?>.Success(cirugia);
    }

    public async Task<Result<List<Cirugia>>> GetList(
        Expression<Func<Cirugia, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var cirugias = await contexto.Cirugias
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Procedimiento)
            .Where(criterio)
            .ToListAsync();

        return Result<List<Cirugia>>.Success(cirugias);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int cirugiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var info = await contexto.Cirugias
            .Where(c => c.CirugiaId == cirugiaId)
            .Select(c => new
            {
                TieneRelacionados =
                    c.Preoperatorios.Any() ||
                    c.ControlesPostoperatorios.Any() ||
                    c.Fotografia.Any() ||
                    c.Pagos.Any() ||
                    c.Receta.Any()
            })
            .FirstOrDefaultAsync();

        if (info is null)
            return Fallo(NoEncontrada);

        if (info.TieneRelacionados)
            return Fallo(TieneRegistrosRelacionados);

        try
        {
            bool eliminado = await contexto.Cirugias
                .Where(c => c.CirugiaId == cirugiaId)
                .ExecuteDeleteAsync() > 0;

            return eliminado
                ? Result.Success(true)
                : Fallo(NoEncontrada);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Fallo(ErrorAlEliminar);
        }
    }
}