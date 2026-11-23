package com.tangerclinic.gestioncirugiaplastica.data.network

import android.content.Context
import android.content.SharedPreferences
import com.google.gson.GsonBuilder
import com.tangerclinic.gestioncirugiaplastica.BuildConfig
import okhttp3.HttpUrl
import okhttp3.HttpUrl.Companion.toHttpUrl
import okhttp3.HttpUrl.Companion.toHttpUrlOrNull
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.io.IOException
import java.security.SecureRandom
import java.security.cert.X509Certificate
import java.util.concurrent.TimeUnit
import java.util.concurrent.atomic.AtomicReference
import javax.net.ssl.HostnameVerifier
import javax.net.ssl.SSLContext
import javax.net.ssl.TrustManager
import javax.net.ssl.X509TrustManager

/**
 * Cliente Retrofit/OkHttp con detección automática USB (adb reverse) -> Wi-Fi:
 * 1. Prueba primero https://localhost:7214/ y http://localhost:5258/ (timeout corto de 2s).
 * 2. Si localhost no responde, usa automáticamente la IP Wi-Fi guardada en SharedPreferences
 *    (https://<IP>:7214/ y http://<IP>:5258/).
 * - Confía en certificados autofirmados locales.
 * - Adjunta automáticamente Authorization: Bearer <token> tras iniciar sesión.
 */
object ApiClient {

    private const val PREFS_NAME = "tanger_clinic_network_prefs"
    private const val KEY_WIFI_HOST = "wifi_server_host"
    private const val USB_CONNECT_TIMEOUT_SECONDS = 2
    private const val WIFI_CONNECT_TIMEOUT_SECONDS = 10

    private val localHttpsUrl = BuildConfig.API_HTTPS_BASE_URL.toHttpUrl()
    private val localHttpUrl = BuildConfig.API_HTTP_BASE_URL.toHttpUrl()

    val defaultWifiHost: String = BuildConfig.DEFAULT_WIFI_IP

    @Volatile
    private var prefs: SharedPreferences? = null

    private val cachedEndpoint = AtomicReference<HttpUrl?>(null)

    private data class EndpointCandidate(
        val baseUrl: HttpUrl,
        val connectTimeoutSeconds: Int
    )

    fun init(context: Context) {
        if (prefs == null) {
            prefs = context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        }
    }

    fun getWifiHost(): String {
        val saved = prefs?.getString(KEY_WIFI_HOST, null)?.trim()
        return if (!saved.isNullOrBlank()) saved else defaultWifiHost
    }

    fun setWifiHost(rawInput: String): String {
        val sanitized = sanitizeHost(rawInput).ifBlank { defaultWifiHost }
        prefs?.edit()?.putString(KEY_WIFI_HOST, sanitized)?.apply()
        resetEndpointPreference()
        return sanitized
    }

    fun resetWifiHost(): String {
        prefs?.edit()?.remove(KEY_WIFI_HOST)?.apply()
        resetEndpointPreference()
        return defaultWifiHost
    }

    private fun sanitizeHost(rawInput: String): String {
        val trimmed = rawInput.trim()
        if (trimmed.isBlank()) return ""
        val asUrl = if (trimmed.contains("://")) trimmed else "http://$trimmed"
        return asUrl.toHttpUrlOrNull()?.host
            ?: trimmed
                .removePrefix("https://")
                .removePrefix("http://")
                .substringBefore("/")
                .substringBefore(":")
                .trim()
    }

    private fun buildCandidates(): List<EndpointCandidate> {
        val wifiHost = getWifiHost()
        val list = mutableListOf(
            EndpointCandidate(localHttpsUrl, USB_CONNECT_TIMEOUT_SECONDS),
            EndpointCandidate(localHttpUrl, USB_CONNECT_TIMEOUT_SECONDS)
        )
        "https://$wifiHost:7214/".toHttpUrlOrNull()?.let {
            list.add(EndpointCandidate(it, WIFI_CONNECT_TIMEOUT_SECONDS))
        }
        "http://$wifiHost:5258/".toHttpUrlOrNull()?.let {
            list.add(EndpointCandidate(it, WIFI_CONNECT_TIMEOUT_SECONDS))
        }
        return list
    }

