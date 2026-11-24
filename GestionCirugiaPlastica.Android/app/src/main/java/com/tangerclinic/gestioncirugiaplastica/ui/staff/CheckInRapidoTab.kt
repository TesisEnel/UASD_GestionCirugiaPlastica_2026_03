package com.tangerclinic.gestioncirugiaplastica.ui.staff

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.EventAvailable
import androidx.compose.material.icons.filled.HowToReg
import androidx.compose.material.icons.filled.MedicalServices
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.tangerclinic.gestioncirugiaplastica.data.dto.CitaDto
import com.tangerclinic.gestioncirugiaplastica.ui.components.AppointmentStatusBadge
import com.tangerclinic.gestioncirugiaplastica.ui.components.ClinicDateUtils
import com.tangerclinic.gestioncirugiaplastica.ui.components.EmptyStateCard
import com.tangerclinic.gestioncirugiaplastica.ui.components.InsuranceBadge
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusGreenBg
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusGreenText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBlush
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBorder
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerMuted
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerSurfaceTint
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWhite
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWine
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWineDark
import java.util.Locale

private enum class CheckInStatusFilter(val label: String) {
    TODAS("Todas"),
    POR_LLEGAR("Por llegar"),
    EN_SALA("En sala"),
    ATENDIDAS("Atendidas")
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun StaffCheckInTab(
    state: StaffPanelUiState,
    canChangeStatus: Boolean,
    onUpdateStatus: (citaId: Int, estadoApi: String, etiquetaVisual: String) -> Unit
) {
    var searchQuery by remember { mutableStateOf("") }
    var statusFilter by remember { mutableStateOf(CheckInStatusFilter.TODAS) }

    val todaysAppointments = remember(state.citas) {
        state.citas
            .filter { ClinicDateUtils.isToday(it.fecha) }
            .sortedBy { ClinicDateUtils.parseApiDateTime(it.fecha) }
    }

    val filteredAppointments = remember(todaysAppointments, state.pacientes, searchQuery, statusFilter) {
        todaysAppointments
            .filter { cita ->
                val normalized = cita.estado.trim().lowercase(Locale.ROOT)
                when (statusFilter) {
                    CheckInStatusFilter.TODAS -> true
                    CheckInStatusFilter.POR_LLEGAR -> normalized == "pendiente" || normalized == "confirmada"
                    CheckInStatusFilter.EN_SALA -> normalized == "llegó" || normalized == "llego"
                    CheckInStatusFilter.ATENDIDAS -> normalized == "atendida" || normalized == "completada"
                }
            }
            .filter { cita ->
                if (searchQuery.isBlank()) true
                else {
                    val term = searchQuery.trim()
                    val patientName = cita.paciente?.nombreCompleto ?: state.patientNameById(cita.pacienteId)
                    val cedula = cita.paciente?.cedula ?: state.patientCedulaById(cita.pacienteId).orEmpty()
                    patientName.contains(term, ignoreCase = true) ||
                        cedula.contains(term, ignoreCase = true)
                }
            }
    }

    val waitingCount = remember(todaysAppointments) {
        todaysAppointments.count {
            val s = it.estado.trim().lowercase(Locale.ROOT)
            s == "llegó" || s == "llego"
        }
    }
    val pendingCount = remember(todaysAppointments) {
        todaysAppointments.count {
            val s = it.estado.trim().lowercase(Locale.ROOT)
            s == "pendiente" || s == "confirmada"
        }
    }
    val attendedCount = remember(todaysAppointments) {
        todaysAppointments.count {
            val s = it.estado.trim().lowercase(Locale.ROOT)
            s == "atendida" || s == "completada"
        }
    }

    LazyColumn(
        contentPadding = PaddingValues(top = 12.dp, bottom = 28.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text(
                    text = "Check-in Rápido · Recepción",
                    style = MaterialTheme.typography.titleLarge,
                    color = TangerWineDark
                )
                Text(
                    text = if (canChangeStatus) {
                        "Busca citas de hoy por nombre o cédula y registra la llegada del paciente en un toque."
                    } else {
                        "Consulta rápida en tiempo real de las citas programadas para hoy."
                    },
                    style = MaterialTheme.typography.bodyMedium,
                    color = TangerMuted
                )

                // Buscador instantáneo de citas del día
                OutlinedTextField(
                    value = searchQuery,
                    onValueChange = { searchQuery = it },
                    placeholder = { Text("Buscar cita de hoy por nombre o cédula...") },
                    leadingIcon = { Icon(Icons.Default.Search, contentDescription = null, tint = TangerWine) },
                    trailingIcon = {
                        if (searchQuery.isNotEmpty()) {
                            IconButton(onClick = { searchQuery = "" }) {
                                Icon(Icons.Default.Close, contentDescription = "Limpiar búsqueda", tint = TangerMuted)
                            }
                        }
                    },
                    singleLine = true,
                    shape = RoundedCornerShape(14.dp),
                    modifier = Modifier.fillMaxWidth()
                )

                FlowRow(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    verticalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    FilterChip(
                        selected = statusFilter == CheckInStatusFilter.TODAS,
                        onClick = { statusFilter = CheckInStatusFilter.TODAS },
                        label = { Text("Hoy (${todaysAppointments.size})") },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = TangerWine,
                            selectedLabelColor = TangerWhite
                        )
                    )
                    FilterChip(
                        selected = statusFilter == CheckInStatusFilter.POR_LLEGAR,
                        onClick = { statusFilter = CheckInStatusFilter.POR_LLEGAR },
                        label = { Text("Por llegar ($pendingCount)") },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = TangerWine,
                            selectedLabelColor = TangerWhite
                        )
                    )
                    FilterChip(
                        selected = statusFilter == CheckInStatusFilter.EN_SALA,
                        onClick = { statusFilter = CheckInStatusFilter.EN_SALA },
                        label = { Text("En sala ($waitingCount)") },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = TangerWine,
                            selectedLabelColor = TangerWhite
                        )
                    )
                    FilterChip(
                        selected = statusFilter == CheckInStatusFilter.ATENDIDAS,
                        onClick = { statusFilter = CheckInStatusFilter.ATENDIDAS },
                        label = { Text("Atendidas ($attendedCount)") },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = TangerWine,
                            selectedLabelColor = TangerWhite
                        )
                    )
                }
            }
        }

        if (filteredAppointments.isEmpty()) {
            item {
                EmptyStateCard(
                    icon = Icons.Default.HowToReg,
                    title = if (todaysAppointments.isEmpty()) "Sin citas programadas para hoy" else "Sin coincidencias",
                    subtitle = if (todaysAppointments.isEmpty()) {
                        "No hay citas registradas en la fecha de hoy."
                    } else {
                        "No se encontraron citas de hoy con ese nombre, cédula o filtro."
                    }
                )
            }
        } else {
            items(filteredAppointments, key = { it.citaId }) { cita ->
                val pacienteDetalle = cita.paciente ?: state.pacientes.firstOrNull { it.pacienteId == cita.pacienteId }
                val patientName = pacienteDetalle?.nombreCompleto ?: state.patientNameById(cita.pacienteId)
                val cedula = pacienteDetalle?.cedula
                val tieneSeguro = pacienteDetalle?.tieneSeguro ?: false

                CheckInArrivalCard(
                    cita = cita,
                    patientName = patientName,
                    cedula = cedula,
                    tieneSeguro = tieneSeguro,
                    canChangeStatus = canChangeStatus,
                    isUpdatingThisCard = state.isUpdating && state.updatingCitaId == cita.citaId,
                    isGlobalUpdating = state.isUpdating,
                    isJustUpdated = state.lastUpdatedCitaId == cita.citaId,
                    onUpdateStatus = onUpdateStatus
                )
            }
        }
    }
}

