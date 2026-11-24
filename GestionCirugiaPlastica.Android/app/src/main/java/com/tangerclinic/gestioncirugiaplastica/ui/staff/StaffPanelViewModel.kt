package com.tangerclinic.gestioncirugiaplastica.ui.staff

import android.content.Context
import android.net.Uri
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tangerclinic.gestioncirugiaplastica.data.dto.CirugiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.CitaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.ConsultaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.CrearConsultaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.FotografiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.NotificacionDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PacienteDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.RecetaDto
import com.tangerclinic.gestioncirugiaplastica.data.network.ClinicRole
import com.tangerclinic.gestioncirugiaplastica.data.network.SessionManager
import com.tangerclinic.gestioncirugiaplastica.data.repository.ApiResult
import com.tangerclinic.gestioncirugiaplastica.data.repository.ClinicRepository
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

data class StaffPanelUiState(
    val role: ClinicRole = ClinicRole.DOCTORA,
    val isLoading: Boolean = true,
    val isUpdating: Boolean = false,
    val isUploadingPhoto: Boolean = false,
    val citas: List<CitaDto> = emptyList(),
    val pacientes: List<PacienteDto> = emptyList(),
    val consultas: List<ConsultaDto> = emptyList(),
    val recetas: List<RecetaDto> = emptyList(),
    val cirugias: List<CirugiaDto> = emptyList(),
    val fotografias: List<FotografiaDto> = emptyList(),
    val photoThumbnails: Map<Int, ByteArray> = emptyMap(),
    val notificaciones: List<NotificacionDto> = emptyList(),
    val unreadNotificationsCount: Int = 0,
    val isLoadingNotifications: Boolean = false,
    val updatingCitaId: Int? = null,
    val lastUpdatedCitaId: Int? = null,
    val photoModalError: String? = null,
    val errorMessage: String? = null,
    val successMessage: String? = null
) {
    fun patientNameById(pacienteId: Int): String {
        val found = pacientes.firstOrNull { it.pacienteId == pacienteId }
        return found?.nombreCompleto ?: "Paciente #$pacienteId"
    }

    fun patientCedulaById(pacienteId: Int): String? =
        pacientes.firstOrNull { it.pacienteId == pacienteId }?.cedula

    fun cirugiaById(cirugiaId: Int): CirugiaDto? =
        cirugias.firstOrNull { it.cirugiaId == cirugiaId }

    fun patientNameForCirugia(cirugiaId: Int): String {
        val cirugia = cirugiaById(cirugiaId) ?: return "Expediente · Cirugía #$cirugiaId"
        return cirugia.paciente?.nombreCompleto ?: patientNameById(cirugia.pacienteId)
    }
}

