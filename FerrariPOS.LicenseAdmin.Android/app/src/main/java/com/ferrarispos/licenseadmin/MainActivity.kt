package com.ferrarispos.licenseadmin

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.util.Base64
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import org.json.JSONObject
import java.nio.charset.StandardCharsets
import java.security.KeyFactory
import java.security.PrivateKey
import java.security.Signature
import java.security.spec.PKCS8EncodedKeySpec
import java.time.Instant
import java.time.ZoneOffset
import java.time.format.DateTimeFormatterBuilder
import java.time.temporal.ChronoUnit
import java.util.UUID

private const val PREFIX = "FPOS-LIC-3"

/*
 * Administrador de licencias 100% OFFLINE.
 * Esta es la MISMA clave privada RSA que usa FerrariPOS.LicenseManager.Windows.
 * Se almacena como DER PKCS#8 puro para que Android/Conscrypt no intente
 * interpretar un PEM como certificado y produzca "Error parsing private key".
 */
private const val PRIVATE_KEY_DER_B64 = "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"

private fun privateKey(): PrivateKey {
    val der = Base64.decode(PRIVATE_KEY_DER_B64, Base64.DEFAULT)
    return KeyFactory.getInstance("RSA").generatePrivate(PKCS8EncodedKeySpec(der))
}

private fun b64Url(bytes: ByteArray): String =
    Base64.encodeToString(bytes, Base64.URL_SAFE or Base64.NO_WRAP or Base64.NO_PADDING)

private fun sign(payload: String): String {
    val data = payload.toByteArray(StandardCharsets.UTF_8)
    val key = privateKey()

    // Android/Conscrypt no ofrece RSASSA-PSS en algunos dispositivos/versiones.
    // Usamos primero el proveedor del sistema y, si no existe, un PSS SHA-256
    // compatible con Windows RSA/RSASignaturePadding.Pss (salt = 32 bytes,
    // MGF1 = SHA-256). El fallback usa la operación RSA "NoPadding" disponible
    // desde API 26, por lo que no requiere librerías externas.
    val signed = try {
        val signature = Signature.getInstance("RSASSA-PSS")
        signature.setParameter(
            java.security.spec.PSSParameterSpec(
                "SHA-256", "MGF1", java.security.spec.MGF1ParameterSpec.SHA256, 32, 1
            )
        )
        signature.initSign(key)
        signature.update(data)
        signature.sign()
    } catch (_: java.security.GeneralSecurityException) {
        signRsaPssSha256Fallback(data, key)
    }

    return PREFIX + "." + b64Url(data) + "." + b64Url(signed)
}

/**
 * Implementación mínima de EMSA-PSS (RFC 8017) para RSA SHA-256.
 * Coincide con .NET RSASignaturePadding.Pss + SHA-256.
 */
