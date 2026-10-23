using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using FloatView.App.Persistence;
using FloatView.App.Platform;

namespace FloatView.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (NativeMessagingRelay.IsNativeMessagingInvocation(e.Args))
        {
            NativeMessagingRelay.Run(e.Args);
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
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
