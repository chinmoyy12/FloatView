using System;
using System.IO;

namespace FloatView.App.Persistence;

public static class StoragePaths
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FloatView");

    private static readonly string LocalAppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FloatView");

    static StoragePaths()
    {
        Directory.CreateDirectory(AppDataFolder);
        Directory.CreateDirectory(LocalAppDataFolder);
        Directory.CreateDirectory(WebView2UserDataFolder);
    }

    public static string SettingsFile => Path.Combine(AppDataFolder, "settings.json");
    public static string WorkspacesFile => Path.Combine(AppDataFolder, "workspaces.json");
    public static string FavoritesFile => Path.Combine(AppDataFolder, "favorites.json");
    public static string NotesFile => Path.Combine(AppDataFolder, "notes.json");
    public static string PrivacyRulesFile => Path.Combine(AppDataFolder, "privacy_rules.json");
    public static string SessionFile => Path.Combine(AppDataFolder, "session.json");
    public static string LogFile => Path.Combine(AppDataFolder, "floatview.log");

    public static string WebView2UserDataFolder => Path.Combine(LocalAppDataFolder, "WebView2Profile");
}
