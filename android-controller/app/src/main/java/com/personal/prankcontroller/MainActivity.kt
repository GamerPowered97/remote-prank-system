package com.personal.prankcontroller

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.runtime.*
import androidx.lifecycle.lifecycleScope
import com.personal.prankcontroller.data.discovery.NsdDiscoveryManager
import com.personal.prankcontroller.data.network.WebSocketClient
import com.personal.prankcontroller.domain.ConnectionState
import com.personal.prankcontroller.domain.models.PrankModule
import com.personal.prankcontroller.domain.models.UpdateStatusResponse
import com.personal.prankcontroller.ui.screens.DashboardScreen
import com.personal.prankcontroller.ui.screens.NukeCountdownDialog
import com.personal.prankcontroller.ui.theme.RemotePrankControllerTheme
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

class MainActivity : ComponentActivity() {

    private lateinit var discoveryManager: NsdDiscoveryManager
    private lateinit var webSocketClient: WebSocketClient

    private var connectionState by mutableStateOf<ConnectionState>(ConnectionState.Searching)
    private var modules by mutableStateOf<List<PrankModule>>(emptyList())
    private var activeModuleId by mutableStateOf<String?>(null)
    private var updateStatus by mutableStateOf<UpdateStatusResponse?>(null)
    private var isCheckingUpdate by mutableStateOf(false)

    // Nuke state
    private var isNuked by mutableStateOf(false)
    private var nukeRemainingSeconds by mutableStateOf(120)
    private var nukeJob: Job? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        initNetwork()

        setContent {
            RemotePrankControllerTheme {
                DashboardScreen(
                    connectionState = connectionState,
                    modules = modules,
                    activeModuleId = activeModuleId,
                    updateStatus = updateStatus,
                    isCheckingUpdate = isCheckingUpdate,
                    onTriggerModule = { moduleId, params ->
                        if (activeModuleId == moduleId) {
                            webSocketClient.sendKill()
                            activeModuleId = null
                        } else {
                            webSocketClient.sendExecute(moduleId, params)
                        }
                    },
                    onUpdateParams = { moduleId, params ->
                        webSocketClient.sendUpdateParams(moduleId, params)
                    },
                    onEmergencyStop = {
                        webSocketClient.sendKill()
                        activeModuleId = null
                    },
                    onNuke = {
                        webSocketClient.sendNuke(120)
                    },
                    onCheckUpdate = {
                        if (!webSocketClient.isConnected) {
                            isCheckingUpdate = false
                            updateStatus = UpdateStatusResponse(
                                type = "update_status",
                                hasUpdate = false,
                                currentVersion = "Offline",
                                latestVersion = null,
                                releaseNotes = null,
                                downloadUrl = null,
                                error = "Not connected to PC receiver. Please connect via Wi-Fi first."
                            )
                        } else {
                            isCheckingUpdate = true
                            webSocketClient.sendCheckUpdate()
                            lifecycleScope.launch {
                                delay(10000)
                                if (isCheckingUpdate) {
                                    isCheckingUpdate = false
                                    if (updateStatus == null || updateStatus?.error == null) {
                                        updateStatus = UpdateStatusResponse(
                                            type = "update_status",
                                            hasUpdate = false,
                                            currentVersion = (connectionState as? ConnectionState.Connected)?.serverVersion ?: "Unknown",
                                            latestVersion = null,
                                            releaseNotes = null,
                                            downloadUrl = null,
                                            error = "Update check timed out after 10s. Check PC internet connection."
                                        )
                                    }
                                }
                            }
                        }
                    },
                    onApplyUpdate = { downloadUrl ->
                        webSocketClient.sendApplyUpdate(downloadUrl)
                    },
                    onRefreshDiscovery = {
                        startDiscovery()
                    },
                    onManualConnect = { host, port ->
                        connectToHost(host, port)
                    }
                )

                if (isNuked) {
                    NukeCountdownDialog(
                        remainingSeconds = nukeRemainingSeconds,
                        totalSeconds = 120,
                        onDismiss = {
                            isNuked = false
                            nukeJob?.cancel()
                        }
                    )
                }
            }
        }

        startDiscovery()
    }

    private fun initNetwork() {
        discoveryManager = NsdDiscoveryManager(this)

        webSocketClient = WebSocketClient(
            onConnected = {
                // Connected, awaiting welcome
            },
            onDisconnected = { reason ->
                runOnUiThread {
                    isCheckingUpdate = false
                    if (!isNuked) {
                        connectionState = ConnectionState.Disconnected(reason)
                        activeModuleId = null
                    }
                }
            },
            onWelcome = { welcome ->
                runOnUiThread {
                    connectionState = ConnectionState.Connected(
                        serverName = welcome.serverName,
                        serverVersion = welcome.version,
                        host = "Connected",
                        port = 48888
                    )
                    modules = welcome.modules
                }
            },
            onStatus = { status ->
                runOnUiThread {
                    activeModuleId = if (status.state == "RUNNING") status.activeModule else null
                }
            },
            onNukeAck = { nukeAck ->
                runOnUiThread {
                    startNukeCountdown(nukeAck.sleepSeconds)
                }
            },
            onUpdateStatus = { status ->
                runOnUiThread {
                    isCheckingUpdate = false
                    updateStatus = status
                }
            },
            onError = { _ ->
                runOnUiThread {
                    isCheckingUpdate = false
                }
            }
        )
    }

    private fun startDiscovery() {
        connectionState = ConnectionState.Searching
        discoveryManager.stopDiscovery()
        discoveryManager.startDiscovery { host, port, _ ->
            runOnUiThread {
                discoveryManager.stopDiscovery()
                connectToHost(host, port)
            }
        }
    }

    private fun connectToHost(host: String, port: Int) {
        connectionState = ConnectionState.Connecting(host, port)
        webSocketClient.connect(host, port)
    }

    private fun startNukeCountdown(seconds: Int) {
        isNuked = true
        nukeRemainingSeconds = seconds
        connectionState = ConnectionState.Nuked(seconds, seconds)
        webSocketClient.disconnect()

        nukeJob?.cancel()
        nukeJob = lifecycleScope.launch {
            while (nukeRemainingSeconds > 0) {
                delay(1000)
                nukeRemainingSeconds--
            }
            isNuked = false
            // Start discovery again to auto-reconnect once PC wakes up
            startDiscovery()
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        discoveryManager.stopDiscovery()
        webSocketClient.disconnect()
        nukeJob?.cancel()
    }
}
