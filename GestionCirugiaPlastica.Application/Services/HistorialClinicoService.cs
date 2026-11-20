using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class HistorialClinicoService(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<HistorialClinico, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error PacienteRequerido =
        new("Historial.PacienteRequerido", "Debe indicar un paciente válido.");

    private static readonly Error PacienteNoExiste =
        new("Historial.PacienteNoExiste", "El paciente indicado no existe.");

    private static readonly Error HistorialDuplicado =
        new("Historial.Duplicado", "Ese paciente ya tiene un historial clínico.");

    private static readonly Error HistorialNoExiste =
        new("Historial.NoExiste", "El historial indicado no existe.");

    private static readonly Error PacienteNoPuedeCambiar =
        new("Historial.PacienteNoPuedeCambiar", "Un historial no se puede pasar a otro paciente.");

    private static readonly Error ErrorAlGuardar =
        new("Historial.ErrorAlGuardar", "No se pudo guardar el historial clínico.");

    private static readonly Error ErrorAlEliminar =
        new("Historial.ErrorAlEliminar", "No se pudo eliminar el historial clínico.");

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(HistorialClinico historial)
    {
        if (historial == null || historial.PacienteId <= 0)
            return Result.Failure(PacienteRequerido);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        // El paciente debe existir.
        bool pacienteExiste = await contexto.Pacientes
            .AnyAsync(p => p.PacienteId == historial.PacienteId);

        if (!pacienteExiste)
            return Result.Failure(PacienteNoExiste);

        // Un paciente solo puede tener UN historial (índice único sobre PacienteId).
        // Se excluye el propio historial para poder editarlo.
        bool yaTieneHistorial = await contexto.HistorialClinicos
            .AnyAsync(h => h.PacienteId == historial.PacienteId
                        && h.HistoriaId != historial.HistoriaId);

        if (yaTieneHistorial)
            return Result.Failure(HistorialDuplicado);

        try
        {
            if (historial.HistoriaId == 0)
            {
                // Se copian solo los campos propios del historial. Así no se guarda
                // por accidente el Paciente que venga anidado en el JSON.
                var nuevo = new HistorialClinico
                {
                    PacienteId = historial.PacienteId,
                    FechaActualizacion = DateTime.Now,
                    Antecedentes = historial.Antecedentes?.Trim(),
                    AntecedentesPersonales = historial.AntecedentesPersonales?.Trim(),
                    AntecedentesFamiliares = historial.AntecedentesFamiliares?.Trim(),
                    Alergias = historial.Alergias?.Trim(),
                    Medicamentos = historial.Medicamentos?.Trim(),
                    NotasMedicas = historial.NotasMedicas?.Trim()
                };

                contexto.HistorialClinicos.Add(nuevo);
            }
            else
            {
                var existente = await contexto.HistorialClinicos
                    .FirstOrDefaultAsync(h => h.HistoriaId == historial.HistoriaId);

                if (existente == null)
                    return Result.Failure(HistorialNoExiste);

                if (existente.PacienteId != historial.PacienteId)
                    return Result.Failure(PacienteNoPuedeCambiar);

                // FechaActualizacion = última vez que se modificó: la pone el servidor,
                // no el cliente.
                existente.FechaActualizacion = DateTime.Now;
                existente.Antecedentes = historial.Antecedentes?.Trim();
                existente.AntecedentesPersonales = historial.AntecedentesPersonales?.Trim();
                existente.AntecedentesFamiliares = historial.AntecedentesFamiliares?.Trim();
                existente.Alergias = historial.Alergias?.Trim();
                existente.Medicamentos = historial.Medicamentos?.Trim();
                existente.NotasMedicas = historial.NotasMedicas?.Trim();
            }

            await contexto.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            // Por si dos peticiones simultáneas pasan la validación de duplicado a la vez.
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // ── Consultas ──────────────────────────────────────────────────────────
    // Buscar historial por ID
    public async Task<Result<HistorialClinico?>> Buscar(int historiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var historial = await contexto.HistorialClinicos
            .AsNoTracking()
            .Include(h => h.Paciente)
            .FirstOrDefaultAsync(h => h.HistoriaId == historiaId);

        return Result<HistorialClinico?>.Success(historial);
    }

    // Buscar historial por nombre o apellido del paciente
    public async Task<Result<List<HistorialClinico>>> BuscarPorNombre(
        string nombre)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var historiales = await contexto.HistorialClinicos
            .AsNoTracking()
            .Include(h => h.Paciente)
            .Where(h =>
                h.Paciente.Nombre.Contains(nombre) ||
                h.Paciente.Apellido.Contains(nombre))
            .ToListAsync();

        return Result<List<HistorialClinico>>.Success(historiales);
    }

    // Buscar historial por cédula del paciente
    public async Task<Result<List<HistorialClinico>>> BuscarPorCedula(
        string cedula)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var historiales = await contexto.HistorialClinicos
            .AsNoTracking()
            .Include(h => h.Paciente)
            .Where(h => h.Paciente.Cedula == cedula)
            .ToListAsync();

        return Result<List<HistorialClinico>>.Success(historiales);
    }

    // Listar historiales mediante criterios personalizados
    public async Task<Result<List<HistorialClinico>>> GetList(
        Expression<Func<HistorialClinico, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var historiales = await contexto.HistorialClinicos
            .AsNoTracking()
            .Include(h => h.Paciente)
            .Where(criterio)
            .ToListAsync();

        return Result<List<HistorialClinico>>.Success(historiales);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int historiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        try
        {
            bool eliminado = await contexto.HistorialClinicos
                .Where(h => h.HistoriaId == historiaId)
                .ExecuteDeleteAsync() > 0;

            return eliminado ? Result.Success() : Result.Failure(HistorialNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}
