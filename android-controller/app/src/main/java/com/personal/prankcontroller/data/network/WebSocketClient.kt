package com.personal.prankcontroller.data.network

import android.util.Log
import com.google.gson.Gson
import com.personal.prankcontroller.domain.models.*
import okhttp3.*
import java.util.concurrent.TimeUnit

class WebSocketClient(
    private val onConnected: () -> Unit = {},
    private val onDisconnected: (String) -> Unit = {},
    private val onWelcome: (WelcomeResponse) -> Unit = {},
    private val onStatus: (StatusResponse) -> Unit = {},
    private val onNukeAck: (NukeAckResponse) -> Unit = {},
    private val onUpdateStatus: (UpdateStatusResponse) -> Unit = {},
    private val onError: (String) -> Unit = {}
) {
    private val gson = Gson()
    private val okHttpClient = OkHttpClient.Builder()
        .readTimeout(0, TimeUnit.MILLISECONDS)
        .pingInterval(15, TimeUnit.SECONDS)
        .build()

    private var webSocket: WebSocket? = null
    var isConnected = false
        private set

    companion object {
        private const val TAG = "WebSocketClient"
    }

    fun connect(host: String, port: Int) {
        disconnect()

        val url = "ws://$host:$port/ws"
        Log.d(TAG, "Connecting to $url")

        val request = Request.Builder()
            .url(url)
            .build()

        webSocket = okHttpClient.newWebSocket(request, object : WebSocketListener() {
            override fun onOpen(webSocket: WebSocket, response: Response) {
                isConnected = true
                Log.d(TAG, "WebSocket connected")
                onConnected()
            }

            override fun onMessage(webSocket: WebSocket, text: String) {
                handleIncomingMessage(text)
            }

            override fun onClosing(webSocket: WebSocket, code: Int, reason: String) {
                Log.d(TAG, "WebSocket closing: $code / $reason")
                webSocket.close(1000, null)
            }

            override fun onClosed(webSocket: WebSocket, code: Int, reason: String) {
                isConnected = false
                Log.d(TAG, "WebSocket closed: $reason")
                onDisconnected(reason.ifEmpty { "Disconnected ($code)" })
            }

            override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
                isConnected = false
                Log.e(TAG, "WebSocket failure: ${t.message}", t)
                onDisconnected(t.message ?: "Connection failed")
            }
        })
    }

    private fun handleIncomingMessage(text: String) {
        try {
            val base = gson.fromJson(text, BaseMessage::class.java) ?: return
            when (base.type) {
                "welcome" -> {
                    val welcome = gson.fromJson(text, WelcomeResponse::class.java)
                    onWelcome(welcome)
                }
                "status" -> {
                    val status = gson.fromJson(text, StatusResponse::class.java)
                    onStatus(status)
                }
                "nuke_ack" -> {
                    val nukeAck = gson.fromJson(text, NukeAckResponse::class.java)
                    onNukeAck(nukeAck)
                }
                "update_status" -> {
                    val updateStatus = gson.fromJson(text, UpdateStatusResponse::class.java)
                    onUpdateStatus(updateStatus)
                }
                "error" -> {
                    onError(text)
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error parsing message: $text", e)
        }
    }

    fun sendExecute(moduleId: String, parameters: Map<String, Any>? = null) {
        val cmd = ExecuteCommand(moduleId = moduleId, parameters = parameters)
        sendJson(cmd)
    }

    fun sendUpdateParams(moduleId: String, parameters: Map<String, Any>) {
        val cmd = UpdateParamsCommand(moduleId = moduleId, parameters = parameters)
        sendJson(cmd)
    }

    fun sendKill() {
        sendJson(KillCommand())
    }

    fun sendNuke(sleepSeconds: Int = 120) {
        sendJson(NukeCommand(sleepSeconds = sleepSeconds))
    }

    fun sendCheckUpdate() {
        sendJson(CheckUpdateCommand())
    }

    fun sendApplyUpdate(downloadUrl: String) {
        sendJson(ApplyUpdateCommand(downloadUrl = downloadUrl))
    }

    fun sendQueryStatus() {
        sendJson(QueryStatusCommand())
    }

    private fun sendJson(obj: Any) {
        val json = gson.toJson(obj)
        Log.d(TAG, "Sending: $json")
        webSocket?.send(json)
    }

    fun disconnect() {
        try {
            webSocket?.close(1000, "User disconnected")
        } catch (e: Exception) {
            // ignored
        } finally {
            webSocket = null
            isConnected = false
        }
    }
}
