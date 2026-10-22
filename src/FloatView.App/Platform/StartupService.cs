using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace FloatView.App.Platform;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "FloatView";

    public static bool IsRunAtStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetRunAtStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return false;

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\" --minimized");
                    return true;
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
