using System.Windows;
using Receiver.App.Views;
using Receiver.Core.Dispatcher;
using Receiver.Core.Lifecycle;
using Receiver.Core.Networking;
using Receiver.Core.Update;
using Receiver.Modules.Audio;
using Receiver.Modules.Input;
using Receiver.Modules.Notification;
using Receiver.Modules.Visual;

namespace Receiver.App;

public partial class App : System.Windows.Application
{
    private const int ServicePort = 48888;
    private const string CurrentAppVersion = "1.0.0";

    private PrankDispatcher? _dispatcher;
    private WebSocketServer? _webSocketServer;
    private DiscoveryService? _discoveryService;
    private GitHubUpdateChecker? _updateChecker;
    private TrayIconManager? _trayManager;
    private LogViewerWindow? _logViewerWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Ensure automatic launch on user login
        StartupManager.EnsureStartupRegistered();

        // 2. Initialize Dispatcher and register modules
        _dispatcher = new PrankDispatcher();
        _dispatcher.RegisterModule(new GlitchScreenModule());
        _dispatcher.RegisterModule(new MouseDriftModule());
        _dispatcher.RegisterModule(new AudioStingerModule());
        _dispatcher.RegisterModule(new FunnyToastModule());

        // 3. Initialize status & log viewer (kept hidden until requested)
        _logViewerWindow = new LogViewerWindow(_dispatcher);

        // 4. Initialize GitHub update checker
        _updateChecker = new GitHubUpdateChecker(CurrentAppVersion);

        // 5. Initialize & start WebSocket server
        _webSocketServer = new WebSocketServer(ServicePort, _dispatcher, CurrentAppVersion);

        _webSocketServer.NukeRequested += async (sleepSeconds) =>
        {
            _dispatcher.Log($"Nuke requested from remote. Sleeping for {sleepSeconds}s...");
            await _dispatcher.StopActiveAsync("Nuke shutdown");
            NukeManager.TriggerNuke(sleepSeconds);
        };

        _webSocketServer.CheckUpdateRequested += async () =>
        {
            _dispatcher.Log("Checking GitHub Releases for updates...");
            return await _updateChecker.CheckForUpdatesAsync();
        };

        _webSocketServer.ApplyUpdateRequested += async (downloadUrl) =>
        {
            _dispatcher.Log($"Applying update from: {downloadUrl}");
            try
            {
                string tempExe = await _updateChecker.DownloadUpdateAsync(downloadUrl);
                _dispatcher.Log($"Downloaded update to: {tempExe}. Launching updater...");
                UpdateLauncher.ApplyUpdate(tempExe);
            }
            catch (Exception ex)
            {
                _dispatcher.Log($"Failed to apply update: {ex.Message}");
            }
        };

        _webSocketServer.Start();

        // 6. Initialize & start mDNS discovery responder
        _discoveryService = new DiscoveryService(ServicePort, Environment.MachineName, msg => _dispatcher.Log(msg));
        _discoveryService.Start();

        // 7. Initialize System Tray Icon
        _trayManager = new TrayIconManager(_dispatcher, _logViewerWindow, ShutdownApp);

        _dispatcher.Log("Remote Companion Receiver initialized successfully in System Tray.");
        _trayManager.ShowBalloon("Remote Companion Receiver", "Running in System Tray on port 48888. Double-click tray icon to cancel any active effect.");
    }

    private void ShutdownApp()
    {
        _dispatcher?.Log("Shutting down Receiver application...");
        _trayManager?.Dispose();
        _discoveryService?.Stop();
        _webSocketServer?.Stop();
        _ = _dispatcher?.StopActiveAsync("Application Exit");
        Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        _discoveryService?.Stop();
        _webSocketServer?.Stop();
        base.OnExit(e);
    }
}
