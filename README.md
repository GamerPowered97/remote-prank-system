# Remote Prank System

A lightweight, personal-use remote control system for triggering harmless visual and audio effects on a Windows PC from an Android device over the local network.

## Components

### 1. Windows Receiver (`windows-receiver/`)
- Built with **C# / .NET 8 WPF**.
- Runs in the background with **no main window**, **no taskbar button**, and **no Alt+Tab presence**.
- Visible only in the **Windows System Tray** and **Task Manager** (under Background Processes).
- **Auto-starts on Windows login** via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Immediate Kill Switch**: Double-click the system tray icon to instantly abort any running action.
- **Harmless Actions**:
  - Fullscreen transparent glitch overlay with animated scanlines (`WS_EX_TRANSPARENT` allows mouse clicks to pass through).
  - Subtle mouse drift.
  - Humorous toast notification.
  - Retro audio chime.
- **Nuke Feature**: Closes the application completely for 2 minutes, then automatically relaunches via `nuke.bat`.
- **GitHub Updates**: Queries GitHub Releases, downloads new single-file executables, and self-restarts via `updater.bat`.

### 2. Android Controller (`android-controller/`)
- Built with **Kotlin & Jetpack Compose (Material 3)**.
- Discovers the Windows PC on the local Wi-Fi network via **mDNS (`_prankctl._tcp.local`)** or manual IP entry.
- Direct connection with zero pairing ceremony (same Wi-Fi is the trust boundary).
- Dashboard with module trigger cards, an **Emergency Stop** button, a **Nuke (Sleep 2m)** countdown dialog, and a GitHub update check/install view.

## Pre-built Releases
Pre-compiled binaries are available in the [Releases](https://github.com/GamerPowered97/remote-prank-system/releases) section:
- **`RemotePrankReceiver.exe`**: Windows single-file executable.
- **`app-debug.apk`**: Android ready-to-install package.
