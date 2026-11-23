package com.tangerclinic.gestioncirugiaplastica.ui.patient

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Login
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Cancel
import androidx.compose.material.icons.filled.EditCalendar
import androidx.compose.material.icons.filled.EventAvailable
import androidx.compose.material.icons.filled.EventBusy
import androidx.compose.material.icons.filled.ExpandLess
import androidx.compose.material.icons.filled.ExpandMore
import androidx.compose.material.icons.filled.Medication
import androidx.compose.material.icons.filled.Newspaper
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.tangerclinic.gestioncirugiaplastica.data.dto.NoticiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalCitaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PortalRecetaDto
import com.tangerclinic.gestioncirugiaplastica.data.network.UserSessionState
import com.tangerclinic.gestioncirugiaplastica.ui.components.AppointmentStatusBadge
import com.tangerclinic.gestioncirugiaplastica.ui.components.ClinicDateUtils
import com.tangerclinic.gestioncirugiaplastica.ui.components.DateAndHalfHourSlotSelector
import com.tangerclinic.gestioncirugiaplastica.ui.components.EmptyStateCard
import com.tangerclinic.gestioncirugiaplastica.ui.components.FeedbackBanner
import com.tangerclinic.gestioncirugiaplastica.ui.components.InsuranceBadge
import com.tangerclinic.gestioncirugiaplastica.ui.components.LoadingState
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusRedText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBackground
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBlush
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBorder
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerMuted
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerSurfaceTint
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWhite
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWine
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWineDark
import java.time.LocalDate
import java.time.LocalTime

