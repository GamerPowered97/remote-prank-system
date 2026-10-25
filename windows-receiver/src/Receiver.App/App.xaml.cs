using System.IO;
using System.Windows;
using Receiver.App.Views;
using Receiver.Core.Dispatcher;
using Receiver.Core.Lifecycle;
using Receiver.Core.Networking;
using Receiver.Core.Update;
using Receiver.Modules.Hardware;
using Receiver.Modules.Input;
using Receiver.Modules.Notification;
using Receiver.Modules.Visual;

namespace Receiver.App;

public partial class App : System.Windows.Application
{
    private const int ServicePort = 48888;
    private const string CurrentAppVersion = "1.1.0";

    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RemotePrankReceiver", "logs", "startup.log");

    public static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFile)!);
            File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [PID:{Environment.ProcessId}] {msg}\n");
        }
        catch { }
    }

    private static Mutex? _singleInstanceMutex;
    private static EventWaitHandle? _showWindowEvent;

    private PrankDispatcher? _dispatcher;
    private WebSocketServer? _webSocketServer;
    private DiscoveryService? _discoveryService;
    private GitHubUpdateChecker? _updateChecker;
    private TrayIconManager? _trayManager;
    private LogViewerWindow? _logViewerWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ENSURE NO PREMATURE SHUTDOWN
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Check for existing instance
        bool createdNew;
        try
        {
            _singleInstanceMutex = new Mutex(true, "Local\\RemotePrankReceiver_SingleInstanceMutex", out createdNew);
        }
        catch
        {
            createdNew = true;
        }

        if (!createdNew)
        {
            // Already running! Signal the running instance to show its status/log window
            try
            {
                using var showEvent = EventWaitHandle.OpenExisting("Local\\RemotePrankReceiver_ShowWindowEvent");
                showEvent.Set();
            }
            catch { }
            Shutdown(0);
            return;
        }

        Log("App.OnStartup called. Single instance lock acquired. ShutdownMode set to OnExplicitShutdown.");

        try
        {
            _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\RemotePrankReceiver_ShowWindowEvent");
            var showWindowThread = new Thread(() =>
            {
                while (true)
                {
                    _showWindowEvent.WaitOne();
                    Dispatcher.Invoke(() =>
                    {
                        if (_logViewerWindow != null)
                        {
                            _logViewerWindow.BringToForeground();
                        }
                    });
                }
            }) { IsBackground = true };
            showWindowThread.Start();
        }
        catch { }

        DispatcherUnhandledException += (sender, args) =>
        {
            Log($"[FATAL DispatcherException] {args.Exception}");
            System.Windows.MessageBox.Show($"Receiver Error:\n{args.Exception.Message}\n\nCheck {LogFile} for details.", "Remote Prank Receiver Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Log($"[FATAL AppDomainException] {args.ExceptionObject}");
        };

        try
        {
            Log("Registering startup...");
            // 1. Ensure automatic launch on user login
            StartupManager.EnsureStartupRegistered();
            Log("Startup registered.");

            // 2. Initialize Dispatcher and register modules
            Log("Initializing Dispatcher...");
            _dispatcher = new PrankDispatcher();
            _dispatcher.RegisterModule(new GlitchScreenModule());
            _dispatcher.RegisterModule(new MouseDriftModule());
            _dispatcher.RegisterModule(new FanSpeedModule());
            _dispatcher.RegisterModule(new SystemAlertModule());
            Log("Dispatcher initialized.");

            // 3. Initialize status & log viewer (kept hidden until requested)
            Log("Initializing LogViewerWindow...");
            _logViewerWindow = new LogViewerWindow(_dispatcher);
            MainWindow = _logViewerWindow;
            Log("LogViewerWindow initialized.");

            // 4. Initialize GitHub update checker
            Log("Initializing UpdateChecker...");
            _updateChecker = new GitHubUpdateChecker(CurrentAppVersion);
            Log("UpdateChecker initialized.");

            // 5. Initialize & start WebSocket server
            Log("Starting WebSocketServer on port " + ServicePort + "...");
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
            Log("WebSocketServer started.");

            // 6. Initialize & start mDNS discovery responder
            Log("Starting DiscoveryService...");
            _discoveryService = new DiscoveryService(ServicePort, Environment.MachineName, msg => {
                _dispatcher.Log(msg);
                Log("[Discovery] " + msg);
            });
            _discoveryService.Start();
            Log("DiscoveryService started.");

            // 7. Initialize System Tray Icon
            Log("Initializing TrayIconManager...");
            _trayManager = new TrayIconManager(_dispatcher, _logViewerWindow, ShutdownApp);
            Log("TrayIconManager initialized.");

            _dispatcher.Log("Remote Companion Receiver initialized successfully.");
            
            bool startMinimized = e.Args.Any(a => string.Equals(a, "--background", StringComparison.OrdinalIgnoreCase) ||
                                                  string.Equals(a, "--silent", StringComparison.OrdinalIgnoreCase));
            if (!startMinimized)
            {
                _logViewerWindow.BringToForeground();
            }
            else
            {
                _trayManager.ShowBalloon("Remote Companion Receiver", "Running in System Tray on port 48888. Double-click tray icon or executable to open status window.");
            }
            Log("Startup complete. StartMinimized: " + startMinimized);
        }
        catch (Exception ex)
        {
            Log($"[FATAL in OnStartup] {ex}");
            System.Windows.MessageBox.Show($"Failed to start Remote Prank Receiver:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShutdownApp()
    {
        Log("ShutdownApp() was invoked. StackTrace: " + Environment.StackTrace);
        _dispatcher?.Log("Shutting down Receiver application...");
        _trayManager?.Dispose();
        _discoveryService?.Stop();
        _webSocketServer?.Stop();
        _ = _dispatcher?.StopActiveAsync("Application Exit");
        Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log($"App.OnExit called with ExitCode: {e.ApplicationExitCode}. StackTrace: {Environment.StackTrace}");
        _trayManager?.Dispose();
        _discoveryService?.Stop();
        _webSocketServer?.Stop();
        base.OnExit(e);
    }
}
