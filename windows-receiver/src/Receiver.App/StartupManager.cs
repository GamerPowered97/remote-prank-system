using System;
using System.IO;
using Microsoft.Win32;

namespace Receiver.App;

public static class StartupManager
{
    private const string RUN_KEY_PATH = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string APP_NAME = "RemotePrankReceiver";

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, false);
            return key?.GetValue(APP_NAME) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true);
            if (key == null) return;

            if (enable)
            {
                string exePath = Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RemotePrankReceiver.exe");
                key.SetValue(APP_NAME, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(APP_NAME, false);
            }
        }
        catch
        {
            // Ignore registry permissions issues
        }
    }

    public static void EnsureStartupRegistered()
    {
        if (!IsStartupEnabled())
        {
            SetStartup(true);
        }
    }
}
