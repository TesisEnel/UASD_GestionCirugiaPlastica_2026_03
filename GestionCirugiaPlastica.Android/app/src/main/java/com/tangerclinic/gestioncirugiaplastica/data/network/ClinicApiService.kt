package com.tangerclinic.gestioncirugiaplastica.data.network

import com.tangerclinic.gestioncirugiaplastica.data.dto.CancelacionPublicaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.CirugiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.CitaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.ConsultaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.CrearConsultaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.FotografiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.LoginRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.LoginResponse
import com.tangerclinic.gestioncirugiaplastica.data.dto.MensajeResponse
import com.tangerclinic.gestioncirugiaplastica.data.dto.NoticiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.NotificacionDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PacienteDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalCitaActionResponse
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalCitaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalModificacionCitaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalPerfilDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalRecetaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalSolicitudCitaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.RecetaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.ResultEnvelope
import com.tangerclinic.gestioncirugiaplastica.data.dto.SolicitudCitaPublicaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.SolicitudCitaPublicaResponse
import com.tangerclinic.gestioncirugiaplastica.data.dto.UnreadCountDto
import okhttp3.MultipartBody
import okhttp3.RequestBody
import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Part
import retrofit2.http.Path

interface ClinicApiService {

    // ── AuthController ──────────────────────────────────────────────────────

    @POST("api/Auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    // ── CitasController ─────────────────────────────────────────────────────

    @GET("api/Citas")
    suspend fun getCitas(): Response<ResultEnvelope<List<CitaDto>>>

    @PATCH("api/Citas/{id}/estado")
    suspend fun actualizarEstadoCita(
        @Path("id") citaId: Int,
        @Body estado: String
    ): Response<ResponseBody>

    @POST("api/Citas/publica")
    suspend fun agendarCitaPublica(
        @Body request: SolicitudCitaPublicaRequest
    ): Response<SolicitudCitaPublicaResponse>

    @POST("api/Citas/publica/cancelar")
    suspend fun cancelarCitaPublica(
        @Body request: CancelacionPublicaRequest
    ): Response<MensajeResponse>

    // ── PortalPacienteController ────────────────────────────────────────────

    @GET("api/PortalPaciente/perfil")
    suspend fun getPortalPerfil(): Response<PortalPerfilDto>

    @GET("api/PortalPaciente/citas")
    suspend fun getPortalCitas(): Response<List<PortalCitaDto>>

    @POST("api/PortalPaciente/citas")
    suspend fun agendarPortalCita(
        @Body request: PortalSolicitudCitaRequest
    ): Response<PortalCitaActionResponse>

    @PUT("api/PortalPaciente/citas/{citaId}")
    suspend fun reprogramarPortalCita(
        @Path("citaId") citaId: Int,
        @Body request: PortalModificacionCitaRequest
    ): Response<PortalCitaActionResponse>

    @DELETE("api/PortalPaciente/citas/{citaId}")
    suspend fun cancelarPortalCita(
        @Path("citaId") citaId: Int
    ): Response<MensajeResponse>

    @GET("api/PortalPaciente/recetas")
    suspend fun getPortalRecetas(): Response<List<PortalRecetaDto>>

    // ── PacientesController & ExpedienteController ──────────────────────────

    @GET("api/Pacientes")
    suspend fun getPacientes(): Response<ResultEnvelope<List<PacienteDto>>>

    @GET("api/Pacientes/expediente/selector")
    suspend fun getPacientesSelector(): Response<ResultEnvelope<List<PacienteDto>>>

    // ── ConsultasController ─────────────────────────────────────────────────

    @GET("api/Consultas")
    suspend fun getConsultas(): Response<ResultEnvelope<List<ConsultaDto>>>

    @POST("api/Consultas")
    suspend fun guardarConsulta(
        @Body request: CrearConsultaRequest
    ): Response<ResponseBody>

    // ── RecetasController ───────────────────────────────────────────────────

    @GET("api/Recetas")
    suspend fun getRecetas(): Response<ResultEnvelope<List<RecetaDto>>>

    // ── NoticiasController ──────────────────────────────────────────────────

    @GET("api/Noticias")
    suspend fun getNoticiasPublicadas(): Response<List<NoticiaDto>>

    // ── NotificacionesController ────────────────────────────────────────────

    @GET("api/Notificaciones")
    suspend fun getNotificaciones(): Response<List<NotificacionDto>>

    @GET("api/Notificaciones/no-leidas/count")
    suspend fun getNotificacionesNoLeidasCount(): Response<UnreadCountDto>

    @PATCH("api/Notificaciones/{id}/leer")
    suspend fun marcarNotificacionLeida(@Path("id") notificacionId: Int): Response<Unit>

    @PATCH("api/Notificaciones/leer-todas")
    suspend fun marcarTodasNotificacionesLeidas(): Response<Unit>

    // ── CirugiasController & FotografiasController ──────────────────────────

    @GET("api/Cirugias")
    suspend fun getCirugias(): Response<ResultEnvelope<List<CirugiaDto>>>

    @GET("api/Fotografias")
    suspend fun getFotografias(): Response<ResultEnvelope<List<FotografiaDto>>>

    @GET("api/Fotografias/cirugia/{cirugiaId}")
    suspend fun getFotografiasPorCirugia(
        @Path("cirugiaId") cirugiaId: Int
    ): Response<ResultEnvelope<List<FotografiaDto>>>

    @Multipart
    @POST("api/Fotografias/cirugia/{cirugiaId}/archivo")
    suspend fun subirFotografiaCirugia(
        @Path("cirugiaId") cirugiaId: Int,
        @Part archivo: MultipartBody.Part,
        @Part("descripcion") descripcion: RequestBody
    ): Response<FotografiaDto>

    @GET("api/Fotografias/archivo/{id}")
    suspend fun descargarArchivoFotografia(
        @Path("id") fotografiaId: Int
    ): Response<ResponseBody>
}
