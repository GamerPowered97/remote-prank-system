using System.Diagnostics;

namespace Receiver.Core.Update;

public static class UpdateLauncher
{
    public static bool ApplyUpdate(string newExePath, string? scriptPath = null)
    {
        try
        {
            int currentPid = Environment.ProcessId;
            string targetExePath = Environment.ProcessPath ?? AppDomain.CurrentDomain.FriendlyName;

            if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
            {
                string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "updater.bat");
                if (File.Exists(candidate))
                {
                    scriptPath = candidate;
                }
                else
                {
                    candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.bat");
                    if (File.Exists(candidate))
                    {
                        scriptPath = candidate;
                    }
                    else
                    {
                        scriptPath = Path.Combine(Path.GetTempPath(), "prankctl_updater.bat");
                        File.WriteAllText(scriptPath, @"@echo off
set TARGET_PID=%~1
set NEW_EXE=%~2
set TARGET_EXE=%~3
timeout /t 2 /nobreak >nul 2>&1
taskkill /PID %TARGET_PID% /F >nul 2>&1
copy /Y ""%NEW_EXE%"" ""%TARGET_EXE%"" >nul 2>&1
del ""%NEW_EXE%"" >nul 2>&1
start """" ""%TARGET_EXE%""
exit /b 0
");
                    }
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{scriptPath}\" {currentPid} \"{newExePath}\" \"{targetExePath}\"\"",
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
        catch
        {
            return false;
        }
    }
}
