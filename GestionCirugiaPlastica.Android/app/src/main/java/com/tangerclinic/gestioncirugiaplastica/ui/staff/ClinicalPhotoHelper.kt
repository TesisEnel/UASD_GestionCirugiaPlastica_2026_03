package com.tangerclinic.gestioncirugiaplastica.ui.staff

import android.content.Context
import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.graphics.ImageDecoder
import android.net.Uri
import android.os.Build
import androidx.core.content.FileProvider
import com.tangerclinic.gestioncirugiaplastica.BuildConfig
import java.io.ByteArrayOutputStream
import java.io.File
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import kotlin.math.max
import kotlin.math.roundToInt

enum class TipoFotografiaClinica(val label: String, val tag: String) {
    PREOPERATORIA("Preoperatoria", "[Preoperatoria]"),
    POSTOPERATORIA("Postoperatoria", "[Postoperatoria]")
}

data class PreparedPhotoUpload(
    val bytes: ByteArray,
    val fileName: String,
    val mimeType: String
)

object ClinicalPhotoHelper {

    const val MAX_UPLOAD_BYTES = 8 * 1024 * 1024 // 8 MB límite de FotografiasController
    const val MAX_DESCRIPTION_LENGTH = 250       // [StringLength(250)] en Fotografia.Descripcion
    private const val TARGET_COMPRESSED_BYTES = 7 * 1024 * 1024
    private const val MAX_UPLOAD_DIMENSION = 2048

    private val allowedMimeTypes = mapOf(
        "image/jpeg" to ".jpg",
        "image/png" to ".png",
        "image/webp" to ".webp"
    )

    fun createCameraImageUri(context: Context): Uri {
        val dir = File(context.cacheDir, "clinical_photos").apply { mkdirs() }
        val stamp = SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(Date())
        val imageFile = File(dir, "clinica_$stamp.jpg")
        return FileProvider.getUriForFile(
            context,
            "${BuildConfig.APPLICATION_ID}.fileprovider",
            imageFile
        )
    }

    fun formatClinicalDescription(tipo: TipoFotografiaClinica, rawDescription: String): String {
        val clean = rawDescription.trim()
        val combined = if (clean.isBlank()) {
            "${tipo.tag} Fotografía clínica"
        } else {
            "${tipo.tag} $clean"
        }
        return combined.take(MAX_DESCRIPTION_LENGTH)
    }

    fun detectTipoFromDescription(description: String?): TipoFotografiaClinica? {
        val normalized = description?.trim()?.lowercase(Locale.ROOT) ?: return null
        return when {
            normalized.startsWith("[preoperatoria]") || normalized.contains("preoperator") ->
                TipoFotografiaClinica.PREOPERATORIA
            normalized.startsWith("[postoperatoria]") || normalized.contains("postoperator") ->
                TipoFotografiaClinica.POSTOPERATORIA
            else -> null
        }
    }

    fun stripTipoPrefix(description: String?): String {
        if (description.isNullOrBlank()) return "Sin descripción"
        return description
            .replace(Regex("^\\[(Preoperatoria|Postoperatoria)]\\s*", RegexOption.IGNORE_CASE), "")
            .trim()
            .ifBlank { "Fotografía clínica" }
    }

