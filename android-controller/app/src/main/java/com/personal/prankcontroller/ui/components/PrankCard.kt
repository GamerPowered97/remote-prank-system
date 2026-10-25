package com.personal.prankcontroller.ui.components

import androidx.compose.foundation.BorderStroke
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
import com.personal.prankcontroller.domain.models.PrankModule
import com.personal.prankcontroller.ui.theme.*

@Composable
fun PrankCard(
    module: PrankModule,
    isActive: Boolean,
    isEnabled: Boolean,
    onTrigger: () -> Unit,
    modifier: Modifier = Modifier
) {
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
                        "Audio" -> Color(0xFF2E1E3A)
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
                            "Audio" -> Color(0xFFD8B4FE)
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

            Spacer(modifier = Modifier.height(14.dp))

            Button(
                onClick = onTrigger,
                enabled = isEnabled,
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(8.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = if (isActive) AccentRed else AccentBlue,
                    disabledContainerColor = DarkSurfaceVariant
                )
            ) {
                Text(
                    text = if (isActive) "Running..." else "Trigger Effect",
                    fontWeight = FontWeight.SemiBold,
                    fontSize = 14.sp
                )
            }
        }
    }
}
