package com.personal.prankcontroller.domain

sealed interface ConnectionState {
    data class Disconnected(val message: String = "Not connected") : ConnectionState
    data object Searching : ConnectionState
    data class Connecting(val host: String, val port: Int) : ConnectionState
    data class Connected(
        val serverName: String,
        val serverVersion: String,
        val host: String,
        val port: Int
    ) : ConnectionState
    data class Nuked(
        val totalSeconds: Int,
        val remainingSeconds: Int
    ) : ConnectionState
}
