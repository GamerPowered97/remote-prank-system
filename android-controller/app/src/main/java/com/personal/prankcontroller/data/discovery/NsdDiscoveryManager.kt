package com.personal.prankcontroller.data.discovery

import android.content.Context
import android.net.nsd.NsdManager
import android.net.nsd.NsdServiceInfo
import android.os.Build
import android.util.Log

class NsdDiscoveryManager(context: Context) {
    private val nsdManager = context.getSystemService(Context.NSD_SERVICE) as? NsdManager
    private var discoveryListener: NsdManager.DiscoveryListener? = null
    private var isDiscovering = false

    companion object {
        private const val TAG = "NsdDiscoveryManager"
        private const val SERVICE_TYPE = "_prankctl._tcp."
    }

    fun startDiscovery(onServiceFound: (host: String, port: Int, serviceName: String) -> Unit) {
        if (nsdManager == null || isDiscovering) return

        discoveryListener = object : NsdManager.DiscoveryListener {
            override fun onDiscoveryStarted(regType: String) {
                isDiscovering = true
                Log.d(TAG, "Discovery started for $regType")
            }

            override fun onServiceFound(service: NsdServiceInfo) {
                Log.d(TAG, "Service found: ${service.serviceName}")
                resolveService(service, onServiceFound)
            }

            override fun onServiceLost(service: NsdServiceInfo) {
                Log.d(TAG, "Service lost: ${service.serviceName}")
            }

            override fun onDiscoveryStopped(serviceType: String) {
                isDiscovering = false
                Log.d(TAG, "Discovery stopped: $serviceType")
            }

            override fun onStartDiscoveryFailed(serviceType: String, errorCode: Int) {
                isDiscovering = false
                Log.e(TAG, "Discovery failed to start: $errorCode")
            }

            override fun onStopDiscoveryFailed(serviceType: String, errorCode: Int) {
                isDiscovering = false
                Log.e(TAG, "Discovery failed to stop: $errorCode")
            }
        }

        try {
            nsdManager.discoverServices(SERVICE_TYPE, NsdManager.PROTOCOL_DNS_SD, discoveryListener)
        } catch (e: Exception) {
            Log.e(TAG, "Error starting discovery", e)
        }
    }

    private fun resolveService(
        service: NsdServiceInfo,
        onServiceFound: (host: String, port: Int, serviceName: String) -> Unit
    ) {
        val resolver = object : NsdManager.ResolveListener {
            override fun onResolveFailed(serviceInfo: NsdServiceInfo, errorCode: Int) {
                Log.e(TAG, "Resolve failed for ${serviceInfo.serviceName}: error $errorCode")
            }

            override fun onServiceResolved(serviceInfo: NsdServiceInfo) {
                val host = serviceInfo.host?.hostAddress ?: return
                val port = serviceInfo.port
                Log.d(TAG, "Resolved service ${serviceInfo.serviceName} at $host:$port")
                onServiceFound(host, port, serviceInfo.serviceName)
            }
        }

        try {
            nsdManager?.resolveService(service, resolver)
        } catch (e: Exception) {
            Log.e(TAG, "Error resolving service", e)
        }
    }

    fun stopDiscovery() {
        if (isDiscovering && nsdManager != null && discoveryListener != null) {
            try {
                nsdManager.stopServiceDiscovery(discoveryListener)
            } catch (e: Exception) {
                Log.e(TAG, "Error stopping discovery", e)
            } finally {
                isDiscovering = false
                discoveryListener = null
            }
        }
    }
}
