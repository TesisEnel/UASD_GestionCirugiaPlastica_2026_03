using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCirugiaPlastica.Infrastructure.Models;

[Table("Noticias")]
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

    [Column(TypeName = "datetime2")]
    public DateTime FechaPublicacion { get; set; }

    public bool Publicada { get; set; }

    public int? CreadaPorUsuarioId { get; set; }

    public Usuario? CreadaPorUsuario { get; set; }
}
