package com.personal.prankcontroller.domain.models

import com.google.gson.annotations.SerializedName

data class PrankModule(
    @SerializedName("id") val id: String,
    @SerializedName("name") val name: String,
    @SerializedName("description") val description: String,
    @SerializedName("category") val category: String = "General",
    @SerializedName("default_parameters") val defaultParameters: Map<String, Any>? = null
)

data class BaseMessage(
    @SerializedName("type") val type: String
)

data class ExecuteCommand(
    @SerializedName("type") val type: String = "execute",
    @SerializedName("module_id") val moduleId: String,
    @SerializedName("parameters") val parameters: Map<String, Any>? = null
)

data class KillCommand(
    @SerializedName("type") val type: String = "kill"
)

data class NukeCommand(
    @SerializedName("type") val type: String = "nuke",
    @SerializedName("sleep_seconds") val sleepSeconds: Int = 120
)

data class CheckUpdateCommand(
    @SerializedName("type") val type: String = "check_update"
)

data class ApplyUpdateCommand(
    @SerializedName("type") val type: String = "apply_update",
    @SerializedName("download_url") val downloadUrl: String
)

data class QueryStatusCommand(
    @SerializedName("type") val type: String = "query_status"
)

data class WelcomeResponse(
    @SerializedName("type") val type: String,
    @SerializedName("server_name") val serverName: String,
    @SerializedName("version") val version: String,
    @SerializedName("modules") val modules: List<PrankModule> = emptyList()
)

data class StatusResponse(
    @SerializedName("type") val type: String,
    @SerializedName("active_module") val activeModule: String?,
    @SerializedName("state") val state: String,
    @SerializedName("message") val message: String
)

data class NukeAckResponse(
    @SerializedName("type") val type: String,
    @SerializedName("sleep_seconds") val sleepSeconds: Int,
    @SerializedName("message") val message: String
)

data class UpdateStatusResponse(
    @SerializedName("type") val type: String,
    @SerializedName("has_update") val hasUpdate: Boolean,
    @SerializedName("current_version") val currentVersion: String,
    @SerializedName("latest_version") val latestVersion: String?,
    @SerializedName("release_notes") val releaseNotes: String?,
    @SerializedName("download_url") val downloadUrl: String?,
    @SerializedName("error") val error: String?
)
