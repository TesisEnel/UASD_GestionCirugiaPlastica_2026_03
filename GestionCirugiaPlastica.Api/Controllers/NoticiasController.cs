using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GestionCirugiaPlastica.Infrastructure.Data;
using GestionCirugiaPlastica.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NoticiasController(IDbContextFactory<GestionCirugiaPlasticaContext> contextFactory) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetPublicadas()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var noticias = await db.Noticias.AsNoTracking()
            .Where(n => n.Publicada)
            .OrderByDescending(n => n.FechaPublicacion)
            .Select(n => new NoticiaDto(n.NoticiaId, n.Titulo, n.Resumen, n.Contenido, n.FechaPublicacion, n.Publicada))
            .ToListAsync();
        return Ok(noticias);
    }

    [Authorize(Roles = "Doctora,Secretaria")]
    [HttpGet("admin")]
    public async Task<IActionResult> GetTodas()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var noticias = await db.Noticias.AsNoTracking()
            .OrderByDescending(n => n.FechaPublicacion)
            .Select(n => new NoticiaDto(n.NoticiaId, n.Titulo, n.Resumen, n.Contenido, n.FechaPublicacion, n.Publicada))
            .ToListAsync();
        return Ok(noticias);
    }

    [Authorize(Roles = "Doctora,Secretaria")]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] NoticiaRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var noticia = new Noticia
        {
            Titulo = request.Titulo.Trim(),
            Resumen = string.IsNullOrWhiteSpace(request.Resumen) ? null : request.Resumen.Trim(),
            Contenido = request.Contenido.Trim(),
            FechaPublicacion = DateTime.Now,
            Publicada = request.Publicada,
            CreadaPorUsuarioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null
        };
        await using var db = await contextFactory.CreateDbContextAsync();
        db.Noticias.Add(noticia);
        await db.SaveChangesAsync();
        return Ok(new NoticiaDto(noticia.NoticiaId, noticia.Titulo, noticia.Resumen, noticia.Contenido, noticia.FechaPublicacion, noticia.Publicada));
    }

    [Authorize(Roles = "Doctora,Secretaria")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] NoticiaRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await using var db = await contextFactory.CreateDbContextAsync();
        var noticia = await db.Noticias.FirstOrDefaultAsync(n => n.NoticiaId == id);
        if (noticia is null) return NotFound();
        noticia.Titulo = request.Titulo.Trim();
        noticia.Resumen = string.IsNullOrWhiteSpace(request.Resumen) ? null : request.Resumen.Trim();
        noticia.Contenido = request.Contenido.Trim();
        noticia.Publicada = request.Publicada;
        if (request.Publicada && noticia.FechaPublicacion > DateTime.Now)
            noticia.FechaPublicacion = DateTime.Now;
        await db.SaveChangesAsync();
        return NoContent();
    }

    public sealed record NoticiaDto(int NoticiaId, string Titulo, string? Resumen, string Contenido, DateTime FechaPublicacion, bool Publicada);

    public sealed class NoticiaRequest
    {
        [Required, StringLength(200)] public string Titulo { get; set; } = "";
        [StringLength(500)] public string? Resumen { get; set; }
        [Required, StringLength(10000)] public string Contenido { get; set; } = "";
        public bool Publicada { get; set; }
    }
}
