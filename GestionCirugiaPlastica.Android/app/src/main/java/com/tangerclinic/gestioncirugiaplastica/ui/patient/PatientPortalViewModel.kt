package com.tangerclinic.gestioncirugiaplastica.ui.patient

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tangerclinic.gestioncirugiaplastica.data.dto.NoticiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalCitaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalPerfilDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalRecetaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.SolicitudCitaPublicaRequest
import com.tangerclinic.gestioncirugiaplastica.data.dto.SolicitudCitaPublicaResponse
import com.tangerclinic.gestioncirugiaplastica.data.network.SessionManager
import com.tangerclinic.gestioncirugiaplastica.data.repository.ApiResult
import com.tangerclinic.gestioncirugiaplastica.data.repository.ClinicRepository
import com.tangerclinic.gestioncirugiaplastica.ui.components.ClinicDateUtils
import kotlinx.coroutines.async
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime

data class VisitorBookedCita(
    val citaId: Int,
    val fecha: String,
    val tipoCita: String,
    val estado: String,
    val codigoGestion: String
)

data class PatientPortalUiState(
    val isAuthenticatedPatient: Boolean = false,
    val isLoading: Boolean = true,
    val isSubmitting: Boolean = false,
    val perfil: PortalPerfilDto? = null,
    val citas: List<PortalCitaDto> = emptyList(),
    val visitorCitas: List<VisitorBookedCita> = emptyList(),
    val recetas: List<PortalRecetaDto> = emptyList(),
    val noticias: List<NoticiaDto> = emptyList(),
    val errorMessage: String? = null,
    val successMessage: String? = null
)

