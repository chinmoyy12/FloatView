using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FloatView.App.Models;
using FloatView.App.Persistence;

namespace FloatView.App.Platform;

[ComImport]
[Guid("77AA99A0-1BD6-447F-8F77-44042EC15EF5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionManager2
{
    [PreserveSig]
    int GetAudioSessionControl(IntPtr AudioSessionGuid, int StreamFlags, out IntPtr SessionControl);
    [PreserveSig]
    int GetSimpleAudioVolume(IntPtr AudioSessionGuid, int StreamFlags, out IntPtr AudioVolume);
    [PreserveSig]
    int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);
    [PreserveSig]
    int RegisterSessionNotification(IntPtr NewSessionNotifications);
    [PreserveSig]
    int UnregisterSessionNotification(IntPtr NewSessionNotifications);
    [PreserveSig]
    int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionID, IntPtr duckNotification);
    [PreserveSig]
    int UnregisterDuckNotification(IntPtr duckNotification);
}

[ComImport]
[Guid("E2F5EE11-2079-4DC5-955B-2F7B3E25436E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionEnumerator
{
    [PreserveSig]
    int GetCount(out int SessionCount);
    [PreserveSig]
    int GetSession(int SessionIndex, out IAudioSessionControl Session);
}

[ComImport]
[Guid("F4B41860-6743-4AB8-A780-28129EB3D022"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionControl
{
    [PreserveSig]
    int GetState(out AudioSessionState pRetVal);
    [PreserveSig]
    int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, IntPtr EventContext);
    [PreserveSig]
    int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, IntPtr EventContext);
    [PreserveSig]
    int GetGroupingParam(out Guid pRetVal);
    [PreserveSig]
    int SetGroupingParam(ref Guid Override, IntPtr EventContext);
    [PreserveSig]
    int RegisterAudioSessionNotification(IntPtr NewNotifications);
    [PreserveSig]
    int UnregisterAudioSessionNotification(IntPtr NewNotifications);
}

[ComImport]
[Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c31"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionControl2
{
    // Inherited from IAudioSessionControl
    [PreserveSig]
    int GetState(out AudioSessionState pRetVal);
    [PreserveSig]
    int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, IntPtr EventContext);
    [PreserveSig]
    int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, IntPtr EventContext);
    [PreserveSig]
    int GetGroupingParam(out Guid pRetVal);
    [PreserveSig]
    int SetGroupingParam(ref Guid Override, IntPtr EventContext);
    [PreserveSig]
    int RegisterAudioSessionNotification(IntPtr NewNotifications);
    [PreserveSig]
    int UnregisterAudioSessionNotification(IntPtr NewNotifications);

    // IAudioSessionControl2 specific
    [PreserveSig]
    int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
    [PreserveSig]
    int GetProcessId(out uint pRetVal);
    [PreserveSig]
    int IsSystemSoundsSession();
    [PreserveSig]
    int SetDuckingPreference(bool optOut);
}

public enum AudioSessionState
{
    Inactive = 0,
    Active = 1,
    Expired = 2
}

public class AudioSessionWatcher : IDisposable
{
    private static readonly Guid IidAudioSessionManager2 = new("77AA99A0-1BD6-447F-8F77-44042EC15EF5");
    private const int CLSCTX_INPROC_SERVER = 1;
    private const int eRender = 0;
    private const int eMultimedia = 1;

    private readonly Timer? _pollTimer;
    private bool _isScanning = false;
    private bool _disposed = false;

    public event Action<List<MediaCandidate>>? AudioSessionsUpdated;

    private static readonly HashSet<string> KnownBrowserProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome",
        "msedge",
        "brave",
        "firefox",
        "opera",
        "vivaldi",
        "arc",
        "waterfox"
    };

    public AudioSessionWatcher(bool autoStart = true, int intervalMs = 800)
    {
        if (autoStart)
        {
            _pollTimer = new Timer(OnPollTimerTick, null, 1000, intervalMs);
        }
    }

    private void OnPollTimerTick(object? state)
    {
        if (_disposed || _isScanning) return;
        _isScanning = true;
        try
        {
            var candidates = ScanAudioSessions();
            AudioSessionsUpdated?.Invoke(candidates);
        }
        catch (Exception ex)
        {
            SettingsService.Log($"AudioSessionWatcher tick error: {ex.Message}");
        }
        finally
        {
            _isScanning = false;
        }
    }

    public List<MediaCandidate> ScanAudioSessions()
    {
        var results = new List<MediaCandidate>();
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;

        try
        {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            int hr = enumerator.GetDefaultAudioEndpoint(eRender, eMultimedia, out device);
            if (hr != 0 || device == null) return results;

            var iid = IidAudioSessionManager2;
            hr = device.Activate(ref iid, CLSCTX_INPROC_SERVER, IntPtr.Zero, out IntPtr pManager);
            if (hr != 0 || pManager == IntPtr.Zero) return results;

            IAudioSessionManager2? manager = null;
            try
            {
                var managerObj = Marshal.GetObjectForIUnknown(pManager);
                manager = managerObj as IAudioSessionManager2;
                if (manager == null) return results;

                hr = manager.GetSessionEnumerator(out var sessionEnum);
                if (hr != 0 || sessionEnum == null) return results;

                hr = sessionEnum.GetCount(out int count);
                if (hr != 0 || count <= 0) return results;

                int currentPid = Environment.ProcessId;

                for (int i = 0; i < count; i++)
                {
                    hr = sessionEnum.GetSession(i, out var sessionControl);
                    if (hr != 0 || sessionControl == null) continue;

                    try
                    {
                        if (sessionControl is IAudioSessionControl2 control2)
                        {
                            hr = control2.GetState(out var state);
                            if (hr != 0 || state != AudioSessionState.Active) continue;

                            hr = control2.GetProcessId(out uint pid);
                            if (hr != 0 || pid == 0 || pid == currentPid) continue;

                            float peak = 0f;
                            if (sessionControl is IAudioMeterInformation meterInfo)
                            {
                                meterInfo.GetPeakValue(out peak);
                            }

                            // If peak is negligible or zero, it's not currently producing audible media
                            if (peak < 0.005f) continue;

                            // Determine process name
                            string processName = string.Empty;
                            try
                            {
                                using var proc = Process.GetProcessById((int)pid);
                                processName = proc.ProcessName;
                            }
                            catch
                            {
                                continue;
                            }

                            if (!KnownBrowserProcesses.Contains(processName))
                            {
                                continue; // Focus on external browsers
                            }

                            string browserFriendlyName = GetBrowserDisplayName(processName);
                            string? windowTitle = WindowsApi.GetMainWindowTitleForProcess((int)pid);

                            var candidate = ParseCandidateFromProcess(
                                (int)pid,
                                processName,
                                browserFriendlyName,
                                windowTitle,
                                peak);

                            if (candidate != null)
                            {
                                results.Add(candidate);
                            }
                        }
                    }
                    finally
                    {
                        if (Marshal.IsComObject(sessionControl))
                        {
                            Marshal.ReleaseComObject(sessionControl);
                        }
                    }
                }
            }
            finally
            {
                Marshal.Release(pManager);
            }
        }
        catch (Exception ex)
        {
            SettingsService.Log($"ScanAudioSessions exception: {ex.Message}");
        }
        finally
        {
            if (device != null && Marshal.IsComObject(device)) Marshal.ReleaseComObject(device);
            if (enumerator != null && Marshal.IsComObject(enumerator)) Marshal.ReleaseComObject(enumerator);
        }

        return results;
    }

    public static MediaCandidate? ParseCandidateFromProcess(
        int pid,
        string processName,
        string browserFriendlyName,
        string? windowTitle,
        float peak)
    {
        string rawTitle = windowTitle?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawTitle))
        {
            rawTitle = $"{browserFriendlyName} Media Audio";
        }

        string cleanTitle = rawTitle;
        string domain = string.Empty;
        string inferredUrl = string.Empty;

        // Clean browser name suffixes: " - Google Chrome", " - Microsoft​ Edge", etc.
        string[] browserSuffixes = {
            " - Google Chrome",
            " - Microsoft Edge",
            " - Brave",
            " - Mozilla Firefox",
            " - Firefox"
        };

        foreach (var suffix in browserSuffixes)
        {
            if (cleanTitle.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                cleanTitle = cleanTitle[..^suffix.Length].Trim();
            }
        }

        // Detect known streaming domains from title
        if (cleanTitle.Contains("YouTube", StringComparison.OrdinalIgnoreCase))
        {
            domain = "youtube.com";
            cleanTitle = cleanTitle.Replace(" - YouTube", "", StringComparison.OrdinalIgnoreCase).Trim();
            inferredUrl = "https://www.youtube.com";
        }
        else if (cleanTitle.Contains("Twitch", StringComparison.OrdinalIgnoreCase))
        {
            domain = "twitch.tv";
            cleanTitle = cleanTitle.Replace(" - Twitch", "", StringComparison.OrdinalIgnoreCase).Trim();
            inferredUrl = "https://www.twitch.tv";
        }
        else if (cleanTitle.Contains("Netflix", StringComparison.OrdinalIgnoreCase))
        {
            domain = "netflix.com";
            inferredUrl = "https://www.netflix.com";
        }
        else if (cleanTitle.Contains("SoundCloud", StringComparison.OrdinalIgnoreCase))
        {
            domain = "soundcloud.com";
            inferredUrl = "https://www.soundcloud.com";
        }
        else if (cleanTitle.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
        {
            domain = "spotify.com";
            inferredUrl = "https://open.spotify.com";
        }
        else
        {
            domain = $"{processName}.exe";
            inferredUrl = string.Empty;
        }

        return new MediaCandidate
        {
            Id = $"wasapi:{pid}",
            Title = string.IsNullOrWhiteSpace(cleanTitle) ? $"{browserFriendlyName} Media" : cleanTitle,
            Url = inferredUrl,
            Domain = domain,
            BrowserName = browserFriendlyName,
            BrowserProcessName = processName,
            ProcessId = pid,
            TabId = -1,
            IsPlaying = true,
            AudioPeak = peak,
            SourceType = MediaSourceType.AudioSessionFallback,
            DetectedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };
    }

    private static string GetBrowserDisplayName(string processName)
    {
        return processName.ToLowerInvariant() switch
        {
            "chrome" => "Google Chrome",
            "msedge" => "Microsoft Edge",
            "brave" => "Brave Browser",
            "firefox" => "Mozilla Firefox",
            "opera" => "Opera",
            "vivaldi" => "Vivaldi",
            "arc" => "Arc Browser",
            _ => char.ToUpper(processName[0]) + processName[1..]
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pollTimer?.Dispose();
    }
}
