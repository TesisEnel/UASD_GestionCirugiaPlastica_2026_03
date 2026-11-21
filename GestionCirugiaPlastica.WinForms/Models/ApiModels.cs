namespace GestionCirugiaPlastica.WinForms;

internal sealed class ApiPaciente
{
    public int PacienteId { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? Cedula { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public bool TieneSeguro { get; set; }
    public string? Sexo { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public DateTime FechaRegistro { get; set; }
}
internal sealed class ApiCita
{
    public int CitaId { get; set; }
    public DateTime Fecha { get; set; }
    public int PacienteId { get; set; }
    public ApiPaciente? Paciente { get; set; }
    public string? TipoCita { get; set; }
    public decimal Costo { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string EstadoPago { get; set; } = "Pendiente";
    public string? Observaciones { get; set; }
    public int? CitaAnteriorId { get; set; }
}
internal sealed class ApiCirugia
{
    public int CirugiaId { get; set; }
    public DateTime FechaCirugia { get; set; }
    public int PacienteId { get; set; }
    public ApiPaciente? Paciente { get; set; }
    public int ProcedimientoId { get; set; }
    public ApiProcedimiento? Procedimiento { get; set; }
    public string? DiagnosticoPreoperatorio { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = "";
}
internal sealed class ApiProcedimiento { public int ProcedimientoId { get; set; } public string Nombre { get; set; } = ""; }
internal sealed class ApiSurgeryOption
{
    public int CirugiaId { get; set; }
    public int PacienteId { get; set; }
    public DateTime FechaCirugia { get; set; }
    public string Procedimiento { get; set; } = "";
    public string Paciente { get; set; } = "";
}
internal sealed class ApiEstadoCuentaPago
{
    public int? PagoId { get; set; }
    public int PacienteId { get; set; }
    public string Paciente { get; set; } = "";
    public int? CitaId { get; set; }
    public int? CirugiaId { get; set; }
    public string Destino { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Concepto { get; set; }
    public string Estado { get; set; } = "";
    public bool EsRecibido { get; set; }
}
internal sealed class ApiPago
{
    public int PagoId { get; set; }
    public DateTime Fecha { get; set; }
    public int? CirugiaId { get; set; }
    public int? CitaId { get; set; }
    public decimal Monto { get; set; }
    public decimal MontoSeguro { get; set; }
    public int? PacienteId { get; set; }
    public string? Paciente { get; set; }
    public string? Estado { get; set; }
    public string? MetodoPago { get; set; }
    public string? Concepto { get; set; }
    public ApiCirugia? Cirugia { get; set; }
    public ApiCita? Cita { get; set; }
}

internal sealed class ApiReceta
{
    public int RecetaId { get; set; }
    public DateTime Fecha { get; set; }
    public int PacienteId { get; set; }
    public int? ConsultaId { get; set; }
    public int? CirugiaId { get; set; }
    public string? Indicaciones { get; set; }
    public List<ApiDetalleReceta> DetallesReceta { get; set; } = [];
}
internal sealed class ApiDetalleReceta
{
    public int DetalleRecetaId { get; set; }
    public string Medicamento { get; set; } = "";
    public string? Dosis { get; set; }
    public string? Frecuencia { get; set; }
    public string? Duracion { get; set; }
    public string? Instrucciones { get; set; }
}
internal sealed class ApiSeguro
{
    public int SeguroId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
}
internal sealed class ApiUsuario
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? NombreUsuario { get; set; }
    public string? Email { get; set; }
    public int RolId { get; set; }
    public bool Activo { get; set; }
    public int RolSeleccionadoId { get; set; }
}
internal sealed class ApiRol { public int RolId { get; set; } public string Nombre { get; set; } = ""; }
