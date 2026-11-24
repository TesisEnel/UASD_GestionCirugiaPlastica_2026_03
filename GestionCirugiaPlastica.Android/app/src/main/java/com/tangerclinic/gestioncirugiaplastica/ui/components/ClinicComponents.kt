package com.tangerclinic.gestioncirugiaplastica.ui.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.ErrorOutline
import androidx.compose.material.icons.filled.Schedule
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusAmberBg
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusAmberText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusBlueBg
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusBlueText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusGreenBg
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusGreenText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusRedBg
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusRedText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBlush
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBorder
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerMuted
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWhite
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWine
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWineDark
import java.time.Instant
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.OffsetDateTime
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import java.util.Locale

object ClinicDateUtils {
    private val displayDateFormatter = DateTimeFormatter.ofPattern("dd/MM/yyyy", Locale.getDefault())
    private val displayDateTimeFormatter = DateTimeFormatter.ofPattern("dd/MM/yyyy · hh:mm a", Locale.US)
    private val displayTimeFormatter = DateTimeFormatter.ofPattern("hh:mm a", Locale.US)
    private val apiIsoFormatter = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss", Locale.US)

    val halfHourSlots: List<LocalTime> = (0..20).map { step ->
        LocalTime.of(8, 0).plusMinutes(step * 30L)
    }

    fun parseApiDateTime(raw: String?): LocalDateTime? {
        if (raw.isNullOrBlank()) return null
        val cleaned = raw.trim()
        return runCatching { LocalDateTime.parse(cleaned) }.getOrNull()
            ?: runCatching { OffsetDateTime.parse(cleaned).toLocalDateTime() }.getOrNull()
            ?: runCatching {
                LocalDate.parse(cleaned.take(10)).atStartOfDay()
            }.getOrNull()
    }

    fun formatDateTime(raw: String?): String =
        parseApiDateTime(raw)?.format(displayDateTimeFormatter) ?: (raw ?: "—")

    fun formatDate(raw: String?): String =
        parseApiDateTime(raw)?.format(displayDateFormatter) ?: (raw ?: "—")

    fun formatTime(raw: String?): String =
        parseApiDateTime(raw)?.format(displayTimeFormatter) ?: "—"

    fun formatLocalDate(date: LocalDate): String = date.format(displayDateFormatter)

    fun formatSlot(time: LocalTime): String = time.format(displayTimeFormatter)

    fun toApiIso(date: LocalDate, time: LocalTime): String =
        LocalDateTime.of(date, time).format(apiIsoFormatter)

    fun isToday(raw: String?): Boolean =
        parseApiDateTime(raw)?.toLocalDate() == LocalDate.now()
}

fun displayAppointmentStatus(rawStatus: String?): String = when (rawStatus?.trim()?.lowercase(Locale.ROOT)) {
    "llegó", "llego" -> "En sala de espera"
    "atendida" -> "Atendida"
    "completada" -> "Completada"
    "confirmada" -> "Confirmada"
    "cancelada" -> "Cancelada"
    "pendiente" -> "Pendiente"
    else -> rawStatus?.ifBlank { "Pendiente" } ?: "Pendiente"
}

@Composable
fun StatusBadge(
    label: String,
    backgroundColor: Color,
    contentColor: Color,
    modifier: Modifier = Modifier
) {
    Surface(
        modifier = modifier,
        shape = RoundedCornerShape(50),
        color = backgroundColor
    ) {
        Text(
            text = label,
            color = contentColor,
            style = MaterialTheme.typography.labelMedium,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp)
        )
    }
}

@Composable
fun AppointmentStatusBadge(rawStatus: String?, modifier: Modifier = Modifier) {
    val label = displayAppointmentStatus(rawStatus)
    val (bg, fg) = when (rawStatus?.trim()?.lowercase(Locale.ROOT)) {
        "llegó", "llego" -> StatusAmberBg to StatusAmberText
        "atendida", "completada" -> StatusGreenBg to StatusGreenText
        "confirmada" -> StatusBlueBg to StatusBlueText
        "cancelada" -> StatusRedBg to StatusRedText
        else -> TangerBlush to TangerWineDark
    }
    StatusBadge(label = label, backgroundColor = bg, contentColor = fg, modifier = modifier)
}

@Composable
fun InsuranceBadge(tieneSeguro: Boolean, modifier: Modifier = Modifier) {
    if (tieneSeguro) {
        StatusBadge(
            label = "Con seguro",
            backgroundColor = StatusGreenBg,
            contentColor = StatusGreenText,
            modifier = modifier
        )
    } else {
        StatusBadge(
            label = "Privado",
            backgroundColor = TangerBlush,
            contentColor = TangerWineDark,
            modifier = modifier
        )
    }
}

