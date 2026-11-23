package com.tangerclinic.gestioncirugiaplastica.ui.login

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Favorite
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Public
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.tangerclinic.gestioncirugiaplastica.data.network.ApiClient
import com.tangerclinic.gestioncirugiaplastica.ui.components.FeedbackBanner
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBackground
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBlush
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerBorder
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerMuted
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWhite
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWine
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerWineDark

@Composable
fun LoginScreen(
    viewModel: LoginViewModel = viewModel()
) {
    val state by viewModel.uiState.collectAsState()
    var showPassword by remember { mutableStateOf(false) }
    var showServerDialog by remember { mutableStateOf(false) }
    var wifiIpInput by remember { mutableStateOf(ApiClient.getWifiHost()) }

    Scaffold(containerColor = TangerBackground) { innerPadding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .imePadding()
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 22.dp, vertical = 28.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
            Box(
                modifier = Modifier
                    .size(72.dp)
                    .background(TangerWine, CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Default.Favorite,
                    contentDescription = null,
                    tint = TangerWhite,
                    modifier = Modifier.size(34.dp)
                )
            }

            Spacer(modifier = Modifier.height(14.dp))

            Text(
                text = "TANGER CLINIC",
                style = MaterialTheme.typography.headlineMedium,
                color = TangerWineDark,
                letterSpacing = 1.6.sp
            )
            Text(
                text = "Cirugía Plástica · Plataforma Móvil",
                style = MaterialTheme.typography.bodyMedium,
                color = TangerMuted
            )

            Spacer(modifier = Modifier.height(24.dp))

            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(20.dp),
                colors = CardDefaults.cardColors(containerColor = TangerWhite),
                border = BorderStroke(1.dp, TangerBorder),
                elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(22.dp),
                    verticalArrangement = Arrangement.spacedBy(14.dp)
                ) {
                    Text(
                        text = "Iniciar sesión",
                        style = MaterialTheme.typography.titleLarge,
                        color = TangerWineDark
                    )
                    Text(
                        text = "Accede como Doctora, Asistente Clínico, Secretaria o Paciente.",
                        style = MaterialTheme.typography.bodyMedium,
                        color = TangerMuted
                    )

                    if (state.errorMessage != null) {
                        FeedbackBanner(
                            message = state.errorMessage!!,
                            isError = true,
                            onDismiss = viewModel::clearError
                        )
                    }

                    OutlinedTextField(
                        value = state.identifier,
                        onValueChange = viewModel::onIdentifierChanged,
                        label = { Text("Usuario, correo, cédula o teléfono") },
                        leadingIcon = { Icon(Icons.Default.Person, contentDescription = null, tint = TangerWine) },
                        singleLine = true,
                        enabled = !state.isLoading,
                        keyboardOptions = KeyboardOptions(
                            keyboardType = KeyboardType.Email,
                            imeAction = ImeAction.Next
                        ),
                        shape = RoundedCornerShape(12.dp),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = TangerWine,
                            unfocusedBorderColor = TangerBorder,
                            focusedLabelColor = TangerWine
                        ),
                        modifier = Modifier.fillMaxWidth()
                    )

                    OutlinedTextField(
                        value = state.password,
                        onValueChange = viewModel::onPasswordChanged,
                        label = { Text("Contraseña") },
                        leadingIcon = { Icon(Icons.Default.Lock, contentDescription = null, tint = TangerWine) },
                        trailingIcon = {
                            IconButton(onClick = { showPassword = !showPassword }) {
                                Icon(
                                    imageVector = if (showPassword) Icons.Default.VisibilityOff else Icons.Default.Visibility,
                                    contentDescription = if (showPassword) "Ocultar contraseña" else "Mostrar contraseña",
                                    tint = TangerMuted
                                )
                            }
                        },
                        singleLine = true,
                        enabled = !state.isLoading,
                        visualTransformation = if (showPassword) VisualTransformation.None else PasswordVisualTransformation(),
                        keyboardOptions = KeyboardOptions(
                            keyboardType = KeyboardType.Password,
                            imeAction = ImeAction.Done
                        ),
                        keyboardActions = KeyboardActions(onDone = { viewModel.login() }),
                        shape = RoundedCornerShape(12.dp),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = TangerWine,
                            unfocusedBorderColor = TangerBorder,
                            focusedLabelColor = TangerWine
                        ),
                        modifier = Modifier.fillMaxWidth()
                    )

                    Button(
                        onClick = viewModel::login,
                        enabled = !state.isLoading,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(50.dp),
                        shape = RoundedCornerShape(12.dp),
                        colors = ButtonDefaults.buttonColors(
                            containerColor = TangerWine,
                            contentColor = TangerWhite
                        )
                    ) {
                        if (state.isLoading) {
                            CircularProgressIndicator(
                                color = TangerWhite,
                                strokeWidth = 2.dp,
                                modifier = Modifier.size(20.dp)
                            )
                            Spacer(modifier = Modifier.width(10.dp))
                            Text("Verificando credenciales...")
                        } else {
                            Text("Iniciar sesión", fontWeight = FontWeight.Bold)
                        }
                    }

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        HorizontalDivider(modifier = Modifier.weight(1f), color = TangerBorder)
                        Text(
                            text = "  o continuar sin cuenta  ",
                            style = MaterialTheme.typography.labelMedium,
                            color = TangerMuted
                        )
                        HorizontalDivider(modifier = Modifier.weight(1f), color = TangerBorder)
                    }

                    OutlinedButton(
                        onClick = viewModel::enterAsPublicVisitor,
                        enabled = !state.isLoading,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(48.dp),
                        shape = RoundedCornerShape(12.dp),
                        border = BorderStroke(1.dp, TangerWine),
                        colors = ButtonDefaults.outlinedButtonColors(
                            containerColor = TangerBlush.copy(alpha = 0.45f),
                            contentColor = TangerWineDark
                        )
                    ) {
                        Icon(Icons.Default.Public, contentDescription = null, modifier = Modifier.size(18.dp))
                        Spacer(modifier = Modifier.width(8.dp))
                        Text("Entrar como Visitante Público", fontWeight = FontWeight.SemiBold)
                    }
                }
            }

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                text = "Como visitante puedes solicitar o cancelar citas y consultar las noticias de la clínica.",
                style = MaterialTheme.typography.bodyMedium,
                color = TangerMuted,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(horizontal = 12.dp)
            )
            }

            TextButton(
                onClick = {
                    wifiIpInput = ApiClient.getWifiHost()
                    showServerDialog = true
                },
                modifier = Modifier
                    .align(Alignment.TopEnd)
                    .padding(top = 8.dp, end = 12.dp)
            ) {
                Icon(
                    imageVector = Icons.Default.Settings,
                    contentDescription = "Configurar servidor",
                    tint = TangerMuted,
                    modifier = Modifier.size(16.dp)
                )
                Spacer(modifier = Modifier.width(6.dp))
                Text(
                    text = "Servidor / IP",
                    style = MaterialTheme.typography.labelMedium,
                    color = TangerMuted
                )
            }
        }
    }

    if (showServerDialog) {
        AlertDialog(
            onDismissRequest = { showServerDialog = false },
            containerColor = TangerWhite,
            title = {
                Text(
                    text = "Configurar servidor Wi-Fi",
                    color = TangerWineDark,
                    style = MaterialTheme.typography.titleLarge
                )
            },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    Text(
                        text = "La app intenta conectar por cable USB (localhost) primero y, si no responde, usa esta IP Wi-Fi (puertos 7214 HTTPS / 5258 HTTP).",
                        style = MaterialTheme.typography.bodyMedium,
                        color = TangerMuted
                    )
                    OutlinedTextField(
                        value = wifiIpInput,
                        onValueChange = { wifiIpInput = it },
                        label = { Text("IP de la PC en Wi-Fi") },
                        placeholder = { Text(ApiClient.defaultWifiHost) },
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(
                            keyboardType = KeyboardType.Uri,
                            imeAction = ImeAction.Done
                        ),
                        shape = RoundedCornerShape(12.dp),
                        colors = OutlinedTextFieldDefaults.colors(
                            focusedBorderColor = TangerWine,
                            unfocusedBorderColor = TangerBorder,
                            focusedLabelColor = TangerWine
                        ),
                        modifier = Modifier.fillMaxWidth()
                    )
                    TextButton(
                        onClick = {
                            wifiIpInput = ApiClient.defaultWifiHost
                        }
                    ) {
                        Text(
                            text = "Restablecer predeterminada (${ApiClient.defaultWifiHost})",
                            color = TangerWine
                        )
                    }
                }
            },
            confirmButton = {
                Button(
                    onClick = {
                        ApiClient.setWifiHost(wifiIpInput)
                        showServerDialog = false
                    },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = TangerWine,
                        contentColor = TangerWhite
                    )
                ) {
                    Text("Guardar")
                }
            },
            dismissButton = {
                TextButton(onClick = { showServerDialog = false }) {
                    Text("Cancelar", color = TangerMuted)
                }
            }
        )
    }
}
