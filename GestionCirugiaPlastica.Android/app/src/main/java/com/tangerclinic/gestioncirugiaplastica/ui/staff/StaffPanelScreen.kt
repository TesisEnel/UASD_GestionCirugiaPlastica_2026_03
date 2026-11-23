package com.tangerclinic.gestioncirugiaplastica.ui.staff

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.CalendarToday
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.DoneAll
import androidx.compose.material.icons.filled.MedicalServices
import androidx.compose.material.icons.filled.Medication
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.People
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuAnchorType
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
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.tangerclinic.gestioncirugiaplastica.data.dto.CitaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.ConsultaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.NotificacionDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PacienteDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.RecetaDto
import com.tangerclinic.gestioncirugiaplastica.data.network.ClinicRole
import com.tangerclinic.gestioncirugiaplastica.data.network.UserSessionState
import com.tangerclinic.gestioncirugiaplastica.ui.components.AppointmentStatusBadge
import com.tangerclinic.gestioncirugiaplastica.ui.components.ClinicDateUtils
import com.tangerclinic.gestioncirugiaplastica.ui.components.EmptyStateCard
import com.tangerclinic.gestioncirugiaplastica.ui.components.FeedbackBanner
import com.tangerclinic.gestioncirugiaplastica.ui.components.InsuranceBadge
import com.tangerclinic.gestioncirugiaplastica.ui.components.LoadingState
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusAmberText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusGreenText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusRedText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBackground
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBlush
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBorder
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerMuted
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerSurfaceTint
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWhite
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWine
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWineDark

