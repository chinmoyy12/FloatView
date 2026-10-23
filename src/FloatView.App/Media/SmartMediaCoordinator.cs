using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FloatView.App.Floating;
using FloatView.App.Models;
using FloatView.App.Persistence;
using FloatView.App.Platform;

namespace FloatView.App.Media;

public class SmartMediaCoordinator : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly FloatingWindowManager _windowManager;
    private readonly BrowserBridgeService _browserBridge;
    private readonly AudioSessionWatcher _audioWatcher;

    private readonly ConcurrentDictionary<string, MediaCandidate> _candidates = new();
    private readonly ConcurrentDictionary<string, DateTime> _firstSeenPlaying = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastNotified = new();

    private readonly Timer _housekeepingTimer;
    private bool _disposed = false;

    public event Action<MediaCandidate>? NotificationRequested;
    public event Action? CandidatesChanged;

    public IReadOnlyList<MediaCandidate> ActiveCandidates
    {
        get
        {
            PruneInactiveCandidates();
            return _candidates.Values
                .Where(c => c.IsPlaying)
                .OrderByDescending(c => c.SourceType == MediaSourceType.Extension)
                .ThenByDescending(c => c.AudioPeak)
                .ThenByDescending(c => c.LastActiveAt)
                .ToList();
        }
    }

    public SmartMediaCoordinator(
        SettingsService settingsService,
        FloatingWindowManager windowManager,
        BrowserBridgeService browserBridge,
        AudioSessionWatcher audioWatcher)
    {
        _settingsService = settingsService;
        _windowManager = windowManager;
        _browserBridge = browserBridge;
        _audioWatcher = audioWatcher;

        _browserBridge.MediaEventReceived += OnExtensionMediaEvent;
        _browserBridge.MediaEndedReceived += OnExtensionMediaEnded;
        _audioWatcher.AudioSessionsUpdated += OnAudioSessionsUpdated;

        _housekeepingTimer = new Timer(OnHousekeepingTick, null, 2000, 2000);
    }

    private void OnExtensionMediaEvent(MediaCandidate candidate, MediaEventMessage rawMsg)
    {
        if (!_settingsService.Settings.SmartMediaDetectionEnabled) return;

        // Ignore if URL is already floating in FloatView
        if (IsAlreadyFloating(candidate.Url)) return;

        candidate.LastActiveAt = DateTime.UtcNow;
        _candidates[candidate.Id] = candidate;

        if (candidate.IsPlaying)
        {
            CheckDebounceAndNotify(candidate);
        }
        else
        {
            _firstSeenPlaying.TryRemove(candidate.Id, out _);
        }

        CandidatesChanged?.Invoke();
    }

    private void OnExtensionMediaEnded(int tabId, string url)
    {
        var keysToRemove = _candidates.Keys.Where(k => k.StartsWith($"ext:{tabId}:")).ToList();
        foreach (var key in keysToRemove)
        {
            _candidates.TryRemove(key, out _);
            _firstSeenPlaying.TryRemove(key, out _);
        }
        CandidatesChanged?.Invoke();
    }

    private void OnAudioSessionsUpdated(List<MediaCandidate> audioCandidates)
    {
        if (!_settingsService.Settings.SmartMediaDetectionEnabled) return;
        if (!_settingsService.Settings.SmartDetectionAudioFallbackEnabled) return;

        // If we already have fresh extension candidates, audio session fallback is suppressed for matching processes
        var extensionCandidates = _candidates.Values
            .Where(c => c.SourceType == MediaSourceType.Extension && (DateTime.UtcNow - c.LastActiveAt).TotalSeconds < 5)
            .ToList();

        var activePids = new HashSet<int>();

        foreach (var audioCand in audioCandidates)
        {
            activePids.Add(audioCand.ProcessId);

            // If extension already covers this browser, do not duplicate
            bool alreadyCovered = extensionCandidates.Any(e =>
                e.BrowserProcessName.Equals(audioCand.BrowserProcessName, StringComparison.OrdinalIgnoreCase) ||
                e.BrowserName.Equals(audioCand.BrowserName, StringComparison.OrdinalIgnoreCase));

            if (alreadyCovered) continue;

            audioCand.LastActiveAt = DateTime.UtcNow;
            _candidates[audioCand.Id] = audioCand;

            CheckDebounceAndNotify(audioCand);
        }

        // Remove stale wasapi candidates whose process audio stopped
        var staleWasapiKeys = _candidates.Values
            .Where(c => c.SourceType == MediaSourceType.AudioSessionFallback && !activePids.Contains(c.ProcessId))
            .Select(c => c.Id)
            .ToList();

        foreach (var key in staleWasapiKeys)
        {
            _candidates.TryRemove(key, out _);
            _firstSeenPlaying.TryRemove(key, out _);
        }

        CandidatesChanged?.Invoke();
    }

    private void CheckDebounceAndNotify(MediaCandidate candidate)
    {
        if (!_settingsService.Settings.SmartDetectionShowToast) return;

        var now = DateTime.UtcNow;
        var firstSeen = _firstSeenPlaying.GetOrAdd(candidate.Id, now);

        int debounceSecs = Math.Max(1, _settingsService.Settings.SmartDetectionDebounceSeconds);
        if ((now - firstSeen).TotalSeconds < debounceSecs)
        {
            // Still in debounce verification period
            return;
        }

        // Check Cooldown
        string cooldownKey = !string.IsNullOrWhiteSpace(candidate.Url) ? candidate.Url : candidate.Domain;
        if (string.IsNullOrWhiteSpace(cooldownKey)) cooldownKey = candidate.Id;

        int cooldownSecs = Math.Max(5, _settingsService.Settings.SmartDetectionCooldownSeconds);
        if (_lastNotified.TryGetValue(cooldownKey, out var lastTime))
        {
            if ((now - lastTime).TotalSeconds < cooldownSecs)
            {
                return; // In cooldown
            }
        }

        // Check if user is currently floating this media
        if (IsAlreadyFloating(candidate.Url)) return;

        _lastNotified[cooldownKey] = now;
        NotificationRequested?.Invoke(candidate);
    }

    public bool IsAlreadyFloating(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        try
        {
            var targetUri = new Uri(url);
            foreach (var win in _windowManager.ActiveWindows)
            {
                if (Uri.TryCreate(win.Config.Url, UriKind.Absolute, out var activeUri))
                {
                    if (string.Equals(activeUri.Host, targetUri.Host, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(activeUri.AbsolutePath, targetUri.AbsolutePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch {}
        return false;
    }

    public static string BuildHandoffUrl(MediaCandidate candidate)
    {
        string url = candidate.Url?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            if (!string.IsNullOrWhiteSpace(candidate.Title))
            {
                return $"https://www.google.com/search?q={Uri.EscapeDataString(candidate.Title)}";
            }
            return "https://www.youtube.com";
        }

        // YouTube timestamp handoff
        if (candidate.CurrentTime > 2 && (url.Contains("youtube.com") || url.Contains("youtu.be")))
        {
            int seekSecs = (int)Math.Floor(candidate.CurrentTime);
            // If already has timestamp, replace or append
            if (url.Contains("#t=") || url.Contains("&t=") || url.Contains("?t="))
            {
                return url; // already specified
            }
            if (url.Contains("?"))
            {
                return $"{url}&t={seekSecs}s";
            }
            return $"{url}?t={seekSecs}s";
        }

        return url;
    }

    public async Task<FloatingWindow?> InitiateFloatAsync(MediaCandidate candidate)
    {
        string targetUrl = BuildHandoffUrl(candidate);

        var config = new FloatingWindowConfig
        {
            Url = targetUrl,
            Title = candidate.Title,
            Preset = WindowPreset.Compact,
            IsTopmost = _settingsService.Settings.DefaultTopmost,
            Opacity = _settingsService.Settings.DefaultWindowOpacity
        };

        var window = _windowManager.OpenFloat(config);

        // Safe handoff policy
        if (_settingsService.Settings.SmartDetectionSafeHandoff && candidate.SourceType == MediaSourceType.Extension && candidate.TabId > 0)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Allow the destination float window to initialize and verify playback before pausing source
                    await Task.Delay(2500);
                    _browserBridge.SendPauseCommand(candidate.TabId, candidate.Url);
                    SettingsService.Log($"Safe handoff: Source tab {candidate.TabId} paused after float confirmation.");
                }
                catch (Exception ex)
                {
                    SettingsService.Log($"Safe handoff pause error: {ex.Message}");
                }
            });
        }

        // Put candidate in cooldown so user isn't prompted again immediately
        string cooldownKey = !string.IsNullOrWhiteSpace(candidate.Url) ? candidate.Url : candidate.Domain;
        _lastNotified[cooldownKey] = DateTime.UtcNow;

        return window;
    }

    public void DismissCandidate(MediaCandidate candidate)
    {
        string cooldownKey = !string.IsNullOrWhiteSpace(candidate.Url) ? candidate.Url : candidate.Domain;
        _lastNotified[cooldownKey] = DateTime.UtcNow;
    }

    private void OnHousekeepingTick(object? state)
    {
        PruneInactiveCandidates();
    }

    private void PruneInactiveCandidates()
    {
        var now = DateTime.UtcNow;
        var staleKeys = _candidates.Values
            .Where(c => (now - c.LastActiveAt).TotalSeconds > 10)
            .Select(c => c.Id)
            .ToList();

        foreach (var key in staleKeys)
        {
            _candidates.TryRemove(key, out _);
            _firstSeenPlaying.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _housekeepingTimer.Dispose();
        _browserBridge.MediaEventReceived -= OnExtensionMediaEvent;
        _browserBridge.MediaEndedReceived -= OnExtensionMediaEnded;
        _audioWatcher.AudioSessionsUpdated -= OnAudioSessionsUpdated;
    }
}
