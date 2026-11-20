using System.Security.Claims;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.Api.Services;

internal static class AuditoriaClinica
{
    public static async Task RegistrarAsync(IDbContextFactory<GestionCirugiaPlasticaContext> factory, ClaimsPrincipal principal,
        int pacienteId, string entidad, int? registroId, string accion, string detalle)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            throw new InvalidOperationException("El usuario autenticado no tiene un identificador válido.");
        await using var db = await factory.CreateDbContextAsync();
        db.AuditoriaExpedientes.Add(new AuditoriaExpediente
        {
            PacienteId = pacienteId, UsuarioId = usuarioId, Fecha = DateTime.UtcNow,
            Entidad = entidad, RegistroId = registroId, Accion = accion, Detalle = detalle
        });
        await db.SaveChangesAsync();
    }

    public static async Task<int?> PacienteDeCirugiaAsync(IDbContextFactory<GestionCirugiaPlasticaContext> factory, int cirugiaId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Cirugias.Where(c => c.CirugiaId == cirugiaId).Select(c => (int?)c.PacienteId).SingleOrDefaultAsync();
    }
}
