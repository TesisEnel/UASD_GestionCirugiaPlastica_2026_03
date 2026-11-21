using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCirugiaPlastica.Infrastructure.Models;

[Table("Notificaciones")]
public sealed class Notificacion
{
    [Key]
    public int NotificacionId { get; set; }

    [Required, StringLength(50)]
    public string Tipo { get; set; } = "";

    [Required, StringLength(200)]
    public string Titulo { get; set; } = "";

    [Required, StringLength(1000)]
    public string Mensaje { get; set; } = "";

    public int? CitaId { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime FechaCreacion { get; set; }

    public bool Leida { get; set; }

    public Cita? Cita { get; set; }
}
