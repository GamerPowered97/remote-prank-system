using System.Runtime.InteropServices;
using Receiver.Core.Dispatcher;

namespace Receiver.Modules.Notification;

public class SystemAlertModule : IPrankModule
{
    public string Id => "system_alert";
    public string DisplayName => "Windows System Dialog";
    public string Description => "Spawns an authentic native Windows modal dialog window with an OK button.";
    public string Category => "System";

    public IReadOnlyDictionary<string, object> DefaultParameters => new Dictionary<string, object>
    {
        { "title", "System Diagnostic Alert" },
        { "message", "A critical unexpected system exception has occurred. Please restart your coffee machine." },
        { "icon", "Warning" }
    };

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const uint MB_OK = 0x00000000;
    private const uint MB_ICONWARNING = 0x00000030;
    private const uint MB_ICONERROR = 0x00000010;
    private const uint MB_ICONINFORMATION = 0x00000040;
    private const uint MB_TOPMOST = 0x00040000;
    private const uint MB_SETFOREGROUND = 0x00010000;
    private const uint WM_CLOSE = 0x0010;

    private volatile string _activeTitle = "System Diagnostic Alert";
    private volatile bool _isShowing;

    public void UpdateParameters(IReadOnlyDictionary<string, object> parameters)
    {
        // Stored for dynamic parameter updates
    }

    public async Task ExecuteAsync(PrankContext context, CancellationToken cancellationToken)
    {
        string title = "System Diagnostic Alert";
        if (context.Parameters.TryGetValue("title", out var tVal) && tVal != null)
        {
            title = tVal.ToString() ?? title;
        }

        string message = "A critical unexpected system exception has occurred. Please restart your coffee machine.";
        if (context.Parameters.TryGetValue("message", out var mVal) && mVal != null)
        {
            message = mVal.ToString() ?? message;
        }

        string iconType = "Warning";
        if (context.Parameters.TryGetValue("icon", out var iVal) && iVal != null)
        {
            iconType = iVal.ToString() ?? iconType;
        }

        uint iconFlag = iconType.ToLowerInvariant() switch
        {
            "error" => MB_ICONERROR,
            "info" or "information" => MB_ICONINFORMATION,
            _ => MB_ICONWARNING
        };

        uint flags = MB_OK | iconFlag | MB_TOPMOST | MB_SETFOREGROUND;

        _activeTitle = title;
        _isShowing = true;
        context.LogInfo($"Displaying native Windows message box: [{title}] '{message}'");

        var dialogTask = Task.Run(() =>
        {
            try
            {
                MessageBox(IntPtr.Zero, message, title, flags);
            }
            finally
            {
                _isShowing = false;
            }
        });

        // Register cancellation hook to close window if stopped remotely
        using var reg = cancellationToken.Register(() =>
        {
            DismissNativeDialog(_activeTitle);
        });

        try
        {
            await dialogTask;
        }
        catch (OperationCanceledException)
        {
            DismissNativeDialog(_activeTitle);
        }
        finally
        {
            await RevertAsync();
        }
    }

    public Task RevertAsync()
    {
        if (_isShowing)
        {
            DismissNativeDialog(_activeTitle);
            _isShowing = false;
        }
        return Task.CompletedTask;
    }

    private static void DismissNativeDialog(string title)
    {
        try
        {
            // #32770 is the Win32 class for standard dialogs / message boxes
            IntPtr hWnd = FindWindow("#32770", title);
            if (hWnd != IntPtr.Zero)
            {
                PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
        catch
        {
            // Ignore failure to dismiss
        }
    }
}
