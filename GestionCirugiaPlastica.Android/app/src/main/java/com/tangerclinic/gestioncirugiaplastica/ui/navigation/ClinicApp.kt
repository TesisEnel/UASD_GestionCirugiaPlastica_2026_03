package com.tangerclinic.gestioncirugiaplastica.ui.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.tangerclinic.gestioncirugiaplastica.data.network.SessionManager
import com.tangerclinic.gestioncirugiaplastica.ui.login.LoginScreen
import com.tangerclinic.gestioncirugiaplastica.ui.patient.PatientPortalScreen
import com.tangerclinic.gestioncirugiaplastica.ui.staff.StaffPanelScreen

private object Routes {
    const val LOGIN = "login"
    const val PATIENT_PORTAL = "patient_portal"
    const val STAFF_PANEL = "staff_panel"
}

@Composable
fun ClinicApp() {
    val navController = rememberNavController()
    val session by SessionManager.session.collectAsState()

    val targetRoute = when {
        !session.hasEnteredApp -> Routes.LOGIN
        session.role?.isStaff == true -> Routes.STAFF_PANEL
        else -> Routes.PATIENT_PORTAL
    }

    LaunchedEffect(targetRoute) {
        val current = navController.currentDestination?.route
        if (current != targetRoute) {
            navController.navigate(targetRoute) {
                popUpTo(0) { inclusive = true }
                launchSingleTop = true
            }
        }
    }

    NavHost(
        navController = navController,
        startDestination = Routes.LOGIN
    ) {
        composable(Routes.LOGIN) {
            LoginScreen()
        }
        composable(Routes.PATIENT_PORTAL) {
            PatientPortalScreen(session = session)
        }
        composable(Routes.STAFF_PANEL) {
            StaffPanelScreen(session = session)
        }
    }
}