class PatientPortalViewModel(
    private val repository: ClinicRepository = ClinicRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(PatientPortalUiState())
    val uiState: StateFlow<PatientPortalUiState> = _uiState.asStateFlow()

    fun loadAll(isAuthenticatedPatient: Boolean) {
        viewModelScope.launch {
            _uiState.update {
                it.copy(
                    isAuthenticatedPatient = isAuthenticatedPatient,
                    isLoading = true,
                    errorMessage = null
                )
            }

            val noticiasDeferred = async { repository.getNoticias() }
            if (isAuthenticatedPatient) {
                val perfilDeferred = async { repository.getPortalPerfil() }
                val citasDeferred = async { repository.getPortalCitas() }
                val recetasDeferred = async { repository.getPortalRecetas() }

                val perfilRes = perfilDeferred.await()
                val citasRes = citasDeferred.await()
                val recetasRes = recetasDeferred.await()
                val noticiasRes = noticiasDeferred.await()

                val firstError = listOf(perfilRes, citasRes, recetasRes, noticiasRes)
                    .filterIsInstance<ApiResult.Error>()
                    .firstOrNull()?.message

                _uiState.update { state ->
                    state.copy(
                        isLoading = false,
                        perfil = (perfilRes as? ApiResult.Success)?.data ?: state.perfil,
                        citas = (citasRes as? ApiResult.Success)?.data ?: state.citas,
                        recetas = (recetasRes as? ApiResult.Success)?.data ?: state.recetas,
                        noticias = (noticiasRes as? ApiResult.Success)?.data ?: state.noticias,
                        errorMessage = firstError
                    )
                }
            } else {
                val noticiasRes = noticiasDeferred.await()
                _uiState.update { state ->
                    state.copy(
                        isLoading = false,
                        noticias = (noticiasRes as? ApiResult.Success)?.data ?: state.noticias,
                        errorMessage = (noticiasRes as? ApiResult.Error)?.message
                    )
                }
            }
        }
    }

    fun clearFeedback() {
        _uiState.update { it.copy(errorMessage = null, successMessage = null) }
    }

    fun solicitarCitaPaciente(
        date: LocalDate,
        time: LocalTime,
        tipoCita: String,
        observaciones: String,
        onSuccess: () -> Unit
    ) {
        if (LocalDateTime.of(date, time) <= LocalDateTime.now()) {
            _uiState.update { it.copy(errorMessage = "Selecciona una fecha y hora futuras.") }
            return
        }
        if (tipoCita.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Selecciona el tipo de cita.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, errorMessage = null, successMessage = null) }
            val result = repository.solicitarCitaPaciente(
                fechaIso = ClinicDateUtils.toApiIso(date, time),
                tipoCita = tipoCita,
                observaciones = observaciones
            )
            when (result) {
                is ApiResult.Success -> {
                    _uiState.update {
                        it.copy(
                            isSubmitting = false,
                            successMessage = "Tu solicitud de cita fue registrada correctamente."
                        )
                    }
                    onSuccess()
                    loadAll(isAuthenticatedPatient = true)
                }
                is ApiResult.Error -> {
                    _uiState.update { it.copy(isSubmitting = false, errorMessage = result.message) }
                }
            }
        }
    }

    fun reprogramarCitaPaciente(
        citaId: Int,
        newDate: LocalDate,
        newTime: LocalTime,
        onSuccess: () -> Unit
    ) {
        if (LocalDateTime.of(newDate, newTime) <= LocalDateTime.now()) {
            _uiState.update { it.copy(errorMessage = "La nueva fecha y hora deben ser futuras.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, errorMessage = null, successMessage = null) }
            when (val res = repository.reprogramarCitaPaciente(citaId, ClinicDateUtils.toApiIso(newDate, newTime))) {
                is ApiResult.Success -> {
                    _uiState.update {
                        it.copy(
                            isSubmitting = false,
                            successMessage = "Tu cita fue reprogramada correctamente."
                        )
                    }
                    onSuccess()
                    loadAll(isAuthenticatedPatient = true)
                }
                is ApiResult.Error -> {
                    _uiState.update { it.copy(isSubmitting = false, errorMessage = res.message) }
                }
            }
        }
    }

    fun cancelarCitaPaciente(citaId: Int) {
        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, errorMessage = null, successMessage = null) }
            when (val res = repository.cancelarCitaPaciente(citaId)) {
                is ApiResult.Success -> {
                    _uiState.update {
                        it.copy(
                            isSubmitting = false,
                            successMessage = res.data
                        )
                    }
                    loadAll(isAuthenticatedPatient = true)
                }
                is ApiResult.Error -> {
                    _uiState.update { it.copy(isSubmitting = false, errorMessage = res.message) }
                }
            }
        }
    }

    fun solicitarCitaPublica(
        nombre: String,
        apellido: String,
        cedula: String,
        telefono: String,
        emailOpcional: String,
        date: LocalDate,
        time: LocalTime,
        tipoCita: String,
        observaciones: String,
        onSuccess: (SolicitudCitaPublicaResponse) -> Unit
    ) {
        if (nombre.isBlank() || apellido.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Indica tu nombre y apellido.") }
            return
        }
        if (LocalDateTime.of(date, time) <= LocalDateTime.now()) {
            _uiState.update { it.copy(errorMessage = "Selecciona una fecha y hora futuras.") }
            return
        }
        if (tipoCita.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Indica el tipo de cita.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, errorMessage = null, successMessage = null) }
            val request = SolicitudCitaPublicaRequest(
                nombre = nombre.trim(),
                apellido = apellido.trim(),
                cedula = cedula.trim().ifBlank { null },
                telefono = telefono.trim().ifBlank { null },
                email = emailOpcional.trim().ifBlank { null },
                fecha = ClinicDateUtils.toApiIso(date, time),
                tipoCita = tipoCita.trim(),
                observaciones = observaciones.trim().ifBlank { null }
            )
            when (val res = repository.solicitarCitaPublica(request)) {
                is ApiResult.Success -> {
                    val created = res.data
                    val booked = VisitorBookedCita(
                        citaId = created.citaId,
                        fecha = created.fecha.ifBlank { request.fecha },
                        tipoCita = request.tipoCita,
                        estado = created.estado.ifBlank { "Pendiente" },
                        codigoGestion = created.codigoGestion
                    )
                    _uiState.update { state ->
                        state.copy(
                            isSubmitting = false,
                            visitorCitas = listOf(booked) + state.visitorCitas,
                            successMessage = if (created.codigoGestion.isNotBlank()) {
                                "Cita solicitada. Guarda tu código de gestión: ${created.codigoGestion}"
                            } else {
                                "Solicitud de cita registrada correctamente."
                            }
                        )
                    }
                    onSuccess(created)
                }
                is ApiResult.Error -> {
                    _uiState.update { it.copy(isSubmitting = false, errorMessage = res.message) }
                }
            }
        }
    }

    fun cancelarCitaPublica(
        codigoGestion: String,
        cedula: String,
        telefono: String,
        onSuccess: () -> Unit
    ) {
        if (codigoGestion.isBlank() && (cedula.isBlank() || telefono.isBlank())) {
            _uiState.update {
                it.copy(errorMessage = "Indica tu código de gestión o completa cédula y teléfono.")
            }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, errorMessage = null, successMessage = null) }
            when (val res = repository.cancelarCitaPublica(codigoGestion, cedula, telefono)) {
                is ApiResult.Success -> {
                    val codeClean = codigoGestion.trim()
                    _uiState.update { state ->
                        state.copy(
                            isSubmitting = false,
                            visitorCitas = state.visitorCitas.map { item ->
                                if (codeClean.isNotBlank() && item.codigoGestion.equals(codeClean, ignoreCase = true)) {
                                    item.copy(estado = "Cancelada")
                                } else {
                                    item
                                }
                            },
                            successMessage = res.data
                        )
                    }
                    onSuccess()
                }
                is ApiResult.Error -> {
                    _uiState.update { it.copy(isSubmitting = false, errorMessage = res.message) }
                }
            }
        }
    }

    fun logoutOrReturnToLogin() {
        SessionManager.clearSession()
    }
}