private fun signRsaPssSha256Fallback(data: ByteArray, key: PrivateKey): ByteArray {
    val rsaKey = key as? java.security.interfaces.RSAPrivateKey
        ?: throw java.security.GeneralSecurityException("La clave privada no es RSA")

    val digest = java.security.MessageDigest.getInstance("SHA-256")
    val mHash = digest.digest(data)
    val salt = ByteArray(32)
    java.security.SecureRandom().nextBytes(salt)

    // H = SHA256(0x00*8 || mHash || salt)
    digest.reset()
    digest.update(ByteArray(8))
    digest.update(mHash)
    digest.update(salt)
    val h = digest.digest()

    val modulusBits = rsaKey.modulus.bitLength()
    val emBits = modulusBits - 1
    val emLen = (emBits + 7) / 8
    val hLen = h.size
    val sLen = salt.size
    if (emLen < hLen + sLen + 2) {
        throw java.security.GeneralSecurityException("Clave RSA demasiado pequeña para PSS SHA-256")
    }

    val psLen = emLen - sLen - hLen - 2
    val db = ByteArray(emLen - hLen - 1)
    // PS ya queda en cero; después: PS || 0x01 || salt
    db[psLen] = 1
    System.arraycopy(salt, 0, db, psLen + 1, sLen)

    val dbMask = mgf1Sha256(h, db.size)
    for (i in db.indices) db[i] = (db[i].toInt() xor dbMask[i].toInt()).toByte()

    // El primer byte debe tener en cero los bits que exceden emBits.
    val unusedBits = 8 * emLen - emBits
    db[0] = (db[0].toInt() and (0xFF ushr unusedBits)).toByte()

    val em = ByteArray(emLen)
    System.arraycopy(db, 0, em, 0, db.size)
    System.arraycopy(h, 0, em, db.size, h.size)
    em[em.lastIndex] = 0xBC.toByte()

    // RSA privado sin padding: s = EM^d mod n.
    val cipher = javax.crypto.Cipher.getInstance("RSA/ECB/NoPadding")
    cipher.init(javax.crypto.Cipher.ENCRYPT_MODE, key)
    val raw = cipher.doFinal(em)

    // La firma RSA debe tener exactamente k bytes (tamaño del módulo).
    val k = (modulusBits + 7) / 8
    return if (raw.size == k) raw
    else if (raw.size < k) ByteArray(k - raw.size) + raw
    else raw.copyOfRange(raw.size - k, raw.size)
}

private fun mgf1Sha256(seed: ByteArray, maskLen: Int): ByteArray {
    val out = ByteArray(maskLen)
    val digest = java.security.MessageDigest.getInstance("SHA-256")
    val counter = ByteArray(4)
    var offset = 0
    var c = 0
    while (offset < maskLen) {
        counter[0] = (c ushr 24).toByte()
        counter[1] = (c ushr 16).toByte()
        counter[2] = (c ushr 8).toByte()
        counter[3] = c.toByte()
        digest.reset()
        digest.update(seed)
        digest.update(counter)
        val block = digest.digest()
        val take = minOf(block.size, maskLen - offset)
        System.arraycopy(block, 0, out, offset, take)
        offset += take
        c++
    }
    return out
}

private val windowsUtcFormatter = DateTimeFormatterBuilder().appendInstant(7).toFormatter().withZone(ZoneOffset.UTC)
private fun windowsUtc(instant: Instant): String = windowsUtcFormatter.format(instant)
private fun normalizeMachine(value: String): String = value.filterNot(Char::isWhitespace).uppercase()
private fun newLicenseId(): String = "FPOS-" + UUID.randomUUID().toString().replace("-", "").take(16).uppercase()

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent { LicenseAdminOfflineScreen() }
    }
}

