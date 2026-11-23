package com.tangerclinic.gestioncirugiaplastica.ui.staff

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tangerclinic.gestioncirugiaplastica.data.dto.CitaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.ConsultaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.CrearConsultaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.NotificacionDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PacienteDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.RecetaDto
import com.tangerclinic.gestioncirugiaplastica.data.network.ClinicRole
import com.tangerclinic.gestioncirugiaplastica.data.network.SessionManager
import com.tangerclinic.gestioncirugiaplastica.data.repository.ApiResult
import com.tangerclinic.gestioncirugiaplastica.data.repository.ClinicRepository
import kotlinx.coroutines.async
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

data class StaffPanelUiState(
    val role: ClinicRole = ClinicRole.DOCTORA,
    val isLoading: Boolean = true,
    val isUpdating: Boolean = false,
    val citas: List<CitaDto> = emptyList(),
    val pacientes: List<PacienteDto> = emptyList(),
    val consultas: List<ConsultaDto> = emptyList(),
    val recetas: List<RecetaDto> = emptyList(),
    val notificaciones: List<NotificacionDto> = emptyList(),
    val unreadNotificationsCount: Int = 0,
    val isLoadingNotifications: Boolean = false,
    val errorMessage: String? = null,
    val successMessage: String? = null
) {
    fun patientNameById(pacienteId: Int): String {
        val found = pacientes.firstOrNull { it.pacienteId == pacienteId }
        return found?.nombreCompleto ?: "Paciente #$pacienteId"
    }

    fun patientCedulaById(pacienteId: Int): String? =
        pacientes.firstOrNull { it.pacienteId == pacienteId }?.cedula
}

class StaffPanelViewModel(
    private val repository: ClinicRepository = ClinicRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(StaffPanelUiState())
    val uiState: StateFlow<StaffPanelUiState> = _uiState.asStateFlow()

    fun loadAll(role: ClinicRole) {
        viewModelScope.launch {
            _uiState.update { it.copy(role = role, isLoading = true, errorMessage = null) }

            val citasDef = async { repository.getCitasStaff() }
            val pacientesDef = async { repository.getPacientesForRole(role) }
            val consultasDef = if (role.isClinical) async { repository.getConsultas() } else null
            val recetasDef = if (role.isClinical) async { repository.getRecetas() } else null
            val unreadDef = if (role == ClinicRole.DOCTORA) async { repository.getNotificacionesNoLeidasCount() } else null

            val citasRes = citasDef.await()
            val pacientesRes = pacientesDef.await()
            val consultasRes = consultasDef?.await()
            val recetasRes = recetasDef?.await()
            val unreadRes = unreadDef?.await()

            val firstError = listOfNotNull(citasRes, pacientesRes, consultasRes, recetasRes)
                .filterIsInstance<ApiResult.Error>()
                .firstOrNull()?.message

            _uiState.update { state ->
                state.copy(
                    isLoading = false,
                    citas = (citasRes as? ApiResult.Success)?.data ?: state.citas,
                    pacientes = (pacientesRes as? ApiResult.Success)?.data ?: state.pacientes,
                    consultas = (consultasRes as? ApiResult.Success)?.data ?: state.consultas,
                    recetas = (recetasRes as? ApiResult.Success)?.data ?: state.recetas,
                    unreadNotificationsCount = (unreadRes as? ApiResult.Success)?.data ?: state.unreadNotificationsCount,
                    errorMessage = firstError
                )
            }
        }
    }

    fun clearFeedback() {
        _uiState.update { it.copy(errorMessage = null, successMessage = null) }
    }

    fun cambiarEstadoCita(citaId: Int, estadoApi: String, etiquetaVisual: String) {
        viewModelScope.launch {
            _uiState.update { it.copy(isUpdating = true, errorMessage = null, successMessage = null) }
            when (val res = repository.actualizarEstadoCita(citaId, estadoApi)) {
                is ApiResult.Success -> {
                    _uiState.update { state ->
                        state.copy(
                            isUpdating = false,
                            citas = state.citas.map { c ->
                                if (c.citaId == citaId) c.copy(estado = estadoApi) else c
                            },
                            successMessage = "Cita #$citaId marcada como \"$etiquetaVisual\"."
                        )
                    }
                    if (_uiState.value.role == ClinicRole.DOCTORA) {
                        refreshUnreadCount()
                    }
                }
                is ApiResult.Error -> {
                    _uiState.update { it.copy(isUpdating = false, errorMessage = res.message) }
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
