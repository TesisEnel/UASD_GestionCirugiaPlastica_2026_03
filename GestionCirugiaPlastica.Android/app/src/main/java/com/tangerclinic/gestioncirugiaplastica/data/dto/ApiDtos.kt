package com.tangerclinic.gestioncirugiaplastica.data.dto

import com.google.gson.annotations.SerializedName

// ── Contenedor estándar Aplicada1.Core.Result<T> ────────────────────────────

data class ResultEnvelope<T>(
    @SerializedName("isSuccess") val isSuccess: Boolean? = null,
    @SerializedName("isFailure") val isFailure: Boolean? = null,
    @SerializedName("value") val value: T? = null,
    @SerializedName("error") val error: ApiErrorDetail? = null
)

data class ApiErrorDetail(
    @SerializedName("code") val code: String? = null,
    @SerializedName("description") val description: String? = null,
    @SerializedName("message") val message: String? = null
)

data class MensajeResponse(
    @SerializedName("mensaje") val mensaje: String? = null,
    @SerializedName("message") val message: String? = null
)

// ── Auth (AuthController) ───────────────────────────────────────────────────

data class LoginRequest(
    @SerializedName("identifier") val identifier: String,
    @SerializedName("password") val password: String
)

data class LoginResponse(
    @SerializedName("accessToken") val accessToken: String = "",
    @SerializedName("expiresAtUtc") val expiresAtUtc: String? = null,
    @SerializedName("name") val name: String = "",
    @SerializedName("role") val role: String = ""
)

// ── Pacientes (PacientesController / ExpedienteController) ──────────────────

data class PacienteDto(
    @SerializedName("pacienteId") val pacienteId: Int = 0,
    @SerializedName("fechaRegistro") val fechaRegistro: String? = null,
    @SerializedName("nombre") val nombre: String = "",
    @SerializedName("apellido") val apellido: String = "",
    @SerializedName("cedula") val cedula: String? = null,
    @SerializedName("fechaNacimiento") val fechaNacimiento: String? = null,
    @SerializedName("tieneSeguro") val tieneSeguro: Boolean? = null,
    @SerializedName("sexo") val sexo: String? = null,
    @SerializedName("telefono") val telefono: String? = null,
    @SerializedName("email") val email: String? = null,
    @SerializedName("direccion") val direccion: String? = null
) {
    val nombreCompleto: String
        get() = "$nombre $apellido".trim().ifBlank { "Paciente #$pacienteId" }
}

// ── Citas (CitasController) ─────────────────────────────────────────────────

data class CitaDto(
    @SerializedName("citaId") val citaId: Int = 0,
    @SerializedName("fecha") val fecha: String = "",
    @SerializedName("pacienteId") val pacienteId: Int = 0,
    @SerializedName("tipoCita") val tipoCita: String? = null,
    @SerializedName("costo") val costo: Double = 0.0,
    @SerializedName("estado") val estado: String = "Pendiente",
    @SerializedName("estadoPago") val estadoPago: String? = null,
    @SerializedName("observaciones") val observaciones: String? = null,
    @SerializedName("citaAnteriorId") val citaAnteriorId: Int? = null,
    @SerializedName("paciente") val paciente: PacienteDto? = null
)

data class SolicitudCitaPublicaRequest(
    @SerializedName("nombre") val nombre: String,
    @SerializedName("apellido") val apellido: String,
    @SerializedName("cedula") val cedula: String? = null,
    @SerializedName("telefono") val telefono: String? = null,
    @SerializedName("email") val email: String? = null,
    @SerializedName("fecha") val fecha: String,
    @SerializedName("tipoCita") val tipoCita: String,
    @SerializedName("observaciones") val observaciones: String? = null
)

data class SolicitudCitaPublicaResponse(
    @SerializedName("citaId") val citaId: Int = 0,
    @SerializedName("fecha") val fecha: String = "",
    @SerializedName("estado") val estado: String = "Pendiente",
    @SerializedName("codigoGestion") val codigoGestion: String = ""
)

data class CancelacionPublicaRequest(
    @SerializedName("codigoGestion") val codigoGestion: String? = null,
    @SerializedName("cedula") val cedula: String? = null,
    @SerializedName("telefono") val telefono: String? = null
)

// ── Portal del Paciente (PortalPacienteController) ──────────────────────────

data class PortalPerfilDto(
    @SerializedName("vinculado") val vinculado: Boolean = false,
    @SerializedName("pacienteId") val pacienteId: Int? = null,
    @SerializedName("nombre") val nombre: String? = null,
    @SerializedName("apellido") val apellido: String? = null,
    @SerializedName("cedula") val cedula: String? = null,
    @SerializedName("telefono") val telefono: String? = null,
    @SerializedName("email") val email: String? = null,
    @SerializedName("tieneSeguro") val tieneSeguro: Boolean = false
)