@Composable
private fun LicenseAdminOfflineScreen() {
    val context = androidx.compose.ui.platform.LocalContext.current
    var machineId by remember { mutableStateOf("") }
    var customerName by remember { mutableStateOf("") }
    var licenseId by remember { mutableStateOf("") }
    var action by remember { mutableStateOf("ACTIVATE") }
    var type by remember { mutableStateOf("CUSTOM") }
    var daysText by remember { mutableStateOf("90") }
    var token by remember { mutableStateOf("") }
    var status by remember { mutableStateOf("OFFLINE · LISTO PARA GENERAR") }
    var busy by remember { mutableStateOf(false) }

    fun generate() {
        val machine = normalizeMachine(machineId)
        if (machine.length < 16) { status = "ERROR · Machine ID no válido. Pegalo completo."; token = ""; return }
        val actionValue = action.trim().uppercase()
        val typeValue = type.trim().uppercase()
        val needsId = actionValue == "RENEW" || actionValue == "DEACTIVATE"
        if (needsId && licenseId.trim().isBlank()) { status = "ERROR · Para $actionValue necesitás el ID de licencia actual."; token = ""; return }
        val days = daysText.trim().toIntOrNull()
        if (actionValue != "DEACTIVATE" && typeValue != "PERMANENT" && (days == null || days < 1 || days > 36500)) { status = "ERROR · Días válidos: 1 a 36500."; token = ""; return }
        busy = true
        try {
            val now = Instant.now()
            val id = if (licenseId.trim().isBlank()) newLicenseId() else licenseId.trim().uppercase()
            val expires = when {
                actionValue == "DEACTIVATE" -> null
                typeValue == "PERMANENT" -> "9999-12-31T23:59:59.9999999Z"
                else -> windowsUtc(now.plus(days!!.toLong(), ChronoUnit.DAYS))
            }
            val payload = JSONObject().apply {
                put("Action", actionValue)
                put("LicenseId", id)
                put("MachineId", machine)
                put("Type", typeValue)
                put("IssuedAtUtc", windowsUtc(now))
                if (expires == null) put("ExpiresAtUtc", JSONObject.NULL) else put("ExpiresAtUtc", expires)
                // Igual que el desarrollador Windows: en licencias finitas se usa
                // una fecha absoluta y DurationDays queda NULL.
                put("DurationDays", JSONObject.NULL)
                put("CustomerName", customerName.trim())
            }.toString()
            token = sign(payload)
            status = when {
                actionValue == "DEACTIVATE" -> "✓ GENERADA Y FIRMADA · DESACTIVACIÓN · COMPATIBLE WINDOWS"
                typeValue == "PERMANENT" -> "✓ GENERADA Y FIRMADA · PERMANENTE · COMPATIBLE WINDOWS"
                else -> "✓ GENERADA Y FIRMADA · $days DÍAS EXACTOS · COMPATIBLE WINDOWS"
            }
            licenseId = id
        } catch (e: Exception) {
            token = ""
            status = "✕ ERROR AL GENERAR: ${e.javaClass.simpleName} · ${e.message ?: "desconocido"}"
        } finally { busy = false }
    }

    fun copyToken() {
        if (token.isBlank()) return
        val cm = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
        cm.setPrimaryClip(ClipData.newPlainText("FerrariPOS License", token))
        status = "✓ CÓDIGO COPIADO AL PORTAPAPELES"
    }

    fun shareToken() {
        if (token.isBlank()) return

        // Android Sharesheet robusto: algunas capas OEM/Samsung pueden mostrar
        // “Ninguna aplicación puede realizar esta acción” cuando el ACTION_SEND
        // no lleva ClipData o cuando las aplicaciones de mensajería no quedan
        // visibles para el resolver. Dejamos el intent genérico para que
        // aparezcan todas las apps compatibles y agregamos destinos conocidos
        // (WhatsApp/WhatsApp Business/Telegram/Gmail) como accesos directos
        // explícitos cuando están instalados.
        val sendIntent = Intent(Intent.ACTION_SEND).apply {
            type = "text/plain"
            putExtra(Intent.EXTRA_TEXT, token)
            putExtra(Intent.EXTRA_TITLE, "Licencia FerrariPOS")
            clipData = ClipData.newPlainText("FerrariPOS License", token)
            addFlags(Intent.FLAG_ACTIVITY_NEW_DOCUMENT)
        }

        val packageManager = context.packageManager
        val initialTargets = mutableListOf<Intent>()
        val knownPackages = listOf(
            "com.whatsapp",
            "com.whatsapp.w4b",
            "org.telegram.messenger",
            "com.google.android.gm"
        )

        for (packageName in knownPackages) {
            val target = Intent(sendIntent).setPackage(packageName)
            if (target.resolveActivity(packageManager) != null) {
                initialTargets.add(target)
            }
        }

        val genericAvailable = sendIntent.resolveActivity(packageManager) != null
        if (!genericAvailable && initialTargets.isEmpty()) {
            status = "✕ NO HAY UNA APLICACIÓN COMPATIBLE PARA ENVIAR EL CÓDIGO"
            return
        }

        val chooser = Intent.createChooser(sendIntent, "Enviar licencia").apply {
            if (initialTargets.isNotEmpty()) {
                putExtra(Intent.EXTRA_INITIAL_INTENTS, initialTargets.toTypedArray())
            }
        }
        context.startActivity(chooser)
    }

    MaterialTheme(colorScheme = darkColorScheme(
        background = Color(0xFF030405),
        surface = Color(0xFF101419),
        primary = Color(0xFFFFB52E),
        secondary = Color(0xFF00E5FF)
    )) {
        Box(
            Modifier
                .fillMaxSize()
                .background(Brush.verticalGradient(listOf(Color(0xFF020304), Color(0xFF171A20))))
        ) {
            Column(
                Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 18.dp, vertical = 16.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Spacer(Modifier.height(8.dp))
                androidx.compose.foundation.Image(
                    painter = painterResource(com.ferrarispos.licenseadmin.R.drawable.ferrari_license_logo),
                    contentDescription = "FerrariPOS Licencias",
                    contentScale = ContentScale.Fit,
                    modifier = Modifier.size(108.dp)
                )
                Spacer(Modifier.height(5.dp))
                Text("FerrariPOS", color = Color.White, fontSize = 30.sp, fontWeight = FontWeight.Bold)
                Text("ADMINISTRADOR DE LICENCIAS", color = Color(0xFFFFB52E), fontSize = 14.sp, fontWeight = FontWeight.Bold)
                Text(
                    "100% OFFLINE · MISMO FORMATO FPOS-LIC-3 DE WINDOWS",
                    color = Color(0xFFBFC6CF),
                    fontSize = 11.sp
                )
                Spacer(Modifier.height(16.dp))

                Card(
                    Modifier.fillMaxWidth().widthIn(max = 600.dp),
                    colors = CardDefaults.cardColors(containerColor = Color(0xFF101419)),
                    shape = RoundedCornerShape(22.dp)
                ) {
                    Column(Modifier.padding(18.dp)) {
                        Text("NUEVA LICENCIA", color = Color.White, fontSize = 19.sp, fontWeight = FontWeight.Bold)
                        Spacer(Modifier.height(3.dp))
                        Text(
                            "Generá el código en este teléfono, copialo y mandalo al cliente. No necesita Internet.",
                            color = Color(0xFFB7C0CA),
                            fontSize = 13.sp
                        )
                        Spacer(Modifier.height(13.dp))

                        OutlinedTextField(
                            value = machineId,
                            onValueChange = { machineId = it.uppercase() },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true,
                            label = { Text("MACHINE ID") },
                            placeholder = { Text("Pegá el Machine ID completo") }
                        )
                        Spacer(Modifier.height(8.dp))

                        Row(
                            horizontalArrangement = Arrangement.spacedBy(8.dp),
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            OutlinedTextField(
                                value = customerName,
                                onValueChange = { customerName = it },
                                modifier = Modifier.weight(1f),
                                singleLine = true,
                                label = { Text("CLIENTE / EMPRESA") }
                            )
                            OutlinedTextField(
                                value = licenseId,
                                onValueChange = { licenseId = it.uppercase() },
                                modifier = Modifier.weight(1f),
                                singleLine = true,
                                label = { Text("ID DE LICENCIA") },
                                placeholder = { Text("Vacío = nuevo") }
                            )
                        }
                        Spacer(Modifier.height(12.dp))

                        Text("ACCIÓN", color = Color(0xFFB7C0CA), fontSize = 11.sp, fontWeight = FontWeight.Bold)
                        Spacer(Modifier.height(5.dp))
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(6.dp)
                        ) {
                            listOf("ACTIVATE", "RENEW", "DEACTIVATE").forEach { value ->
                                val selected = action == value
                                Button(
                                    onClick = { action = value },
                                    modifier = Modifier.weight(1f).height(44.dp),
                                    shape = RoundedCornerShape(10.dp),
                                    contentPadding = PaddingValues(horizontal = 3.dp),
                                    colors = ButtonDefaults.buttonColors(
                                        containerColor = if (selected) Color(0xFF4A405E) else Color(0xFF151A20),
                                        contentColor = if (selected) Color(0xFFFFD58A) else Color(0xFFCAD0D8)
                                    ),
                                    border = if (!selected) androidx.compose.foundation.BorderStroke(1.dp, Color(0xFF626A74)) else null
                                ) {
                                    Text(value, fontSize = 9.sp, fontWeight = FontWeight.Bold, maxLines = 1, softWrap = false)
                                }
                            }
                        }
                        Spacer(Modifier.height(12.dp))

                        Text("TIPO DE LICENCIA", color = Color(0xFFB7C0CA), fontSize = 11.sp, fontWeight = FontWeight.Bold)
                        Spacer(Modifier.height(5.dp))
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(6.dp)
                        ) {
                            listOf("CUSTOM", "ANNUAL", "PERMANENT").forEach { value ->
                                val selected = type == value
                                Button(
                                    onClick = { type = value },
                                    modifier = Modifier.weight(1f).height(44.dp),
                                    shape = RoundedCornerShape(10.dp),
                                    contentPadding = PaddingValues(horizontal = 3.dp),
                                    colors = ButtonDefaults.buttonColors(
                                        containerColor = if (selected) Color(0xFF4A405E) else Color(0xFF151A20),
                                        contentColor = if (selected) Color(0xFFFFD58A) else Color(0xFFCAD0D8)
                                    ),
                                    border = if (!selected) androidx.compose.foundation.BorderStroke(1.dp, Color(0xFF626A74)) else null
                                ) {
                                    Text(value, fontSize = 9.sp, fontWeight = FontWeight.Bold, maxLines = 1, softWrap = false)
                                }
                            }
                        }
                        Spacer(Modifier.height(10.dp))

                        OutlinedTextField(
                            value = daysText,
                            onValueChange = { daysText = it.filter(Char::isDigit).take(5) },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = true,
                            label = { Text("DÍAS EXACTOS") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                            enabled = action != "DEACTIVATE" && type != "PERMANENT"
                        )
                        Spacer(Modifier.height(14.dp))

                        Button(
                            onClick = { generate() },
                            enabled = !busy,
                            modifier = Modifier.fillMaxWidth().height(56.dp),
                            shape = RoundedCornerShape(15.dp),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = Color(0xFFFFB52E),
                                contentColor = Color(0xFF34250B)
                            )
                        ) {
                            Text(if (busy) "GENERANDO..." else "GENERAR CÓDIGO", fontWeight = FontWeight.Bold)
                        }
                        Spacer(Modifier.height(10.dp))
                        Text(
                            status,
                            color = if (status.startsWith("✓")) Color(0xFF55FF88) else if (status.startsWith("✕") || status.startsWith("ERROR")) Color(0xFFFF7070) else Color(0xFFBFC6CF),
                            fontSize = 12.sp
                        )
                    }
                }

                if (token.isNotBlank()) {
                    Spacer(Modifier.height(14.dp))
                    Card(
                        Modifier.fillMaxWidth().widthIn(max = 600.dp),
                        colors = CardDefaults.cardColors(containerColor = Color(0xFF0D1116)),
                        shape = RoundedCornerShape(18.dp)
                    ) {
                        Column(Modifier.padding(16.dp)) {
                            Text("CÓDIGO LARGO PARA ENVIAR AL CLIENTE", color = Color.White, fontWeight = FontWeight.Bold)
                            Spacer(Modifier.height(8.dp))
                            Text(token, color = Color(0xFFD9DEE6), fontSize = 9.sp)
                            Spacer(Modifier.height(10.dp))
                            Row(
                                Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.spacedBy(10.dp)
                            ) {
                                OutlinedButton(onClick = { copyToken() }, modifier = Modifier.weight(1f)) { Text("COPIAR") }
                                Button(onClick = { shareToken() }, modifier = Modifier.weight(1f)) { Text("ENVIAR") }
                            }
                        }
                    }
                }
                Spacer(Modifier.height(18.dp))
                Text("Sin servidor · Sin cuenta · Sin conexión requerida", color = Color(0xFF7E8791), fontSize = 11.sp)
                Spacer(Modifier.height(6.dp))
            }
        }
    }
}
