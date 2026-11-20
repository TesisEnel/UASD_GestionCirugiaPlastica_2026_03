using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCirugiaPlastica.Infrastructure.Models;

[Table("DocumentosClinicos")]
public class DocumentoClinico
{
    [Key]
    public int DocumentoClinicoId { get; set; }
    public int PacienteId { get; set; }
    public int? CirugiaId { get; set; }
    public int? ConsultaId { get; set; }
    [StringLength(40)] public string Categoria { get; set; } = "Otro";
    [StringLength(250)] public string NombreArchivo { get; set; } = null!;
    [StringLength(120)] public string ContentType { get; set; } = null!;
    [StringLength(500)] public string RutaArchivo { get; set; } = null!;
    [StringLength(500)] public string? Descripcion { get; set; }
    public DateTime FechaCarga { get; set; }
    public int UsuarioId { get; set; }
}
