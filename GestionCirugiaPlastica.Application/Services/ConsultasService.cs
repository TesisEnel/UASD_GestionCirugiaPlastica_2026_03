
using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class ConsultasService(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Consulta, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error ConsultaRequerida =
        new("Consulta.Requerida", "Debe indicar los datos de la consulta.");

    private static readonly Error PacienteNoExiste =
        new("Consulta.PacienteNoExiste", "El paciente indicado no existe.");

    private static readonly Error FechaRequerida =
        new("Consulta.FechaRequerida", "Debe indicar la fecha de la consulta.");

    private static readonly Error MotivoMuyLargo =
        new("Consulta.MotivoMuyLargo", "El motivo no puede pasar de 250 caracteres.");

    private static readonly Error CitaNoExiste =
        new("Consulta.CitaNoExiste", "La cita indicada no existe.");

    private static readonly Error CitaDeOtroPaciente =
        new("Consulta.CitaDeOtroPaciente", "La cita indicada pertenece a otro paciente.");

    private static readonly Error ConsultaNoExiste =
        new("Consulta.NoExiste", "La consulta indicada no existe.");

    private static readonly Error ErrorAlGuardar =
        new("Consulta.ErrorAlGuardar", "No se pudo guardar la consulta.");

    private static readonly Error TieneRecetas =
        new("Consulta.TieneRecetas", "No se puede eliminar: la consulta tiene recetas asociadas.");

    private static readonly Error ErrorAlEliminar =
        new("Consulta.ErrorAlEliminar", "No se pudo eliminar la consulta.");

    // ── Guardar ────────────────────────────────────────────────────────────
    public async Task<Result> Guardar(Consulta consulta)
    {
        if (consulta == null || consulta.PacienteId <= 0)
            return Result.Failure(ConsultaRequerida);

        if (consulta.Fecha == default)
            return Result.Failure(FechaRequerida);

        if (consulta.Motivo?.Length > 250)
            return Result.Failure(MotivoMuyLargo);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        // 1. El paciente debe existir.
        bool pacienteExiste = await contexto.Pacientes
            .AnyAsync(p => p.PacienteId == consulta.PacienteId);

        if (!pacienteExiste)
            return Result.Failure(PacienteNoExiste);

        // 2. Si tiene cita: debe existir y ser del mismo paciente.
        if (consulta.CitaId.HasValue)
        {
            int? pacienteDeLaCita = await contexto.Citas
                .Where(c => c.CitaId == consulta.CitaId.Value)
                .Select(c => (int?)c.PacienteId)
                .FirstOrDefaultAsync();

            if (pacienteDeLaCita == null)
                return Result.Failure(CitaNoExiste);

            if (pacienteDeLaCita.Value != consulta.PacienteId)
                return Result.Failure(CitaDeOtroPaciente);
        }

        try
        {
            if (consulta.ConsultaId == 0)
            {
                // Se copian solo los campos propios de la consulta. Así no se guarda
                // por accidente el Paciente o la Cita que vengan anidados en el JSON.
                var nueva = new Consulta
                {
                    Fecha = consulta.Fecha,
                    PacienteId = consulta.PacienteId,
                    CitaId = consulta.CitaId,
                    Motivo = consulta.Motivo?.Trim(),
                    Diagnostico = consulta.Diagnostico?.Trim(),
                    Observaciones = consulta.Observaciones?.Trim()
                };

                contexto.Consultas.Add(nueva);
            }
            else
            {
                var existente = await contexto.Consultas
                    .FirstOrDefaultAsync(c => c.ConsultaId == consulta.ConsultaId);

                if (existente == null)
                    return Result.Failure(ConsultaNoExiste);

                existente.Fecha = consulta.Fecha;
                existente.PacienteId = consulta.PacienteId;
                existente.CitaId = consulta.CitaId;
                existente.Motivo = consulta.Motivo?.Trim();
                existente.Diagnostico = consulta.Diagnostico?.Trim();
                existente.Observaciones = consulta.Observaciones?.Trim();
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
    public async Task<Result<Consulta?>> Buscar(int consultaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var consulta = await contexto.Consultas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Cita)
            .FirstOrDefaultAsync(c => c.ConsultaId == consultaId);

        return Result<Consulta?>.Success(consulta);
    }

    // Buscar consultas por nombre o apellido del paciente
    public async Task<Result<List<Consulta>>> BuscarPorNombre(string nombre)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var consultas = await contexto.Consultas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Cita)
            .Where(c =>
                c.Paciente.Nombre.Contains(nombre) ||
                c.Paciente.Apellido.Contains(nombre))
            .ToListAsync();

        return Result<List<Consulta>>.Success(consultas);
    }

    // Buscar consultas por cédula del paciente
    public async Task<Result<List<Consulta>>> BuscarPorCedula(string cedula)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var consultas = await contexto.Consultas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Cita)
            .Where(c => c.Paciente.Cedula == cedula)
            .ToListAsync();

        return Result<List<Consulta>>.Success(consultas);
    }

    // Listar consultas mediante un criterio personalizado
    public async Task<Result<List<Consulta>>> GetList(
        Expression<Func<Consulta, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var consultas = await contexto.Consultas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Include(c => c.Cita)
            .Where(criterio)
            .ToListAsync();

        return Result<List<Consulta>>.Success(consultas);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int consultaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        // No se puede borrar una consulta que ya tiene recetas.
        bool tieneRecetas = await contexto.Recetas
            .AnyAsync(r => r.ConsultaId == consultaId);

        if (tieneRecetas)
            return Result.Failure(TieneRecetas);

        try
        {
            bool eliminado = await contexto.Consultas
                .Where(c => c.ConsultaId == consultaId)
                .ExecuteDeleteAsync() > 0;

            return eliminado ? Result.Success() : Result.Failure(ConsultaNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}