private data class StaffTabItem(val id: String, val label: String, val icon: ImageVector)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StaffPanelScreen(
    session: UserSessionState,
    viewModel: StaffPanelViewModel = viewModel()
) {
    val role = session.role ?: ClinicRole.DOCTORA
    val state by viewModel.uiState.collectAsState()

    val tabs = remember(role) {
        buildList {
            add(StaffTabItem("agenda", "Agenda", Icons.Default.CalendarToday))
            add(StaffTabItem("pacientes", "Pacientes", Icons.Default.People))
            if (role.isClinical) {
                add(StaffTabItem("consultas", "Consultas", Icons.Default.MedicalServices))
                add(StaffTabItem("recetas", "Recetas", Icons.Default.Medication))
            }
        }
    }

    var selectedTabIndex by remember { mutableIntStateOf(0) }
    var showNotificationsDialog by remember { mutableStateOf(false) }
    var showNewConsultaDialog by remember { mutableStateOf(false) }

    LaunchedEffect(role) {
        viewModel.loadAll(role)
    }

    val safeTabIndex = selectedTabIndex.coerceIn(0, tabs.lastIndex)
    val currentTab = tabs[safeTabIndex]

    Scaffold(
        containerColor = TangerBackground,
        topBar = {
            TopAppBar(
                title = {
                    Column {
                        Text(
                            text = "Tanger Clinic",
                            style = MaterialTheme.typography.titleLarge,
                            color = TangerWhite
                        )
                        Text(
                            text = "${session.displayName} · ${role.displayLabel}",
                            style = MaterialTheme.typography.labelMedium,
                            color = TangerBlush
                        )
                    }
                },
                actions = {
                    if (role == ClinicRole.DOCTORA) {
                        IconButton(
                            onClick = {
                                showNotificationsDialog = true
                                viewModel.loadNotifications()
                            }
                        ) {
                            BadgedBox(
                                badge = {
                                    if (state.unreadNotificationsCount > 0) {
                                        Badge(containerColor = TangerWhite, contentColor = TangerWine) {
                                            Text(
                                                text = if (state.unreadNotificationsCount > 99) "99+" else state.unreadNotificationsCount.toString(),
                                                fontWeight = FontWeight.Bold
                                            )
                                        }
                                    }
                                }
                            ) {
                                Icon(
                                    imageVector = Icons.Default.Notifications,
                                    contentDescription = "Notificaciones",
                                    tint = TangerWhite
                                )
                            }
                        }
                    }
                    IconButton(onClick = { viewModel.loadAll(role) }) {
                        Icon(Icons.Default.Refresh, contentDescription = "Actualizar", tint = TangerWhite)
                    }
                    IconButton(onClick = viewModel::logout) {
                        Icon(Icons.AutoMirrored.Filled.Logout, contentDescription = "Cerrar sesión", tint = TangerWhite)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = TangerWine)
            )
        },
        floatingActionButton = {
            if (currentTab.id == "consultas" && role.isClinical) {
                FloatingActionButton(
                    onClick = { showNewConsultaDialog = true },
                    containerColor = TangerWine,
                    contentColor = TangerWhite
                ) {
                    Icon(Icons.Default.Add, contentDescription = "Nueva consulta")
                }
            }
        },
        bottomBar = {
            NavigationBar(containerColor = TangerWhite) {
                tabs.forEachIndexed { index, item ->
                    NavigationBarItem(
                        selected = safeTabIndex == index,
                        onClick = { selectedTabIndex = index },
                        icon = { Icon(item.icon, contentDescription = item.label) },
                        label = { Text(item.label) },
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
                when (currentTab.id) {
                    "agenda" -> StaffAgendaTab(
                        citas = state.citas,
                        canChangeStatus = role.canUpdateAppointmentStatus,
                        isUpdating = state.isUpdating,
                        onUpdateStatus = { citaId, estadoApi, label ->
                            viewModel.cambiarEstadoCita(citaId, estadoApi, label)
                        }
                    )
                    "pacientes" -> StaffPacientesTab(
                        pacientes = state.pacientes,
                        showInsuranceBadge = role.canReadFullPatients
                    )
                    "consultas" -> StaffConsultasTab(
                        consultas = state.consultas,
                        state = state
                    )
                    "recetas" -> StaffRecetasTab(
                        recetas = state.recetas,
                        state = state
                    )
                }
            }
        }
    }

    if (showNotificationsDialog && role == ClinicRole.DOCTORA) {
        NotificationsDialog(
            notifications = state.notificaciones,
            isLoading = state.isLoadingNotifications,
            onDismiss = { showNotificationsDialog = false },
            onMarkRead = viewModel::marcarNotificacionLeida,
            onMarkAllRead = viewModel::marcarTodasNotificacionesLeidas
        )
    }

    if (showNewConsultaDialog && role.isClinical) {
        NewConsultaDialog(
            pacientes = state.pacientes,
            isSubmitting = state.isUpdating,
            onDismiss = { showNewConsultaDialog = false },
            onConfirm = { pacienteId, motivo, diagnostico, observaciones ->
                viewModel.registrarConsulta(pacienteId, motivo, diagnostico, observaciones) {
                    showNewConsultaDialog = false
                }
            }
        )
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun StaffAgendaTab(
    citas: List<CitaDto>,
    canChangeStatus: Boolean,
    isUpdating: Boolean,
    onUpdateStatus: (Int, String, String) -> Unit
) {
    var onlyToday by remember { mutableStateOf(true) }
    var search by remember { mutableStateOf("") }

    val filtered = remember(citas, onlyToday, search) {
        citas
            .filter { cita -> !onlyToday || ClinicDateUtils.isToday(cita.fecha) }
            .filter { cita ->
                if (search.isBlank()) true
                else {
                    val term = search.trim()
                    val name = cita.paciente?.nombreCompleto ?: "Paciente #${cita.pacienteId}"
                    val cedula = cita.paciente?.cedula.orEmpty()
                    val tipo = cita.tipoCita.orEmpty()
                    name.contains(term, ignoreCase = true) ||
                        cedula.contains(term, ignoreCase = true) ||
                        tipo.contains(term, ignoreCase = true)
                }
            }
            .sortedBy { ClinicDateUtils.parseApiDateTime(it.fecha) }
    }

    LazyColumn(
        contentPadding = PaddingValues(top = 12.dp, bottom = 28.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text(
                    text = if (onlyToday) "Agenda de citas de hoy" else "Todas las citas",
                    style = MaterialTheme.typography.titleLarge,
                    color = TangerWineDark
                )
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    FilterChip(
                        selected = onlyToday,
                        onClick = { onlyToday = true },
                        label = { Text("Hoy (${citas.count { ClinicDateUtils.isToday(it.fecha) }})") },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = TangerWine,
                            selectedLabelColor = TangerWhite
                        )
                    )
                    FilterChip(
                        selected = !onlyToday,
                        onClick = { onlyToday = false },
                        label = { Text("Todas (${citas.size})") },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = TangerWine,
                            selectedLabelColor = TangerWhite
                        )
                    )
                }
                OutlinedTextField(
                    value = search,
                    onValueChange = { search = it },
                    placeholder = { Text("Buscar por paciente, cédula o motivo...") },
                    leadingIcon = { Icon(Icons.Default.Search, contentDescription = null, tint = TangerWine) },
                    singleLine = true,
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier.fillMaxWidth()
                )
            }
        }

        if (filtered.isEmpty()) {
            item {
                EmptyStateCard(
                    icon = Icons.Default.CalendarToday,
                    title = if (onlyToday) "Sin citas para hoy" else "Sin citas registradas",
                    subtitle = if (onlyToday) "Puedes cambiar al filtro \"Todas\" para consultar el calendario completo." else "No se encontraron citas con ese criterio."
                )
            }
        } else {
            items(filtered, key = { it.citaId }) { cita ->
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
                            text = cita.paciente?.nombreCompleto ?: "Paciente #${cita.pacienteId}",
                            style = MaterialTheme.typography.titleMedium
                        )
                        Text(
                            text = "${cita.tipoCita ?: "Cita"} · Pago: ${cita.estadoPago ?: "Pendiente"}",
                            style = MaterialTheme.typography.bodyMedium,
                            color = TangerMuted
                        )
                        if (!cita.observaciones.isNullOrBlank()) {
                            Text(cita.observaciones, style = MaterialTheme.typography.bodyMedium)
                        }

                        if (canChangeStatus) {
                            HorizontalDivider(color = TangerBorder)
                            Text(
                                text = "Cambiar estado:",
                                style = MaterialTheme.typography.labelMedium,
                                color = TangerMuted
                            )
                            FlowRow(
                                horizontalArrangement = Arrangement.spacedBy(8.dp),
                                verticalArrangement = Arrangement.spacedBy(6.dp)
                            ) {
                                OutlinedButton(
                                    onClick = { onUpdateStatus(cita.citaId, "Llegó", "En sala de espera") },
                                    enabled = !isUpdating && !cita.estado.equals("Llegó", ignoreCase = true),
                                    shape = RoundedCornerShape(10.dp),
                                    border = BorderStroke(1.dp, StatusAmberText.copy(alpha = 0.6f)),
                                    contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp)
                                ) {
                                    Text("En sala de espera", color = StatusAmberText, style = MaterialTheme.typography.labelMedium)
                                }
                                OutlinedButton(
                                    onClick = { onUpdateStatus(cita.citaId, "Atendida", "Atendida") },
                                    enabled = !isUpdating && !cita.estado.equals("Atendida", ignoreCase = true),
                                    shape = RoundedCornerShape(10.dp),
                                    border = BorderStroke(1.dp, StatusGreenText.copy(alpha = 0.6f)),
                                    contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp)
                                ) {
                                    Text("Atendida", color = StatusGreenText, style = MaterialTheme.typography.labelMedium)
                                }
                                OutlinedButton(
                                    onClick = { onUpdateStatus(cita.citaId, "Cancelada", "Cancelada") },
                                    enabled = !isUpdating && !cita.estado.equals("Cancelada", ignoreCase = true),
                                    shape = RoundedCornerShape(10.dp),
                                    border = BorderStroke(1.dp, StatusRedText.copy(alpha = 0.6f)),
                                    contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp)
                                ) {
                                    Text("Cancelada", color = StatusRedText, style = MaterialTheme.typography.labelMedium)
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
private fun StaffPacientesTab(
    pacientes: List<PacienteDto>,
    showInsuranceBadge: Boolean
) {
    var search by remember { mutableStateOf("") }
    var onlyWithInsurance by remember { mutableStateOf(false) }

    val filtered = remember(pacientes, search, onlyWithInsurance) {
        pacientes
            .filter { p -> !onlyWithInsurance || (p.tieneSeguro == true) }
            .filter { p ->
                if (search.isBlank()) true
                else {
                    val term = search.trim()
                    p.nombreCompleto.contains(term, ignoreCase = true) ||
                        p.cedula.orEmpty().contains(term, ignoreCase = true) ||
                        p.telefono.orEmpty().contains(term, ignoreCase = true)
                }
            }
            .sortedBy { it.nombreCompleto.lowercase() }
    }

    LazyColumn(
        contentPadding = PaddingValues(top = 12.dp, bottom = 28.dp),
        verticalArrangement = Arrangement.spacedBy(10.dp)
    ) {
        item {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text("Directorio de Pacientes", style = MaterialTheme.typography.titleLarge, color = TangerWineDark)
                OutlinedTextField(
                    value = search,
                    onValueChange = { search = it },
                    placeholder = { Text("Buscar por nombre, cédula o teléfono...") },
                    leadingIcon = { Icon(Icons.Default.Search, contentDescription = null, tint = TangerWine) },
                    singleLine = true,
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier.fillMaxWidth()
                )
                if (showInsuranceBadge) {
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        FilterChip(
                            selected = !onlyWithInsurance,
                            onClick = { onlyWithInsurance = false },
                            label = { Text("Todos (${pacientes.size})") },
                            colors = FilterChipDefaults.filterChipColors(
                                selectedContainerColor = TangerWine,
                                selectedLabelColor = TangerWhite
                            )
                        )
                        FilterChip(
                            selected = onlyWithInsurance,
                            onClick = { onlyWithInsurance = true },
                            label = { Text("Con seguro (${pacientes.count { it.tieneSeguro == true }})") },
                            colors = FilterChipDefaults.filterChipColors(
                                selectedContainerColor = TangerWine,
                                selectedLabelColor = TangerWhite
                            )
                        )
                    }
                }
            }
        }

        if (filtered.isEmpty()) {
            item {
                EmptyStateCard(
                    icon = Icons.Default.People,
                    title = "Sin pacientes encontrados",
                    subtitle = "Ajusta el buscador o los filtros para ver resultados."
                )
            }
        } else {
            items(filtered, key = { it.pacienteId }) { paciente ->
                Card(
                    colors = CardDefaults.cardColors(containerColor = TangerWhite),
                    border = BorderStroke(1.dp, TangerBorder),
                    shape = RoundedCornerShape(14.dp)
                ) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(14.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Box(
                            modifier = Modifier
                                .size(42.dp)
                                .background(TangerBlush, CircleShape),
                            contentAlignment = Alignment.Center
                        ) {
                            val initials = "${paciente.nombre.take(1)}${paciente.apellido.take(1)}".uppercase()
                            Text(initials.ifBlank { "P" }, color = TangerWineDark, fontWeight = FontWeight.Bold)
                        }
                        Spacer(modifier = Modifier.width(12.dp))
                        Column(modifier = Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(2.dp)) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(
                                    text = paciente.nombreCompleto,
                                    style = MaterialTheme.typography.titleMedium,
                                    modifier = Modifier.weight(1f)
                                )
                                if (showInsuranceBadge && paciente.tieneSeguro != null) {
                                    InsuranceBadge(tieneSeguro = paciente.tieneSeguro)
                                }
                            }
                            Text(
                                text = "Cédula: ${paciente.cedula ?: "—"} · ID #${paciente.pacienteId}",
                                style = MaterialTheme.typography.bodyMedium,
                                color = TangerMuted
                            )
                            val contacto = listOfNotNull(
                                paciente.telefono?.takeIf { it.isNotBlank() },
                                paciente.email?.takeIf { it.isNotBlank() }
                            ).joinToString(" · ")
                            if (contacto.isNotBlank()) {
                                Text(contacto, style = MaterialTheme.typography.bodyMedium, color = TangerMuted)
                            }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun StaffConsultasTab(
    consultas: List<ConsultaDto>,
    state: StaffPanelUiState
) {
    var search by remember { mutableStateOf("") }

    val filtered = remember(consultas, search, state.pacientes) {
        consultas
            .filter { c ->
                if (search.isBlank()) true
                else {
                    val term = search.trim()
                    val name = c.paciente?.nombreCompleto ?: state.patientNameById(c.pacienteId)
                    val cedula = c.paciente?.cedula ?: state.patientCedulaById(c.pacienteId).orEmpty()
                    name.contains(term, ignoreCase = true) ||
                        cedula.contains(term, ignoreCase = true) ||
                        c.motivo.orEmpty().contains(term, ignoreCase = true) ||
                        c.diagnostico.orEmpty().contains(term, ignoreCase = true)
                }
            }
            .sortedByDescending { ClinicDateUtils.parseApiDateTime(it.fecha) }
    }

    LazyColumn(
        contentPadding = PaddingValues(top = 12.dp, bottom = 88.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text("Consultas Clínicas", style = MaterialTheme.typography.titleLarge, color = TangerWineDark)
                OutlinedTextField(
                    value = search,
                    onValueChange = { search = it },
                    placeholder = { Text("Buscar por paciente, cédula, motivo o diagnóstico...") },
                    leadingIcon = { Icon(Icons.Default.Search, contentDescription = null, tint = TangerWine) },
                    singleLine = true,
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier.fillMaxWidth()
                )
            }
        }

        if (filtered.isEmpty()) {
            item {
                EmptyStateCard(
                    icon = Icons.Default.MedicalServices,
                    title = "Sin consultas registradas",
                    subtitle = "Usa el botón + para registrar una nueva consulta clínica."
                )
            }
        } else {
            items(filtered, key = { it.consultaId }) { consulta ->
                val patientName = consulta.paciente?.nombreCompleto ?: state.patientNameById(consulta.pacienteId)
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
                                text = ClinicDateUtils.formatDateTime(consulta.fecha),
                                style = MaterialTheme.typography.labelLarge,
                                color = TangerWine
                            )
                            if (consulta.citaId != null) {
                                Text(
                                    text = "Cita #${consulta.citaId}",
                                    style = MaterialTheme.typography.labelMedium,
                                    color = StatusGreenText
                                )
                            }
                        }
                        Text(patientName, style = MaterialTheme.typography.titleMedium, color = TangerWineDark)
                        Surface(
                            color = TangerSurfaceTint,
                            shape = RoundedCornerShape(10.dp),
                            border = BorderStroke(1.dp, TangerBorder),
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Column(
                                modifier = Modifier.padding(10.dp),
                                verticalArrangement = Arrangement.spacedBy(4.dp)
                            ) {
                                Text(
                                    text = "Motivo: ${consulta.motivo?.ifBlank { "—" } ?: "—"}",
                                    style = MaterialTheme.typography.bodyMedium,
                                    fontWeight = FontWeight.SemiBold
                                )
                                Text(
                                    text = "Diagnóstico: ${consulta.diagnostico?.ifBlank { "Sin diagnóstico" } ?: "Sin diagnóstico"}",
                                    style = MaterialTheme.typography.bodyMedium
                                )
                                if (!consulta.observaciones.isNullOrBlank()) {
                                    Text(
                                        text = "Evaluación: ${consulta.observaciones}",
                                        style = MaterialTheme.typography.bodyMedium,
                                        color = TangerMuted
                                    )
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
private fun StaffRecetasTab(
    recetas: List<RecetaDto>,
    state: StaffPanelUiState
) {
    var search by remember { mutableStateOf("") }

    val filtered = remember(recetas, search, state.pacientes) {
        recetas.filter { r ->
            if (search.isBlank()) true
            else {
                val term = search.trim()
                val patientName = state.patientNameById(r.pacienteId)
                val meds = r.detallesReceta.joinToString(" ") { it.medicamento }
                patientName.contains(term, ignoreCase = true) ||
                    meds.contains(term, ignoreCase = true) ||
                    r.indicaciones.orEmpty().contains(term, ignoreCase = true)
            }
        }
    }

    LazyColumn(
        contentPadding = PaddingValues(top = 12.dp, bottom = 28.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        item {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text("Recetas Médicas", style = MaterialTheme.typography.titleLarge, color = TangerWineDark)
                OutlinedTextField(
                    value = search,
                    onValueChange = { search = it },
                    placeholder = { Text("Buscar por paciente o medicamento...") },
                    leadingIcon = { Icon(Icons.Default.Search, contentDescription = null, tint = TangerWine) },
                    singleLine = true,
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier.fillMaxWidth()
                )
            }
        }

        if (filtered.isEmpty()) {
            item {
                EmptyStateCard(
                    icon = Icons.Default.Medication,
                    title = "Sin recetas encontradas",
                    subtitle = "No hay recetas médicas registradas para ese criterio."
                )
            }
        } else {
            items(filtered, key = { it.recetaId }) { receta ->
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
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = state.patientNameById(receta.pacienteId),
                                style = MaterialTheme.typography.titleMedium,
                                color = TangerWineDark
                            )
                            Text(
                                text = ClinicDateUtils.formatDate(receta.fecha),
                                style = MaterialTheme.typography.labelMedium,
                                color = TangerWine
                            )
                        }
                        if (!receta.indicaciones.isNullOrBlank()) {
                            Text(receta.indicaciones, style = MaterialTheme.typography.bodyMedium)
                        }
                        receta.detallesReceta.forEach { d ->
                            Surface(
                                color = TangerSurfaceTint,
                                shape = RoundedCornerShape(10.dp),
                                border = BorderStroke(1.dp, TangerBorder),
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Column(modifier = Modifier.padding(10.dp)) {
                                    Text(d.medicamento, fontWeight = FontWeight.Bold, color = TangerWineDark)
                                    val info = listOfNotNull(d.dosis, d.frecuencia, d.duracion)
                                        .filter { it.isNotBlank() }
                                        .joinToString(" · ")
                                    if (info.isNotBlank()) {
                                        Text(info, style = MaterialTheme.typography.bodyMedium, color = TangerMuted)
                                    }
                                    if (!d.instrucciones.isNullOrBlank()) {
                                        Text(d.instrucciones, style = MaterialTheme.typography.bodyMedium)
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
private fun NotificationsDialog(
    notifications: List<NotificacionDto>,
    isLoading: Boolean,
    onDismiss: () -> Unit,
    onMarkRead: (Int) -> Unit,
    onMarkAllRead: () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        containerColor = TangerWhite,
        title = {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text("Notificaciones", color = TangerWineDark, fontWeight = FontWeight.Bold)
                if (notifications.any { !it.leida }) {
                    TextButton(onClick = onMarkAllRead) {
                        Icon(Icons.Default.DoneAll, contentDescription = null, modifier = Modifier.size(16.dp), tint = TangerWine)
                        Spacer(modifier = Modifier.width(4.dp))
                        Text("Leer todas", color = TangerWine)
                    }
                }
            }
        },
        text = {
            if (isLoading) {
                LoadingState("Cargando notificaciones...")
            } else if (notifications.isEmpty()) {
                Text("No tienes notificaciones registradas.", color = TangerMuted)
            } else {
                LazyColumn(
                    modifier = Modifier.heightIn(max = 380.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    items(notifications, key = { it.notificacionId }) { notif ->
                        Surface(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable {
                                    if (!notif.leida) onMarkRead(notif.notificacionId)
                                },
                            color = if (notif.leida) TangerWhite else TangerBlush.copy(alpha = 0.55f),
                            shape = RoundedCornerShape(12.dp),
                            border = BorderStroke(1.dp, if (notif.leida) TangerBorder else TangerWine)
                        ) {
                            Column(
                                modifier = Modifier.padding(12.dp),
                                verticalArrangement = Arrangement.spacedBy(4.dp)
                            ) {
                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Text(
                                        text = notif.titulo,
                                        style = MaterialTheme.typography.titleMedium,
                                        color = TangerWineDark,
                                        modifier = Modifier.weight(1f)
                                    )
                                    if (notif.leida) {
                                        Icon(
                                            Icons.Default.CheckCircle,
                                            contentDescription = "Leída",
                                            tint = StatusGreenText,
                                            modifier = Modifier.size(16.dp)
                                        )
                                    }
                                }
                                Text(notif.mensaje, style = MaterialTheme.typography.bodyMedium)
                                Text(
                                    text = ClinicDateUtils.formatDateTime(notif.fechaCreacion),
                                    style = MaterialTheme.typography.labelMedium,
                                    color = TangerMuted
                                )
                            }
                        }
                    }
                }
            }
        },
        confirmButton = {
            TextButton(onClick = onDismiss) {
                Text("Cerrar", color = TangerWine, fontWeight = FontWeight.Bold)
            }
        }
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun NewConsultaDialog(
    pacientes: List<PacienteDto>,
    isSubmitting: Boolean,
    onDismiss: () -> Unit,
    onConfirm: (Int, String, String, String) -> Unit
) {
    var expanded by remember { mutableStateOf(false) }
    var selectedPaciente by remember { mutableStateOf<PacienteDto?>(null) }
    var motivo by remember { mutableStateOf("") }
    var diagnostico by remember { mutableStateOf("") }
    var observaciones by remember { mutableStateOf("") }

    AlertDialog(
        onDismissRequest = onDismiss,
        containerColor = TangerWhite,
        title = { Text("Registrar nueva consulta", color = TangerWineDark, fontWeight = FontWeight.Bold) },
        text = {
            Column(
                modifier = Modifier.verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                ExposedDropdownMenuBox(
                    expanded = expanded,
                    onExpandedChange = { expanded = !expanded }
                ) {
                    OutlinedTextField(
                        value = selectedPaciente?.nombreCompleto ?: "-- Selecciona un paciente --",
                        onValueChange = {},
                        readOnly = true,
                        label = { Text("Paciente *") },
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
                        modifier = Modifier
                            .menuAnchor(MenuAnchorType.PrimaryNotEditable)
                            .fillMaxWidth()
                    )
                    ExposedDropdownMenu(
                        expanded = expanded,
                        onDismissRequest = { expanded = false }
                    ) {
                        pacientes.forEach { paciente ->
                            DropdownMenuItem(
                                text = {
                                    Text(
                                        paciente.nombreCompleto +
                                            if (!paciente.cedula.isNullOrBlank()) " · ${paciente.cedula}" else ""
                                    )
                                },
                                onClick = {
                                    selectedPaciente = paciente
                                    expanded = false
                                }
                            )
                        }
                    }
                }

                OutlinedTextField(
                    value = motivo,
                    onValueChange = { if (it.length <= 250) motivo = it },
                    label = { Text("Motivo de consulta * (${motivo.length}/250)") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = diagnostico,
                    onValueChange = { diagnostico = it },
                    label = { Text("Diagnóstico") },
                    maxLines = 3,
                    modifier = Modifier.fillMaxWidth()
                )
                OutlinedTextField(
                    value = observaciones,
                    onValueChange = { observaciones = it },
                    label = { Text("Observaciones / Evaluación clínica") },
                    maxLines = 4,
                    modifier = Modifier.fillMaxWidth()
                )
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    onConfirm(selectedPaciente?.pacienteId ?: 0, motivo, diagnostico, observaciones)
                },
                enabled = !isSubmitting,
                colors = ButtonDefaults.buttonColors(containerColor = TangerWine)
            ) {
                Text(if (isSubmitting) "Guardando..." else "Guardar consulta")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss, enabled = !isSubmitting) {
                Text("Cancelar", color = TangerMuted)
            }
        }
    )
}
