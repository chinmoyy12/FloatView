using System;
using System.Collections.Generic;
using System.Linq;
using FloatView.App.Models;
using FloatView.App.Persistence;
using FloatView.App.Platform;
using FloatView.App.Privacy;

namespace FloatView.App.Floating;

public class FloatingWindowManager
{
    private readonly SettingsService _settingsService;
    private readonly PrivacyFilterService _privacyService;
    private readonly List<FloatingWindow> _activeWindows = new();
    private bool _allFloatsHidden = false;

    public event Action? WindowsListChanged;
    public event Action<CaptionNote>? NoteCaptured;

    public IReadOnlyList<FloatingWindow> ActiveWindows => _activeWindows;
    public int ActiveCount => _activeWindows.Count;

    public FloatingWindowManager(SettingsService settingsService, PrivacyFilterService privacyService)
    {
        _settingsService = settingsService;
        _privacyService = privacyService;
    }

    public FloatingWindow? OpenFloat(FloatingWindowConfig? config = null)
    {
        int maxFloats = _settingsService.Settings.MaxActiveFloats;
        if (_activeWindows.Count >= maxFloats)
        {
            // Focus the first existing window
            _activeWindows[0].Activate();
            return _activeWindows[0];
        }

        config ??= new FloatingWindowConfig
        {
            Url = _settingsService.Settings.HomepageUrl,
            Left = 120 + (_activeWindows.Count * 40),
            Top = 120 + (_activeWindows.Count * 40),
            Width = 480,
            Height = 270,
            Opacity = _settingsService.Settings.DefaultWindowOpacity,
            IsTopmost = _settingsService.Settings.DefaultTopmost
        };

        // Ensure safe positioning
        var safe = DisplayService.EnsureVisible(config.Left, config.Top, config.Width, config.Height);
        config.Left = safe.Left;
        config.Top = safe.Top;

        var window = new FloatingWindow(config, _privacyService);
        window.ClosedRequested += OnWindowClosedRequested;
        window.ConfigChanged += OnWindowConfigChanged;
        window.NoteSaved += note => NoteCaptured?.Invoke(note);

        _activeWindows.Add(window);
        window.Show();

        SaveActiveSession();
        WindowsListChanged?.Invoke();
        return window;
    }

    public void CloseWindow(FloatingWindow window)
    {
        if (_activeWindows.Contains(window))
        {
            window.Close();
        }
    }

    public void CloseAll()
    {
        var list = _activeWindows.ToList();
        foreach (var w in list)
        {
            w.Close();
        }
        _activeWindows.Clear();
        _settingsService.ClearSession();
        WindowsListChanged?.Invoke();
    }

    public void ToggleAllVisibility()
    {
        _allFloatsHidden = !_allFloatsHidden;
        foreach (var w in _activeWindows)
        {
            w.Visibility = _allFloatsHidden ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }
    }

    public void ToggleTopmostAll()
    {
        if (_activeWindows.Count == 0) return;
        bool anyTopmost = _activeWindows.Any(w => w.Topmost);
        foreach (var w in _activeWindows)
        {
            w.Topmost = !anyTopmost;
        }
    }

    public void ToggleClickThroughAll()
    {
        foreach (var w in _activeWindows)
        {
            w.ToggleClickThrough();
        }
    }

    public async System.Threading.Tasks.Task TogglePlayPauseActiveAsync()
    {
        var target = _activeWindows.LastOrDefault(w => w.IsActive) ?? _activeWindows.LastOrDefault();
        if (target != null)
        {
            await target.TogglePlayPauseAsync();
        }
    }

    public void ArrangeSideBySide()
    {
        if (_activeWindows.Count == 0) return;
        var monitor = DisplayService.GetCurrentMonitor(_activeWindows[0].Left, _activeWindows[0].Top);
        var slots = WindowPositionService.ArrangeSideBySide(monitor, _activeWindows.Count);

        for (int i = 0; i < Math.Min(_activeWindows.Count, slots.Count); i++)
        {
            var w = _activeWindows[i];
            EdgeActivationService.AnimateWindowPosition(w, slots[i].Left, slots[i].Top, 220);
            w.Width = slots[i].Width;
            w.Height = slots[i].Height;
        }
        SaveActiveSession();
    }

    public void ArrangeStacked()
    {
        if (_activeWindows.Count == 0) return;
        var monitor = DisplayService.GetCurrentMonitor(_activeWindows[0].Left, _activeWindows[0].Top);
        var slots = WindowPositionService.ArrangeStacked(monitor, _activeWindows.Count);

        for (int i = 0; i < Math.Min(_activeWindows.Count, slots.Count); i++)
        {
            var w = _activeWindows[i];
            EdgeActivationService.AnimateWindowPosition(w, slots[i].Left, slots[i].Top, 220);
            w.Width = slots[i].Width;
            w.Height = slots[i].Height;
        }
        SaveActiveSession();
    }

    public void ArrangeGrid()
    {
        if (_activeWindows.Count < 2) return;
        var monitor = DisplayService.GetCurrentMonitor(_activeWindows[0].Left, _activeWindows[0].Top);
        var slots = WindowPositionService.ArrangeThreeWindowGrid(monitor);

        for (int i = 0; i < Math.Min(_activeWindows.Count, slots.Count); i++)
        {
            var w = _activeWindows[i];
            EdgeActivationService.AnimateWindowPosition(w, slots[i].Left, slots[i].Top, 220);
            w.Width = slots[i].Width;
            w.Height = slots[i].Height;
        }
        SaveActiveSession();
    }

    public void RestoreSession()
    {
        var sessionConfigs = _settingsService.LoadSession();
        if (sessionConfigs.Count > 0)
        {
            foreach (var cfg in sessionConfigs)
            {
                OpenFloat(cfg);
            }
        }
    }

    public void ApplyWorkspace(WorkspaceModel workspace)
    {
        CloseAll();
        foreach (var cfg in workspace.Windows)
        {
            OpenFloat(cfg);
        }
    }

    private void OnWindowClosedRequested(FloatingWindow window)
    {
        _activeWindows.Remove(window);
        SaveActiveSession();
        WindowsListChanged?.Invoke();
    }

    private void OnWindowConfigChanged(FloatingWindow window, FloatingWindowConfig config)
    {
        SaveActiveSession();
    }

    private void SaveActiveSession()
    {
        var configs = _activeWindows.Select(w => w.Config).ToList();
        _settingsService.SaveSession(configs);
    }
}
