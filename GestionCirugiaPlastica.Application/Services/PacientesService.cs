using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class PacientesService(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Paciente, int>
{
    
    private static readonly Error NombreRequerido =
        new("Paciente.NombreRequerido", "El nombre es obligatorio.");

    private static readonly Error ApellidoRequerido =
        new("Paciente.ApellidoRequerido", "El apellido es obligatorio.");

    private static readonly Error CedulaDuplicada =
        new("Paciente.CedulaDuplicada", "Ya existe un paciente con esa cédula.");

    private static readonly Error ErrorAlGuardar =
        new("Paciente.ErrorAlGuardar", "No se pudo guardar el paciente.");

    private static readonly Error TieneRegistrosRelacionados =
        new("Paciente.TieneRegistrosRelacionados",
            "No se puede eliminar: el paciente tiene citas, consultas, cirugías, recetas, seguros o historial asociados.");

    private static readonly Error ErrorAlEliminar =
        new("Paciente.ErrorAlEliminar", "No se pudo eliminar el paciente.");

    private static Result Fallo(Error error) => Result.Failure(error);

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(Paciente paciente)
    {
        if (string.IsNullOrWhiteSpace(paciente.Nombre))
            return Fallo(NombreRequerido);

        if (string.IsNullOrWhiteSpace(paciente.Apellido))
            return Fallo(ApellidoRequerido);

        if (!string.IsNullOrWhiteSpace(paciente.Cedula)
            && await CedulaEnUso(paciente.Cedula, paciente.PacienteId))
            return Fallo(CedulaDuplicada);

        try
        {
            bool existe = await Existe(paciente.PacienteId);

            bool guardado = existe
                ? await Modificar(paciente)
                : await Insertar(paciente);

            return Result.Success(guardado);
        }
        catch (DbUpdateException)
        {
           
            return Fallo(ErrorAlGuardar);
        }
    }

    private async Task<bool> CedulaEnUso(string cedula, int pacienteIdActual)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

       
        return await contexto.Pacientes
            .AnyAsync(p => p.Cedula == cedula && p.PacienteId != pacienteIdActual);
    }

    private async Task<bool> Existe(int pacienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        return await contexto.Pacientes
            .AnyAsync(p => p.PacienteId == pacienteId);
    }

    private async Task<bool> Insertar(Paciente paciente)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        contexto.Pacientes.Add(paciente);

        return await contexto.SaveChangesAsync() > 0;
    }

    private async Task<bool> Modificar(Paciente paciente)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        contexto.Pacientes.Update(paciente);

        return await contexto.SaveChangesAsync() > 0;
    }

    // ── Consultas ──────────────────────────────────────────────────────────
    public async Task<Result<Paciente?>> Buscar(int pacienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var paciente = await contexto.Pacientes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PacienteId == pacienteId);

        return Result<Paciente?>.Success(paciente);
    }

    public async Task<Result<List<Paciente>>> GetList(
        Expression<Func<Paciente, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pacientes = await contexto.Pacientes
            .AsNoTracking()
            .Where(criterio)
            .ToListAsync();

        return Result<List<Paciente>>.Success(pacientes);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int pacienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        bool tieneRelacionados =
            await contexto.Citas.AnyAsync(x => x.PacienteId == pacienteId) ||
            await contexto.Consultas.AnyAsync(x => x.PacienteId == pacienteId) ||
            await contexto.Cirugias.AnyAsync(x => x.PacienteId == pacienteId) ||
            await contexto.Recetas.AnyAsync(x => x.PacienteId == pacienteId) ||
            await contexto.PacienteSeguros.AnyAsync(x => x.PacienteId == pacienteId) ||
            await contexto.HistorialClinicos.AnyAsync(x => x.PacienteId == pacienteId);

        if (tieneRelacionados)
            return Fallo(TieneRegistrosRelacionados);

        try
        {
            bool eliminado = await contexto.Pacientes
                .Where(p => p.PacienteId == pacienteId)
                .ExecuteDeleteAsync() > 0;

            return Result.Success(eliminado);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
           
            return Fallo(ErrorAlEliminar);
        }
    }
}