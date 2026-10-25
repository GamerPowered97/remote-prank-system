package com.personal.prankcontroller.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Stop
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.personal.prankcontroller.domain.ConnectionState
import com.personal.prankcontroller.domain.models.PrankModule
import com.personal.prankcontroller.domain.models.UpdateStatusResponse
import com.personal.prankcontroller.ui.components.PrankCard
import com.personal.prankcontroller.ui.components.StatusBadge
import com.personal.prankcontroller.ui.theme.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardScreen(
    connectionState: ConnectionState,
    modules: List<PrankModule>,
    activeModuleId: String?,
    updateStatus: UpdateStatusResponse?,
    isCheckingUpdate: Boolean,
    onTriggerModule: (String, Map<String, Any>) -> Unit,
    onUpdateParams: (String, Map<String, Any>) -> Unit,
    onEmergencyStop: () -> Unit,
    onNuke: () -> Unit,
    onCheckUpdate: () -> Unit,
    onApplyUpdate: (String) -> Unit,
    onRefreshDiscovery: () -> Unit,
    onManualConnect: (String, Int) -> Unit
) {
    var showManualConnectDialog by remember { mutableStateOf(false) }
    var manualHost by remember { mutableStateOf("192.168.1.") }
    var manualPort by remember { mutableStateOf("48888") }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "⚡ Prank Controller",
                        fontWeight = FontWeight.Bold,
                        fontSize = 20.sp,
                        color = TextPrimary
                    )
                },
                actions = {
                    IconButton(onClick = onRefreshDiscovery) {
                        Icon(
                            imageVector = Icons.Default.Refresh,
                            contentDescription = "Search mDNS",
                            tint = AccentBlue
                        )
                    }
                    TextButton(onClick = { showManualConnectDialog = true }) {
                        Text(text = "IP", color = AccentBlue, fontWeight = FontWeight.Bold)
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = DarkBackground)
            )
        },
        bottomBar = {
            // Floating Emergency Stop Button
            Surface(
                color = DarkBackground,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    Button(
                        onClick = onEmergencyStop,
                        colors = ButtonDefaults.buttonColors(containerColor = AccentRed),
                        shape = RoundedCornerShape(12.dp),
                        modifier = Modifier
                            .weight(2f)
                            .height(56.dp)
                    ) {
                        Icon(
                            imageVector = Icons.Default.Stop,
                            contentDescription = "Stop",
                            modifier = Modifier.size(24.dp)
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(
                            text = "EMERGENCY STOP",
                            fontWeight = FontWeight.Bold,
                            fontSize = 15.sp
                        )
                    }

                    Button(
                        onClick = onNuke,
                        colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF6B21A8)),
                        shape = RoundedCornerShape(12.dp),
                        modifier = Modifier
                            .weight(1f)
                            .height(56.dp)
                    ) {
                        Text(
                            text = "NUKE (2m)",
                            fontWeight = FontWeight.Bold,
                            fontSize = 12.sp,
                            color = Color.White
                        )
                    }
                }
            }
        },
        containerColor = DarkBackground
    ) { paddingValues ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
                .padding(horizontal = 16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            item {
                StatusBadge(
                    connectionState = connectionState,
                    modifier = Modifier.fillMaxWidth()
                )
            }

            if (modules.isEmpty()) {
                item {
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(14.dp),
                        colors = CardDefaults.cardColors(containerColor = DarkSurface)
                    ) {
                        Column(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(24.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(
                                text = "No Active Receiver Connected",
                                fontWeight = FontWeight.SemiBold,
                                color = TextPrimary
                            )
                            Spacer(modifier = Modifier.height(6.dp))
                            Text(
                                text = "Ensure the Windows receiver is running on the same Wi-Fi.",
                                color = TextSecondary,
                                fontSize = 13.sp
                            )
                        }
                    }
                }
            } else {
                items(modules) { module ->
                    val isActive = module.id == activeModuleId
                    PrankCard(
                        module = module,
                        isActive = isActive,
                        isEnabled = connectionState is ConnectionState.Connected,
                        onTrigger = { params -> onTriggerModule(module.id, params) },
                        onUpdateParams = { params -> onUpdateParams(module.id, params) }
                    )
                }
            }

            item {
                UpdateScreen(
                    updateStatus = updateStatus,
                    isChecking = isCheckingUpdate,
                    onCheckUpdate = onCheckUpdate,
                    onApplyUpdate = onApplyUpdate
                )
            }

            item {
                Spacer(modifier = Modifier.height(16.dp))
            }
        }
    }

    if (showManualConnectDialog) {
        AlertDialog(
            onDismissRequest = { showManualConnectDialog = false },
            title = { Text(text = "Connect via Manual IP", color = TextPrimary) },
            text = {
                Column {
                    OutlinedTextField(
                        value = manualHost,
                        onValueChange = { manualHost = it },
                        label = { Text("PC IP Address") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth()
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    OutlinedTextField(
                        value = manualPort,
                        onValueChange = { manualPort = it },
                        label = { Text("Port (default 48888)") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth()
                    )
                }
            },
            confirmButton = {
                Button(
                    onClick = {
                        showManualConnectDialog = false
                        val portInt = manualPort.toIntOrNull() ?: 48888
                        onManualConnect(manualHost.trim(), portInt)
                    }
                ) {
                    Text("Connect")
                }
            },
            dismissButton = {
                TextButton(onClick = { showManualConnectDialog = false }) {
                    Text("Cancel")
                }
            },
            containerColor = DarkSurface
        )
    }
}
