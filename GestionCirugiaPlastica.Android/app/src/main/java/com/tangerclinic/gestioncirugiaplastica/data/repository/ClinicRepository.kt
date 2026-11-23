package com.tangerclinic.gestioncirugiaplastica.data.repository

import com.google.gson.JsonParser
import com.tangerclinic.gestioncirugiaplastica.data.dto.CancelacionPublicaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.CitaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.ConsultaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.CrearConsultaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.LoginRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.NoticiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.NotificacionDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PacienteDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalCitaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalModificacionCitaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalPerfilDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalRecetaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalSolicitudCitaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.RecetaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.SolicitudCitaPublicaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.SolicitudCitaPublicaResponse
import com.tangerclinic.gestioncirugiaplastica.data.network.ApiClient
import com.tangerclinic.gestioncirugiaplastica.data.network.ClinicApiService
import com.tangerclinic.gestioncirugiaplastica.data.network.ClinicRole
import com.tangerclinic.gestioncirugiaplastica.data.network.SessionManager
import retrofit2.Response
import java.io.IOException

sealed interface ApiResult<out T> {
    data class Success<T>(val data: T) : ApiResult<T>
    data class Error(val message: String) : ApiResult<Nothing>
}

class ClinicRepository(
    private val api: ClinicApiService = ApiClient.service
) {

    // ── Autenticación ───────────────────────────────────────────────────────

    suspend fun login(identifier: String, password: String): ApiResult<ClinicRole> = safeCall {
        val response = api.login(LoginRequest(identifier.trim(), password))
        if (!response.isSuccessful) {
            return@safeCall ApiResult.Error(extractError(response, "No se pudo iniciar sesión."))
        }

        val body = response.body()
            ?: return@safeCall ApiResult.Error("La API no devolvió datos de autenticación.")
        if (body.accessToken.isBlank()) {
            return@safeCall ApiResult.Error("La API no devolvió un token JWT válido.")
        }

        val role = ClinicRole.fromApi(body.role)
            ?: return@safeCall ApiResult.Error("Rol no reconocido: ${body.role}.")

        SessionManager.startAuthenticatedSession(
            accessToken = body.accessToken,
            expiresAtUtc = body.expiresAtUtc,
            displayName = body.name,
            role = role
        )
        ApiResult.Success(role)
    }

    // ── Noticias (Público y autenticado) ────────────────────────────────────

    suspend fun getNoticias(): ApiResult<List<NoticiaDto>> = safeCall {
        val response = api.getNoticiasPublicadas()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudieron cargar las noticias."))
        } else {
            ApiResult.Success(response.body().orEmpty())
        }
    }

    // ── Portal del Paciente (Rol Paciente) ──────────────────────────────────

    suspend fun getPortalPerfil(): ApiResult<PortalPerfilDto> = safeCall {
        val response = api.getPortalPerfil()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo cargar tu perfil de paciente."))
        } else {
            ApiResult.Success(response.body() ?: PortalPerfilDto())
        }
    }

    suspend fun getPortalCitas(): ApiResult<List<PortalCitaDto>> = safeCall {
        val response = api.getPortalCitas()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudieron cargar tus citas."))
        } else {
            ApiResult.Success(response.body().orEmpty())
        }
    }

    suspend fun solicitarCitaPaciente(
        fechaIso: String,
        tipoCita: String,
        observaciones: String?
    ): ApiResult<Unit> = safeCall {
        val response = api.agendarPortalCita(
            PortalSolicitudCitaRequest(
                fecha = fechaIso,
                tipoCita = tipoCita.trim(),
                observaciones = observaciones?.trim()?.ifBlank { null }
            )
        )
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo solicitar la cita."))
        } else {
            ApiResult.Success(Unit)
        }
    }

    suspend fun reprogramarCitaPaciente(citaId: Int, nuevaFechaIso: String): ApiResult<Unit> = safeCall {
        val response = api.reprogramarPortalCita(
            citaId = citaId,
            request = PortalModificacionCitaRequest(fecha = nuevaFechaIso)
        )
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo reprogramar la cita."))
        } else {
            ApiResult.Success(Unit)
        }
    }

    suspend fun cancelarCitaPaciente(citaId: Int): ApiResult<String> = safeCall {
        val response = api.cancelarPortalCita(citaId)
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo cancelar la cita."))
        } else {
            val msg = response.body()?.mensaje ?: "La cita fue cancelada."
            ApiResult.Success(msg)
        }
    }

    suspend fun getPortalRecetas(): ApiResult<List<PortalRecetaDto>> = safeCall {
        val response = api.getPortalRecetas()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudieron cargar tus recetas."))
        } else {
            ApiResult.Success(response.body().orEmpty())
        }
    }

    // ── Citas Públicas (Visitante Público) ──────────────────────────────────

    suspend fun solicitarCitaPublica(
        request: SolicitudCitaPublicaRequest
    ): ApiResult<SolicitudCitaPublicaResponse> = safeCall {
        val response = api.agendarCitaPublica(request)
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo agendar la cita."))
        } else {
            ApiResult.Success(response.body() ?: SolicitudCitaPublicaResponse())
        }
    }

    suspend fun cancelarCitaPublica(
        codigoGestion: String?,
        cedula: String?,
        telefono: String?
    ): ApiResult<String> = safeCall {
        val response = api.cancelarCitaPublica(
            CancelacionPublicaRequest(
                codigoGestion = codigoGestion?.trim()?.ifBlank { null },
                cedula = cedula?.trim()?.ifBlank { null },
                telefono = telefono?.trim()?.ifBlank { null }
            )
        )
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo cancelar la cita."))
        } else {
            ApiResult.Success(response.body()?.mensaje ?: "La cita fue cancelada.")
        }
    }

    // ── Panel Clínico y Recepción ───────────────────────────────────────────

    suspend fun getCitasStaff(): ApiResult<List<CitaDto>> = safeCall {
        val response = api.getCitas()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudieron cargar las citas."))
        } else {
            ApiResult.Success(response.body()?.value.orEmpty())
        }
    }

    suspend fun actualizarEstadoCita(citaId: Int, estadoApi: String): ApiResult<Unit> = safeCall {
        val response = api.actualizarEstadoCita(citaId, estadoApi)
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo actualizar el estado de la cita."))
        } else {
            ApiResult.Success(Unit)
        }
    }

    suspend fun getPacientesForRole(role: ClinicRole): ApiResult<List<PacienteDto>> = safeCall {
        val response = if (role.canReadFullPatients) {
            api.getPacientes()
        } else {
            api.getPacientesSelector()
        }
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudieron cargar los pacientes."))
        } else {
            ApiResult.Success(response.body()?.value.orEmpty())
        }
    }

    suspend fun getConsultas(): ApiResult<List<ConsultaDto>> = safeCall {
        val response = api.getConsultas()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudieron cargar las consultas."))
        } else {
            ApiResult.Success(response.body()?.value.orEmpty())
        }
    }

    suspend fun guardarConsulta(request: CrearConsultaRequest): ApiResult<Unit> = safeCall {
        val response = api.guardarConsulta(request)
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo registrar la consulta."))
        } else {
            ApiResult.Success(Unit)
        }
    }

    suspend fun getRecetas(): ApiResult<List<RecetaDto>> = safeCall {
        val response = api.getRecetas()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudieron cargar las recetas."))
        } else {
            ApiResult.Success(response.body()?.value.orEmpty())
        }
    }

    // ── Notificaciones (Rol Doctora) ────────────────────────────────────────

    suspend fun getNotificaciones(): ApiResult<List<NotificacionDto>> = safeCall {
        val response = api.getNotificaciones()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudieron cargar las notificaciones."))
        } else {
            ApiResult.Success(response.body().orEmpty())
        }
    }

    suspend fun getNotificacionesNoLeidasCount(): ApiResult<Int> = safeCall {
        val response = api.getNotificacionesNoLeidasCount()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo consultar el conteo de notificaciones."))
        } else {
            ApiResult.Success(response.body()?.count ?: 0)
        }
    }

    suspend fun marcarNotificacionLeida(id: Int): ApiResult<Unit> = safeCall {
        val response = api.marcarNotificacionLeida(id)
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudo marcar la notificación como leída."))
        } else {
            ApiResult.Success(Unit)
        }
    }

    suspend fun marcarTodasNotificacionesLeidas(): ApiResult<Unit> = safeCall {
        val response = api.marcarTodasNotificacionesLeidas()
        if (!response.isSuccessful) {
            ApiResult.Error(extractError(response, "No se pudieron marcar todas como leídas."))
        } else {
            ApiResult.Success(Unit)
        }
    }

    // ── Helpers de red y manejo de errores ──────────────────────────────────

    private suspend fun <T> safeCall(block: suspend () -> ApiResult<T>): ApiResult<T> {
        return try {
            block()
        } catch (_: IOException) {
            ApiResult.Error(
                "No se pudo conectar con la API (${ApiClient.activeBaseUrl}). " +
                    "Verifica que GestionCirugiaPlastica.Api esté ejecutándose en tu equipo."
            )
        } catch (ex: Exception) {
            ApiResult.Error(ex.message ?: "Ocurrió un error inesperado.")
        }
    }

    private fun extractError(response: Response<*>, fallback: String): String {
        val raw = try {
            response.errorBody()?.string().orEmpty().trim()
        } catch (_: Exception) {
            ""
        }

        if (raw.isBlank()) {
            return when (response.code()) {
                401 -> "Sesión vencida o credenciales inválidas."
                403 -> "Tu rol no tiene permiso para esta operación."
                404 -> "No se encontró el recurso solicitado."
                else -> "$fallback (HTTP ${response.code()})."
            }
        }

        return try {
            val element = JsonParser.parseString(raw)
            when {
                element.isJsonPrimitive -> element.asString.ifBlank { fallback }
                element.isJsonObject -> {
                    val obj = element.asJsonObject
                    val direct = listOf("message", "mensaje", "detail", "title")
                        .firstNotNullOfOrNull { key ->
                            obj.get(key)?.takeIf { it.isJsonPrimitive }?.asString?.takeIf { it.isNotBlank() }
                        }
                    if (direct != null) return direct

                    val errObj = obj.get("error")
                    if (errObj != null && errObj.isJsonObject) {
                        val desc = listOf("description", "message", "code")
                            .firstNotNullOfOrNull { key ->
                                errObj.asJsonObject.get(key)?.takeIf { it.isJsonPrimitive }?.asString?.takeIf { it.isNotBlank() }
                            }
                        if (desc != null) return desc
                    }

                    val errorsObj = obj.get("errors")
                    if (errorsObj != null && errorsObj.isJsonObject) {
                        val msgs = errorsObj.asJsonObject.entrySet().flatMap { (_, v) ->
                            if (v.isJsonArray) v.asJsonArray.mapNotNull { item ->
                                item.takeIf { it.isJsonPrimitive }?.asString
                            } else emptyList()
                        }
                        if (msgs.isNotEmpty()) return msgs.joinToString(" ")
                    }
                    fallback
                }
                else -> fallback
            }
        } catch (_: Exception) {
            raw.trim('"').take(260).ifBlank { fallback }
        }
    }
}
