package com.tangerclinic.gestioncirugiaplastica.ui.staff

import android.Manifest
import android.content.ActivityNotFoundException
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AddAPhoto
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.CloudUpload
import androidx.compose.material.icons.filled.DeleteOutline
import androidx.compose.material.icons.filled.Image
import androidx.compose.material.icons.filled.PhotoCamera
import androidx.compose.material.icons.filled.PhotoLibrary
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.WarningAmber
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import androidx.core.content.ContextCompat
import com.tangerclinic.gestioncirugiaplastica.data.dto.CirugiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.FotografiaDto
import com.tangerclinic.gestioncirugiaplastica.data.dto.PacienteDto
import com.tangerclinic.gestioncirugiaplastica.ui.components.ClinicDateUtils
import com.tangerclinic.gestioncirugiaplastica.ui.components.EmptyStateCard
import com.tangerclinic.gestioncirugiaplastica.ui.components.FeedbackBanner
import com.tangerclinic.gestioncirugiaplastica.ui.components.StatusBadge
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusAmberBg
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusAmberText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusBlueBg
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusBlueText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusGreenBg
import com.tangerclinic.gestioncirugiaplastica.ui.theme.StatusGreenText
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBlush
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBorder
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerMuted
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerSurfaceTint
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWhite
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWine
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWineDark
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

@Composable
fun StaffFotografiasTab(
    state: StaffPanelUiState,
    onOpenCaptureModal: (Int?) -> Unit,
    onRequestThumbnail: (Int) -> Unit
) {
    var search by remember { mutableStateOf("") }
    var selectedFilter by remember { mutableStateOf<TipoFotografiaClinica?>(null) }

    val filtered = remember(state.fotografias, state.cirugias, state.pacientes, search, selectedFilter) {
        state.fotografias
            .filter { foto ->
                val detected = ClinicalPhotoHelper.detectTipoFromDescription(foto.descripcion)
                selectedFilter == null || detected == selectedFilter
            }
            .filter { foto ->
                if (search.isBlank()) true
                else {
                    val term = search.trim()
                    val cirugia = state.cirugiaById(foto.cirugiaId)
                    val patientName = state.patientNameForCirugia(foto.cirugiaId)
                    val proc = cirugia?.procedimientoNombre.orEmpty()
                    patientName.contains(term, ignoreCase = true) ||
                        proc.contains(term, ignoreCase = true) ||
                        foto.descripcion.orEmpty().contains(term, ignoreCase = true) ||
                        "cirugia #${foto.cirugiaId}".contains(term, ignoreCase = true)
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
                Text(
                    text = "Fotografías Clínicas",
                    style = MaterialTheme.typography.titleLarge,
                    color = TangerWineDark
                )
                Text(
                    text = "Captura y sube registros pre y postoperatorios directamente al expediente clínico.",
                    style = MaterialTheme.typography.bodyMedium,
                    color = TangerMuted
                )

                Button(
                    onClick = { onOpenCaptureModal(null) },
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = TangerWine,
                        contentColor = TangerWhite
                    ),
                    contentPadding = PaddingValues(vertical = 12.dp)
                ) {
                    Icon(Icons.Default.AddAPhoto, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Nueva Fotografía Clínica", fontWeight = FontWeight.Bold)
                }

                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    FilterChip(
                        selected = selectedFilter == null,
                        onClick = { selectedFilter = null },
                        label = { Text("Todas (${state.fotografias.size})") },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = TangerWine,
                            selectedLabelColor = TangerWhite
                        )
                    )
                    FilterChip(
                        selected = selectedFilter == TipoFotografiaClinica.PREOPERATORIA,
                        onClick = { selectedFilter = TipoFotografiaClinica.PREOPERATORIA },
                        label = { Text("Preoperatoria") },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = TangerWine,
                            selectedLabelColor = TangerWhite
                        )
                    )
                    FilterChip(
                        selected = selectedFilter == TipoFotografiaClinica.POSTOPERATORIA,
                        onClick = { selectedFilter = TipoFotografiaClinica.POSTOPERATORIA },
                        label = { Text("Postoperatoria") },
                        colors = FilterChipDefaults.filterChipColors(
                            selectedContainerColor = TangerWine,
                            selectedLabelColor = TangerWhite
                        )
                    )
                }

                OutlinedTextField(
                    value = search,
                    onValueChange = { search = it },
                    placeholder = { Text("Buscar por paciente, cirugía o descripción...") },
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
                    icon = Icons.Default.PhotoCamera,
                    title = "Sin fotografías clínicas",
                    subtitle = if (state.cirugias.isEmpty()) {
                        "Aún no hay cirugías registradas para adjuntar fotografías."
                    } else {
                        "Pulsa \"Nueva Fotografía Clínica\" para tomar una foto con la cámara o elegir una de la galería."
                    }
                )
            }
        } else {
            items(filtered, key = { it.fotografiaId }) { foto ->
                ClinicalPhotoHistoryCard(
                    foto = foto,
                    cirugia = state.cirugiaById(foto.cirugiaId),
                    patientName = state.patientNameForCirugia(foto.cirugiaId),
                    thumbnailBytes = state.photoThumbnails[foto.fotografiaId],
                    onRequestThumbnail = { onRequestThumbnail(foto.fotografiaId) }
                )
            }
        }
    }
}