@Composable
private fun CheckInArrivalCard(
    cita: CitaDto,
    patientName: String,
    cedula: String?,
    tieneSeguro: Boolean,
    canChangeStatus: Boolean,
    isUpdatingThisCard: Boolean,
    isGlobalUpdating: Boolean,
    isJustUpdated: Boolean,
    onUpdateStatus: (citaId: Int, estadoApi: String, etiquetaVisual: String) -> Unit
) {
    val normalizedStatus = cita.estado.trim().lowercase(Locale.ROOT)
    val isInWaitingRoom = normalizedStatus == "llegó" || normalizedStatus == "llego"
    val isConfirmed = normalizedStatus == "confirmada"
    val isAttended = normalizedStatus == "atendida" || normalizedStatus == "completada"
    val isCancelled = normalizedStatus == "cancelada"

    val borderColor = if (isJustUpdated) TangerWine else TangerBorder
    val borderWidth = if (isJustUpdated) 1.5.dp else 1.dp

    Card(
        colors = CardDefaults.cardColors(containerColor = TangerWhite),
        border = BorderStroke(borderWidth, borderColor),
        shape = RoundedCornerShape(16.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            // Fila superior: Hora de la cita + Estado actual
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    Box(
                        modifier = Modifier
                            .background(TangerBlush, RoundedCornerShape(8.dp))
                            .padding(horizontal = 8.dp, vertical = 4.dp)
                    ) {
                        Row(
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(4.dp)
                        ) {
                            Icon(
                                imageVector = Icons.Default.Schedule,
                                contentDescription = null,
                                tint = TangerWine,
                                modifier = Modifier.size(15.dp)
                            )
                            Text(
                                text = ClinicDateUtils.formatTime(cita.fecha),
                                style = MaterialTheme.typography.labelLarge,
                                color = TangerWineDark,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    }
                    if (isJustUpdated) {
                        Surface(
                            color = StatusGreenBg,
                            shape = RoundedCornerShape(50)
                        ) {
                            Row(
                                modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp),
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.spacedBy(3.dp)
                            ) {
                                Icon(
                                    imageVector = Icons.Default.CheckCircle,
                                    contentDescription = null,
                                    tint = StatusGreenText,
                                    modifier = Modifier.size(13.dp)
                                )
                                Text(
                                    text = "Actualizado",
                                    style = MaterialTheme.typography.labelSmall,
                                    color = StatusGreenText,
                                    fontWeight = FontWeight.Bold
                                )
                            }
                        }
                    }
                }
                AppointmentStatusBadge(rawStatus = cita.estado)
            }

            // Datos del paciente + Badge TieneSeguro
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                Column(
                    modifier = Modifier.weight(1f),
                    verticalArrangement = Arrangement.spacedBy(2.dp)
                ) {
                    Text(
                        text = patientName,
                        style = MaterialTheme.typography.titleMedium,
                        color = TangerWineDark,
                        fontWeight = FontWeight.Bold
                    )
                    if (!cedula.isNullOrBlank()) {
                        Text(
                            text = "Cédula: $cedula",
                            style = MaterialTheme.typography.bodyMedium,
                            color = TangerMuted
                        )
                    }
                }
                Spacer(modifier = Modifier.width(8.dp))
                InsuranceBadge(tieneSeguro = tieneSeguro)
            }

            // Motivo / procedimiento
            Surface(
                color = TangerSurfaceTint,
                shape = RoundedCornerShape(10.dp),
                border = BorderStroke(1.dp, TangerBorder),
                modifier = Modifier.fillMaxWidth()
            ) {
                Row(
                    modifier = Modifier.padding(horizontal = 10.dp, vertical = 8.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Icon(
                        imageVector = Icons.Default.MedicalServices,
                        contentDescription = null,
                        tint = TangerWine,
                        modifier = Modifier.size(16.dp)
                    )
                    Text(
                        text = cita.tipoCita?.takeIf { it.isNotBlank() } ?: "Consulta / Procedimiento",
                        style = MaterialTheme.typography.bodyMedium,
                        color = TangerWineDark,
                        fontWeight = FontWeight.Medium
                    )
                }
            }

            if (canChangeStatus) {
                HorizontalDivider(color = TangerBorder)

                // Botón principal: Marcar en Sala de Espera
                Button(
                    onClick = { onUpdateStatus(cita.citaId, "Llegó", "En sala de espera") },
                    enabled = !isGlobalUpdating && !isInWaitingRoom && !isAttended && !isCancelled,
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = TangerWine,
                        contentColor = TangerWhite,
                        disabledContainerColor = TangerBlush,
                        disabledContentColor = TangerWineDark
                    ),
                    contentPadding = PaddingValues(vertical = 11.dp)
                ) {
                    if (isUpdatingThisCard) {
                        CircularProgressIndicator(
                            color = TangerWhite,
                            strokeWidth = 2.dp,
                            modifier = Modifier.size(16.dp)
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Text("Actualizando...", fontWeight = FontWeight.Bold)
                    } else {
                        Icon(
                            imageVector = Icons.Default.HowToReg,
                            contentDescription = null,
                            modifier = Modifier.size(18.dp)
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = if (isInWaitingRoom) "Ya en Sala de Espera" else "Marcar en Sala de Espera",
                            fontWeight = FontWeight.Bold
                        )
                    }
                }

                // Botones secundarios: Confirmar Llegada y Atendida
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    OutlinedButton(
                        onClick = { onUpdateStatus(cita.citaId, "Confirmada", "Confirmada") },
                        enabled = !isGlobalUpdating && !isConfirmed && !isAttended && !isCancelled,
                        modifier = Modifier.weight(1f),
                        shape = RoundedCornerShape(10.dp),
                        border = BorderStroke(1.dp, TangerWine.copy(alpha = 0.55f)),
                        contentPadding = PaddingValues(horizontal = 8.dp, vertical = 8.dp)
                    ) {
                        Icon(
                            imageVector = Icons.Default.EventAvailable,
                            contentDescription = null,
                            tint = TangerWine,
                            modifier = Modifier.size(16.dp)
                        )
                        Spacer(modifier = Modifier.width(4.dp))
                        Text(
                            text = "Confirmar Llegada",
                            color = TangerWine,
                            style = MaterialTheme.typography.labelMedium,
                            fontWeight = FontWeight.SemiBold
                        )
                    }

                    OutlinedButton(
                        onClick = { onUpdateStatus(cita.citaId, "Atendida", "Atendida") },
                        enabled = !isGlobalUpdating && !isAttended && !isCancelled,
                        modifier = Modifier.weight(1f),
                        shape = RoundedCornerShape(10.dp),
                        border = BorderStroke(1.dp, StatusGreenText.copy(alpha = 0.55f)),
                        contentPadding = PaddingValues(horizontal = 8.dp, vertical = 8.dp)
                    ) {
                        Icon(
                            imageVector = Icons.Default.CheckCircle,
                            contentDescription = null,
                            tint = StatusGreenText,
                            modifier = Modifier.size(16.dp)
                        )
                        Spacer(modifier = Modifier.width(4.dp))
                        Text(
                            text = "Atendida",
                            color = StatusGreenText,
                            style = MaterialTheme.typography.labelMedium,
                            fontWeight = FontWeight.SemiBold
                        )
                    }
                }
            }
        }
    }
}
