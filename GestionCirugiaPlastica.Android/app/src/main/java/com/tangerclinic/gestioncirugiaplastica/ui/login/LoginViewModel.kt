package com.tangerclinic.gestioncirugiaplastica.ui.login

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.tangerclinic.gestioncirugiaplastica.data.network.SessionManager
import com.tangerclinic.gestioncirugiaplastica.data.repository.ApiResult
import com.tangerclinic.gestioncirugiaplastica.data.repository.ClinicRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class LoginUiState(
    val identifier: String = "",
    val password: String = "",
    val isLoading: Boolean = false,
    val errorMessage: String? = null
)

class LoginViewModel(
    private val repository: ClinicRepository = ClinicRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(LoginUiState())
    val uiState: StateFlow<LoginUiState> = _uiState.asStateFlow()

    fun onIdentifierChanged(value: String) {
        _uiState.update { it.copy(identifier = value, errorMessage = null) }
    }

    fun onPasswordChanged(value: String) {
        _uiState.update { it.copy(password = value, errorMessage = null) }
    }

    fun clearError() {
        _uiState.update { it.copy(errorMessage = null) }
    }

    fun login() {
        val current = _uiState.value
        if (current.identifier.isBlank() || current.password.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Ingresa tu usuario (o correo) y contraseña.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, errorMessage = null) }
            when (val result = repository.login(current.identifier, current.password)) {
                is ApiResult.Success -> {
                    _uiState.update { it.copy(isLoading = false, password = "", errorMessage = null) }
                }
                is ApiResult.Error -> {
                    _uiState.update { it.copy(isLoading = false, errorMessage = result.message) }
                }
            }
        }
    }

    fun enterAsPublicVisitor() {
        SessionManager.startPublicVisitorSession()
    }
}
