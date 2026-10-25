package com.personal.prankcontroller

import com.google.gson.Gson
import com.personal.prankcontroller.domain.models.*
import org.junit.Assert.*
import org.junit.Test

class ProtocolSerializationTest {

    private val gson = Gson()

    @Test
    fun testExecuteCommandSerialization() {
        val cmd = ExecuteCommand(
            moduleId = "glitch_screen",
            parameters = mapOf("duration_seconds" to 10)
        )
        val json = gson.toJson(cmd)
        assertTrue(json.contains("\"type\":\"execute\""))
        assertTrue(json.contains("\"module_id\":\"glitch_screen\""))
        assertTrue(json.contains("\"duration_seconds\":10"))
    }

    @Test
    fun testUpdateParamsCommandSerialization() {
        val cmd = UpdateParamsCommand(
            moduleId = "glitch_screen",
            parameters = mapOf("intensity" to 8, "speed" to 7)
        )
        val json = gson.toJson(cmd)
        assertTrue(json.contains("\"type\":\"update_params\""))
        assertTrue(json.contains("\"module_id\":\"glitch_screen\""))
        assertTrue(json.contains("\"intensity\":8"))
        assertTrue(json.contains("\"speed\":7"))
    }

    @Test
    fun testKillCommandSerialization() {
        val cmd = KillCommand()
        val json = gson.toJson(cmd)
        assertEquals("{\"type\":\"kill\"}", json)
    }

    @Test
    fun testNukeCommandSerialization() {
        val cmd = NukeCommand(sleepSeconds = 120)
        val json = gson.toJson(cmd)
        assertTrue(json.contains("\"sleep_seconds\":120"))
    }

    @Test
    fun testWelcomeResponseDeserialization() {
        val json = """
            {
                "type": "welcome",
                "server_name": "DESKTOP-RIG",
                "version": "1.0.0",
                "modules": [
                    {
                        "id": "glitch_screen",
                        "name": "Screen Glitch",
                        "description": "Glitch effect",
                        "category": "Visual"
                    },
                    {
                        "id": "mouse_drift",
                        "name": "Mouse Drift",
                        "description": "Drift mouse",
                        "category": "Input"
                    }
                ]
            }
        """.trimIndent()

        val resp = gson.fromJson(json, WelcomeResponse::class.java)
        assertNotNull(resp)
        assertEquals("welcome", resp.type)
        assertEquals("DESKTOP-RIG", resp.serverName)
        assertEquals("1.0.0", resp.version)
        assertEquals(2, resp.modules.size)
        assertEquals("glitch_screen", resp.modules[0].id)
        assertEquals("Visual", resp.modules[0].category)
        assertEquals("mouse_drift", resp.modules[1].id)
    }

    @Test
    fun testStatusResponseDeserialization() {
        val json = """
            {
                "type": "status",
                "active_module": "glitch_screen",
                "state": "RUNNING",
                "message": "Glitch running"
            }
        """.trimIndent()

        val status = gson.fromJson(json, StatusResponse::class.java)
        assertNotNull(status)
        assertEquals("status", status.type)
        assertEquals("glitch_screen", status.activeModule)
        assertEquals("RUNNING", status.state)
    }

    @Test
    fun testNukeAckResponseDeserialization() {
        val json = """
            {
                "type": "nuke_ack",
                "sleep_seconds": 120,
                "message": "Shutting down for 120 seconds"
            }
        """.trimIndent()

        val nukeAck = gson.fromJson(json, NukeAckResponse::class.java)
        assertNotNull(nukeAck)
        assertEquals("nuke_ack", nukeAck.type)
        assertEquals(120, nukeAck.sleepSeconds)
    }

    @Test
    fun testUpdateStatusResponseDeserialization() {
        val json = """
            {
                "type": "update_status",
                "has_update": true,
                "current_version": "1.0.0",
                "latest_version": "1.1.0",
                "release_notes": "Added new features",
                "download_url": "https://example.com/download.exe"
            }
        """.trimIndent()

        val update = gson.fromJson(json, UpdateStatusResponse::class.java)
        assertNotNull(update)
        assertTrue(update.hasUpdate)
        assertEquals("1.1.0", update.latestVersion)
        assertEquals("https://example.com/download.exe", update.downloadUrl)
    }
}
