using System.Diagnostics;

namespace Receiver.Core.Lifecycle;

public static class NukeManager
{
    public static bool TriggerNuke(int sleepSeconds = 120, string? scriptPath = null)
    {
        try
        {
            int currentPid = Environment.ProcessId;
            string exePath = Environment.ProcessPath ?? AppDomain.CurrentDomain.FriendlyName;

            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                // Fallback: look next to executable or create a temp runner
                string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "nuke.bat");
                if (File.Exists(candidate))
                {
                    scriptPath = candidate;
                }
                else
                {
                    candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nuke.bat");
                    if (File.Exists(candidate))
                    {
                        scriptPath = candidate;
                    }
                    else
                    {
                        // Write to temp file if not found
                        scriptPath = Path.Combine(Path.GetTempPath(), "prankctl_nuke.bat");
                        File.WriteAllText(scriptPath, @"@echo off
set TARGET_PID=%~1
set SLEEP_SECS=%~2
set EXE_PATH=%~3
timeout /t 2 /nobreak >nul 2>&1
taskkill /PID %TARGET_PID% /F >nul 2>&1
timeout /t %SLEEP_SECS% /nobreak >nul 2>&1
start """" ""%EXE_PATH%""
exit /b 0
");
                    }
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{scriptPath}\" {currentPid} {sleepSeconds} \"{exePath}\"\"",
                CreateNoWindow = true,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo);

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                Environment.Exit(0);
            });

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
