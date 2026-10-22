using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using FloatView.App.Persistence;

namespace FloatView.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        SettingsService.Log($"[FATAL DISPATCHER] {e.Exception}");
        try
        {
            var dir = StoragePaths.AppDataFolder;
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "fatal.log"), e.Exception.ToString());
        }
        catch {}
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        SettingsService.Log($"[FATAL APPDOMAIN] {e.ExceptionObject}");
        try
        {
            var dir = StoragePaths.AppDataFolder;
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "fatal.log"), e.ExceptionObject?.ToString() ?? "Unknown fatal error");
        }
        catch {}
    }
}
