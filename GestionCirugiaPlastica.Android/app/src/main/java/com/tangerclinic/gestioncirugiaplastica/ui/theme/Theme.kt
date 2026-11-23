package com.tangerclinic.gestioncirugiaplastica.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val TangerLightColorScheme = lightColorScheme(
    primary = TangerWine,
    onPrimary = TangerWhite,
    primaryContainer = TangerBlush,
    onPrimaryContainer = TangerWineDark,
    secondary = TangerMauve,
    onSecondary = TangerWhite,
    secondaryContainer = TangerSurfaceTint,
    onSecondaryContainer = TangerWineDark,
    tertiary = TangerRose,
    onTertiary = TangerWhite,
    background = TangerBackground,
    onBackground = TangerCharcoal,
    surface = TangerWhite,
    onSurface = TangerCharcoal,
    surfaceVariant = TangerSurfaceTint,
    onSurfaceVariant = TangerMuted,
    outline = TangerBorder,
    outlineVariant = TangerBorder,
    error = StatusRedText,
    onError = TangerWhite,
    errorContainer = StatusRedBg,
    onErrorContainer = StatusRedText
)

@Composable
fun TangerClinicTheme(
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = TangerLightColorScheme,
        typography = TangerTypography,
        content = content
    )
}