@Composable
private fun ClinicalPhotoHistoryCard(
    foto: FotografiaDto,
    cirugia: CirugiaDto?,
    patientName: String,
    thumbnailBytes: ByteArray?,
    onRequestThumbnail: () -> Unit
) {
    LaunchedEffect(foto.fotografiaId, thumbnailBytes) {
        if (thumbnailBytes == null && foto.fotografiaId > 0) {
            onRequestThumbnail()
        }
    }

    var decodedBitmap by remember(foto.fotografiaId, thumbnailBytes) { mutableStateOf<Bitmap?>(null) }
    LaunchedEffect(thumbnailBytes) {
        if (thumbnailBytes != null) {
            decodedBitmap = withContext(Dispatchers.IO) {
                ClinicalPhotoHelper.decodeBytesToBitmap(thumbnailBytes, maxDimension = 900)
            }
        }
    }

    val tipo = ClinicalPhotoHelper.detectTipoFromDescription(foto.descripcion)

    Card(
        colors = CardDefaults.cardColors(containerColor = TangerWhite),
        border = BorderStroke(1.dp, TangerBorder),
        shape = RoundedCornerShape(16.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = ClinicDateUtils.formatDateTime(foto.fecha),
                    style = MaterialTheme.typography.labelLarge,
                    color = TangerWine
                )
                when (tipo) {
                    TipoFotografiaClinica.PREOPERATORIA -> StatusBadge(
                        label = "Preoperatoria",
                        backgroundColor = StatusAmberBg,
                        contentColor = StatusAmberText
                    )
                    TipoFotografiaClinica.POSTOPERATORIA -> StatusBadge(
                        label = "Postoperatoria",
                        backgroundColor = StatusGreenBg,
                        contentColor = StatusGreenText
                    )
                    null -> StatusBadge(
                        label = "Clínica",
                        backgroundColor = StatusBlueBg,
                        contentColor = StatusBlueText
                    )
                }
            }

            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(180.dp)
                    .clip(RoundedCornerShape(12.dp))
                    .background(TangerSurfaceTint),
                contentAlignment = Alignment.Center
            ) {
                val bmp = decodedBitmap
                if (bmp != null) {
                    Image(
                        bitmap = bmp.asImageBitmap(),
                        contentDescription = foto.descripcion ?: "Fotografía clínica",
                        contentScale = ContentScale.Crop,
                        modifier = Modifier.fillMaxSize()
                    )
                } else {
                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                        Icon(
                            imageVector = Icons.Default.Image,
                            contentDescription = null,
                            tint = TangerMuted,
                            modifier = Modifier.size(32.dp)
                        )
                        Spacer(modifier = Modifier.height(4.dp))
                        Text(
                            text = "Cargando vista previa...",
                            style = MaterialTheme.typography.labelMedium,
                            color = TangerMuted
                        )
                    }
                }
            }

            Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
                Text(
                    text = patientName,
                    style = MaterialTheme.typography.titleMedium,
                    color = TangerWineDark
                )
                Text(
                    text = cirugia?.let {
                        "${it.procedimientoNombre} · ${ClinicDateUtils.formatDate(it.fechaCirugia)} (Cirugía #${it.cirugiaId})"
                    } ?: "Cirugía #${foto.cirugiaId}",
                    style = MaterialTheme.typography.bodyMedium,
                    color = TangerMuted
                )
                Text(
                    text = ClinicalPhotoHelper.stripTipoPrefix(foto.descripcion),
                    style = MaterialTheme.typography.bodyMedium
                )
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ClinicalPhotoDialog(
    pacientes: List<PacienteDto>,
    cirugias: List<CirugiaDto>,
    initialPacienteId: Int?,
    isUploading: Boolean,
    errorMessage: String?,
    onClearError: () -> Unit,
    onDismiss: () -> Unit,
    onUpload: (cirugiaId: Int, imageUri: Uri, tipo: TipoFotografiaClinica, descripcion: String) -> Unit
) {
    val context = LocalContext.current

    var selectedPacienteId by rememberSaveable(initialPacienteId) {
        mutableStateOf(initialPacienteId)
    }
    var selectedCirugiaId by rememberSaveable(initialPacienteId) {
        mutableStateOf<Int?>(null)
    }
    var selectedTipoName by rememberSaveable {
        mutableStateOf(TipoFotografiaClinica.PREOPERATORIA.name)
    }
    var descripcion by rememberSaveable { mutableStateOf("") }
    var selectedImageUriString by rememberSaveable { mutableStateOf<String?>(null) }
    var pendingCameraUriString by rememberSaveable { mutableStateOf<String?>(null) }
    var imageSourceLabel by rememberSaveable { mutableStateOf<String?>(null) }
    var localError by remember { mutableStateOf<String?>(null) }

    var patientMenuExpanded by remember { mutableStateOf(false) }
    var surgeryMenuExpanded by remember { mutableStateOf(false) }
    var previewBitmap by remember { mutableStateOf<Bitmap?>(null) }
    var isDecodingPreview by remember { mutableStateOf(false) }

    val selectedTipo = remember(selectedTipoName) {
        TipoFotografiaClinica.entries.firstOrNull { it.name == selectedTipoName }
            ?: TipoFotografiaClinica.PREOPERATORIA
    }

    val sortedPatients = remember(pacientes) {
        pacientes.sortedBy { it.nombreCompleto.lowercase() }
    }

    val availableSurgeries = remember(cirugias, selectedPacienteId) {
        cirugias
            .filter { selectedPacienteId == null || it.pacienteId == selectedPacienteId }
            .sortedByDescending { ClinicDateUtils.parseApiDateTime(it.fechaCirugia) }
    }

    // Mantiene la cirugía seleccionada sincronizada con el filtro de paciente.
    LaunchedEffect(availableSurgeries) {
        if (availableSurgeries.none { it.cirugiaId == selectedCirugiaId }) {
            selectedCirugiaId = availableSurgeries.firstOrNull()?.cirugiaId
        }
    }

    val selectedPaciente = remember(sortedPatients, selectedPacienteId) {
        sortedPatients.firstOrNull { it.pacienteId == selectedPacienteId }
    }
    val selectedCirugia = remember(availableSurgeries, selectedCirugiaId) {
        availableSurgeries.firstOrNull { it.cirugiaId == selectedCirugiaId }
    }

    val selectedImageUri = remember(selectedImageUriString) {
        selectedImageUriString?.let { Uri.parse(it) }
    }

    LaunchedEffect(selectedImageUriString) {
        val uri = selectedImageUriString?.let { Uri.parse(it) }
        if (uri == null) {
            previewBitmap = null
            isDecodingPreview = false
        } else {
            isDecodingPreview = true
            val decoded = withContext(Dispatchers.IO) {
                ClinicalPhotoHelper.decodePreviewBitmap(context, uri, maxDimension = 1280)
            }
            previewBitmap = decoded
            isDecodingPreview = false
            if (decoded == null) {
                localError = "No se pudo mostrar la vista previa de la imagen seleccionada."
            }
        }
    }

    val takePictureLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.TakePicture()
    ) { success ->
        val capturedUri = pendingCameraUriString
        if (success && !capturedUri.isNullOrBlank()) {
            selectedImageUriString = capturedUri
            imageSourceLabel = "Cámara"
            localError = null
            onClearError()
        }
    }

    val launchCameraCapture: () -> Unit = {
        localError = null
        onClearError()
        try {
            val photoUri = ClinicalPhotoHelper.createCameraImageUri(context)
            pendingCameraUriString = photoUri.toString()
            takePictureLauncher.launch(photoUri)
        } catch (_: ActivityNotFoundException) {
            localError = "No se encontró una aplicación de cámara disponible en este dispositivo."
        } catch (ex: Exception) {
            localError = ex.message ?: "No se pudo abrir la cámara."
        }
    }

    val cameraPermissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestPermission()
    ) { granted ->
        if (granted) {
            launchCameraCapture()
        } else {
            localError = "Permiso de cámara denegado. Habilítalo para tomar fotografías clínicas."
        }
    }

    val galleryLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.GetContent()
    ) { uri ->
        if (uri != null) {
            selectedImageUriString = uri.toString()
            imageSourceLabel = "Galería"
            localError = null
            onClearError()
        }
    }

    val activeError = localError ?: errorMessage
    val maxUserDescription = ClinicalPhotoHelper.MAX_DESCRIPTION_LENGTH - selectedTipo.tag.length - 1

    Dialog(
        onDismissRequest = { if (!isUploading) onDismiss() },
        properties = DialogProperties(usePlatformDefaultWidth = false)
    ) {
        Surface(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 20.dp)
                .heightIn(max = 760.dp),
            shape = RoundedCornerShape(20.dp),
            color = TangerWhite,
            tonalElevation = 4.dp
        ) {
            Column(modifier = Modifier.fillMaxWidth()) {
                // Encabezado del modal
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(TangerWine)
                        .padding(horizontal = 18.dp, vertical = 14.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(10.dp),
                        modifier = Modifier.weight(1f)
                    ) {
                        Icon(
                            imageVector = Icons.Default.PhotoCamera,
                            contentDescription = null,
                            tint = TangerWhite
                        )
                        Column {
                            Text(
                                text = "Fotografía Clínica",
                                style = MaterialTheme.typography.titleLarge,
                                color = TangerWhite,
                                fontWeight = FontWeight.Bold
                            )
                            Text(
                                text = "Registro Pre / Postoperatorio en Expediente",
                                style = MaterialTheme.typography.labelMedium,
                                color = TangerBlush
                            )
                        }
                    }
                    IconButton(
                        onClick = { if (!isUploading) onDismiss() },
                        enabled = !isUploading
                    ) {
                        Icon(
                            imageVector = Icons.Default.Close,
                            contentDescription = "Cerrar",
                            tint = TangerWhite
                        )
                    }
                }

                // Contenido desplazable
                Column(
                    modifier = Modifier
                        .weight(1f, fill = false)
                        .verticalScroll(rememberScrollState())
                        .padding(18.dp),
                    verticalArrangement = Arrangement.spacedBy(14.dp)
                ) {
                    if (activeError != null) {
                        FeedbackBanner(
                            message = activeError,
                            isError = true,
                            onDismiss = {
                                localError = null
                                onClearError()
                            }
                        )
                    }

                    // Selector de Paciente
                    ExposedDropdownMenuBox(
                        expanded = patientMenuExpanded,
                        onExpandedChange = { if (!isUploading) patientMenuExpanded = !patientMenuExpanded }
                    ) {
                        OutlinedTextField(
                            value = selectedPaciente?.nombreCompleto ?: "Todos los pacientes",
                            onValueChange = {},
                            readOnly = true,
                            label = { Text("Paciente") },
                            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = patientMenuExpanded) },
                            shape = RoundedCornerShape(12.dp),
                            modifier = Modifier
                                .menuAnchor(MenuAnchorType.PrimaryNotEditable)
                                .fillMaxWidth()
                        )
                        ExposedDropdownMenu(
                            expanded = patientMenuExpanded,
                            onDismissRequest = { patientMenuExpanded = false }
                        ) {
                            DropdownMenuItem(
                                text = { Text("Todos los pacientes", fontWeight = FontWeight.SemiBold) },
                                onClick = {
                                    selectedPacienteId = null
                                    patientMenuExpanded = false
                                }
                            )
                            sortedPatients.forEach { paciente ->
                                DropdownMenuItem(
                                    text = {
                                        Text(
                                            paciente.nombreCompleto +
                                                if (!paciente.cedula.isNullOrBlank()) " · ${paciente.cedula}" else ""
                                        )
                                    },
                                    onClick = {
                                        selectedPacienteId = paciente.pacienteId
                                        patientMenuExpanded = false
                                    }
                                )
                            }
                        }
                    }

                    // Selector de Cirugía
                    if (availableSurgeries.isEmpty()) {
                        Surface(
                            color = StatusAmberBg,
                            shape = RoundedCornerShape(12.dp),
                            border = BorderStroke(1.dp, StatusAmberText.copy(alpha = 0.35f)),
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Row(
                                modifier = Modifier.padding(12.dp),
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.spacedBy(10.dp)
                            ) {
                                Icon(
                                    imageVector = Icons.Default.WarningAmber,
                                    contentDescription = null,
                                    tint = StatusAmberText
                                )
                                Text(
                                    text = if (selectedPaciente != null) {
                                        "${selectedPaciente.nombreCompleto} aún no tiene cirugías registradas. Selecciona otro paciente o registra primero la cirugía."
                                    } else {
                                        "No hay cirugías registradas en la API para asociar fotografías."
                                    },
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = StatusAmberText
                                )
                            }
                        }
                    } else {
                        ExposedDropdownMenuBox(
                            expanded = surgeryMenuExpanded,
                            onExpandedChange = { if (!isUploading) surgeryMenuExpanded = !surgeryMenuExpanded }
                        ) {
                            val surgeryLabel = selectedCirugia?.let { cirugia ->
                                val patientPart = if (selectedPaciente == null) {
                                    val pName = cirugia.paciente?.nombreCompleto
                                        ?: pacientes.firstOrNull { it.pacienteId == cirugia.pacienteId }?.nombreCompleto
                                        ?: "Paciente #${cirugia.pacienteId}"
                                    "$pName · "
                                } else ""
                                "$patientPart${cirugia.procedimientoNombre} (${ClinicDateUtils.formatDate(cirugia.fechaCirugia)})"
                            } ?: "-- Selecciona una cirugía --"

                            OutlinedTextField(
                                value = surgeryLabel,
                                onValueChange = {},
                                readOnly = true,
                                label = { Text("Cirugía *") },
                                trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = surgeryMenuExpanded) },
                                singleLine = true,
                                shape = RoundedCornerShape(12.dp),
                                modifier = Modifier
                                    .menuAnchor(MenuAnchorType.PrimaryNotEditable)
                                    .fillMaxWidth()
                            )
                            ExposedDropdownMenu(
                                expanded = surgeryMenuExpanded,
                                onDismissRequest = { surgeryMenuExpanded = false }
                            ) {
                                availableSurgeries.forEach { cirugia ->
                                    val patientName = cirugia.paciente?.nombreCompleto
                                        ?: pacientes.firstOrNull { it.pacienteId == cirugia.pacienteId }?.nombreCompleto
                                        ?: "Paciente #${cirugia.pacienteId}"
                                    DropdownMenuItem(
                                        text = {
                                            Column {
                                                Text(
                                                    text = "${cirugia.procedimientoNombre} · #${cirugia.cirugiaId}",
                                                    fontWeight = FontWeight.SemiBold,
                                                    maxLines = 1,
                                                    overflow = TextOverflow.Ellipsis
                                                )
                                                Text(
                                                    text = "$patientName · ${ClinicDateUtils.formatDate(cirugia.fechaCirugia)} · ${cirugia.estado}",
                                                    style = MaterialTheme.typography.labelMedium,
                                                    color = TangerMuted,
                                                    maxLines = 1,
                                                    overflow = TextOverflow.Ellipsis
                                                )
                                            }
                                        },
                                        onClick = {
                                            selectedCirugiaId = cirugia.cirugiaId
                                            if (selectedPacienteId == null) {
                                                selectedPacienteId = cirugia.pacienteId
                                            }
                                            surgeryMenuExpanded = false
                                        }
                                    )
                                }
                            }
                        }
                    }

                    // Tipo de fotografía: Preoperatoria / Postoperatoria
                    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                        Text(
                            text = "Tipo de fotografía *",
                            style = MaterialTheme.typography.labelLarge,
                            color = TangerWineDark
                        )
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(10.dp)
                        ) {
                            TipoFotografiaClinica.entries.forEach { tipo ->
                                val isSelected = selectedTipo == tipo
                                FilterChip(
                                    selected = isSelected,
                                    onClick = { if (!isUploading) selectedTipoName = tipo.name },
                                    label = {
                                        Text(
                                            text = tipo.label,
                                            fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium
                                        )
                                    },
                                    modifier = Modifier.weight(1f),
                                    colors = FilterChipDefaults.filterChipColors(
                                        selectedContainerColor = TangerWine,
                                        selectedLabelColor = TangerWhite,
                                        containerColor = TangerSurfaceTint,
                                        labelColor = TangerWineDark
                                    )
                                )
                            }
                        }
                    }

                    // Descripción breve
                    OutlinedTextField(
                        value = descripcion,
                        onValueChange = { if (it.length <= maxUserDescription) descripcion = it },
                        enabled = !isUploading,
                        label = { Text("Descripción breve (${descripcion.length}/$maxUserDescription)") },
                        placeholder = { Text("Ej. Perfil derecho, control a los 15 días...") },
                        maxLines = 3,
                        shape = RoundedCornerShape(12.dp),
                        modifier = Modifier.fillMaxWidth()
                    )

                    // Botones de Cámara y Galería
                    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Text(
                            text = "Origen de la imagen *",
                            style = MaterialTheme.typography.labelLarge,
                            color = TangerWineDark
                        )
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(10.dp)
                        ) {
                            OutlinedButton(
                                onClick = {
                                    val granted = ContextCompat.checkSelfPermission(
                                        context,
                                        Manifest.permission.CAMERA
                                    ) == PackageManager.PERMISSION_GRANTED
                                    if (granted) {
                                        launchCameraCapture()
                                    } else {
                                        cameraPermissionLauncher.launch(Manifest.permission.CAMERA)
                                    }
                                },
                                enabled = !isUploading,
                                modifier = Modifier.weight(1f),
                                shape = RoundedCornerShape(12.dp),
                                border = BorderStroke(1.dp, TangerWine),
                                contentPadding = PaddingValues(horizontal = 10.dp, vertical = 12.dp)
                            ) {
                                Icon(
                                    imageVector = Icons.Default.PhotoCamera,
                                    contentDescription = null,
                                    tint = TangerWine,
                                    modifier = Modifier.size(18.dp)
                                )
                                Spacer(modifier = Modifier.width(6.dp))
                                Text(
                                    text = "Tomar Foto con Cámara",
                                    color = TangerWine,
                                    style = MaterialTheme.typography.labelLarge,
                                    maxLines = 2
                                )
                            }

                            OutlinedButton(
                                onClick = {
                                    localError = null
                                    onClearError()
                                    galleryLauncher.launch("image/*")
                                },
                                enabled = !isUploading,
                                modifier = Modifier.weight(1f),
                                shape = RoundedCornerShape(12.dp),
                                border = BorderStroke(1.dp, TangerBorder),
                                contentPadding = PaddingValues(horizontal = 10.dp, vertical = 12.dp)
                            ) {
                                Icon(
                                    imageVector = Icons.Default.PhotoLibrary,
                                    contentDescription = null,
                                    tint = TangerWineDark,
                                    modifier = Modifier.size(18.dp)
                                )
                                Spacer(modifier = Modifier.width(6.dp))
                                Text(
                                    text = "Elegir de Galería",
                                    color = TangerWineDark,
                                    style = MaterialTheme.typography.labelLarge,
                                    maxLines = 2
                                )
                            }
                        }
                    }

                    // Vista previa de la imagen seleccionada
                    Surface(
                        color = TangerSurfaceTint,
                        shape = RoundedCornerShape(14.dp),
                        border = BorderStroke(1.dp, TangerBorder),
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Column(modifier = Modifier.padding(10.dp)) {
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .height(210.dp)
                                    .clip(RoundedCornerShape(10.dp))
                                    .background(TangerBlush.copy(alpha = 0.45f)),
                                contentAlignment = Alignment.Center
                            ) {
                                when {
                                    isDecodingPreview -> {
                                        CircularProgressIndicator(color = TangerWine)
                                    }
                                    previewBitmap != null -> {
                                        Image(
                                            bitmap = previewBitmap!!.asImageBitmap(),
                                            contentDescription = "Vista previa de fotografía clínica",
                                            contentScale = ContentScale.Crop,
                                            modifier = Modifier.fillMaxSize()
                                        )
                                    }
                                    else -> {
                                        Column(
                                            horizontalAlignment = Alignment.CenterHorizontally,
                                            verticalArrangement = Arrangement.spacedBy(6.dp),
                                            modifier = Modifier.padding(16.dp)
                                        ) {
                                            Icon(
                                                imageVector = Icons.Default.Image,
                                                contentDescription = null,
                                                tint = TangerWine,
                                                modifier = Modifier.size(38.dp)
                                            )
                                            Text(
                                                text = "Vista previa de la fotografía",
                                                style = MaterialTheme.typography.titleSmall,
                                                color = TangerWineDark,
                                                fontWeight = FontWeight.SemiBold
                                            )
                                            Text(
                                                text = "Toma una foto con la cámara o selecciona una imagen (JPEG, PNG o WebP, máx. 8 MB).",
                                                style = MaterialTheme.typography.bodySmall,
                                                color = TangerMuted
                                            )
                                        }
                                    }
                                }
                            }

                            if (selectedImageUri != null) {
                                Spacer(modifier = Modifier.height(8.dp))
                                Row(
                                    modifier = Modifier.fillMaxWidth(),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Text(
                                        text = "Origen: ${imageSourceLabel ?: "Imagen"} · ${selectedTipo.label}",
                                        style = MaterialTheme.typography.labelMedium,
                                        color = TangerWineDark,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                    TextButton(
                                        onClick = {
                                            selectedImageUriString = null
                                            previewBitmap = null
                                            imageSourceLabel = null
                                        },
                                        enabled = !isUploading,
                                        contentPadding = PaddingValues(horizontal = 8.dp, vertical = 2.dp)
                                    ) {
                                        Icon(
                                            imageVector = Icons.Default.DeleteOutline,
                                            contentDescription = null,
                                            tint = TangerMuted,
                                            modifier = Modifier.size(16.dp)
                                        )
                                        Spacer(modifier = Modifier.width(4.dp))
                                        Text("Quitar", color = TangerMuted)
                                    }
                                }
                            }
                        }
                    }
                }

                HorizontalDivider(color = TangerBorder)

                // Pie del modal: Cancelar y Guardar en Expediente
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 18.dp, vertical = 14.dp),
                    horizontalArrangement = Arrangement.spacedBy(10.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    TextButton(
                        onClick = onDismiss,
                        enabled = !isUploading
                    ) {
                        Text("Cancelar", color = TangerMuted)
                    }
                    Button(
                        onClick = {
                            val cirugiaId = selectedCirugia?.cirugiaId
                            val uri = selectedImageUri
                            if (cirugiaId == null) {
                                localError = "Selecciona una cirugía para guardar la fotografía."
                                return@Button
                            }
                            if (uri == null) {
                                localError = "Toma una fotografía o elige una imagen de la galería."
                                return@Button
                            }
                            localError = null
                            onUpload(cirugiaId, uri, selectedTipo, descripcion)
                        },
                        enabled = !isUploading && selectedCirugia != null && selectedImageUri != null,
                        modifier = Modifier.weight(1f),
                        shape = RoundedCornerShape(12.dp),
                        colors = ButtonDefaults.buttonColors(
                            containerColor = TangerWine,
                            contentColor = TangerWhite
                        ),
                        contentPadding = PaddingValues(vertical = 12.dp)
                    ) {
                        if (isUploading) {
                            CircularProgressIndicator(
                                color = TangerWhite,
                                strokeWidth = 2.dp,
                                modifier = Modifier.size(18.dp)
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Text("Subiendo...", fontWeight = FontWeight.Bold)
                        } else {
                            Icon(Icons.Default.CloudUpload, contentDescription = null, modifier = Modifier.size(18.dp))
                            Spacer(modifier = Modifier.width(8.dp))
                            Text("Guardar en Expediente", fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }
        }
    }
}
