package com.tangerclinic.gestioncirugiaplastica.data.network

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

enum class ClinicRole(val apiValue: String, val displayLabel: String) {
    DOCTORA("Doctora", "Doctora"),
    ASISTENTE_CLINICO("AsistenteClinico", "Asistente Clínico"),
    SECRETARIA("Secretaria", "Secretaria"),
    PACIENTE("Paciente", "Paciente"),
    VISITANTE("Visitante", "Visitante Público");

    val isStaff: Boolean
        get() = this == DOCTORA || this == ASISTENTE_CLINICO || this == SECRETARIA

    val isClinical: Boolean
        get() = this == DOCTORA || this == ASISTENTE_CLINICO

    val canUpdateAppointmentStatus: Boolean
        get() = this == DOCTORA || this == SECRETARIA

    val canReadFullPatients: Boolean
        get() = this == DOCTORA || this == SECRETARIA

    companion object {
        fun fromApi(role: String?): ClinicRole? {
            if (role.isNullOrBlank()) return null
            return entries.firstOrNull { it.apiValue.equals(role.trim(), ignoreCase = true) }
        }
    }
}

data class UserSessionState(
    val accessToken: String? = null,
    val expiresAtUtc: String? = null,
    val displayName: String = "",
    val role: ClinicRole? = null,
    val isPublicVisitor: Boolean = false
) {
    val isAuthenticated: Boolean
        get() = !accessToken.isNullOrBlank() && role != null && !isPublicVisitor

    val hasEnteredApp: Boolean
        get() = isAuthenticated || isPublicVisitor
}

object SessionManager {
    private val _session = MutableStateFlow(UserSessionState())
    val session: StateFlow<UserSessionState> = _session.asStateFlow()

    val currentToken: String?
        get() = _session.value.accessToken

    val currentRole: ClinicRole?
        get() = _session.value.role

    fun startAuthenticatedSession(
        accessToken: String,
        expiresAtUtc: String?,
        displayName: String,
        role: ClinicRole
    ) {
        _session.value = UserSessionState(
            accessToken = accessToken.trim(),
            expiresAtUtc = expiresAtUtc,
            displayName = displayName.trim().ifBlank { role.displayLabel },
            role = role,
            isPublicVisitor = false
        )
    }

    fun startPublicVisitorSession() {
        _session.value = UserSessionState(
            accessToken = null,
            expiresAtUtc = null,
            displayName = "Visitante Público",
            role = ClinicRole.VISITANTE,
            isPublicVisitor = true
        )
    }

    fun clearSession() {
        _session.value = UserSessionState()
    }
}
