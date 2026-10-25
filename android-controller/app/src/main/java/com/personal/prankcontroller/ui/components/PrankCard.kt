package com.personal.prankcontroller.ui.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Stop
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.personal.prankcontroller.domain.models.PrankModule
import com.personal.prankcontroller.ui.theme.*
import kotlin.math.roundToInt

@Composable
fun PrankCard(
    module: PrankModule,
    isActive: Boolean,
    isEnabled: Boolean,
    onTrigger: (Map<String, Any>) -> Unit,
    onUpdateParams: (Map<String, Any>) -> Unit,
    modifier: Modifier = Modifier
) {
    var intensity by remember(module.id) {
        val def = (module.defaultParameters?.get("intensity") as? Number)?.toFloat() ?: 5f
        mutableFloatStateOf(def)
    }
    var speed by remember(module.id) {
        val def = (module.defaultParameters?.get("speed") as? Number)?.toFloat() ?: 5f
        mutableFloatStateOf(def)
    }
    var strength by remember(module.id) {
        val def = (module.defaultParameters?.get("strength") as? Number)?.toFloat() ?: 5f
        mutableFloatStateOf(def)
    }
    var alertTitle by remember(module.id) {
        val def = module.defaultParameters?.get("title")?.toString() ?: "System Diagnostic Alert"
        mutableStateOf(def)
    }
    var alertMessage by remember(module.id) {
        val def = module.defaultParameters?.get("message")?.toString() ?: "A critical unexpected system exception has occurred. Please restart your coffee machine."
        mutableStateOf(def)
    }
    var alertIcon by remember(module.id) {
        val def = module.defaultParameters?.get("icon")?.toString() ?: "Warning"
        mutableStateOf(def)
    }

    fun buildCurrentParams(): Map<String, Any> {
        return when (module.id) {
            "glitch_screen" -> mapOf(
                "intensity" to intensity.roundToInt(),
                "speed" to speed.roundToInt(),
                "duration_seconds" to 15
            )
            "mouse_drift" -> mapOf(
                "speed" to speed.roundToInt(),
                "strength" to strength.roundToInt(),
                "duration_seconds" to 15
            )
            "fan_speed" -> mapOf(
                "intensity" to intensity.roundToInt(),
                "duration_seconds" to 20
            )
            "system_alert" -> mapOf(
                "title" to alertTitle,
                "message" to alertMessage,
                "icon" to alertIcon
            )
            else -> emptyMap()
        }
    }

    Card(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(
            containerColor = if (isActive) Color(0xFF2D1B2D) else DarkSurface
        ),
        border = BorderStroke(
            1.dp,
            if (isActive) AccentRed else Color(0xFF333340)
        )
    ) {
        Column(
            modifier = Modifier.padding(16.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = module.name,
                    fontWeight = FontWeight.Bold,
                    fontSize = 16.sp,
                    color = TextPrimary
                )

                Surface(
                    shape = RoundedCornerShape(12.dp),
                    color = when (module.category) {
                        "Visual" -> Color(0xFF1E3A5F)
                        "Input" -> Color(0xFF3A2E1E)
                        "Hardware" -> Color(0xFF3B1E1E)
                        "System" -> Color(0xFF2E1E3A)
                        else -> Color(0xFF252530)
                    }
                ) {
                    Text(
                        text = module.category,
                        fontSize = 11.sp,
                        fontWeight = FontWeight.Medium,
                        color = when (module.category) {
                            "Visual" -> AccentBlue
                            "Input" -> AccentAmber
                            "Hardware" -> Color(0xFFFF6B6B)
                            "System" -> Color(0xFFD8B4FE)
                            else -> TextSecondary
                        },
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(6.dp))

            Text(
                text = module.description,
                fontSize = 13.sp,
                color = TextSecondary,
                lineHeight = 18.sp
            )

            Spacer(modifier = Modifier.height(12.dp))

            when (module.id) {
                "glitch_screen" -> {
                    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(text = "Glitch Intensity", color = TextSecondary, fontSize = 12.sp)
                            Text(text = "${intensity.roundToInt()} / 10", color = AccentBlue, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                        }
                        Slider(
                            value = intensity,
                            onValueChange = {
                                intensity = it
                                if (isActive) onUpdateParams(buildCurrentParams())
                            },
                            valueRange = 1f..10f,
                            steps = 8,
                            colors = SliderDefaults.colors(
                                thumbColor = AccentBlue,
                                activeTrackColor = AccentBlue
                            )
                        )

                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(text = "Refresh Speed", color = TextSecondary, fontSize = 12.sp)
                            Text(text = "${speed.roundToInt()} / 10", color = AccentBlue, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                        }
                        Slider(
                            value = speed,
                            onValueChange = {
                                speed = it
                                if (isActive) onUpdateParams(buildCurrentParams())
                            },
                            valueRange = 1f..10f,
                            steps = 8,
                            colors = SliderDefaults.colors(
                                thumbColor = AccentBlue,
                                activeTrackColor = AccentBlue
                            )
                        )
                    }
                }

                "mouse_drift" -> {
                    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(text = "Drift Speed", color = TextSecondary, fontSize = 12.sp)
                            Text(text = "${speed.roundToInt()} / 10", color = AccentAmber, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                        }
                        Slider(
                            value = speed,
                            onValueChange = {
                                speed = it
                                if (isActive) onUpdateParams(buildCurrentParams())
                            },
                            valueRange = 1f..10f,
                            steps = 8,
                            colors = SliderDefaults.colors(
                                thumbColor = AccentAmber,
                                activeTrackColor = AccentAmber
                            )
                        )

                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(text = "Movement Strength", color = TextSecondary, fontSize = 12.sp)
                            Text(text = "${strength.roundToInt()} / 10", color = AccentAmber, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                        }
                        Slider(
                            value = strength,
                            onValueChange = {
                                strength = it
                                if (isActive) onUpdateParams(buildCurrentParams())
                            },
                            valueRange = 1f..10f,
                            steps = 8,
                            colors = SliderDefaults.colors(
                                thumbColor = AccentAmber,
                                activeTrackColor = AccentAmber
                            )
                        )
                    }
                }

                "fan_speed" -> {
                    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(text = "Vent & Fan Turbo Power", color = TextSecondary, fontSize = 12.sp)
                            Text(text = "${intensity.roundToInt()} / 10", color = Color(0xFFFF6B6B), fontSize = 12.sp, fontWeight = FontWeight.Bold)
                        }
                        Slider(
                            value = intensity,
                            onValueChange = {
                                intensity = it
                                if (isActive) onUpdateParams(buildCurrentParams())
                            },
                            valueRange = 1f..10f,
                            steps = 8,
                            colors = SliderDefaults.colors(
                                thumbColor = Color(0xFFFF6B6B),
                                activeTrackColor = Color(0xFFFF6B6B)
                            )
                        )
                    }
                }

                "system_alert" -> {
                    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        OutlinedTextField(
                            value = alertTitle,
                            onValueChange = { alertTitle = it },
                            label = { Text("Window Title", fontSize = 12.sp) },
                            singleLine = true,
                            modifier = Modifier.fillMaxWidth(),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedBorderColor = AccentBlue,
                                unfocusedBorderColor = Color(0xFF444455),
                                focusedTextColor = TextPrimary,
                                unfocusedTextColor = TextPrimary
                            )
                        )

                        OutlinedTextField(
                            value = alertMessage,
                            onValueChange = { alertMessage = it },
                            label = { Text("Window Message Text", fontSize = 12.sp) },
                            maxLines = 3,
                            modifier = Modifier.fillMaxWidth(),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedBorderColor = AccentBlue,
                                unfocusedBorderColor = Color(0xFF444455),
                                focusedTextColor = TextPrimary,
                                unfocusedTextColor = TextPrimary
                            )
                        )

                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            listOf("Warning", "Error", "Information").forEach { iconName ->
                                val selected = alertIcon.equals(iconName, ignoreCase = true)
                                Surface(
                                    shape = RoundedCornerShape(8.dp),
                                    color = if (selected) AccentBlue else DarkSurfaceVariant,
                                    modifier = Modifier
                                        .weight(1f)
                                        .clickable { alertIcon = iconName }
                                ) {
                                    Text(
                                        text = iconName,
                                        fontSize = 11.sp,
                                        fontWeight = if (selected) FontWeight.Bold else FontWeight.Normal,
                                        color = if (selected) Color.White else TextSecondary,
                                        modifier = Modifier.padding(vertical = 8.dp),
                                        textAlign = TextAlign.Center
                                    )
                                }
                            }
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.height(14.dp))

            Button(
                onClick = { onTrigger(buildCurrentParams()) },
                enabled = isEnabled,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(8.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = if (isActive) AccentRed else AccentBlue,
                    disabledContainerColor = DarkSurfaceVariant
                )
            ) {
                Icon(
                    imageVector = if (isActive) Icons.Default.Stop else Icons.Default.PlayArrow,
                    contentDescription = if (isActive) "Stop" else "Trigger",
                    modifier = Modifier.size(18.dp)
                )
                Spacer(modifier = Modifier.width(6.dp))
                Text(
                    text = if (isActive) "STOP EFFECT" else "TRIGGER EFFECT",
                    fontWeight = FontWeight.Bold,
                    fontSize = 13.sp
                )
            }
        }
    }
}
