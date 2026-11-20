using System.ComponentModel.DataAnnotations;

namespace GestionCirugiaPlastica.Infrastructure.Models;

public sealed class Noticia
{
    [Key]
    public int NoticiaId { get; set; }

    [Required, StringLength(200)]
    public string Titulo { get; set; } = "";

    [StringLength(500)]
    public string? Resumen { get; set; }

    [Required]
    public string Contenido { get; set; } = "";

    public DateTime FechaPublicacion { get; set; }

    public bool Publicada { get; set; }

    public int? CreadaPorUsuarioId { get; set; }

    public Usuario? CreadaPorUsuario { get; set; }
}
