using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GestionCirugiaPlastica.Infrastructure.Models;

[Index("Fecha", Name = "IX_Pagos_Fecha")]
[Index("PacienteId", Name = "IX_Pagos_PacienteId")]
public partial class Pago
{
    [Key]
    public int PagoId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Fecha { get; set; }

    public int? CirugiaId { get; set; }

    public int? CitaId { get; set; }

    public int? PacienteId { get; set; }

    public int? SeguroId { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal Monto { get; set; }

    [Column(TypeName = "decimal(12, 2)")]
    public decimal MontoSeguro { get; set; }

    [StringLength(250)]
    public string Estado { get; set; } = "Pagado";

    [StringLength(250)]
    public string? MetodoPago { get; set; }

    [StringLength(250)]
    public string? Concepto { get; set; }

    [ForeignKey("CirugiaId")]
    [InverseProperty("Pagos")]
    public virtual Cirugia? Cirugia { get; set; }

    [ForeignKey("CitaId")]
    [InverseProperty("Pagos")]
    public virtual Cita? Cita { get; set; }

    [ForeignKey("SeguroId")]
    public virtual Seguro? Seguro { get; set; }

    [ForeignKey("PacienteId")]
    [InverseProperty("Pago")]
    public virtual Paciente? Paciente { get; set; }
}
