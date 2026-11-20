using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class PagosServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Pago, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error PagoRequerido =
        new("Pago.Requerido", "Debe enviar los datos del pago.");

    private static readonly Error MontoInvalido =
        new("Pago.MontoInvalido", "El monto del pago debe ser mayor que cero.");

    private static readonly Error DestinoRequerido =
        new("Pago.DestinoRequerido", "El pago debe estar asociado a un paciente, una cirugía o una cita.");

    private static readonly Error DestinoDuplicado =
        new("Pago.DestinoDuplicado", "El pago no puede estar asociado a una cirugía y a una cita a la vez.");

    private static readonly Error PacienteNoExiste =
        new("Pago.PacienteNoExiste", "El paciente indicado no existe.");

    private static readonly Error DestinoPacienteNoCoincide =
        new("Pago.DestinoPacienteNoCoincide", "El paciente debe coincidir con el paciente de la cita o cirugía seleccionada.");

    private static readonly Error SeguroPacienteNoCoincide =
        new("Pago.SeguroPacienteNoCoincide", "La aseguradora seleccionada no está vinculada a este paciente o no está activa.");

    private static readonly Error MontoSeguroInvalido =
        new("Pago.MontoSeguroInvalido", "La cobertura del seguro debe estar entre cero y el monto total.");

    private static readonly Error EstadoInvalido =
        new("Pago.EstadoInvalido", "El estado debe ser Pagado o Pendiente.");

    private static readonly Error TextoMuyLargo =
        new("Pago.TextoMuyLargo", "El método de pago y el concepto no pueden pasar de 250 caracteres.");

    private static readonly Error CirugiaNoExiste =
        new("Pago.CirugiaNoExiste", "La cirugía indicada no existe.");

    private static readonly Error CitaNoExiste =
        new("Pago.CitaNoExiste", "La cita indicada no existe.");

    private static readonly Error CitaCancelada =
        new("Pago.CitaCancelada", "No se pueden registrar nuevos pagos para una cita cancelada.");

    private static readonly Error PagoNoExiste =
        new("Pago.NoExiste", "El pago indicado no existe.");

    private static readonly Error ErrorAlGuardar =
        new("Pago.ErrorAlGuardar", "No se pudo guardar el pago.");

    private static readonly Error ErrorAlEliminar =
        new("Pago.ErrorAlEliminar", "No se pudo eliminar el pago.");

    // ── Guardar ────────────────────────────────────────────────────────────
    // Registrar o modificar un pago
    public async Task<Result> Guardar(Pago pago)
    {
        if (pago == null)
            return Result.Failure(PagoRequerido);

        if (pago.Monto <= 0)
            return Result.Failure(MontoInvalido);

        if (pago.CirugiaId == null && pago.CitaId == null && pago.PacienteId == null)
            return Result.Failure(DestinoRequerido);

        if (pago.CirugiaId.HasValue && pago.CitaId.HasValue)
            return Result.Failure(DestinoDuplicado);

        if (pago.MontoSeguro < 0 || pago.MontoSeguro > pago.Monto)
            return Result.Failure(MontoSeguroInvalido);

        var estado = string.IsNullOrWhiteSpace(pago.Estado) ? "Pagado" : pago.Estado.Trim();
        if (estado is not ("Pagado" or "Pendiente"))
            return Result.Failure(EstadoInvalido);

        if (pago.MetodoPago?.Trim().Length > 250 ||
            pago.Concepto?.Trim().Length > 250)
            return Result.Failure(TextoMuyLargo);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        int? destinoPacienteId = pago.PacienteId;
        Cita? citaDestino = null;

        if (pago.PacienteId.HasValue && !await contexto.Pacientes.AnyAsync(p => p.PacienteId == pago.PacienteId.Value))
            return Result.Failure(PacienteNoExiste);

        if (pago.CirugiaId.HasValue)
        {
            var cirugiaPacienteId = await contexto.Cirugias
                .Where(c => c.CirugiaId == pago.CirugiaId.Value)
                .Select(c => (int?)c.PacienteId)
                .FirstOrDefaultAsync();

            if (cirugiaPacienteId is null)
                return Result.Failure(CirugiaNoExiste);

            if (destinoPacienteId.HasValue && destinoPacienteId != cirugiaPacienteId)
                return Result.Failure(DestinoPacienteNoCoincide);

            destinoPacienteId = cirugiaPacienteId;
        }

        if (pago.CitaId.HasValue)
        {
            citaDestino = await contexto.Citas
                .FirstOrDefaultAsync(c => c.CitaId == pago.CitaId.Value);

            if (citaDestino is null)
                return Result.Failure(CitaNoExiste);

            if (destinoPacienteId.HasValue && destinoPacienteId.Value != citaDestino.PacienteId)
                return Result.Failure(DestinoPacienteNoCoincide);

            destinoPacienteId = citaDestino.PacienteId;
            if (pago.PagoId == 0 && string.Equals(citaDestino.Estado, "Cancelada", StringComparison.OrdinalIgnoreCase))
                return Result.Failure(CitaCancelada);
        }

        if (!destinoPacienteId.HasValue)
            return Result.Failure(DestinoRequerido);

        if (pago.SeguroId.HasValue && !await contexto.PacienteSeguros.AnyAsync(ps =>
                ps.PacienteId == destinoPacienteId.Value && ps.SeguroId == pago.SeguroId.Value && ps.Seguro.Activo))
            return Result.Failure(SeguroPacienteNoCoincide);

        pago.PacienteId = destinoPacienteId;
        pago.Estado = estado;

        try
        {
            int? citaAnteriorId = null;
            if (pago.PagoId == 0)
            {
                // Se copian solo los campos propios del pago, así no se guarda por
                // accidente la Cirugía o la Cita que vengan anidadas en el JSON.
                contexto.Pagos.Add(new Pago
                {
                    // Conserva la fecha elegida para el movimiento; usa la fecha actual si no se envió.
                    Fecha = pago.Fecha == default ? DateTime.Now : pago.Fecha,
                    CirugiaId = pago.CirugiaId,
                    CitaId = pago.CitaId,
                    PacienteId = destinoPacienteId,
                    SeguroId = pago.SeguroId,
                    Monto = pago.Monto,
                    MontoSeguro = pago.MontoSeguro,
                    Estado = estado,
                    MetodoPago = pago.MetodoPago?.Trim(),
                    Concepto = pago.Concepto?.Trim()
                });
            }
            else
            {
                var existente = await contexto.Pagos
                    .FirstOrDefaultAsync(p => p.PagoId == pago.PagoId);

                if (existente == null)
                    return Result.Failure(PagoNoExiste);

                citaAnteriorId = existente.CitaId;
                // La fecha original del pago no se modifica.
                existente.CirugiaId = pago.CirugiaId;
                existente.CitaId = pago.CitaId;
                existente.PacienteId = destinoPacienteId;
                existente.SeguroId = pago.SeguroId;
                existente.Monto = pago.Monto;
                existente.MontoSeguro = pago.MontoSeguro;
                existente.Estado = estado;
                existente.MetodoPago = pago.MetodoPago?.Trim();
                existente.Concepto = pago.Concepto?.Trim();
            }

            if (citaDestino is not null)
                citaDestino.EstadoPago = estado;

            await contexto.SaveChangesAsync();

            var citasAActualizar = new HashSet<int>();
            if (citaAnteriorId.HasValue) citasAActualizar.Add(citaAnteriorId.Value);
            if (pago.CitaId.HasValue) citasAActualizar.Add(pago.CitaId.Value);
            foreach (var citaId in citasAActualizar)
                await ActualizarEstadoPagoCita(contexto, citaId);

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    /// <summary>
    /// Devuelve cobros reales y saldos pendientes calculados desde el costo de cada cita.
    /// Los saldos no se insertan en Pagos, por lo que nunca se contabilizan como dinero recibido.
    /// </summary>
    public async Task<Result<List<EstadoCuentaPagoDto>>> ObtenerEstadoCuenta()
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var movimientos = await contexto.Pagos.AsNoTracking()
            .Select(p => new EstadoCuentaPagoDto
            {
                PagoId = p.PagoId,
                PacienteId = p.PacienteId ?? (p.CitaId.HasValue ? p.Cita!.PacienteId : p.CirugiaId.HasValue ? p.Cirugia!.PacienteId : 0),
                Paciente = p.PacienteId.HasValue
                    ? p.Paciente!.Nombre + " " + p.Paciente.Apellido
                    : p.CitaId.HasValue
                        ? p.Cita!.Paciente!.Nombre + " " + p.Cita.Paciente.Apellido
                        : p.CirugiaId.HasValue
                            ? p.Cirugia!.Paciente.Nombre + " " + p.Cirugia.Paciente.Apellido
                            : "Paciente",
                CitaId = p.CitaId,
                CirugiaId = p.CirugiaId,
                Destino = p.CitaId.HasValue ? "Cita #" + p.CitaId : p.CirugiaId.HasValue ? "Cirugía #" + p.CirugiaId : "Pago directo al paciente",
                Fecha = p.Fecha,
                Monto = p.Monto,
                MetodoPago = p.MetodoPago,
                Concepto = p.Concepto,
                Estado = "Realizado",
                EsRecibido = true
            })
            .ToListAsync();

        var cuentasCitas = await contexto.Citas.AsNoTracking()
            .Where(c => c.Costo > 0)
            .Select(c => new
            {
                c.CitaId,
                c.PacienteId,
                Paciente = c.Paciente!.Nombre + " " + c.Paciente.Apellido,
                c.Fecha,
                c.Estado,
                c.Costo,
                Recibido = c.Pagos.Select(p => (decimal?)p.Monto).Sum() ?? 0m
            })
            .ToListAsync();

        foreach (var cita in cuentasCitas)
        {
            var saldo = Math.Max(cita.Costo - cita.Recibido, 0m);
            if (saldo <= 0) continue;

            var cancelada = string.Equals(cita.Estado, "Cancelada", StringComparison.OrdinalIgnoreCase);
            movimientos.Add(new EstadoCuentaPagoDto
            {
                PacienteId = cita.PacienteId,
                Paciente = cita.Paciente,
                CitaId = cita.CitaId,
                Destino = $"Cita #{cita.CitaId}",
                Fecha = cita.Fecha,
                Monto = saldo,
                Concepto = cancelada ? "Saldo no cobrado de cita cancelada" : "Saldo pendiente de la cita",
                Estado = cancelada ? "Cancelado" : cita.Recibido > 0 ? "Parcial" : "Pendiente",
                EsRecibido = false
            });
        }

        return Result<List<EstadoCuentaPagoDto>>.Success(
            movimientos.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.PagoId ?? 0).ToList());
    }

    /// <summary>Opciones de cuenta que puede seleccionar el personal al registrar un cobro.</summary>
    public async Task<Result<List<ReferenciaPagoDto>>> ObtenerReferencias(int pacienteId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var citas = await contexto.Citas.AsNoTracking()
            .Where(c => c.PacienteId == pacienteId && c.Costo > 0 && c.Estado != "Cancelada")
            .Select(c => new
            {
                c.CitaId,
                c.Fecha,
                c.TipoCita,
                c.Costo,
                Recibido = c.Pagos.Select(p => (decimal?)p.Monto).Sum() ?? 0m
            })
            .ToListAsync();

        var referencias = citas
            .Where(c => c.Costo - c.Recibido > 0)
            .Select(c => new ReferenciaPagoDto
            {
                Clave = $"CITA:{c.CitaId}",
                CitaId = c.CitaId,
                Etiqueta = $"Cita #{c.CitaId} · {c.TipoCita ?? "Consulta"} · {c.Fecha:dd/MM/yyyy}",
                SaldoPendiente = c.Costo - c.Recibido
            })
            .ToList();

        var cirugias = await contexto.Cirugias.AsNoTracking()
            .Where(c => c.PacienteId == pacienteId)
            .Select(c => new { c.CirugiaId, c.FechaCirugia, Procedimiento = c.Procedimiento.Nombre })
            .ToListAsync();

        referencias.AddRange(cirugias.Select(c => new ReferenciaPagoDto
        {
            Clave = $"CIRUGIA:{c.CirugiaId}",
            CirugiaId = c.CirugiaId,
            Etiqueta = $"Cirugía #{c.CirugiaId} · {c.Procedimiento} · {c.FechaCirugia:dd/MM/yyyy}",
            SaldoPendiente = null
        }));

        return Result<List<ReferenciaPagoDto>>.Success(referencias);
    }

    private static async Task ActualizarEstadoPagoCita(
        GestionCirugiaPlasticaContext contexto,
        int citaId)
    {
        var cita = await contexto.Citas.FirstOrDefaultAsync(c => c.CitaId == citaId);
        if (cita is null) return;

        var recibido = await contexto.Pagos
            .Where(p => p.CitaId == citaId)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;
        cita.EstadoPago = EstadoPagoCita.Calcular(cita.Costo, recibido);
        await contexto.SaveChangesAsync();
    }

    // ── Consultas ──────────────────────────────────────────────────────────
    // Buscar pago por ID
    public async Task<Result<Pago?>> Buscar(int pagoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pago = await ConRelaciones(contexto)
            .FirstOrDefaultAsync(p => p.PagoId == pagoId);

        return Result<Pago?>.Success(pago);
    }

    // Listar pagos mediante un criterio personalizado
    public async Task<Result<List<Pago>>> GetList(
        Expression<Func<Pago, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pagos = await ConRelaciones(contexto)
            .Where(criterio)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    // Buscar pagos por cirugía
    public async Task<Result<List<Pago>>> BuscarPorCirugia(int cirugiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pagos = await ConRelaciones(contexto)
            .Where(p => p.CirugiaId == cirugiaId)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    // Buscar pagos por cita
    public async Task<Result<List<Pago>>> BuscarPorCita(int citaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pagos = await ConRelaciones(contexto)
            .Where(p => p.CitaId == citaId)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    // Buscar pagos de una fecha específica (todo el día, sin importar la hora)
    public async Task<Result<List<Pago>>> BuscarPorFecha(DateTime fecha)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        DateTime inicio = fecha.Date;
        DateTime fin = inicio.AddDays(1);

        var pagos = await ConRelaciones(contexto)
            .Where(p => p.Fecha >= inicio && p.Fecha < fin)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    // Buscar pagos entre dos fechas (ambas incluidas)
    public async Task<Result<List<Pago>>> BuscarPorRangoFechas(
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        if (fechaInicio.Date > fechaFin.Date)
            return Result<List<Pago>>.Success(new List<Pago>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        DateTime inicio = fechaInicio.Date;
        DateTime fin = fechaFin.Date.AddDays(1);

        var pagos = await ConRelaciones(contexto)
            .Where(p => p.Fecha >= inicio && p.Fecha < fin)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.PagoId)
            .ToListAsync();

        return Result<List<Pago>>.Success(pagos);
    }

    private static IQueryable<Pago> ConRelaciones(GestionCirugiaPlasticaContext contexto) =>
        contexto.Pagos
            .AsNoTracking()
            .Include(p => p.Paciente)
            .Include(p => p.Seguro)
            .Include(p => p.Cirugia).ThenInclude(c => c!.Paciente)
            .Include(p => p.Cita).ThenInclude(c => c!.Paciente);

    // ── Eliminar ───────────────────────────────────────────────────────────
    public async Task<Result> Eliminar(int pagoId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        try
        {
            var citaId = await contexto.Pagos.AsNoTracking()
                .Where(p => p.PagoId == pagoId)
                .Select(p => p.CitaId)
                .FirstOrDefaultAsync();

            bool eliminado = await contexto.Pagos
                .Where(p => p.PagoId == pagoId)
                .ExecuteDeleteAsync() > 0;

            if (!eliminado) return Result.Failure(PagoNoExiste);
            if (citaId.HasValue) await ActualizarEstadoPagoCita(contexto, citaId.Value);
            return Result.Success();
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return Result.Failure(ErrorAlEliminar);
        }
    }
}

public sealed class EstadoCuentaPagoDto
{
    public int? PagoId { get; set; }
    public int PacienteId { get; set; }
    public string Paciente { get; set; } = "";
    public int? CitaId { get; set; }
    public int? CirugiaId { get; set; }
    public string Destino { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Concepto { get; set; }
    public string Estado { get; set; } = "";
    public bool EsRecibido { get; set; }
}

public sealed class ReferenciaPagoDto
{
    public string Clave { get; set; } = "";
    public int? CitaId { get; set; }
    public int? CirugiaId { get; set; }
    public string Etiqueta { get; set; } = "";
    public decimal? SaldoPendiente { get; set; }
}
