package com.personal.prankcontroller.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.personal.prankcontroller.domain.models.UpdateStatusResponse
import com.personal.prankcontroller.ui.theme.*

@Composable
fun UpdateScreen(
    updateStatus: UpdateStatusResponse?,
    isChecking: Boolean,
    onCheckUpdate: () -> Unit,
    onApplyUpdate: (String) -> Unit,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = DarkSurface)
    ) {
        Column(
            modifier = Modifier.padding(18.dp)
        ) {
            Text(
                text = "📦 PC Receiver Self-Update",
                fontWeight = FontWeight.Bold,
                fontSize = 17.sp,
                color = TextPrimary
            )

            Spacer(modifier = Modifier.height(10.dp))

            if (updateStatus != null) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(text = "Current Version:", color = TextSecondary, fontSize = 14.sp)
                    Text(text = updateStatus.currentVersion, color = TextPrimary, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                }

                if (updateStatus.latestVersion != null) {
                    Spacer(modifier = Modifier.height(4.dp))
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(text = "Latest Version:", color = TextSecondary, fontSize = 14.sp)
                        Text(
                            text = updateStatus.latestVersion,
                            color = if (updateStatus.hasUpdate) AccentGreen else TextPrimary,
                            fontWeight = FontWeight.SemiBold,
                            fontSize = 14.sp
                        )
                    }
                }

                if (!updateStatus.releaseNotes.isNullOrEmpty()) {
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(text = "Release Notes:", color = TextSecondary, fontSize = 12.sp)
                    Text(
                        text = updateStatus.releaseNotes,
                        color = TextPrimary,
                        fontSize = 12.sp,
                        modifier = Modifier
                            .background(DarkSurfaceVariant, RoundedCornerShape(8.dp))
                            .padding(8.dp)
                            .fillMaxWidth()
                    )
                }

                if (updateStatus.error != null) {
                    Spacer(modifier = Modifier.height(6.dp))
                    Text(text = updateStatus.error, color = AccentRed, fontSize = 12.sp)
                }
            } else {
                Text(
                    text = "Check GitHub Releases for latest receiver binaries.",
                    color = TextSecondary,
                    fontSize = 13.sp
                )
            }

            Spacer(modifier = Modifier.height(16.dp))

            Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                Button(
                    onClick = onCheckUpdate,
                    enabled = !isChecking,
                    modifier = Modifier.weight(1f),
                    colors = ButtonDefaults.buttonColors(containerColor = AccentBlue)
                ) {
                    Text(text = if (isChecking) "Checking..." else "Check GitHub")
                }

                if (updateStatus?.hasUpdate == true && !updateStatus.downloadUrl.isNullOrEmpty()) {
                    Button(
                        onClick = { onApplyUpdate(updateStatus.downloadUrl) },
                        modifier = Modifier.weight(1f),
                        colors = ButtonDefaults.buttonColors(containerColor = AccentGreen)
                    ) {
                        Text(text = "Update & Restart PC")
                    }
                }
            }
        }
    }
}