    fun decodePreviewBitmap(context: Context, uri: Uri, maxDimension: Int = 1280): Bitmap? {
        return runCatching {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                val source = ImageDecoder.createSource(context.contentResolver, uri)
                ImageDecoder.decodeBitmap(source) { decoder, info, _ ->
                    decoder.allocator = ImageDecoder.ALLOCATOR_SOFTWARE
                    val width = info.size.width
                    val height = info.size.height
                    val largest = max(width, height)
                    if (largest > maxDimension && largest > 0) {
                        val scale = maxDimension.toFloat() / largest.toFloat()
                        decoder.setTargetSize(
                            (width * scale).roundToInt().coerceAtLeast(1),
                            (height * scale).roundToInt().coerceAtLeast(1)
                        )
                    }
                }
            } else {
                decodeLegacyBitmap(context, uri, maxDimension)
            }
        }.getOrNull()
    }

    fun decodeBytesToBitmap(bytes: ByteArray, maxDimension: Int = 1024): Bitmap? {
        if (bytes.isEmpty()) return null
        return runCatching {
            val bounds = BitmapFactory.Options().apply { inJustDecodeBounds = true }
            BitmapFactory.decodeByteArray(bytes, 0, bytes.size, bounds)
            val sampleSize = calculateInSampleSize(bounds.outWidth, bounds.outHeight, maxDimension)
            val opts = BitmapFactory.Options().apply { inSampleSize = sampleSize }
            BitmapFactory.decodeByteArray(bytes, 0, bytes.size, opts)
        }.getOrNull()
    }

    fun prepareForUpload(context: Context, uri: Uri): Result<PreparedPhotoUpload> {
        return runCatching {
            val resolver = context.contentResolver
            val detectedMime = resolver.getType(uri)?.lowercase(Locale.ROOT)
            val rawBytes = resolver.openInputStream(uri)?.use { it.readBytes() }
                ?: throw IllegalStateException("No se pudo leer la imagen seleccionada.")

            if (rawBytes.isEmpty()) {
                throw IllegalStateException("La imagen seleccionada está vacía.")
            }

            val stamp = SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(Date())
            val allowedExt = detectedMime?.let { allowedMimeTypes[it] }

            // Si ya es JPEG/PNG/WebP y no supera 8 MB, se sube directamente sin perder calidad.
            if (allowedExt != null && rawBytes.size <= MAX_UPLOAD_BYTES) {
                return@runCatching PreparedPhotoUpload(
                    bytes = rawBytes,
                    fileName = "foto_clinica_$stamp$allowedExt",
                    mimeType = detectedMime
                )
            }

            // Si supera 8 MB o viene en otro formato (ej. HEIC/HEIF), convertimos a JPEG optimizado.
            val bitmap = decodePreviewBitmap(context, uri, MAX_UPLOAD_DIMENSION)
                ?: throw IllegalStateException("No se pudo procesar la imagen seleccionada. Intenta con otra fotografía.")

            val qualities = listOf(88, 78, 66, 52, 40)
            var compressedBytes: ByteArray? = null
            for (quality in qualities) {
                val out = ByteArrayOutputStream()
                bitmap.compress(Bitmap.CompressFormat.JPEG, quality, out)
                val candidate = out.toByteArray()
                compressedBytes = candidate
                if (candidate.size <= TARGET_COMPRESSED_BYTES) {
                    break
                }
            }

            val finalBytes = compressedBytes
                ?: throw IllegalStateException("No se pudo comprimir la fotografía.")
            if (finalBytes.size > MAX_UPLOAD_BYTES) {
                throw IllegalStateException("La imagen supera el límite máximo permitido de 8 MB.")
            }

            PreparedPhotoUpload(
                bytes = finalBytes,
                fileName = "foto_clinica_$stamp.jpg",
                mimeType = "image/jpeg"
            )
        }
    }

    fun formatSizeLabel(bytes: Int): String {
        val kb = bytes / 1024.0
        return if (kb >= 1024.0) {
            String.format(Locale.US, "%.1f MB", kb / 1024.0)
        } else {
            String.format(Locale.US, "%.0f KB", kb.coerceAtLeast(1.0))
        }
    }

    private fun decodeLegacyBitmap(context: Context, uri: Uri, maxDimension: Int): Bitmap? {
        val bounds = BitmapFactory.Options().apply { inJustDecodeBounds = true }
        context.contentResolver.openInputStream(uri)?.use { stream ->
            BitmapFactory.decodeStream(stream, null, bounds)
        }
        val sampleSize = calculateInSampleSize(bounds.outWidth, bounds.outHeight, maxDimension)
        val decodeOpts = BitmapFactory.Options().apply { inSampleSize = sampleSize }
        return context.contentResolver.openInputStream(uri)?.use { stream ->
            BitmapFactory.decodeStream(stream, null, decodeOpts)
        }
    }

    private fun calculateInSampleSize(width: Int, height: Int, maxDimension: Int): Int {
        if (width <= 0 || height <= 0) return 1
        var inSampleSize = 1
        val largest = max(width, height)
        while ((largest / inSampleSize) > maxDimension) {
            inSampleSize *= 2
        }
        return inSampleSize.coerceAtLeast(1)
    }
}