@Composable
fun FeedbackBanner(
    message: String,
    isError: Boolean,
    onDismiss: () -> Unit,
    modifier: Modifier = Modifier
) {
    val bg = if (isError) StatusRedBg else StatusGreenBg
    val fg = if (isError) StatusRedText else StatusGreenText
    val icon = if (isError) Icons.Default.ErrorOutline else Icons.Default.CheckCircle

    Surface(
        modifier = modifier.fillMaxWidth(),
        color = bg,
        shape = RoundedCornerShape(12.dp)
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 12.dp, vertical = 10.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(imageVector = icon, contentDescription = null, tint = fg, modifier = Modifier.size(20.dp))
            Spacer(modifier = Modifier.width(10.dp))
            Text(
                text = message,
                color = fg,
                style = MaterialTheme.typography.bodyMedium,
                modifier = Modifier.weight(1f)
            )
            IconButton(onClick = onDismiss, modifier = Modifier.size(28.dp)) {
                Icon(imageVector = Icons.Default.Close, contentDescription = "Cerrar", tint = fg, modifier = Modifier.size(16.dp))
            }
        }
    }
}

@Composable
fun LoadingState(text: String = "Cargando información...", modifier: Modifier = Modifier) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(vertical = 40.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        CircularProgressIndicator(color = TangerWine)
        Spacer(modifier = Modifier.height(12.dp))
        Text(text = text, style = MaterialTheme.typography.bodyMedium, color = TangerMuted)
    }
}

@Composable
fun EmptyStateCard(
    icon: ImageVector,
    title: String,
    subtitle: String,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = TangerWhite),
        border = BorderStroke(1.dp, TangerBorder),
        shape = RoundedCornerShape(16.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Box(
                modifier = Modifier
                    .size(48.dp)
                    .background(TangerBlush, RoundedCornerShape(14.dp)),
                contentAlignment = Alignment.Center
            ) {
                Icon(imageVector = icon, contentDescription = null, tint = TangerWine)
            }
            Spacer(modifier = Modifier.height(12.dp))
            Text(text = title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Spacer(modifier = Modifier.height(4.dp))
            Text(text = subtitle, style = MaterialTheme.typography.bodyMedium, color = TangerMuted)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
fun DateAndHalfHourSlotSelector(
    selectedDate: LocalDate,
    selectedTime: LocalTime,
    onDateSelected: (LocalDate) -> Unit,
    onTimeSelected: (LocalTime) -> Unit,
    modifier: Modifier = Modifier
) {
    var showDatePicker by remember { mutableStateOf(false) }

    Column(modifier = modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(10.dp)) {
        Text(
            text = "Fecha de la cita",
            style = MaterialTheme.typography.labelLarge,
            color = TangerWineDark
        )
        OutlinedButton(
            onClick = { showDatePicker = true },
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(10.dp),
            border = BorderStroke(1.dp, TangerBorder)
        ) {
            Icon(Icons.Default.CalendarMonth, contentDescription = null, tint = TangerWine)
            Spacer(modifier = Modifier.width(8.dp))
            Text(
                text = ClinicDateUtils.formatLocalDate(selectedDate),
                color = TangerWineDark,
                fontWeight = FontWeight.SemiBold
            )
        }

        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(
                imageVector = Icons.Default.Schedule,
                contentDescription = null,
                tint = TangerWine,
                modifier = Modifier.size(16.dp)
            )
            Spacer(modifier = Modifier.width(6.dp))
            Text(
                text = "Horario disponible (intervalos de 30 min)",
                style = MaterialTheme.typography.labelLarge,
                color = TangerWineDark
            )
        }

        FlowRow(
            horizontalArrangement = Arrangement.spacedBy(6.dp),
            verticalArrangement = Arrangement.spacedBy(6.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            ClinicDateUtils.halfHourSlots.forEach { slot ->
                val isSelected = slot == selectedTime
                FilterChip(
                    selected = isSelected,
                    onClick = { onTimeSelected(slot) },
                    label = { Text(ClinicDateUtils.formatSlot(slot), style = MaterialTheme.typography.labelMedium) },
                    colors = FilterChipDefaults.filterChipColors(
                        selectedContainerColor = TangerWine,
                        selectedLabelColor = TangerWhite,
                        containerColor = TangerWhite,
                        labelColor = TangerWineDark
                    )
                )
            }
        }
    }

    if (showDatePicker) {
        val initialMillis = selectedDate.atStartOfDay(ZoneOffset.UTC).toInstant().toEpochMilli()
        val pickerState = rememberDatePickerState(initialSelectedDateMillis = initialMillis)
        DatePickerDialog(
            onDismissRequest = { showDatePicker = false },
            confirmButton = {
                TextButton(
                    onClick = {
                        pickerState.selectedDateMillis?.let { millis ->
                            val picked = Instant.ofEpochMilli(millis).atZone(ZoneOffset.UTC).toLocalDate()
                            onDateSelected(picked)
                        }
                        showDatePicker = false
                    }
                ) {
                    Text("Aceptar", color = TangerWine, fontWeight = FontWeight.Bold)
                }
            },
            dismissButton = {
                TextButton(onClick = { showDatePicker = false }) {
                    Text("Cancelar", color = TangerMuted)
                }
            }
        ) {
            DatePicker(state = pickerState)
        }
    }
}
