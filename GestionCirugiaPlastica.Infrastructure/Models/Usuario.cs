using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCirugiaPlastica.Infrastructure.Models;

public partial class Usuario
{
    [Key]
    public int UsuarioId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime FechaRegistro { get; set; }

    [StringLength(250)]
    public string Nombre { get; set; } = null!;

    [StringLength(250)]
    public string Apellido { get; set; } = null!;

    [StringLength(250)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? NombreUsuario { get; set; }

    public int? PacienteId { get; set; }

    [StringLength(250)]
    public string PasswordHash { get; set; } = null!;

    public int RolId { get; set; }

    public bool Activo { get; set; }

    [InverseProperty("Usuario")]
    public virtual ICollection<Gasto> Gastos { get; set; } = new List<Gasto>();

    [InverseProperty("Usuario")]
    public virtual ICollection<HistorialEstadosCitas> HistorialEstadosCita { get; set; } = new List<HistorialEstadosCitas>();

    [InverseProperty("Usuario")]
    public virtual ICollection<Receta> Receta { get; set; } = new List<Receta>();

    [ForeignKey("RolId")]
    [InverseProperty("Usuarios")]
    public virtual Role Rol { get; set; } = null!;
}