data class PortalCitaDto(
    @SerializedName("citaId") val citaId: Int = 0,
    @SerializedName("fecha") val fecha: String = "",
    @SerializedName("tipoCita") val tipoCita: String? = null,
    @SerializedName("estado") val estado: String = "Pendiente",
    @SerializedName("observaciones") val observaciones: String? = null
)

data class PortalSolicitudCitaRequest(
    @SerializedName("fecha") val fecha: String,
    @SerializedName("tipoCita") val tipoCita: String,
    @SerializedName("observaciones") val observaciones: String? = null
)

data class PortalModificacionCitaRequest(
    @SerializedName("fecha") val fecha: String
)

data class PortalCitaActionResponse(
    @SerializedName("citaId") val citaId: Int = 0,
    @SerializedName("fecha") val fecha: String = "",
    @SerializedName("estado") val estado: String = ""
)

data class PortalRecetaDto(
    @SerializedName("recetaId") val recetaId: Int = 0,
    @SerializedName("fecha") val fecha: String = "",
    @SerializedName("indicaciones") val indicaciones: String? = null,
    @SerializedName("medicamentos") val medicamentos: List<PortalMedicamentoDto> = emptyList()
)

data class PortalMedicamentoDto(
    @SerializedName("medicamento") val medicamento: String = "",
    @SerializedName("dosis") val dosis: String? = null,
    @SerializedName("frecuencia") val frecuencia: String? = null,
    @SerializedName("duracion") val duracion: String? = null,
    @SerializedName("instrucciones") val instrucciones: String? = null
)

// ── Consultas (ConsultasController) ─────────────────────────────────────────

data class ConsultaDto(
    @SerializedName("consultaId") val consultaId: Int = 0,
    @SerializedName("fecha") val fecha: String = "",
    @SerializedName("pacienteId") val pacienteId: Int = 0,
    @SerializedName("citaId") val citaId: Int? = null,
    @SerializedName("motivo") val motivo: String? = null,
    @SerializedName("diagnostico") val diagnostico: String? = null,
    @SerializedName("observaciones") val observaciones: String? = null,
    @SerializedName("paciente") val paciente: PacienteDto? = null,
    @SerializedName("cita") val cita: CitaDto? = null
)

data class CrearConsultaRequest(
    @SerializedName("consultaId") val consultaId: Int = 0,
    @SerializedName("fecha") val fecha: String,
    @SerializedName("pacienteId") val pacienteId: Int,
    @SerializedName("citaId") val citaId: Int? = null,
    @SerializedName("motivo") val motivo: String?,
    @SerializedName("diagnostico") val diagnostico: String?,
    @SerializedName("observaciones") val observaciones: String?
)

// ── Recetas (RecetasController) ─────────────────────────────────────────────

data class RecetaDto(
    @SerializedName("recetaId") val recetaId: Int = 0,
    @SerializedName("fecha") val fecha: String = "",
    @SerializedName("pacienteId") val pacienteId: Int = 0,
    @SerializedName("consultaId") val consultaId: Int? = null,
    @SerializedName("cirugiaId") val cirugiaId: Int? = null,
    @SerializedName("usuarioId") val usuarioId: Int? = null,
    @SerializedName("indicaciones") val indicaciones: String? = null,
    @SerializedName("detallesReceta") val detallesReceta: List<DetalleRecetaDto> = emptyList()
)

data class DetalleRecetaDto(
    @SerializedName("detalleRecetaId") val detalleRecetaId: Int = 0,
    @SerializedName("recetaId") val recetaId: Int = 0,
    @SerializedName("medicamento") val medicamento: String = "",
    @SerializedName("dosis") val dosis: String? = null,
    @SerializedName("frecuencia") val frecuencia: String? = null,
    @SerializedName("duracion") val duracion: String? = null,
    @SerializedName("instrucciones") val instrucciones: String? = null
)

// ── Noticias (NoticiasController) ───────────────────────────────────────────

data class NoticiaDto(
    @SerializedName("noticiaId") val noticiaId: Int = 0,
    @SerializedName("titulo") val titulo: String = "",
    @SerializedName("resumen") val resumen: String? = null,
    @SerializedName("contenido") val contenido: String = "",
    @SerializedName("fechaPublicacion") val fechaPublicacion: String = "",
    @SerializedName("publicada") val publicada: Boolean = true
)

// ── Notificaciones (NotificacionesController) ───────────────────────────────

data class NotificacionDto(
    @SerializedName("notificacionId") val notificacionId: Int = 0,
    @SerializedName("tipo") val tipo: String = "",
    @SerializedName("titulo") val titulo: String = "",
    @SerializedName("mensaje") val mensaje: String = "",
    @SerializedName("citaId") val citaId: Int? = null,
    @SerializedName("fechaCreacion") val fechaCreacion: String = "",
    @SerializedName("leida") val leida: Boolean = false
)

data class UnreadCountDto(
    @SerializedName("count") val count: Int = 0
)