    val activeBaseUrl: String
        get() = (cachedEndpoint.get() ?: localHttpsUrl).toString()

    fun resetEndpointPreference() {
        cachedEndpoint.set(null)
    }

    private val authBearerInterceptor = Interceptor { chain ->
        val original = chain.request()
        val path = original.url.encodedPath
        val isAnonymousAuth =
            path.endsWith("/api/Auth/login", ignoreCase = true) ||
                path.endsWith("/api/Auth/register", ignoreCase = true)

        val token = SessionManager.currentToken
        val request = if (!isAnonymousAuth && !token.isNullOrBlank()) {
            original.newBuilder()
                .header("Authorization", "Bearer $token")
                .build()
        } else {
            original
        }
        chain.proceed(request)
    }

    private val autoTransportInterceptor = Interceptor { chain ->
        val original = chain.request()
        val currentCached = cachedEndpoint.get()

        if (currentCached != null) {
            try {
                return@Interceptor chain.proceed(rewriteToEndpoint(original, currentCached))
            } catch (_: IOException) {
                cachedEndpoint.compareAndSet(currentCached, null)
            }
        }

        var lastError: IOException? = null
        for (candidate in buildCandidates()) {
            val candidateRequest = rewriteToEndpoint(original, candidate.baseUrl)
            val timedChain = chain.withConnectTimeout(candidate.connectTimeoutSeconds, TimeUnit.SECONDS)
            try {
                val response = timedChain.proceed(candidateRequest)
                cachedEndpoint.set(candidate.baseUrl)
                return@Interceptor response
            } catch (ioEx: IOException) {
                lastError = ioEx
            }
        }

        throw lastError ?: IOException("No se pudo conectar con ningún servidor disponible.")
    }

    private fun rewriteToEndpoint(request: Request, endpoint: HttpUrl): Request {
        val newUrl = request.url.newBuilder()
            .scheme(endpoint.scheme)
            .host(endpoint.host)
            .port(endpoint.port)
            .build()
        return request.newBuilder().url(newUrl).build()
    }

    private fun createDevOkHttpClient(): OkHttpClient {
        val trustAllDevCerts = object : X509TrustManager {
            override fun checkClientTrusted(chain: Array<out X509Certificate>?, authType: String?) = Unit
            override fun checkServerTrusted(chain: Array<out X509Certificate>?, authType: String?) = Unit
            override fun getAcceptedIssuers(): Array<X509Certificate> = emptyArray()
        }

        val sslContext = SSLContext.getInstance("TLS").apply {
            init(null, arrayOf<TrustManager>(trustAllDevCerts), SecureRandom())
        }

        val devHostnameVerifier = HostnameVerifier { hostname, _ ->
            hostname.equals("10.0.2.2", ignoreCase = true) ||
                hostname.equals("localhost", ignoreCase = true) ||
                hostname.equals("127.0.0.1", ignoreCase = true) ||
                hostname.equals(getWifiHost(), ignoreCase = true)
        }

        val logging = HttpLoggingInterceptor().apply {
            level = if (BuildConfig.DEBUG) {
                HttpLoggingInterceptor.Level.BASIC
            } else {
                HttpLoggingInterceptor.Level.NONE
            }
        }

        return OkHttpClient.Builder()
            .sslSocketFactory(sslContext.socketFactory, trustAllDevCerts)
            .hostnameVerifier(devHostnameVerifier)
            .addInterceptor(authBearerInterceptor)
            .addInterceptor(autoTransportInterceptor)
            .addInterceptor(logging)
            .connectTimeout(10, TimeUnit.SECONDS)
            .readTimeout(20, TimeUnit.SECONDS)
            .writeTimeout(20, TimeUnit.SECONDS)
            .build()
    }

    private val gson = GsonBuilder()
        .setLenient()
        .create()

    private val retrofit: Retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(BuildConfig.API_HTTPS_BASE_URL)
            .client(createDevOkHttpClient())
            .addConverterFactory(GsonConverterFactory.create(gson))
            .build()
    }

    val service: ClinicApiService by lazy {
        retrofit.create(ClinicApiService::class.java)
    }
}
