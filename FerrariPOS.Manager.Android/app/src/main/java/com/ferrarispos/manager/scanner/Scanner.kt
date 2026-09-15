package com.ferrarispos.manager.scanner

import android.Manifest
import android.content.pm.PackageManager
import android.util.Size
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.CameraSelector
import androidx.camera.core.ImageAnalysis
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalLifecycleOwner
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.common.InputImage
import java.util.concurrent.Executors
import java.util.concurrent.atomic.AtomicBoolean

@Composable
fun BarcodeScannerScreen(title: String, onResult: (String) -> Unit, onClose: () -> Unit) {
    val context=LocalContext.current
    val lifecycle=LocalLifecycleOwner.current
    var granted by remember { mutableStateOf(ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA)==PackageManager.PERMISSION_GRANTED) }
    val launcher=rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { granted=it }
    LaunchedEffect(Unit) { if(!granted) launcher.launch(Manifest.permission.CAMERA) }

    Box(Modifier.fillMaxSize().background(Color(0xFF080B12))) {
        if(granted) {
            AndroidView(factory={ ctx ->
                val view=PreviewView(ctx)
                val providerFuture=ProcessCameraProvider.getInstance(ctx)
                providerFuture.addListener({
                    val provider=providerFuture.get()
                    val preview=Preview.Builder().build().also { it.setSurfaceProvider(view.surfaceProvider) }
                    val analysis=ImageAnalysis.Builder()
                        .setTargetResolution(Size(1280,720))
                        .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST).build()
                    val executor=Executors.newSingleThreadExecutor()
                    val scanner=BarcodeScanning.getClient()
                    val delivered=AtomicBoolean(false)
                    analysis.setAnalyzer(executor) { proxy ->
                        val media=proxy.image
                        if(media!=null) {
                            val image=InputImage.fromMediaImage(media, proxy.imageInfo.rotationDegrees)
                            scanner.process(image).addOnSuccessListener { codes ->
                                val value=codes.firstOrNull()?.rawValue
                                if(!value.isNullOrBlank() && delivered.compareAndSet(false,true)) onResult(value)
                            }.addOnCompleteListener { proxy.close() }
                        } else proxy.close()
                    }
                    provider.unbindAll()
                    provider.bindToLifecycle(lifecycle, CameraSelector.DEFAULT_BACK_CAMERA, preview, analysis)
                }, ContextCompat.getMainExecutor(ctx))
                view
            }, Modifier.fillMaxSize())
        } else {
            Column(Modifier.fillMaxSize(), horizontalAlignment=Alignment.CenterHorizontally, verticalArrangement=Arrangement.Center) {
                Text("Se necesita acceso a la cámara.", color=Color.White)
                Spacer(Modifier.height(12.dp))
                Button({ launcher.launch(Manifest.permission.CAMERA) }) { Text("PERMITIR CÁMARA") }
            }
        }
        Column(Modifier.fillMaxWidth().align(Alignment.TopCenter).padding(18.dp)) {
            Text(title, color=Color.White, style=MaterialTheme.typography.titleLarge)
            Text("Apuntá al QR o código de barras.", color=Color.LightGray)
        }
        OutlinedButton(onClose, Modifier.align(Alignment.BottomCenter).padding(24.dp)) { Text("CANCELAR") }
    }
}
