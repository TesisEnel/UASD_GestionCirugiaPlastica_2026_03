package com.tangerclinic.gestioncirugiaplastica

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.tangerclinic.gestioncirugiaplastica.data.network.ApiClient
import com.tangerclinic.gestioncirugiaplastica.ui.navigation.ClinicApp
import com.tangerclinic.gestioncirugiaplastica.ui.theme.TangerClinicTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        ApiClient.init(applicationContext)
        enableEdgeToEdge()
        setContent {
            TangerClinicTheme {
                ClinicApp()
            }
        }
    }
}
