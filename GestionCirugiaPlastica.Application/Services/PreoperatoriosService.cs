using Aplicada1.Core;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GestionCirugiaPlastica.Application.Services;

public class PreoperatoriosServices(
    IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory
) : IServiceResult<Preoperatorio, int>
{
    // ── Errores ────────────────────────────────────────────────────────────
    // Recuerda: Result.Success(false) NO es un fallo. Para fallar: Result.Failure(error).
    private static readonly Error PreoperatorioRequerido =
        new("Preoperatorio.Requerido", "Debe enviar los datos de la evaluación preoperatoria.");

    private static readonly Error NoSeEdita =
        new("Preoperatorio.NoSeEdita", "Las evaluaciones preoperatorias solo permiten registros nuevos.");

    private static readonly Error CirugiaRequerida =
        new("Preoperatorio.CirugiaRequerida", "Debe indicar una cirugía válida.");

    private static readonly Error CirugiaNoExiste =
        new("Preoperatorio.CirugiaNoExiste", "La cirugía indicada no existe.");

    private static readonly Error AutorizacionIncompleta =
        new("Preoperatorio.AutorizacionIncompleta",
            "Para autorizar se requiere el análisis de laboratorio y las evaluaciones cardiológica y anestésica.");

    private static readonly Error ErrorAlGuardar =
        new("Preoperatorio.ErrorAlGuardar", "No se pudo guardar la evaluación preoperatoria.");

    private static readonly Error EliminarNoPermitido =
        new("Preoperatorio.EliminarNoPermitido", "No se permite eliminar evaluaciones clínicas.");

    // ── Guardar ────────────────────────────────────────────────────────────
    // Registrar una nueva evaluación preoperatoria (solo inserta; no se edita)
    public async Task<Result> Guardar(Preoperatorio preoperatorio)
    {
        if (preoperatorio == null)
            return Result.Failure(PreoperatorioRequerido);

        if (preoperatorio.PreoperatorioId != 0)
            return Result.Failure(NoSeEdita);

        if (preoperatorio.CirugiaId <= 0)
            return Result.Failure(CirugiaRequerida);

        // Para autorizar deben estar los tres requisitos.
        if (preoperatorio.Autorizado &&
            (string.IsNullOrWhiteSpace(preoperatorio.AnalisisLaboratorio) ||
             string.IsNullOrWhiteSpace(preoperatorio.EvaluacionCardiologica) ||
             string.IsNullOrWhiteSpace(preoperatorio.EvaluacionAnestesica)))
            return Result.Failure(AutorizacionIncompleta);

        await using var contexto = await contextFactory.CreateDbContextAsync();

        bool cirugiaExiste = await contexto.Cirugias
            .AnyAsync(c => c.CirugiaId == preoperatorio.CirugiaId);

        if (!cirugiaExiste)
            return Result.Failure(CirugiaNoExiste);

        try
        {
            // Se copian solo los campos propios, así no se guarda por accidente
            // la Cirugía que venga anidada en el JSON.
            contexto.Preoperatorios.Add(new Preoperatorio
            {
                // La fecha de la evaluación la pone el servidor.
                FechaEvaluacion = DateTime.Now,
                CirugiaId = preoperatorio.CirugiaId,
                AnalisisLaboratorio = preoperatorio.AnalisisLaboratorio?.Trim(),
                EvaluacionCardiologica = preoperatorio.EvaluacionCardiologica?.Trim(),
                EvaluacionAnestesica = preoperatorio.EvaluacionAnestesica?.Trim(),
                Autorizado = preoperatorio.Autorizado,
                Observaciones = preoperatorio.Observaciones?.Trim()
            });

            await contexto.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure(ErrorAlGuardar);
        }
    }

    // ── Consultas ──────────────────────────────────────────────────────────
    // Buscar evaluación por ID
    public async Task<Result<Preoperatorio?>> Buscar(int preoperatorioId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var preoperatorio = await contexto.Preoperatorios
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PreoperatorioId == preoperatorioId);

        return Result<Preoperatorio?>.Success(preoperatorio);
    }

    // Obtener evaluaciones según un criterio
    public async Task<Result<List<Preoperatorio>>> GetList(
        Expression<Func<Preoperatorio, bool>> criterio)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var preoperatorios = await contexto.Preoperatorios
            .AsNoTracking()
            .Where(criterio)
            .OrderByDescending(p => p.FechaEvaluacion)
            .ThenByDescending(p => p.PreoperatorioId)
            .ToListAsync();

        return Result<List<Preoperatorio>>.Success(preoperatorios);
    }

    // Historial de evaluaciones de una cirugía (la más reciente primero)
    public async Task<Result<List<Preoperatorio>>> BuscarHistorial(int cirugiaId)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var historial = await contexto.Preoperatorios
            .AsNoTracking()
            .Where(p => p.CirugiaId == cirugiaId)
            .OrderByDescending(p => p.FechaEvaluacion)
            .ThenByDescending(p => p.PreoperatorioId)
            .ToListAsync();

        return Result<List<Preoperatorio>>.Success(historial);
    }

    // Evaluaciones por cirugía (mismo resultado que BuscarHistorial)
    public Task<Result<List<Preoperatorio>>> BuscarPorCirugia(int cirugiaId)
        => BuscarHistorial(cirugiaId);

    // Evaluaciones según autorización
    public async Task<Result<List<Preoperatorio>>> BuscarPorAutorizacion(bool autorizado)
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var evaluaciones = await contexto.Preoperatorios
            .AsNoTracking()
            .Where(p => p.Autorizado == autorizado)
            .OrderByDescending(p => p.FechaEvaluacion)
            .ThenByDescending(p => p.PreoperatorioId)
            .ToListAsync();

        return Result<List<Preoperatorio>>.Success(evaluaciones);
    }

    // Evaluaciones con requisitos pendientes: sin autorizar o con algún requisito vacío
    public async Task<Result<List<Preoperatorio>>> BuscarRequisitosPendientes()
    {
        await using var contexto = await contextFactory.CreateDbContextAsync();

        var pendientes = await contexto.Preoperatorios
            .AsNoTracking()
            .Where(p =>
                !p.Autorizado ||
                p.AnalisisLaboratorio == null ||
                p.AnalisisLaboratorio.Trim() == "" ||
                p.EvaluacionCardiologica == null ||
                p.EvaluacionCardiologica.Trim() == "" ||
                p.EvaluacionAnestesica == null ||
                p.EvaluacionAnestesica.Trim() == "")
            .OrderByDescending(p => p.FechaEvaluacion)
            .ThenByDescending(p => p.PreoperatorioId)
            .ToListAsync();

        return Result<List<Preoperatorio>>.Success(pendientes);
    }

    // Evaluaciones por rango de fechas (ambas incluidas)
    public async Task<Result<List<Preoperatorio>>> BuscarPorFecha(
        DateTime fechaDesde,
        DateTime fechaHasta)
    {
        if (fechaDesde.Date > fechaHasta.Date)
            return Result<List<Preoperatorio>>.Success(new List<Preoperatorio>());

        await using var contexto = await contextFactory.CreateDbContextAsync();

        DateTime desde = fechaDesde.Date;
        DateTime hasta = fechaHasta.Date.AddDays(1);

        var evaluaciones = await contexto.Preoperatorios
            .AsNoTracking()
            .Where(p => p.FechaEvaluacion >= desde && p.FechaEvaluacion < hasta)
            .OrderByDescending(p => p.FechaEvaluacion)
            .ThenByDescending(p => p.PreoperatorioId)
            .ToListAsync();

        return Result<List<Preoperatorio>>.Success(evaluaciones);
    }

    // ── Eliminar ───────────────────────────────────────────────────────────
    // Las evaluaciones clínicas no se borran: se devuelve un fallo explícito.
    public Task<Result> Eliminar(int preoperatorioId)
        => Task.FromResult(Result.Failure(EliminarNoPermitido));
}