private val APPOINTMENT_TYPES = listOf("Consulta", "Evaluación", "Seguimiento", "Postoperatorio")

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PatientPortalScreen(
    session: UserSessionState,
    viewModel: PatientPortalViewModel = viewModel()
) {
    val state by viewModel.uiState.collectAsState()
    val isPatient = session.isAuthenticated && !session.isPublicVisitor

    var selectedTab by remember { mutableIntStateOf(0) }
    var showBookDialog by remember { mutableStateOf(false) }
    var showPublicCancelDialog by remember { mutableStateOf(false) }
    var prefillCancelCode by remember { mutableStateOf("") }
    var citaToReschedule by remember { mutableStateOf<PortalCitaDto?>(null) }
    var citaToCancel by remember { mutableStateOf<PortalCitaDto?>(null) }

    LaunchedEffect(isPatient) {
        viewModel.loadAll(isAuthenticatedPatient = isPatient)
    }

    val tabs = remember(isPatient) {
        if (isPatient) {
            listOf(
                Triple("Citas", Icons.Default.EventAvailable, 0),
                Triple("Recetas", Icons.Default.Medication, 1),
                Triple("Noticias", Icons.Default.Newspaper, 2)
            )
        } else {
            listOf(
                Triple("Citas", Icons.Default.EventAvailable, 0),
                Triple("Noticias", Icons.Default.Newspaper, 1)
            )
        }
    }

    val safeSelectedTab = selectedTab.coerceIn(0, tabs.lastIndex)

    Scaffold(
        containerColor = TangerBackground,
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            text = if (isPatient) "Portal del Paciente" else "Tanger Clinic · Visitante",
                            style = MaterialTheme.typography.titleLarge,
                            color = TangerWhite
                        )
                        Text(
                            text = session.displayName.ifBlank { "Atención personalizada" },
                            style = MaterialTheme.typography.labelMedium,
                            color = TangerBlush
                        )
                    }
                },
                actions = {
                    IconButton(onClick = { viewModel.loadAll(isAuthenticatedPatient = isPatient) }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Actualizar", tint = TangerWhite)
                    }
                    IconButton(onClick = viewModel::logoutOrReturnToLogin) {
                        Icon(
                            imageVector = if (isPatient) Icons.AutoMirrored.Filled.Logout else Icons.AutoMirrored.Filled.Login,
                            contentDescription = if (isPatient) "Cerrar sesión" else "Iniciar sesión",
                            tint = TangerWhite
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = TangerWine)
            )
        },
        floatingActionButton = {
            if (safeSelectedTab == 0) {
                FloatingActionButton(
                    onClick = { showBookDialog = true },
                    containerColor = TangerWine,
                    contentColor = TangerWhite
                ) {
                    Icon(Icons.Default.Add, contentDescription = "Solicitar cita")
                }
            }
        },
        bottomBar = {
            NavigationBar(containerColor = TangerWhite) {
                tabs.forEachIndexed { idx, (label, icon, _) ->
                    NavigationBarItem(
                        selected = safeSelectedTab == idx,
                        onClick = { selectedTab = idx },
                        icon = { Icon(icon, contentDescription = label) },
                        label = { Text(label) },
                        colors = NavigationBarItemDefaults.colors(
                            selectedIconColor = TangerWine,
                            selectedTextColor = TangerWine,
                            indicatorColor = TangerBlush,
                            unselectedIconColor = TangerMuted,
                            unselectedTextColor = TangerMuted
                        )
                    )
                }
            }
        }
    ) { innerPadding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
                .padding(horizontal = 16.dp)
        ) {
            if (state.errorMessage != null) {
                Spacer(modifier = Modifier.height(10.dp))
                FeedbackBanner(
                    message = state.errorMessage!!,
                    isError = true,
                    onDismiss = viewModel::clearFeedback
                )
            }
            if (state.successMessage != null) {
                Spacer(modifier = Modifier.height(10.dp))
                FeedbackBanner(
                    message = state.successMessage!!,
                    isError = false,
                    onDismiss = viewModel::clearFeedback
                )
            }

            if (state.isLoading) {
                LoadingState()
            } else {
                when (tabs[safeSelectedTab].first) {
                    "Citas" -> {
                        if (isPatient) {
                            AuthenticatedPatientCitasTab(
                                state = state,
                                onRequestNew = { showBookDialog = true },
                                onReschedule = { citaToReschedule = it },
                                onCancel = { citaToCancel = it }
                            )
                        } else {
                            PublicVisitorCitasTab(
                                visitorCitas = state.visitorCitas,
                                onRequestNew = { showBookDialog = true },
                                onOpenCancelDialog = { code ->
                                    prefillCancelCode = code
                                    showPublicCancelDialog = true
                                }
                            )
                        }
                    }
                    "Recetas" -> PatientRecetasTab(recetas = state.recetas)
                    "Noticias" -> NoticiasTab(noticias = state.noticias)
                }
            }
        }
    }

    if (showBookDialog) {
        if (isPatient) {
            PatientBookAppointmentDialog(
                isSubmitting = state.isSubmitting,
                onDismiss = { showBookDialog = false },
                onConfirm = { date, time, tipo, notes ->
                    viewModel.solicitarCitaPaciente(date, time, tipo, notes) {
                        showBookDialog = false
                    }
                }
            )
        } else {
            PublicBookAppointmentDialog(
                isSubmitting = state.isSubmitting,
                onDismiss = { showBookDialog = false },
                onConfirm = { nombre, apellido, cedula, telefono, email, date, time, tipo, notes ->
                    viewModel.solicitarCitaPublica(
                        nombre, apellido, cedula, telefono, email, date, time, tipo, notes
                    ) {
                        showBookDialog = false
                    }
                }
            )
        }
    }

    citaToReschedule?.let { cita ->
        RescheduleAppointmentDialog(
            cita = cita,
            isSubmitting = state.isSubmitting,
            onDismiss = { citaToReschedule = null },
            onConfirm = { date, time ->
                viewModel.reprogramarCitaPaciente(cita.citaId, date, time) {
                    citaToReschedule = null
                }
            }
        )
    }

    citaToCancel?.let { cita ->
        AlertDialog(
            onDismissRequest = { citaToCancel = null },
            containerColor = TangerWhite,
            title = { Text("Cancelar cita", color = TangerWineDark, fontWeight = FontWeight.Bold) },
            text = {
                Text(
                    "¿Confirmas que deseas cancelar tu cita de ${cita.tipoCita ?: "Consulta"} " +
                        "del ${ClinicDateUtils.formatDateTime(cita.fecha)}?"
                )
            },
            confirmButton = {
                Button(
                    onClick = {
                        viewModel.cancelarCitaPaciente(cita.citaId)
                        citaToCancel = null
                    },
                    colors = ButtonDefaults.buttonColors(containerColor = StatusRedText)
                ) {
                    Text("Sí, cancelar")
                }
            },
            dismissButton = {
                TextButton(onClick = { citaToCancel = null }) {
                    Text("Volver", color = TangerMuted)
                }
            }
        )
    }

    if (showPublicCancelDialog) {
        PublicCancelAppointmentDialog(
            initialCode = prefillCancelCode,
            isSubmitting = state.isSubmitting,
            onDismiss = { showPublicCancelDialog = false },
            onConfirm = { code, cedula, telefono ->
                viewModel.cancelarCitaPublica(code, cedula, telefono) {
                    showPublicCancelDialog = false
                }
            }
        )
    }
}

