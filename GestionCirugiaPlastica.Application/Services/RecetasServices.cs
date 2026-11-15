using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class RecetasServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Receta, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error RecetaRequerida =
        new("Receta.Requerida", "Debe enviar los datos de la receta.");

    private static readonly Error PacienteRequerido =
        new("Receta.PacienteRequerido", "Debe indicar un paciente válido.");

    private static readonly Error IndicacionesMuyLargas =
        new("Receta.IndicacionesMuyLargas", "Las indicaciones no pueden pasar de 4000 caracteres.");

    private static readonly Error DetallesRequeridos =
        new("Receta.DetallesRequeridos", "La receta debe tener al menos un medicamento.");

    private static readonly Error MedicamentoRequerido =
        new("Receta.MedicamentoRequerido", "Cada medicamento debe tener nombre.");

    private static readonly Error DetalleMuyLargo =
        new("Receta.DetalleMuyLargo",
            "El medicamento, la dosis, la frecuencia, la duración y las instrucciones no pueden pasar de 250 caracteres.");

    private static readonly Error OrigenDuplicado =
        new("Receta.OrigenDuplicado", "La receta no puede estar vinculada a una consulta y a una cirugía a la vez.");

    private static readonly Error PacienteNoExiste =
        new("Receta.PacienteNoExiste", "El paciente indicado no existe.");

    private static readonly Error ConsultaInvalida =
        new("Receta.ConsultaInvalida", "La consulta indicada no existe o no pertenece al paciente.");

    private static readonly Error CirugiaInvalida =
        new("Receta.CirugiaInvalida", "La cirugía indicada no existe o no pertenece al paciente.");

    private static readonly Error UsuarioNoExiste =
        new("Receta.UsuarioNoExiste", "El usuario indicado no existe.");

    private static readonly Error RecetaNoExiste =
        new("Receta.NoExiste", "La receta indicada no existe.");

    private static readonly Error NoPuedeCambiarOrigen =
        new("Receta.NoPuedeCambiarOrigen", "No se puede cambiar el paciente ni el origen de una receta existente.");

    private static readonly Error DetalleNoExiste =
        new("Receta.DetalleNoExiste", "Uno de los medicamentos indicados no pertenece a esta receta.");

    private static readonly Error ErrorAlGuardar =
        new("Receta.ErrorAlGuardar", "No se pudo guardar la receta.");

    private static readonly Error EliminarNoPermitido =
        new("Receta.EliminarNoPermitido", "No se permite eliminar recetas médicas.");

    // ── Guardar ────────────────────────────────────────────────────────────
    // Registrar o actualizar una receta
    public async Task<Result> Guardar(Receta receta)
    {
        if (receta == null)
            return Result.Failure(RecetaRequerida);

        if (receta.PacienteId <= 0)
            return Result.Failure(PacienteRequerido);

        if (receta.Indicaciones?.Trim().Length > 4000)
            return Result.Failure(IndicacionesMuyLargas);

        if (receta.DetallesReceta == null || receta.DetallesReceta.Count == 0)
            return Result.Failure(DetallesRequeridos);

        if (receta.DetallesReceta.Any(d => string.IsNullOrWhiteSpace(d.Medicamento)))
            return Result.Failure(MedicamentoRequerido);

        if (receta.DetallesReceta.Any(d =>
                d.Medicamento.Trim().Length > 250 ||
                d.Dosis?.Trim().Length > 250 ||
                d.Frecuencia?.Trim().Length > 250 ||
                d.Duracion?.Trim().Length > 250 ||
                d.Instrucciones?.Trim().Length > 250))
            return Result.Failure(DetalleMuyLargo);

        // Una receta no debe estar vinculada simultáneamente a una consulta y a una cirugía.
        if (receta.ConsultaId.HasValue && receta.CirugiaId.HasValue)
            return Result.Failure(OrigenDuplicado);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        bool pacienteExiste = await contexto.Pacientes
            .AnyAsync(p => p.PacienteId == receta.PacienteId);

        if (!pacienteExiste)
            return Result.Failure(PacienteNoExiste);

        if (receta.ConsultaId.HasValue)
        {
            bool consultaValida = await contexto.Consultas
                .AnyAsync(c =>
                    c.ConsultaId == receta.ConsultaId.Value &&
                    c.PacienteId == receta.PacienteId);

            if (!consultaValida)
                return Result.Failure(ConsultaInvalida);
        }

        if (receta.CirugiaId.HasValue)
        {
            bool cirugiaValida = await contexto.Cirugias
                .AnyAsync(c =>
                    c.CirugiaId == receta.CirugiaId.Value &&
                    c.PacienteId == receta.PacienteId);

            if (!cirugiaValida)
                return Result.Failure(CirugiaInvalida);
        }

        if (receta.UsuarioId.HasValue)
        {
            bool usuarioExiste = await contexto.Usuarios
                .AnyAsync(u => u.UsuarioId == receta.UsuarioId.Value);

            if (!usuarioExiste)
                return Result.Failure(UsuarioNoExiste);
        }

        try
        {
            if (receta.RecetaId == 0)
            {
                // Se copian solo los campos propios, así no se guarda por accidente
                // el Paciente, la Consulta, etc. que vengan anidados en el JSON.
                contexto.Recetas.Add(new Receta
                {
                    Fecha = DateTime.Now,
                    PacienteId = receta.PacienteId,
                    ConsultaId = receta.ConsultaId,
                    CirugiaId = receta.CirugiaId,
                    UsuarioId = receta.UsuarioId,
                    Indicaciones = receta.Indicaciones?.Trim(),
                    DetallesReceta = receta.DetallesReceta
                        .Select(NuevoDetalle)
                        .ToList()
                });
            }
            else
            {
                var existente = await contexto.Recetas
                    .Include(r => r.DetallesReceta)
                    .FirstOrDefaultAsync(r => r.RecetaId == receta.RecetaId);

                if (existente == null)
                    return Result.Failure(RecetaNoExiste);

                // No permitir cambiar el paciente ni el origen de la receta.
                if (existente.PacienteId != receta.PacienteId ||
                    existente.ConsultaId != receta.ConsultaId ||
                    existente.CirugiaId != receta.CirugiaId)
                    return Result.Failure(NoPuedeCambiarOrigen);

                // Se valida todo antes de tocar nada: si un detalle no pertenece
                // a la receta, no se aplica ningún cambio.
                bool hayDetalleAjeno = receta.DetallesReceta.Any(d =>
                    d.DetalleRecetaId != 0 &&
                    !existente.DetallesReceta.Any(x => x.DetalleRecetaId == d.DetalleRecetaId));

                if (hayDetalleAjeno)
                    return Result.Failure(DetalleNoExiste);

                existente.Indicaciones = receta.Indicaciones?.Trim();

                // Actualizar o agregar medicamentos, sin eliminar los detalles
                // históricos que no vengan en la solicitud.
                foreach (var detalle in receta.DetallesReceta)
                {
                    if (detalle.DetalleRecetaId == 0)
                    {
                        existente.DetallesReceta.Add(NuevoDetalle(detalle));
                    }
                    else
                    {
                        var detalleExistente = existente.DetallesReceta
                            .First(d => d.DetalleRecetaId == detalle.DetalleRecetaId);

                        detalleExistente.Medicamento = detalle.Medicamento.Trim();
                        detalleExistente.Dosis = detalle.Dosis?.Trim();
                        detalleExistente.Frecuencia = detalle.Frecuencia?.Trim();
                        detalleExistente.Duracion = detalle.Duracion?.Trim();
                        detalleExistente.Instrucciones = detalle.Instrucciones?.Trim();
                    }
                }
            }

            await contexto.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    private static DetallesReceta NuevoDetalle(DetallesReceta d) => new()
    {
        Medicamento = d.Medicamento.Trim(),
        Dosis = d.Dosis?.Trim(),
        Frecuencia = d.Frecuencia?.Trim(),
        Duracion = d.Duracion?.Trim(),
        Instrucciones = d.Instrucciones?.Trim()
    };

    // ── Consultas ──────────────────────────────────────────────────────────
    // Buscar receta por ID, incluyendo sus medicamentos
    public async Task<Result<Receta?>> Buscar(int recetaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var receta = await contexto.Recetas
            .AsNoTracking()
            .Include(r => r.DetallesReceta)
            .FirstOrDefaultAsync(r => r.RecetaId == recetaId);

        return Result<Receta?>.Success(receta);
    }

    // Obtener recetas según un criterio
    public async Task<Result<List<Receta>>> GetList(
        Expression<Func<Receta, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var recetas = await contexto.Recetas
            .AsNoTracking()
            .Include(r => r.DetallesReceta)
            .Where(criterio)
            .OrderByDescending(r => r.Fecha)
            .ThenByDescending(r => r.RecetaId)
            .ToListAsync();

        return Result<List<Receta>>.Success(recetas);
    }

    // Buscar recetas por paciente
    public async Task<Result<List<Receta>>> BuscarPorPaciente(int pacienteId)
    {
        if (pacienteId <= 0)
            return Result<List<Receta>>.Success(new List<Receta>());

        return await GetList(r => r.PacienteId == pacienteId);
    }

    // Buscar recetas por consulta
    public async Task<Result<List<Receta>>> BuscarPorConsulta(int consultaId)
    {
        if (consultaId <= 0)
            return Result<List<Receta>>.Success(new List<Receta>());

        return await GetList(r => r.ConsultaId == consultaId);
    }

    // Buscar recetas por cirugía
    public async Task<Result<List<Receta>>> BuscarPorCirugia(int cirugiaId)
    {
        if (cirugiaId <= 0)
            return Result<List<Receta>>.Success(new List<Receta>());

        return await GetList(r => r.CirugiaId == cirugiaId);
    }

    // Buscar recetas por usuario que las emitió
    public async Task<Result<List<Receta>>> BuscarPorUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            return Result<List<Receta>>.Success(new List<Receta>());

        return await GetList(r => r.UsuarioId == usuarioId);
    }

    // Buscar recetas por rango de fechas (ambas incluidas)
    public async Task<Result<List<Receta>>> BuscarPorFecha(
        DateTime fechaDesde,
        DateTime fechaHasta)
    {
        if (fechaDesde.Date > fechaHasta.Date)
            return Result<List<Receta>>.Success(new List<Receta>());

        DateTime desde = fechaDesde.Date;
        DateTime hasta = fechaHasta.Date.AddDays(1);

        return await GetList(r => r.Fecha >= desde && r.Fecha < hasta);
    }

    // Buscar recetas por medicamento
    public async Task<Result<List<Receta>>> BuscarPorMedicamento(string medicamento)
    {
        if (string.IsNullOrWhiteSpace(medicamento))
            return Result<List<Receta>>.Success(new List<Receta>());

        medicamento = medicamento.Trim();

        return await GetList(r => r.DetallesReceta.Any(d => d.Medicamento.Contains(medicamento)));
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    // Las recetas médicas no se borran: se devuelve un fallo explícito.
    public Task<Result> Eliminar(int recetaId)
        => Task.FromResult(Result.Failure(EliminarNoPermitido));
}