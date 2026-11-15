using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class SegurosServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Seguro, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error NombreRequerido =
        new("Seguro.NombreRequerido", "El nombre de la aseguradora es obligatorio.");

    private static readonly Error TextoMuyLargo =
        new("Seguro.TextoMuyLargo", "Los textos no pueden pasar de 250 caracteres.");

    private static readonly Error NombreDuplicado =
        new("Seguro.NombreDuplicado", "Ya existe una aseguradora con ese nombre.");

    private static readonly Error SeguroNoExiste =
        new("Seguro.NoExiste", "La aseguradora indicada no existe.");

    private static readonly Error SeguroInactivo =
        new("Seguro.Inactivo", "La aseguradora no existe o está inactiva.");

    private static readonly Error YaEstaActivo =
        new("Seguro.YaActivo", "La aseguradora ya está activa.");

    private static readonly Error YaEstaInactivo =
        new("Seguro.YaInactivo", "La aseguradora ya está inactiva.");

    private static readonly Error NoSeEliminan =
        new("Seguro.NoSeElimina", "Las aseguradoras no se eliminan para conservar el historial de pólizas.");

    private static readonly Error PolizaInvalida =
        new("Poliza.Invalida", "Debe indicar un paciente y una aseguradora válidos.");

    private static readonly Error PolizaVencida =
        new("Poliza.FechaVencida", "La fecha de vencimiento no puede ser anterior a hoy.");

    private static readonly Error PacienteNoExiste =
        new("Poliza.PacienteNoExiste", "El paciente indicado no existe.");

    private static readonly Error PolizaDuplicada =
        new("Poliza.Duplicada", "Ese paciente ya tiene registrada esa póliza.");

    private static readonly Error PolizaNoExiste =
        new("Poliza.NoExiste", "La póliza indicada no existe.");

    private static readonly Error PolizaNoPuedeCambiar =
        new("Poliza.NoPuedeCambiar", "No se puede cambiar el paciente ni la aseguradora de una póliza.");

    private static readonly Error ErrorAlGuardar =
        new("Seguro.ErrorAlGuardar", "No se pudo guardar. Verifique que no esté duplicado.");

    // Devuelve null si el texto viene vacío o en blanco.
    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    // 1. Guardar o actualizar una aseguradora
    public async Task<Result> Guardar(Seguro seguro)
    {
        if (seguro == null || string.IsNullOrWhiteSpace(seguro.Nombre))
            return Result.Failure(NombreRequerido);

        if (seguro.Nombre.Trim().Length > 250 ||
            seguro.Telefono?.Length > 250 ||
            seguro.Email?.Length > 250 ||
            seguro.Direccion?.Length > 250)
        {
            return Result.Failure(TextoMuyLargo);
        }

        await using var contexto = await contextFactory.CreateDbContextAsync();

        string nombre = seguro.Nombre.Trim();

        bool nombreExiste = await contexto.Seguros
            .AnyAsync(s =>
                s.Nombre.ToLower() == nombre.ToLower() &&
                s.SeguroId != seguro.SeguroId);

        if (nombreExiste)
            return Result.Failure(NombreDuplicado);

        try
        {
            if (seguro.SeguroId == 0)
            {
                contexto.Seguros.Add(new Seguro
                {
                    FechaRegistro = DateTime.Now,
                    Nombre = nombre,
                    Telefono = Limpiar(seguro.Telefono),
                    Email = Limpiar(seguro.Email),
                    Direccion = Limpiar(seguro.Direccion),
                    Activo = true
                });
            }
            else
            {
                var existente = await contexto.Seguros
                    .FirstOrDefaultAsync(s => s.SeguroId == seguro.SeguroId);

                if (existente == null)
                    return Result.Failure(SeguroNoExiste);

                existente.Nombre = nombre;
                existente.Telefono = Limpiar(seguro.Telefono);
                existente.Email = Limpiar(seguro.Email);
                existente.Direccion = Limpiar(seguro.Direccion);

                // El estado Activo se modifica mediante Desactivar o Activar.
            }

            await contexto.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // 2. Buscar aseguradora por ID
    public async Task<Result<Seguro?>> Buscar(int seguroId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var seguro = await contexto.Seguros
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SeguroId == seguroId);

        return Result<Seguro?>.Success(seguro);
    }

    // 3. Obtener aseguradoras según un criterio
    public async Task<Result<List<Seguro>>> GetList(
        Expression<Func<Seguro, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var seguros = await contexto.Seguros
            .AsNoTracking()
            .Where(criterio)
            .OrderBy(s => s.Nombre)
            .ToListAsync();

        return Result<List<Seguro>>.Success(seguros);
    }

    // 4. Buscar aseguradoras por nombre
    public async Task<Result<List<Seguro>>> BuscarPorNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Result<List<Seguro>>.Success(new List<Seguro>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        nombre = nombre.Trim();

        var seguros = await contexto.Seguros
            .AsNoTracking()
            .Where(s => s.Nombre.Contains(nombre))
            .OrderBy(s => s.Nombre)
            .ToListAsync();

        return Result<List<Seguro>>.Success(seguros);
    }

    // 5. Buscar aseguradoras activas
    public async Task<Result<List<Seguro>>> BuscarActivos()
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var seguros = await contexto.Seguros
            .AsNoTracking()
            .Where(s => s.Activo)
            .OrderBy(s => s.Nombre)
            .ToListAsync();

        return Result<List<Seguro>>.Success(seguros);
    }

    // 6. Buscar aseguradoras de un paciente
    public async Task<Result<List<PacienteSeguro>>> BuscarPorPaciente(
        int pacienteId)
    {
        if (pacienteId <= 0)
            return Result<List<PacienteSeguro>>.Success(new List<PacienteSeguro>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var polizas = await contexto.PacienteSeguros
            .AsNoTracking()
            .Include(ps => ps.Seguro)
            .Where(ps => ps.PacienteId == pacienteId)
            .OrderByDescending(ps => ps.FechaRegistro)
            .ToListAsync();

        return Result<List<PacienteSeguro>>.Success(polizas);
    }

    // 7. Buscar pólizas y pacientes de una aseguradora
    public async Task<Result<List<PacienteSeguro>>> BuscarPacientes(
        int seguroId)
    {
        if (seguroId <= 0)
            return Result<List<PacienteSeguro>>.Success(new List<PacienteSeguro>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var polizas = await contexto.PacienteSeguros
            .AsNoTracking()
            .Include(ps => ps.Paciente)
            .Where(ps => ps.SeguroId == seguroId)
            .OrderBy(ps => ps.Paciente.Nombre)
            .ThenBy(ps => ps.Paciente.Apellido)
            .ToListAsync();

        return Result<List<PacienteSeguro>>.Success(polizas);
    }

    // 8. Registrar una póliza para un paciente
    public async Task<Result> RegistrarSeguroPaciente(
        PacienteSeguro pacienteSeguro)
    {
        if (pacienteSeguro == null ||
            pacienteSeguro.PacienteId <= 0 ||
            pacienteSeguro.SeguroId <= 0)
        {
            return Result.Failure(PolizaInvalida);
        }

        if (pacienteSeguro.NumeroPoliza?.Length > 250 ||
            pacienteSeguro.Titular?.Length > 250 ||
            pacienteSeguro.Observaciones?.Length > 250)
        {
            return Result.Failure(TextoMuyLargo);
        }

        // Una póliza NUEVA no puede nacer ya vencida.
        if (pacienteSeguro.FechaVencimiento.HasValue &&
            pacienteSeguro.FechaVencimiento.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            return Result.Failure(PolizaVencida);
        }

        await using var contexto = await contextFactory.CreateDbContextAsync();

        bool pacienteExiste = await contexto.Pacientes
            .AnyAsync(p => p.PacienteId == pacienteSeguro.PacienteId);

        if (!pacienteExiste)
            return Result.Failure(PacienteNoExiste);

        bool seguroValido = await contexto.Seguros
            .AnyAsync(s => s.SeguroId == pacienteSeguro.SeguroId && s.Activo);

        if (!seguroValido)
            return Result.Failure(SeguroInactivo);

        // Evitar registrar dos veces la misma póliza
        string? numero = Limpiar(pacienteSeguro.NumeroPoliza);

        if (numero != null)
        {
            bool duplicada = await contexto.PacienteSeguros
                .AnyAsync(ps =>
                    ps.PacienteId == pacienteSeguro.PacienteId &&
                    ps.SeguroId == pacienteSeguro.SeguroId &&
                    ps.NumeroPoliza == numero);

            if (duplicada)
                return Result.Failure(PolizaDuplicada);
        }

        try
        {
            contexto.PacienteSeguros.Add(new PacienteSeguro
            {
                FechaRegistro = DateTime.Now,
                PacienteId = pacienteSeguro.PacienteId,
                SeguroId = pacienteSeguro.SeguroId,
                NumeroPoliza = numero,
                Titular = Limpiar(pacienteSeguro.Titular),
                FechaVencimiento = pacienteSeguro.FechaVencimiento,
                Observaciones = Limpiar(pacienteSeguro.Observaciones)
            });

            await contexto.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // 9. Actualizar una póliza existente
    public async Task<Result> ActualizarSeguroPaciente(
        PacienteSeguro pacienteSeguro)
    {
        if (pacienteSeguro == null ||
            pacienteSeguro.PacienteSeguroId <= 0 ||
            pacienteSeguro.PacienteId <= 0 ||
            pacienteSeguro.SeguroId <= 0)
        {
            return Result.Failure(PolizaInvalida);
        }

        if (pacienteSeguro.NumeroPoliza?.Length > 250 ||
            pacienteSeguro.Titular?.Length > 250 ||
            pacienteSeguro.Observaciones?.Length > 250)
        {
            return Result.Failure(TextoMuyLargo);
        }

        await using var contexto = await contextFactory.CreateDbContextAsync();

        var existente = await contexto.PacienteSeguros
            .FirstOrDefaultAsync(ps =>
                ps.PacienteSeguroId == pacienteSeguro.PacienteSeguroId);

        if (existente == null)
            return Result.Failure(PolizaNoExiste);

        // No permitir cambiar el paciente ni la aseguradora
        if (existente.PacienteId != pacienteSeguro.PacienteId ||
            existente.SeguroId != pacienteSeguro.SeguroId)
        {
            return Result.Failure(PolizaNoPuedeCambiar);
        }

        // Solo se valida la fecha si CAMBIÓ. Antes se rechazaba siempre que fuera
        // pasada, y una póliza ya vencida no se podía editar (ni su titular ni sus
        // observaciones) aunque la fecha no se tocara.
        if (pacienteSeguro.FechaVencimiento.HasValue &&
            pacienteSeguro.FechaVencimiento != existente.FechaVencimiento &&
            pacienteSeguro.FechaVencimiento.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            return Result.Failure(PolizaVencida);
        }

        string? numero = Limpiar(pacienteSeguro.NumeroPoliza);

        if (numero != null)
        {
            bool duplicada = await contexto.PacienteSeguros
                .AnyAsync(ps =>
                    ps.PacienteSeguroId != pacienteSeguro.PacienteSeguroId &&
                    ps.PacienteId == pacienteSeguro.PacienteId &&
                    ps.SeguroId == pacienteSeguro.SeguroId &&
                    ps.NumeroPoliza == numero);

            if (duplicada)
                return Result.Failure(PolizaDuplicada);
        }

        try
        {
            existente.NumeroPoliza = numero;
            existente.Titular = Limpiar(pacienteSeguro.Titular);
            existente.FechaVencimiento = pacienteSeguro.FechaVencimiento;
            existente.Observaciones = Limpiar(pacienteSeguro.Observaciones);

            await contexto.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // 10. Buscar póliza por ID
    public async Task<Result<PacienteSeguro?>> BuscarPoliza(int pacienteSeguroId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var poliza = await contexto.PacienteSeguros
            .AsNoTracking()
            .Include(ps => ps.Paciente)
            .Include(ps => ps.Seguro)
            .FirstOrDefaultAsync(ps =>
                ps.PacienteSeguroId == pacienteSeguroId);

        return Result<PacienteSeguro?>.Success(poliza);
    }

    // 11. Buscar pólizas vencidas
    public async Task<Result<List<PacienteSeguro>>> BuscarPolizasVencidas()
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);

        var polizas = await contexto.PacienteSeguros
            .AsNoTracking()
            .Include(ps => ps.Paciente)
            .Include(ps => ps.Seguro)
            .Where(ps =>
                ps.FechaVencimiento.HasValue &&
                ps.FechaVencimiento.Value < hoy)
            .OrderBy(ps => ps.FechaVencimiento)
            .ToListAsync();

        return Result<List<PacienteSeguro>>.Success(polizas);
    }

    // 12. Buscar pólizas próximas a vencer
    public async Task<Result<List<PacienteSeguro>>> BuscarPolizasPorVencer(
        int diasAnticipacion = 30)
    {
        if (diasAnticipacion < 0 || diasAnticipacion > 365)
            return Result<List<PacienteSeguro>>.Success(new List<PacienteSeguro>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
        DateOnly limite = hoy.AddDays(diasAnticipacion);

        var polizas = await contexto.PacienteSeguros
            .AsNoTracking()
            .Include(ps => ps.Paciente)
            .Include(ps => ps.Seguro)
            .Where(ps =>
                ps.FechaVencimiento.HasValue &&
                ps.FechaVencimiento.Value >= hoy &&
                ps.FechaVencimiento.Value <= limite)
            .OrderBy(ps => ps.FechaVencimiento)
            .ToListAsync();

        return Result<List<PacienteSeguro>>.Success(polizas);
    }

    // 13. Desactivar una aseguradora
    public async Task<Result> Desactivar(int seguroId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var seguro = await contexto.Seguros
            .FirstOrDefaultAsync(s => s.SeguroId == seguroId);

        if (seguro == null)
            return Result.Failure(SeguroNoExiste);

        if (!seguro.Activo)
            return Result.Failure(YaEstaInactivo);

        seguro.Activo = false;
        await contexto.SaveChangesAsync();
        return Result.Success();
    }

    // 14. Reactivar una aseguradora
    public async Task<Result> Activar(int seguroId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var seguro = await contexto.Seguros
            .FirstOrDefaultAsync(s => s.SeguroId == seguroId);

        if (seguro == null)
            return Result.Failure(SeguroNoExiste);

        if (seguro.Activo)
            return Result.Failure(YaEstaActivo);

        seguro.Activo = true;
        await contexto.SaveChangesAsync();
        return Result.Success();
    }

    // 15. No permitir eliminar aseguradoras para conservar el historial de pólizas
    public Task<Result> Eliminar(int seguroId)
    {
        return Task.FromResult(Result.Failure(NoSeEliminan));
    }
}