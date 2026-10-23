using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FloatView.App.Models;

namespace FloatView.App.Persistence;

public class SettingsService
{
    private static readonly object FileLock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string ReadAllTextShared(string filePath)
    {
        lock (FileLock)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            return reader.ReadToEnd();
        }
    }

    private AppSettings _currentSettings;

    public SettingsService()
    {
        _currentSettings = LoadSettings();
    }

    public AppSettings Settings => _currentSettings;

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(StoragePaths.SettingsFile))
            {
                var json = ReadAllTextShared(StoragePaths.SettingsFile);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    _currentSettings = loaded;
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            BackupCorruptedFile(StoragePaths.SettingsFile);
            Log($"Failed to read settings: {ex.Message}. Falling back to default settings.");
        }

        _currentSettings = new AppSettings();
        SaveSettings(_currentSettings);
        return _currentSettings;
    }

    public void SaveSettings(AppSettings settings)
    {
        _currentSettings = settings;
        AtomicWrite(StoragePaths.SettingsFile, settings);
    }

    public List<WorkspaceModel> LoadWorkspaces()
    {
        try
        {
            if (File.Exists(StoragePaths.WorkspacesFile))
            {
                var json = ReadAllTextShared(StoragePaths.WorkspacesFile);
                var list = JsonSerializer.Deserialize<List<WorkspaceModel>>(json, JsonOptions);
                if (list != null && list.Count > 0) return list;
            }
        }
        catch (Exception ex)
        {
            BackupCorruptedFile(StoragePaths.WorkspacesFile);
            Log($"Failed to load workspaces: {ex.Message}. Creating default presets.");
        }

        var defaults = GetDefaultWorkspaces();
        SaveWorkspaces(defaults);
        return defaults;
    }

    public void SaveWorkspaces(List<WorkspaceModel> workspaces)
    {
        AtomicWrite(StoragePaths.WorkspacesFile, workspaces);
    }

    public List<FavoriteItem> LoadFavorites()
    {
        try
        {
            if (File.Exists(StoragePaths.FavoritesFile))
            {
                var json = ReadAllTextShared(StoragePaths.FavoritesFile);
                var list = JsonSerializer.Deserialize<List<FavoriteItem>>(json, JsonOptions);
                if (list != null && list.Count > 0) return list;
            }
        }
        catch (Exception ex)
        {
            BackupCorruptedFile(StoragePaths.FavoritesFile);
            Log($"Failed to load favorites: {ex.Message}. Creating defaults.");
        }

        var defaults = GetDefaultFavorites();
        SaveFavorites(defaults);
        return defaults;
    }

    public void SaveFavorites(List<FavoriteItem> favorites)
    {
        AtomicWrite(StoragePaths.FavoritesFile, favorites);
    }

    public List<CaptionNote> LoadNotes()
    {
        try
        {
            if (File.Exists(StoragePaths.NotesFile))
            {
                var json = ReadAllTextShared(StoragePaths.NotesFile);
                var list = JsonSerializer.Deserialize<List<CaptionNote>>(json, JsonOptions);
                if (list != null) return list;
            }
        }
        catch (Exception ex)
        {
            BackupCorruptedFile(StoragePaths.NotesFile);
            Log($"Failed to load notes: {ex.Message}");
        }

        return new List<CaptionNote>();
    }

    public void SaveNotes(List<CaptionNote> notes)
    {
        AtomicWrite(StoragePaths.NotesFile, notes);
    }

    public List<PrivacyRule> LoadPrivacyRules()
    {
        try
        {
            if (File.Exists(StoragePaths.PrivacyRulesFile))
            {
                var json = ReadAllTextShared(StoragePaths.PrivacyRulesFile);
                var list = JsonSerializer.Deserialize<List<PrivacyRule>>(json, JsonOptions);
                if (list != null && list.Count > 0) return list;
            }
        }
        catch (Exception ex)
        {
            BackupCorruptedFile(StoragePaths.PrivacyRulesFile);
            Log($"Failed to load privacy rules: {ex.Message}");
        }

        var defaults = GetDefaultPrivacyRules();
        SavePrivacyRules(defaults);
        return defaults;
    }

    public void SavePrivacyRules(List<PrivacyRule> rules)
    {
        AtomicWrite(StoragePaths.PrivacyRulesFile, rules);
    }

    public List<FloatingWindowConfig> LoadSession()
    {
        try
        {
            if (File.Exists(StoragePaths.SessionFile))
            {
                var json = ReadAllTextShared(StoragePaths.SessionFile);
                var list = JsonSerializer.Deserialize<List<FloatingWindowConfig>>(json, JsonOptions);
                if (list != null) return list;
            }
        }
        catch (Exception ex)
        {
            BackupCorruptedFile(StoragePaths.SessionFile);
            Log($"Failed to load session: {ex.Message}");
        }

        return new List<FloatingWindowConfig>();
    }

    public void SaveSession(List<FloatingWindowConfig> windows)
    {
        AtomicWrite(StoragePaths.SessionFile, windows);
    }

    public void ClearSession()
    {
        try
        {
            if (File.Exists(StoragePaths.SessionFile))
            {
                File.Delete(StoragePaths.SessionFile);
            }
        }
        catch { }
    }

    public void ClearAllData()
    {
        ClearSession();
        try { if (File.Exists(StoragePaths.NotesFile)) File.Delete(StoragePaths.NotesFile); } catch { }
        try { if (File.Exists(StoragePaths.FavoritesFile)) File.Delete(StoragePaths.FavoritesFile); } catch { }
        try { if (File.Exists(StoragePaths.WorkspacesFile)) File.Delete(StoragePaths.WorkspacesFile); } catch { }
        try { if (File.Exists(StoragePaths.PrivacyRulesFile)) File.Delete(StoragePaths.PrivacyRulesFile); } catch { }
    }

    private static void BackupCorruptedFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var bakPath = filePath + ".corrupt.bak";
                File.Copy(filePath, bakPath, overwrite: true);
                Log($"Preserved backup copy of corrupted file: {bakPath}");
            }
        }
        catch { }
    }

    public static void AtomicWrite<T>(string filePath, T data)
    {
        lock (FileLock)
        {
            string? tempPath = null;
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(tempPath, json);

                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Replace(tempPath, filePath, null);
                    }
                    catch
                    {
                        File.Copy(tempPath, filePath, overwrite: true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, filePath);
                }
            }
            catch (Exception ex)
            {
                Log($"Atomic write failed for {filePath}: {ex.Message}");
            }
            finally
            {
                if (tempPath != null && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
    }

    public static void Log(string message)
    {
        try
        {
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(StoragePaths.LogFile, line);
        }
        catch { }
    }

    public static List<WorkspaceModel> GetDefaultWorkspaces()
    {
        return new List<WorkspaceModel>
        {
            new()
            {
                Id = "study",
                Name = "Study Mode",
                Description = "Tutorial video stacked with study music",
                Icon = "BookOpen",
                IsBuiltIn = true,
                Windows = new()
                {
                    new()
                    {
                        Title = "Lecture & Tutorial",
                        Url = "https://www.youtube.com/results?search_query=programming+tutorial",
                        Left = 80,
                        Top = 100,
                        Width = 560,
                        Height = 315,
                        Preset = WindowPreset.Compact,
                        IsTopmost = true
                    },
                    new()
                    {
                        Title = "Study Music Lo-Fi",
                        Url = "https://music.youtube.com",
                        Left = 660,
                        Top = 100,
                        Width = 400,
                        Height = 260,
                        Preset = WindowPreset.UltraMini,
                        IsTopmost = true
                    }
                }
            },
            new()
            {
                Id = "coding",
                Name = "Coding Setup",
                Description = "Corner floating reference for multi-screen editing",
                Icon = "Code",
                IsBuiltIn = true,
                Windows = new()
                {
                    new()
                    {
                        Title = "Dev Documentation",
                        Url = "https://learn.microsoft.com/en-us/dotnet/",
                        Left = 1200,
                        Top = 80,
                        Width = 480,
                        Height = 300,
                        Preset = WindowPreset.Compact,
                        IsTopmost = true,
                        Opacity = 0.95
                    }
                }
            },
            new()
            {
                Id = "gaming",
                Name = "Gaming Stream",
                Description = "Compact borderless stream monitor with click-through readiness",
                Icon = "Gamepad",
                IsBuiltIn = true,
                Windows = new()
                {
                    new()
                    {
                        Title = "Twitch Stream",
                        Url = "https://www.twitch.tv",
                        Left = 50,
                        Top = 50,
                        Width = 480,
                        Height = 270,
                        Preset = WindowPreset.Compact,
                        IsTopmost = true,
                        Opacity = 0.90
                    }
                }
            },
            new()
            {
                Id = "night_music",
                Name = "Night Music",
                Description = "Ultra-mini floating player with smooth low-power visualizer",
                Icon = "Moon",
                IsBuiltIn = true,
                Windows = new()
                {
                    new()
                    {
                        Title = "Night Music",
                        Url = "https://music.youtube.com",
                        Left = 100,
                        Top = 100,
                        Width = 360,
                        Height = 220,
                        Preset = WindowPreset.UltraMini,
                        IsTopmost = true,
                        Opacity = 0.92
                    }
                }
            }
        };
    }

    public static List<FavoriteItem> GetDefaultFavorites()
    {
        return new List<FavoriteItem>
        {
            new() { Title = "YouTube", Url = "https://www.youtube.com", Category = "Media", PreferredPreset = WindowPreset.Compact },
            new() { Title = "YouTube Music", Url = "https://music.youtube.com", Category = "Media", PreferredPreset = WindowPreset.UltraMini },
            new() { Title = "Twitch", Url = "https://www.twitch.tv", Category = "Media", PreferredPreset = WindowPreset.Cinema },
            new() { Title = "GitHub", Url = "https://github.com", Category = "Productivity", PreferredPreset = WindowPreset.Compact },
            new() { Title = "Microsoft Learn", Url = "https://learn.microsoft.com", Category = "Study", PreferredPreset = WindowPreset.Compact },
            new() { Title = "SoundCloud", Url = "https://soundcloud.com", Category = "Media", PreferredPreset = WindowPreset.UltraMini },
            new() { Title = "Localhost Testbed", Url = "http://localhost:5055", Category = "Testing", PreferredPreset = WindowPreset.Compact }
        };
    }

    public static List<PrivacyRule> GetDefaultPrivacyRules()
    {
        return new List<PrivacyRule>
        {
            new() { Domain = "doubleclick.net", IsBlocked = true },
            new() { Domain = "googlesyndication.com", IsBlocked = true },
            new() { Domain = "adservice.google.com", IsBlocked = true },
            new() { Domain = "googleadservices.com", IsBlocked = true },
            new() { Domain = "adnxs.com", IsBlocked = true },
            new() { Domain = "scorecardresearch.com", IsBlocked = true },
            new() { Domain = "quantserve.com", IsBlocked = true },
            new() { Domain = "outbrain.com", IsBlocked = true },
            new() { Domain = "taboola.com", IsBlocked = true }
        };
    }
}
