
using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class CitasService(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Cita, int>
{
    // ERRORES
    private static readonly Error PacienteRequerido =
        new("Cita.PacienteRequerido", "Debe indicar un paciente válido.");

    private static readonly Error PacienteNoExiste =
        new("Cita.PacienteNoExiste", "El paciente indicado no existe.");

    private static readonly Error CitaAnteriorInvalida =
        new("Cita.CitaAnteriorInvalida", "La cita anterior no existe o es la misma cita.");

    private static readonly Error CitaNoExiste =
        new("Cita.NoExiste", "La cita indicada no existe.");

    private static readonly Error ErrorAlGuardar =
        new("Cita.ErrorAlGuardar", "No se pudo guardar la cita.");

    private static readonly Error TieneRegistrosRelacionados =
        new("Cita.TieneRegistrosRelacionados",
            "No se puede eliminar: la cita tiene consultas, pagos, historial de estados u otras citas asociadas.");

    private static readonly Error ErrorAlEliminar =
        new("Cita.ErrorAlEliminar", "No se pudo eliminar la cita.");

    // GUARDAR CITA
    public async Task<Result> Guardar(Cita cita)
    {
        if (cita == null || cita.PacienteId <= 0)
            return Result.Failure(PacienteRequerido);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        // Verificar que el paciente exista.
        bool pacienteExiste = await contexto.Pacientes
            .AnyAsync(p => p.PacienteId == cita.PacienteId);

        if (!pacienteExiste)
            return Result.Failure(PacienteNoExiste);

        // Validar cita anterior.
        if (cita.CitaAnteriorId.HasValue)
        {
            if (cita.CitaAnteriorId.Value == cita.CitaId)
                return Result.Failure(CitaAnteriorInvalida);

            bool anteriorExiste = await contexto.Citas
                .AnyAsync(c => c.CitaId == cita.CitaAnteriorId.Value);

            if (!anteriorExiste)
                return Result.Failure(CitaAnteriorInvalida);
        }

        try
        {
            if (cita.CitaId == 0)
            {
                var nueva = new Cita
                {
                    Fecha = cita.Fecha,
                    PacienteId = cita.PacienteId,
                    TipoCita = cita.TipoCita?.Trim(),
                    Costo = cita.Costo,
                    Estado = cita.Estado,
                    EstadoPago = cita.EstadoPago,
                    Observaciones = cita.Observaciones?.Trim(),
                    CitaAnteriorId = cita.CitaAnteriorId
                };

                contexto.Citas.Add(nueva);
            }
            else
            {
                var existente = await contexto.Citas
                    .FirstOrDefaultAsync(c => c.CitaId == cita.CitaId);

                if (existente == null)
                    return Result.Failure(CitaNoExiste);

                existente.Fecha = cita.Fecha;
                existente.PacienteId = cita.PacienteId;
                existente.TipoCita = cita.TipoCita?.Trim();
                existente.Costo = cita.Costo;
                existente.Observaciones = cita.Observaciones?.Trim();
                existente.CitaAnteriorId = cita.CitaAnteriorId;

                if (!string.IsNullOrWhiteSpace(cita.Estado))
                    existente.Estado = cita.Estado.Trim();

                if (!string.IsNullOrWhiteSpace(cita.EstadoPago))
                    existente.EstadoPago = cita.EstadoPago.Trim();
            }

            await contexto.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // CONSULTAS

    // Buscar por ID
    public async Task<Result<Cita?>> Buscar(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var cita = await contexto.Citas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .FirstOrDefaultAsync(c => c.CitaId == citaId);

        return Result<Cita?>.Success(cita);
    }

    // Buscar por nombre o apellido del paciente
    public async Task<Result<List<Cita>>> BuscarPorNombre(string nombre)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var citas = await contexto.Citas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Where(c =>
                c.Paciente != null &&
                (c.Paciente.Nombre.Contains(nombre) ||
                 c.Paciente.Apellido.Contains(nombre)))
            .ToListAsync();

        return Result<List<Cita>>.Success(citas);
    }

    // Buscar por cédula del paciente
    public async Task<Result<List<Cita>>> BuscarPorCedula(string cedula)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var citas = await contexto.Citas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Where(c =>
                c.Paciente != null &&
                c.Paciente.Cedula == cedula)
            .ToListAsync();

        return Result<List<Cita>>.Success(citas);
    }

    // Listar citas mediante un criterio personalizado
    public async Task<Result<List<Cita>>> GetList(
        Expression<Func<Cita, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var citas = await contexto.Citas
            .AsNoTracking()
            .Include(c => c.Paciente)
            .Where(criterio)
            .ToListAsync();

        return Result<List<Cita>>.Success(citas);
    }

    // ELIMINAR CITA
    public async Task<Result> Eliminar(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        // Verificar registros relacionados.
        bool tieneRelacionados =
            await contexto.Consultas.AnyAsync(c => c.CitaId == citaId) ||
            await contexto.Pagos.AnyAsync(p => p.CitaId == citaId) ||
            await contexto.HistorialEstadosCita.AnyAsync(h => h.CitaId == citaId) ||
            await contexto.Citas.AnyAsync(c => c.CitaAnteriorId == citaId);

        if (tieneRelacionados)
            return Result.Failure(TieneRegistrosRelacionados);

        try
        {
            bool eliminado = await contexto.Citas
                .Where(c => c.CitaId == citaId)
                .ExecuteDeleteAsync() > 0;

            return eliminado
                ? Result.Success()
                : Result.Failure(CitaNoExiste);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}
