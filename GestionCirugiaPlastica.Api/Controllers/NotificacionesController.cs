using GestionCirugiaPlastica.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctora")]
public sealed class NotificacionesController(IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var notifications = await db.Notificaciones.AsNoTracking()
            .OrderByDescending(n => n.FechaCreacion)
            .Take(100)
            .Select(n => new { n.NotificacionId, n.Tipo, n.Titulo, n.Mensaje, n.CitaId, n.FechaCreacion, n.Leida })
            .ToListAsync();
        return Ok(notifications);
    }

    [HttpGet("no-leidas/count")]
    public async Task<IActionResult> CountUnread()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return Ok(new { Count = await db.Notificaciones.CountAsync(n => !n.Leida) });
    }

    [HttpPatch("{id:int}/leer")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var notification = await db.Notificaciones.FirstOrDefaultAsync(n => n.NotificacionId == id);
        if (notification is null) return NotFound();
        notification.Leida = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("leer-todas")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        await db.Notificaciones.Where(n => !n.Leida).ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
        return NoContent();
    }
}
