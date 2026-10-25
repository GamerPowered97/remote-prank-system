package com.personal.prankcontroller.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.personal.prankcontroller.domain.ConnectionState
import com.personal.prankcontroller.ui.theme.*

@Composable
fun StatusBadge(
    connectionState: ConnectionState,
    modifier: Modifier = Modifier
) {
    val (statusColor, statusText) = when (connectionState) {
        is ConnectionState.Connected -> Pair(AccentGreen, "Connected to ${connectionState.serverName} (${connectionState.host}:${connectionState.port})")
        is ConnectionState.Connecting -> Pair(AccentAmber, "Connecting to ${connectionState.host}:${connectionState.port}...")
        is ConnectionState.Searching -> Pair(AccentBlue, "Searching for Receiver via mDNS...")
        is ConnectionState.Nuked -> Pair(AccentRed, "Receiver Nuked (Sleeping...)")
        is ConnectionState.Disconnected -> Pair(AccentRed, connectionState.message)
    }

    Row(
        modifier = modifier
            .clip(RoundedCornerShape(20.dp))
            .background(DarkSurfaceVariant)
            .padding(horizontal = 14.dp, vertical = 8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier
                .size(10.dp)
                .clip(CircleShape)
                .background(statusColor)
        )
        Spacer(modifier = Modifier.width(8.dp))
        Text(
            text = statusText,
            color = TextPrimary,
            fontSize = 13.sp
        )
    }
}
