using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCirugiaPlastica.Infrastructure.Models;

[Table("AuditoriaExpediente")]
public class AuditoriaExpediente
{
    [Key]
    public long AuditoriaExpedienteId { get; set; }
    public int PacienteId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; }
    [StringLength(80)] public string Entidad { get; set; } = null!;
    public int? RegistroId { get; set; }
    [StringLength(40)] public string Accion { get; set; } = null!;
    [StringLength(500)] public string? Detalle { get; set; }
}
