using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class ControlesPostoperatoriosService(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<ControlesPostoperatorio, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error CirugiaRequerida =
        new("Control.CirugiaRequerida", "Debe indicar una cirugía válida.");

    private static readonly Error CirugiaNoExiste =
        new("Control.CirugiaNoExiste", "La cirugía indicada no existe.");

    private static readonly Error FechaRequerida =
        new("Control.FechaRequerida", "Debe indicar la fecha del control.");

    private static readonly Error DolorMuyLargo =
        new("Control.DolorMuyLargo", "El dolor no puede pasar de 250 caracteres.");

    private static readonly Error InflamacionMuyLarga =
        new("Control.InflamacionMuyLarga", "La inflamación no puede pasar de 250 caracteres.");

    private static readonly Error DiasNegativos =
        new("Control.DiasNegativos", "Los días postoperatorios no pueden ser negativos.");

    private static readonly Error ProximoControlAnterior =
        new("Control.ProximoControlAnterior", "El próximo control no puede ser anterior a este control.");

    private static readonly Error ControlNoExiste =
        new("Control.NoExiste", "El control indicado no existe.");

    private static readonly Error ErrorAlGuardar =
        new("Control.ErrorAlGuardar", "No se pudo guardar el control.");

    private static readonly Error ErrorAlEliminar =
        new("Control.ErrorAlEliminar", "No se pudo eliminar el control.");

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(ControlesPostoperatorio control)
    {
        if (control == null || control.CirugiaId <= 0)
            return Result.Failure(CirugiaRequerida);

        if (control.Fecha == default)
            return Result.Failure(FechaRequerida);

        if (control.Dolor?.Length > 250)
            return Result.Failure(DolorMuyLargo);

        if (control.Inflamacion?.Length > 250)
            return Result.Failure(InflamacionMuyLarga);

        if (control.DiasPostoperatorio < 0)
            return Result.Failure(DiasNegativos);

        if (control.ProximoControl.HasValue && control.ProximoControl.Value < control.Fecha)
            return Result.Failure(ProximoControlAnterior);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        // La cirugía debe existir.
        bool cirugiaExiste = await contexto.Cirugias
            .AnyAsync(c => c.CirugiaId == control.CirugiaId);

        if (!cirugiaExiste)
            return Result.Failure(CirugiaNoExiste);

        try
        {
            if (control.ControlId == 0)
            {
                // Se copian solo los campos propios del control. Así no se guarda
                // por accidente la Cirugía que venga anidada en el JSON.
                var nuevo = new ControlesPostoperatorio
                {
                    Fecha = control.Fecha,
                    CirugiaId = control.CirugiaId,
                    DiasPostoperatorio = control.DiasPostoperatorio,
                    Evolucion = control.Evolucion?.Trim(),
                    Dolor = control.Dolor?.Trim(),
                    Inflamacion = control.Inflamacion?.Trim(),
                    Observaciones = control.Observaciones?.Trim(),
                    ProximoControl = control.ProximoControl
                };

                contexto.ControlesPostoperatorios.Add(nuevo);
            }
            else
            {
                var existente = await contexto.ControlesPostoperatorios
                    .FirstOrDefaultAsync(c => c.ControlId == control.ControlId);

                if (existente == null)
                    return Result.Failure(ControlNoExiste);

                existente.Fecha = control.Fecha;
                existente.CirugiaId = control.CirugiaId;
                existente.DiasPostoperatorio = control.DiasPostoperatorio;
                existente.Evolucion = control.Evolucion?.Trim();
                existente.Dolor = control.Dolor?.Trim();
                existente.Inflamacion = control.Inflamacion?.Trim();
                existente.Observaciones = control.Observaciones?.Trim();
                existente.ProximoControl = control.ProximoControl;
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
    public async Task<Result<ControlesPostoperatorio?>> Buscar(int controlId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var control = await contexto.ControlesPostoperatorios
            .AsNoTracking()
            .Include(c => c.Cirugia)
                .ThenInclude(c => c.Paciente)
            .Include(c => c.Cirugia)
                .ThenInclude(c => c.Procedimiento)
            .FirstOrDefaultAsync(c => c.ControlId == controlId);

        return Result<ControlesPostoperatorio?>.Success(control);
    }

    // Buscar por nombre o apellido del paciente
    public async Task<Result<List<ControlesPostoperatorio>>> BuscarPorNombre(
        string nombre)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var controles = await contexto.ControlesPostoperatorios
            .AsNoTracking()
            .Include(c => c.Cirugia)
                .ThenInclude(c => c.Paciente)
            .Include(c => c.Cirugia)
                .ThenInclude(c => c.Procedimiento)
            .Where(c =>
                c.Cirugia.Paciente.Nombre.Contains(nombre) ||
                c.Cirugia.Paciente.Apellido.Contains(nombre))
            .ToListAsync();

        return Result<List<ControlesPostoperatorio>>.Success(controles);
    }

    // Buscar por cédula del paciente
    public async Task<Result<List<ControlesPostoperatorio>>> BuscarPorCedula(
        string cedula)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var controles = await contexto.ControlesPostoperatorios
            .AsNoTracking()
            .Include(c => c.Cirugia)
                .ThenInclude(c => c.Paciente)
            .Include(c => c.Cirugia)
                .ThenInclude(c => c.Procedimiento)
            .Where(c => c.Cirugia.Paciente.Cedula == cedula)
            .ToListAsync();

        return Result<List<ControlesPostoperatorio>>.Success(controles);
    }

    // Listar mediante un criterio personalizado
    public async Task<Result<List<ControlesPostoperatorio>>> GetList(
        Expression<Func<ControlesPostoperatorio, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var controles = await contexto.ControlesPostoperatorios
            .AsNoTracking()
            .Include(c => c.Cirugia)
                .ThenInclude(c => c.Paciente)
            .Include(c => c.Cirugia)
                .ThenInclude(c => c.Procedimiento)
            .Where(criterio)
            .ToListAsync();

        return Result<List<ControlesPostoperatorio>>.Success(controles);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int controlId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        try
        {
            bool eliminado = await contexto.ControlesPostoperatorios
                .Where(c => c.ControlId == controlId)
                .ExecuteDeleteAsync() > 0;

            return eliminado ? Result.Success() : Result.Failure(ControlNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}