@Composable
private fun AuthenticatedPatientCitasTab(
    state: PatientPortalUiState,
    onRequestNew: () -> Unit,
    onReschedule: (PortalCitaDto) -> Unit,
    onCancel: (PortalCitaDto) -> Unit
) {
    LazyColumn(
        contentPadding = PaddingValues(top = 12.dp, bottom = 88.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        state.perfil?.let { perfil ->
            item {
                Card(
                    colors = CardDefaults.cardColors(containerColor = TangerWhite),
                    border = BorderStroke(1.dp, TangerBorder),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(6.dp)
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = if (perfil.vinculado) {
                                    "${perfil.nombre.orEmpty()} ${perfil.apellido.orEmpty()}".trim()
                                } else {
                                    "Cuenta sin ficha vinculada"
                                },
                                style = MaterialTheme.typography.titleMedium,
                                color = TangerWineDark
                            )
                            if (perfil.vinculado) {
                                InsuranceBadge(tieneSeguro = perfil.tieneSeguro)
                            }
                        }
                        if (perfil.vinculado) {
                            Text(
                                text = "Cédula: ${perfil.cedula ?: "—"} · Tel: ${perfil.telefono ?: "—"}",
                                style = MaterialTheme.typography.bodyMedium,
                                color = TangerMuted
                            )
                        } else {
                            Text(
                                text = "Contacta a recepción para vincular tu ficha de paciente y habilitar citas.",
                                style = MaterialTheme.typography.bodyMedium,
                                color = TangerMuted
                            )
                        }
                    }
                }
            }
        }

        item {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text("Mis próximas citas", style = MaterialTheme.typography.titleLarge, color = TangerWineDark)
                TextButton(onClick = onRequestNew) {
                    Text("+ Solicitar", color = TangerWine, fontWeight = FontWeight.Bold)
                }
            }
        }

        if (state.citas.isEmpty()) {
            item {
                EmptyStateCard(
                    icon = Icons.Default.EventAvailable,
                    title = "Sin citas programadas",
                    subtitle = "Presiona + para solicitar tu próxima evaluación en Tanger Clinic."
                )
            }
        } else {
            items(state.citas, key = { it.citaId }) { cita ->
                Card(
                    colors = CardDefaults.cardColors(containerColor = TangerWhite),
                    border = BorderStroke(1.dp, TangerBorder),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = ClinicDateUtils.formatDateTime(cita.fecha),
                                style = MaterialTheme.typography.labelLarge,
                                color = TangerWine
                            )
                            AppointmentStatusBadge(rawStatus = cita.estado)
                        }
                        Text(
                            text = cita.tipoCita ?: "Consulta clínica",
                            style = MaterialTheme.typography.titleMedium
                        )
                        if (!cita.observaciones.isNullOrBlank()) {
                            Text(
                                text = cita.observaciones,
                                style = MaterialTheme.typography.bodyMedium,
                                color = TangerMuted
                            )
                        }
                        HorizontalDivider(color = TangerBorder)
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            OutlinedButton(
                                onClick = { onReschedule(cita) },
                                modifier = Modifier.weight(1f),
                                shape = RoundedCornerShape(10.dp),
                                border = BorderStroke(1.dp, TangerWine)
                            ) {
                                Icon(Icons.Default.EditCalendar, contentDescription = null, modifier = Modifier.size(16.dp), tint = TangerWine)
                                Spacer(modifier = Modifier.width(6.dp))
                                Text("Reprogramar", color = TangerWine)
                            }
                            OutlinedButton(
                                onClick = { onCancel(cita) },
                                modifier = Modifier.weight(1f),
                                shape = RoundedCornerShape(10.dp),
                                border = BorderStroke(1.dp, StatusRedText.copy(alpha = 0.5f))
                            ) {
                                Icon(Icons.Default.Cancel, contentDescription = null, modifier = Modifier.size(16.dp), tint = StatusRedText)
                                Spacer(modifier = Modifier.width(6.dp))
                                Text("Cancelar", color = StatusRedText)
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun PublicVisitorCitasTab(
    visitorCitas: List<VisitorBookedCita>,
    onRequestNew: () -> Unit,
    onOpenCancelDialog: (String) -> Unit
) {
    LazyColumn(
        contentPadding = PaddingValues(top = 12.dp, bottom = 88.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Card(
                colors = CardDefaults.cardColors(containerColor = TangerWhite),
                border = BorderStroke(1.dp, TangerBorder),
                shape = RoundedCornerShape(16.dp)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(18.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Text("Agenda tu cita sin cuenta", style = MaterialTheme.typography.titleLarge, color = TangerWineDark)
                    Text(
                        text = "Solicita una evaluación eligiendo fecha y horario cada 30 minutos. El correo electrónico es opcional.",
                        style = MaterialTheme.typography.bodyMedium,
                        color = TangerMuted
                    )
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        Button(
                            onClick = onRequestNew,
                            modifier = Modifier.weight(1f),
                            shape = RoundedCornerShape(10.dp),
                            colors = ButtonDefaults.buttonColors(containerColor = TangerWine)
                        ) {
                            Icon(Icons.Default.EventAvailable, contentDescription = null, modifier = Modifier.size(18.dp))
                            Spacer(modifier = Modifier.width(6.dp))
                            Text("Solicitar cita")
                        }
                        OutlinedButton(
                            onClick = { onOpenCancelDialog("") },
                            modifier = Modifier.weight(1f),
                            shape = RoundedCornerShape(10.dp),
                            border = BorderStroke(1.dp, StatusRedText.copy(alpha = 0.6f))
                        ) {
                            Icon(Icons.Default.EventBusy, contentDescription = null, modifier = Modifier.size(18.dp), tint = StatusRedText)
                            Spacer(modifier = Modifier.width(6.dp))
                            Text("Cancelar cita", color = StatusRedText)
                        }
                    }
                    Surface(
                        color = TangerSurfaceTint,
                        shape = RoundedCornerShape(10.dp),
                        border = BorderStroke(1.dp, TangerBorder)
                    ) {
                        Text(
                            text = "Para reprogramar citas o consultar recetas médicas, inicia sesión con tu cuenta de Paciente.",
                            style = MaterialTheme.typography.bodyMedium,
                            color = TangerWineDark,
                            modifier = Modifier.padding(10.dp)
                        )
                    }
                }
            }
        }

        if (visitorCitas.isNotEmpty()) {
            item {
                Text(
                    text = "Citas solicitadas en esta sesión",
                    style = MaterialTheme.typography.titleMedium,
                    color = TangerWineDark
                )
            }
            items(visitorCitas, key = { "${it.citaId}-${it.codigoGestion}" }) { cita ->
                Card(
                    colors = CardDefaults.cardColors(containerColor = TangerWhite),
                    border = BorderStroke(1.dp, TangerBorder),
                    shape = RoundedCornerShape(14.dp)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(6.dp)
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(ClinicDateUtils.formatDateTime(cita.fecha), color = TangerWine, fontWeight = FontWeight.SemiBold)
                            AppointmentStatusBadge(rawStatus = cita.estado)
                        }
                        Text(cita.tipoCita, style = MaterialTheme.typography.titleMedium)
                        if (cita.codigoGestion.isNotBlank()) {
                            Text(
                                text = "Código de gestión: ${cita.codigoGestion}",
                                style = MaterialTheme.typography.bodyMedium,
                                color = TangerWineDark,
                                fontWeight = FontWeight.SemiBold
                            )
                        }
                        if (!cita.estado.equals("Cancelada", ignoreCase = true)) {
                            TextButton(
                                onClick = { onOpenCancelDialog(cita.codigoGestion) },
                                contentPadding = PaddingValues(0.dp)
                            ) {
                                Text("Cancelar esta cita", color = StatusRedText, fontWeight = FontWeight.SemiBold)
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun PatientRecetasTab(recetas: List<PortalRecetaDto>) {
    LazyColumn(
        contentPadding = PaddingValues(top = 12.dp, bottom = 28.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Text("Mis recetas médicas", style = MaterialTheme.typography.titleLarge, color = TangerWineDark)
        }
        if (recetas.isEmpty()) {
            item {
                EmptyStateCard(
                    icon = Icons.Default.Medication,
                    title = "Sin recetas registradas",
                    subtitle = "Cuando tu especialista emita una receta aparecerá aquí."
                )
            }
        } else {
            items(recetas, key = { it.recetaId }) { receta ->
                Card(
                    colors = CardDefaults.cardColors(containerColor = TangerWhite),
                    border = BorderStroke(1.dp, TangerBorder),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Text(
                            text = "Receta #${receta.recetaId} · ${ClinicDateUtils.formatDate(receta.fecha)}",
                            style = MaterialTheme.typography.labelLarge,
                            color = TangerWine
                        )
                        if (!receta.indicaciones.isNullOrBlank()) {
                            Text(receta.indicaciones, style = MaterialTheme.typography.bodyMedium)
                        }
                        receta.medicamentos.forEach { med ->
                            Surface(
                                color = TangerSurfaceTint,
                                shape = RoundedCornerShape(10.dp),
                                border = BorderStroke(1.dp, TangerBorder),
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Column(modifier = Modifier.padding(10.dp)) {
                                    Text(med.medicamento, fontWeight = FontWeight.Bold, color = TangerWineDark)
                                    val meta = listOfNotNull(med.dosis, med.frecuencia, med.duracion)
                                        .filter { it.isNotBlank() }
                                        .joinToString(" · ")
                                    if (meta.isNotBlank()) {
                                        Text(meta, style = MaterialTheme.typography.bodyMedium, color = TangerMuted)
                                    }
                                    if (!med.instrucciones.isNullOrBlank()) {
                                        Text(med.instrucciones, style = MaterialTheme.typography.bodyMedium)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun NoticiasTab(noticias: List<NoticiaDto>) {
    LazyColumn(
        contentPadding = PaddingValues(top = 12.dp, bottom = 28.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Text("Tablón de Noticias", style = MaterialTheme.typography.titleLarge, color = TangerWineDark)
        }
        if (noticias.isEmpty()) {
            item {
                EmptyStateCard(
                    icon = Icons.Default.Newspaper,
                    title = "No hay noticias publicadas",
                    subtitle = "Vuelve pronto para conocer novedades y avisos de Tanger Clinic."
                )
            }
        } else {
            items(noticias, key = { it.noticiaId }) { noticia ->
                var expanded by remember(noticia.noticiaId) { mutableStateOf(false) }
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { expanded = !expanded },
                    colors = CardDefaults.cardColors(containerColor = TangerWhite),
                    border = BorderStroke(1.dp, TangerBorder),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(6.dp)
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Text(
                                text = ClinicDateUtils.formatDate(noticia.fechaPublicacion),
                                style = MaterialTheme.typography.labelMedium,
                                color = TangerWine
                            )
                            Icon(
                                imageVector = if (expanded) Icons.Default.ExpandLess else Icons.Default.ExpandMore,
                                contentDescription = null,
                                tint = TangerMuted
                            )
                        }
                        Text(noticia.titulo, style = MaterialTheme.typography.titleMedium, color = TangerWineDark)
                        if (!noticia.resumen.isNullOrBlank()) {
                            Text(noticia.resumen, style = MaterialTheme.typography.bodyMedium, color = TangerMuted)
                        }
                        if (expanded) {
                            HorizontalDivider(color = TangerBorder, modifier = Modifier.padding(vertical = 4.dp))
                            Text(noticia.contenido, style = MaterialTheme.typography.bodyLarge)
                        } else {
                            Text(
                                text = "Toca para leer completo",
                                style = MaterialTheme.typography.labelMedium,
                                color = TangerWine
                            )
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun PatientBookAppointmentDialog(
    isSubmitting: Boolean,
    onDismiss: () -> Unit,
    onConfirm: (LocalDate, LocalTime, String, String) -> Unit
) {
    var selectedDate by remember { mutableStateOf(LocalDate.now().plusDays(1)) }
    var selectedTime by remember { mutableStateOf(LocalTime.of(9, 0)) }
    var tipoCita by remember { mutableStateOf(APPOINTMENT_TYPES.first()) }
    var observaciones by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        containerColor = TangerWhite,
        title = { Text("Solicitar nueva cita", color = TangerWineDark, fontWeight = FontWeight.Bold) },
        text = {
            Column(
                modifier = Modifier.verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                DateAndHalfHourSlotSelector(
                    selectedDate = selectedDate,
                    selectedTime = selectedTime,
                    onDateSelected = { selectedDate = it },
                    onTimeSelected = { selectedTime = it }
                )
                Text("Tipo de cita", style = MaterialTheme.typography.labelLarge, color = TangerWineDark)
                FlowRow(
                    horizontalArrangement = Arrangement.spacedBy(6.dp),
                    verticalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    APPOINTMENT_TYPES.forEach { tipo ->
                        FilterChip(
                            selected = tipoCita == tipo,
                            onClick = { tipoCita = tipo },
                            label = { Text(tipo) },
                            colors = FilterChipDefaults.filterChipColors(
                                selectedContainerColor = TangerWine,
                                selectedLabelColor = TangerWhite
                            )
                        )
                    }
                }
                OutlinedTextField(
                    value = observaciones,
                    onValueChange = { observaciones = it },
                    label = { Text("Observaciones (opcional)") },
                    maxLines = 3,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            Button(
                onClick = { onConfirm(selectedDate, selectedTime, tipoCita, observaciones) },
                enabled = !isSubmitting,
                colors = ButtonDefaults.buttonColors(containerColor = TangerWine)
            ) {
                Text(if (isSubmitting) "Enviando..." else "Solicitar cita")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSubmitting) {
                Text("Cancelar", color = TangerMuted)
            }
        }
    )
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun PublicBookAppointmentDialog(
    isSubmitting: Boolean,
    onDismiss: () -> Unit,
    onConfirm: (String, String, String, String, String, LocalDate, LocalTime, String, String) -> Unit
) {
    var nombre by remember { mutableStateOf("") }
    var apellido by remember { mutableStateOf("") }
    var cedula by remember { mutableStateOf("") }
    var telefono by remember { mutableStateOf("") }
    var email by remember { mutableStateOf("") }
    var selectedDate by remember { mutableStateOf(LocalDate.now().plusDays(1)) }
    var selectedTime by remember { mutableStateOf(LocalTime.of(9, 0)) }
    var tipoCita by remember { mutableStateOf(APPOINTMENT_TYPES.first()) }
    var observaciones by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        containerColor = TangerWhite,
        title = { Text("Solicitar cita pública", color = TangerWineDark, fontWeight = FontWeight.Bold) },
        text = {
            Column(
                modifier = Modifier.verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                OutlinedTextField(
                    value = nombre,
                    onValueChange = { nombre = it },
                    label = { Text("Nombre *") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = apellido,
                    onValueChange = { apellido = it },
                    label = { Text("Apellido *") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = cedula,
                    onValueChange = { cedula = it },
                    label = { Text("Cédula (opcional)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = telefono,
                    onValueChange = { telefono = it },
                    label = { Text("Teléfono (opcional)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = email,
                    onValueChange = { email = it },
                    label = { Text("Correo electrónico (opcional)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                DateAndHalfHourSlotSelector(
                    selectedDate = selectedDate,
                    selectedTime = selectedTime,
                    onDateSelected = { selectedDate = it },
                    onTimeSelected = { selectedTime = it }
                )
                Text("Motivo / tipo de cita", style = MaterialTheme.typography.labelLarge, color = TangerWineDark)
                FlowRow(
                    horizontalArrangement = Arrangement.spacedBy(6.dp),
                    verticalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    APPOINTMENT_TYPES.forEach { tipo ->
                        FilterChip(
                            selected = tipoCita == tipo,
                            onClick = { tipoCita = tipo },
                            label = { Text(tipo) },
                            colors = FilterChipDefaults.filterChipColors(
                                selectedContainerColor = TangerWine,
                                selectedLabelColor = TangerWhite
                            )
                        )
                    }
                }
                OutlinedTextField(
                    value = observaciones,
                    onValueChange = { observaciones = it },
                    label = { Text("Observaciones (opcional)") },
                    maxLines = 3,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    onConfirm(nombre, apellido, cedula, telefono, email, selectedDate, selectedTime, tipoCita, observaciones)
                },
                enabled = !isSubmitting,
                colors = ButtonDefaults.buttonColors(containerColor = TangerWine)
            ) {
                Text(if (isSubmitting) "Enviando..." else "Solicitar")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSubmitting) {
                Text("Cancelar", color = TangerMuted)
            }
        }
    )
}

@Composable
private fun RescheduleAppointmentDialog(
    cita: PortalCitaDto,
    isSubmitting: Boolean,
    onDismiss: () -> Unit,
    onConfirm: (LocalDate, LocalTime) -> Unit
) {
    val parsed = ClinicDateUtils.parseApiDateTime(cita.fecha)
    var selectedDate by remember {
        mutableStateOf(parsed?.toLocalDate()?.takeIf { !it.isBefore(LocalDate.now()) } ?: LocalDate.now().plusDays(1))
    }
    var selectedTime by remember { mutableStateOf(LocalTime.of(9, 0)) }

    AlertDialog(
        onDismissRequest = onDismiss,
        containerColor = TangerWhite,
        title = { Text("Reprogramar cita", color = TangerWineDark, fontWeight = FontWeight.Bold) },
        text = {
            Column(
                modifier = Modifier.verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                Text(
                    text = "Cita actual: ${ClinicDateUtils.formatDateTime(cita.fecha)}",
                    style = MaterialTheme.typography.bodyMedium,
                    color = TangerMuted
                )
                DateAndHalfHourSlotSelector(
                    selectedDate = selectedDate,
                    selectedTime = selectedTime,
                    onDateSelected = { selectedDate = it },
                    onTimeSelected = { selectedTime = it }
                )
            }
        },
        confirmButton = {
            Button(
                onClick = { onConfirm(selectedDate, selectedTime) },
                enabled = !isSubmitting,
                colors = ButtonDefaults.buttonColors(containerColor = TangerWine)
            ) {
                Text(if (isSubmitting) "Guardando..." else "Confirmar cambio")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSubmitting) {
                Text("Cancelar", color = TangerMuted)
            }
        }
    )
}

@Composable
private fun PublicCancelAppointmentDialog(
    initialCode: String,
    isSubmitting: Boolean,
    onDismiss: () -> Unit,
    onConfirm: (String, String, String) -> Unit
) {
    var codigo by remember(initialCode) { mutableStateOf(initialCode) }
    var cedula by remember { mutableStateOf("") }
    var telefono by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        containerColor = TangerWhite,
        title = { Text("Cancelar cita pública", color = TangerWineDark, fontWeight = FontWeight.Bold) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text(
                    text = "Ingresa tu código de gestión o bien tu cédula y teléfono registrados.",
                    style = MaterialTheme.typography.bodyMedium,
                    color = TangerMuted
                )
                OutlinedTextField(
                    value = codigo,
                    onValueChange = { codigo = it },
                    label = { Text("Código de gestión (preferible)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = cedula,
                    onValueChange = { cedula = it },
                    label = { Text("Cédula") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = telefono,
                    onValueChange = { telefono = it },
                    label = { Text("Teléfono") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            Button(
                onClick = { onConfirm(codigo, cedula, telefono) },
                enabled = !isSubmitting,
                colors = ButtonDefaults.buttonColors(containerColor = StatusRedText)
            ) {
                Text(if (isSubmitting) "Cancelando..." else "Cancelar cita")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSubmitting) {
                Text("Volver", color = TangerMuted)
            }
        }
    )
}
