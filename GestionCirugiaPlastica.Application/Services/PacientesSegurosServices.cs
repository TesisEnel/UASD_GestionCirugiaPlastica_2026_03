using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class PacientesSegurosServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<PacienteSeguro, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error DatosRequeridos =
        new("PacienteSeguro.DatosRequeridos", "Debe indicar un paciente y un seguro válidos.");

    private static readonly Error TextoMuyLargo =
        new("PacienteSeguro.TextoMuyLargo", "La póliza, el titular y las observaciones no pueden pasar de 250 caracteres.");

    private static readonly Error PacienteNoExiste =
        new("PacienteSeguro.PacienteNoExiste", "El paciente indicado no existe.");

    private static readonly Error SeguroNoExiste =
        new("PacienteSeguro.SeguroNoExiste", "El seguro indicado no existe.");

    private static readonly Error SeguroInactivo =
        new("PacienteSeguro.SeguroInactivo", "El seguro indicado está inactivo.");

    private static readonly Error RegistroNoExiste =
        new("PacienteSeguro.NoExiste", "El registro de seguro del paciente no existe.");

    private static readonly Error NoPuedeCambiar =
        new("PacienteSeguro.NoPuedeCambiar", "No se puede cambiar el paciente ni el seguro de un registro existente.");

    private static readonly Error ErrorAlGuardar =
        new("PacienteSeguro.ErrorAlGuardar", "No se pudo guardar el seguro del paciente.");

    private static readonly Error ErrorAlEliminar =
        new("PacienteSeguro.ErrorAlEliminar", "No se pudo eliminar el seguro del paciente.");

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(PacienteSeguro pacienteSeguro)
    {
        if (pacienteSeguro == null ||
            pacienteSeguro.PacienteId <= 0 ||
            pacienteSeguro.SeguroId <= 0)
            return Result.Failure(DatosRequeridos);

        if (pacienteSeguro.NumeroPoliza?.Trim().Length > 250 ||
            pacienteSeguro.Titular?.Trim().Length > 250 ||
            pacienteSeguro.Observaciones?.Trim().Length > 250)
            return Result.Failure(TextoMuyLargo);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        bool pacienteExiste = await contexto.Pacientes
            .AnyAsync(p => p.PacienteId == pacienteSeguro.PacienteId);

        if (!pacienteExiste)
            return Result.Failure(PacienteNoExiste);

        var seguro = await contexto.Seguros
            .FirstOrDefaultAsync(s => s.SeguroId == pacienteSeguro.SeguroId);

        if (seguro == null)
            return Result.Failure(SeguroNoExiste);

        if (!seguro.Activo)
            return Result.Failure(SeguroInactivo);

        try
        {
            if (pacienteSeguro.PacienteSeguroId == 0)
            {
                // Se copian solo los campos propios. Así no se guarda por accidente
                // el Paciente o el Seguro que vengan anidados en el JSON.
                contexto.PacienteSeguros.Add(new PacienteSeguro
                {
                    PacienteId = pacienteSeguro.PacienteId,
                    SeguroId = pacienteSeguro.SeguroId,
                    NumeroPoliza = pacienteSeguro.NumeroPoliza?.Trim(),
                    Titular = pacienteSeguro.Titular?.Trim(),
                    FechaVencimiento = pacienteSeguro.FechaVencimiento,
                    Observaciones = pacienteSeguro.Observaciones?.Trim()
                });
            }
            else
            {
                var existente = await contexto.PacienteSeguros
                    .FirstOrDefaultAsync(p => p.PacienteSeguroId == pacienteSeguro.PacienteSeguroId);

                if (existente == null)
                    return Result.Failure(RegistroNoExiste);

                if (existente.PacienteId != pacienteSeguro.PacienteId ||
                    existente.SeguroId != pacienteSeguro.SeguroId)
                    return Result.Failure(NoPuedeCambiar);

                existente.NumeroPoliza = pacienteSeguro.NumeroPoliza?.Trim();
                existente.Titular = pacienteSeguro.Titular?.Trim();
                existente.FechaVencimiento = pacienteSeguro.FechaVencimiento;
                existente.Observaciones = pacienteSeguro.Observaciones?.Trim();
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
    public async Task<Result<PacienteSeguro?>> Buscar(int pacienteSeguroId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pacienteSeguro = await contexto.PacienteSeguros
            .AsNoTracking()
            .Include(p => p.Paciente)
            .Include(p => p.Seguro)
            .FirstOrDefaultAsync(p => p.PacienteSeguroId == pacienteSeguroId);

        return Result<PacienteSeguro?>.Success(pacienteSeguro);
    }

    public async Task<Result<List<PacienteSeguro>>> GetList(
        Expression<Func<PacienteSeguro, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pacientesSeguros = await contexto.PacienteSeguros
            .AsNoTracking()
            .Include(p => p.Paciente)
            .Include(p => p.Seguro)
            .Where(criterio)
            .ToListAsync();

        return Result<List<PacienteSeguro>>.Success(pacientesSeguros);
    }

    public async Task<Result<List<PacienteSeguro>>> BuscarPorPaciente(int pacienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pacientesSeguros = await contexto.PacienteSeguros
            .AsNoTracking()
            .Include(p => p.Seguro)
            .Where(p => p.PacienteId == pacienteId)
            .OrderByDescending(p => p.FechaRegistro)
            .ThenByDescending(p => p.PacienteSeguroId)
            .ToListAsync();

        return Result<List<PacienteSeguro>>.Success(pacientesSeguros);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int pacienteSeguroId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        try
        {
            bool eliminado = await contexto.PacienteSeguros
                .Where(p => p.PacienteSeguroId == pacienteSeguroId)
                .ExecuteDeleteAsync() > 0;

            return eliminado ? Result.Success() : Result.Failure(RegistroNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}