class StaffPanelViewModel(
    private val repository: ClinicRepository = ClinicRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(StaffPanelUiState())
    val uiState: StateFlow<StaffPanelUiState> = _uiState.asStateFlow()
    private val loadingThumbnailIds = mutableSetOf<Int>()

    fun loadAll(role: ClinicRole) {
        viewModelScope.launch {
            _uiState.update { it.copy(role = role, isLoading = true, errorMessage = null) }

            val citasDef = async { repository.getCitasStaff() }
            val pacientesDef = async { repository.getPacientesForRole(role) }
            val consultasDef = if (role.isClinical) async { repository.getConsultas() } else null
            val recetasDef = if (role.isClinical) async { repository.getRecetas() } else null
            val cirugiasDef = if (role.isClinical) async { repository.getCirugias() } else null
            val fotosDef = if (role.isClinical) async { repository.getFotografias() } else null
            val unreadDef = if (role == ClinicRole.DOCTORA) async { repository.getNotificacionesNoLeidasCount() } else null

            val citasRes = citasDef.await()
            val pacientesRes = pacientesDef.await()
            val consultasRes = consultasDef?.await()
            val recetasRes = recetasDef?.await()
            val cirugiasRes = cirugiasDef?.await()
            val fotosRes = fotosDef?.await()
            val unreadRes = unreadDef?.await()

            val firstError = listOfNotNull(citasRes, pacientesRes, consultasRes, recetasRes, cirugiasRes, fotosRes)
                .filterIsInstance<ApiResult.Error>()
                .firstOrNull()?.message

            _uiState.update { state ->
                state.copy(
                    isLoading = false,
                    citas = (citasRes as? ApiResult.Success)?.data ?: state.citas,
                    pacientes = (pacientesRes as? ApiResult.Success)?.data ?: state.pacientes,
                    consultas = (consultasRes as? ApiResult.Success)?.data ?: state.consultas,
                    recetas = (recetasRes as? ApiResult.Success)?.data ?: state.recetas,
                    cirugias = (cirugiasRes as? ApiResult.Success)?.data ?: state.cirugias,
                    fotografias = (fotosRes as? ApiResult.Success)?.data ?: state.fotografias,
                    unreadNotificationsCount = (unreadRes as? ApiResult.Success)?.data ?: state.unreadNotificationsCount,
                    errorMessage = firstError
                )
            }
        }
    }

    fun clearFeedback() {
        _uiState.update { it.copy(errorMessage = null, successMessage = null) }
    }

    fun clearPhotoModalError() {
        _uiState.update { it.copy(photoModalError = null) }
    }

    fun subirFotografiaClinica(
        context: Context,
        cirugiaId: Int,
        imageUri: Uri,
        tipo: TipoFotografiaClinica,
        rawDescripcion: String,
        onSuccess: () -> Unit
    ) {
        if (!_uiState.value.role.isClinical) {
            _uiState.update { it.copy(photoModalError = "Tu rol no tiene permiso para subir fotografías clínicas.") }
            return
        }
        if (cirugiaId <= 0) {
            _uiState.update { it.copy(photoModalError = "Selecciona una cirugía válida.") }
            return
        }

        val appContext = context.applicationContext
        viewModelScope.launch {
            _uiState.update {
                it.copy(
                    isUploadingPhoto = true,
                    photoModalError = null,
                    errorMessage = null,
                    successMessage = null
                )
            }

            val preparedResult = withContext(Dispatchers.IO) {
                ClinicalPhotoHelper.prepareForUpload(appContext, imageUri)
            }

            val prepared = preparedResult.getOrElse { ex ->
                _uiState.update {
                    it.copy(
                        isUploadingPhoto = false,
                        photoModalError = ex.message ?: "No se pudo procesar la imagen seleccionada."
                    )
                }
                return@launch
            }

            val formattedDescription = ClinicalPhotoHelper.formatClinicalDescription(tipo, rawDescripcion)

            when (
                val res = repository.subirFotografiaClinica(
                    cirugiaId = cirugiaId,
                    imageBytes = prepared.bytes,
                    fileName = prepared.fileName,
                    mimeType = prepared.mimeType,
                    descripcion = formattedDescription
                )
            ) {
                is ApiResult.Success -> {
                    val saved = res.data
                    val sizeText = ClinicalPhotoHelper.formatSizeLabel(prepared.bytes.size)
                    _uiState.update { state ->
                        val updatedThumbnails = if (saved.fotografiaId > 0) {
                            state.photoThumbnails + (saved.fotografiaId to prepared.bytes)
                        } else {
                            state.photoThumbnails
                        }
                        val updatedList = if (saved.fotografiaId > 0) {
                            listOf(saved) + state.fotografias.filterNot { it.fotografiaId == saved.fotografiaId }
                        } else {
                            state.fotografias
                        }
                        state.copy(
                            isUploadingPhoto = false,
                            photoModalError = null,
                            fotografias = updatedList,
                            photoThumbnails = updatedThumbnails,
                            successMessage = "Fotografía ${tipo.label.lowercase(Locale.ROOT)} guardada en el expediente ($sizeText)."
                        )
                    }
                    onSuccess()
                    refreshFotografias()
                }
                is ApiResult.Error -> {
                    _uiState.update {
                        it.copy(
                            isUploadingPhoto = false,
                            photoModalError = res.message
                        )
                    }
                }
            }
        }
    }

    fun loadPhotoThumbnail(fotografiaId: Int) {
        if (fotografiaId <= 0) return
        if (_uiState.value.photoThumbnails.containsKey(fotografiaId)) return
        if (!loadingThumbnailIds.add(fotografiaId)) return

        viewModelScope.launch {
            try {
                when (val res = repository.descargarFotografiaBytes(fotografiaId)) {
                    is ApiResult.Success -> {
                        _uiState.update { state ->
                            state.copy(photoThumbnails = state.photoThumbnails + (fotografiaId to res.data))
                        }
                    }
                    is ApiResult.Error -> Unit
                }
            } finally {
                loadingThumbnailIds.remove(fotografiaId)
            }
        }
    }

    private suspend fun refreshFotografias() {
        when (val res = repository.getFotografias()) {
            is ApiResult.Success -> _uiState.update { it.copy(fotografias = res.data) }
            is ApiResult.Error -> Unit
        }
    }

    fun cambiarEstadoCita(citaId: Int, estadoApi: String, etiquetaVisual: String) {
        viewModelScope.launch {
            _uiState.update {
                it.copy(
                    isUpdating = true,
                    updatingCitaId = citaId,
                    errorMessage = null,
                    successMessage = null
                )
            }
            when (val res = repository.actualizarEstadoCita(citaId, estadoApi)) {
                is ApiResult.Success -> {
                    val patientName = _uiState.value.citas
                        .firstOrNull { it.citaId == citaId }
                        ?.let { it.paciente?.nombreCompleto ?: _uiState.value.patientNameById(it.pacienteId) }
                        ?: "Cita #$citaId"
                    _uiState.update { state ->
                        state.copy(
                            isUpdating = false,
                            updatingCitaId = null,
                            lastUpdatedCitaId = citaId,
                            citas = state.citas.map { c ->
                                if (c.citaId == citaId) c.copy(estado = estadoApi) else c
                            },
                            successMessage = "$patientName marcada como \"$etiquetaVisual\"."
                        )
                    }
                    if (_uiState.value.role == ClinicRole.DOCTORA) {
                        refreshUnreadCount()
                    }
                }
                is ApiResult.Error -> {
                    _uiState.update {
                        it.copy(
                            isUpdating = false,
                            updatingCitaId = null,
                            errorMessage = res.message
                        )
                    }
                }
            }
        }
    }

    fun registrarConsulta(
        pacienteId: Int,
        motivo: String,
        diagnostico: String,
        observaciones: String,
        onSuccess: () -> Unit
    ) {
        if (pacienteId <= 0) {
            _uiState.update { it.copy(errorMessage = "Selecciona un paciente para registrar la consulta.") }
            return
        }
        if (motivo.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Indica el motivo de la consulta.") }
            return
        }
        if (motivo.trim().length > 250) {
            _uiState.update { it.copy(errorMessage = "El motivo no puede superar los 250 caracteres.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isUpdating = true, errorMessage = null, successMessage = null) }
            val isoNow = LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss", Locale.US))
            val req = CrearConsultaRequest(
                consultaId = 0,
                fecha = isoNow,
                pacienteId = pacienteId,
                citaId = null,
                motivo = motivo.trim(),
                diagnostico = diagnostico.trim().ifBlank { null },
                observaciones = observaciones.trim().ifBlank { null }
            )
            when (val res = repository.guardarConsulta(req)) {
                is ApiResult.Success -> {
                    _uiState.update {
                        it.copy(isUpdating = false, successMessage = "Consulta médica registrada correctamente.")
                    }
                    onSuccess()
                    loadAll(_uiState.value.role)
                }
                is ApiResult.Error -> {
                    _uiState.update { it.copy(isUpdating = false, errorMessage = res.message) }
                }
            }
        }
    }

    fun loadNotifications() {
        if (_uiState.value.role != ClinicRole.DOCTORA) return
        viewModelScope.launch {
            _uiState.update { it.copy(isLoadingNotifications = true) }
            when (val res = repository.getNotificaciones()) {
                is ApiResult.Success -> {
                    _uiState.update { state ->
                        state.copy(
                            isLoadingNotifications = false,
                            notificaciones = res.data,
                            unreadNotificationsCount = res.data.count { !it.leida }
                        )
                    }
                }
                is ApiResult.Error -> {
                    _uiState.update { it.copy(isLoadingNotifications = false, errorMessage = res.message) }
                }
            }
        }
    }

    fun marcarNotificacionLeida(notificacionId: Int) {
        viewModelScope.launch {
            when (repository.marcarNotificacionLeida(notificacionId)) {
                is ApiResult.Success -> {
                    _uiState.update { state ->
                        val updated = state.notificaciones.map { n ->
                            if (n.notificacionId == notificacionId) n.copy(leida = true) else n
                        }
                        state.copy(
                            notificaciones = updated,
                            unreadNotificationsCount = updated.count { !it.leida }
                        )
                    }
                }
                is ApiResult.Error -> Unit
            }
        }
    }

    fun marcarTodasNotificacionesLeidas() {
        viewModelScope.launch {
            when (repository.marcarTodasNotificacionesLeidas()) {
                is ApiResult.Success -> {
                    _uiState.update { state ->
                        state.copy(
                            notificaciones = state.notificaciones.map { it.copy(leida = true) },
                            unreadNotificationsCount = 0
                        )
                    }
                }
                is ApiResult.Error -> Unit
            }
        }
    }

    private suspend fun refreshUnreadCount() {
        when (val res = repository.getNotificacionesNoLeidasCount()) {
            is ApiResult.Success -> _uiState.update { it.copy(unreadNotificationsCount = res.data) }
            is ApiResult.Error -> Unit
        }
    }

    fun logout() {
        SessionManager.clearSession()
    }
}
