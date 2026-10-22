using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using FloatView.App.Floating;
using FloatView.App.Focus;
using FloatView.App.Media;
using FloatView.App.Models;
using FloatView.App.Palette;
using FloatView.App.Persistence;
using FloatView.App.Platform;
using FloatView.App.Privacy;

namespace FloatView.App;

public partial class MainWindow : Window
{
    private IntPtr _hwnd = IntPtr.Zero;
    private readonly SettingsService _settingsService;
    private readonly PrivacyFilterService _privacyService;
    private readonly FloatingWindowManager _windowManager;
    private readonly FocusSessionService _focusService;
    private readonly GlobalHotkeyService _hotkeyService;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = new SettingsService();
        _privacyService = new PrivacyFilterService(_settingsService);
        _windowManager = new FloatingWindowManager(_settingsService, _privacyService);
        _focusService = new FocusSessionService(
            _settingsService.Settings.FocusDurationMinutes,
            _settingsService.Settings.FocusBreakDurationMinutes);
        _hotkeyService = new GlobalHotkeyService();

        // Wire child views
        ViewDashboard.Initialize(_windowManager, _focusService, _settingsService);
        ViewBrowser.Initialize(_windowManager, _privacyService);
        ViewWorkspaces.Initialize(_windowManager, _settingsService);
        ViewNotes.Initialize(_settingsService);
        ViewPrivacy.Initialize(_privacyService);
        ViewSettings.Initialize(_settingsService);

        // Connect note captured event from any floating window
        _windowManager.NoteCaptured += note => ViewNotes.AddNote(note);
        _windowManager.WindowsListChanged += () =>
        {
            Dispatcher.Invoke(() =>
            {
                TxtNavFloatsCount.Text = $"{_windowManager.ActiveCount} Open";
            });
        };

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowsApi.ApplyImmersiveDarkMode(_hwnd);

        // Register Global System-Wide Hotkeys
        _hotkeyService.Initialize(_hwnd);
        RegisterHotkeys();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Session restoration
        if (_settingsService.Settings.StartupRestoreOption == "LastWorkspace")
        {
            _windowManager.RestoreSession();
        }
    }

    private void RegisterHotkeys()
    {
        // Alt + Space: Command Palette
        _hotkeyService.RegisterAction("CommandPalette", Key.Space, ctrl: false, alt: true, shift: false, win: false, () =>
        {
            Dispatcher.Invoke(ShowCommandPalette);
        });

        // Ctrl + Alt + F: Toggle all floats
        _hotkeyService.RegisterAction("ToggleAllFloats", Key.F, ctrl: true, alt: true, shift: false, win: false, () =>
        {
            Dispatcher.Invoke(() => _windowManager.ToggleAllVisibility());
        });

        // Ctrl + Alt + T: Toggle topmost
        _hotkeyService.RegisterAction("ToggleTopmost", Key.T, ctrl: true, alt: true, shift: false, win: false, () =>
        {
            Dispatcher.Invoke(() => _windowManager.ToggleTopmostAll());
        });

        // Ctrl + Alt + K: Toggle click-through
        _hotkeyService.RegisterAction("ToggleClickThrough", Key.K, ctrl: true, alt: true, shift: false, win: false, () =>
        {
            Dispatcher.Invoke(() => _windowManager.ToggleClickThroughAll());
        });

        // Ctrl + Alt + O: Focus Mode Toggle
        _hotkeyService.RegisterAction("FocusToggle", Key.O, ctrl: true, alt: true, shift: false, win: false, () =>
        {
            Dispatcher.Invoke(() => _focusService.TogglePlayPause());
        });
    }

    private void ShowCommandPalette()
    {
        var commands = new List<PaletteCommand>
        {
            new("New Floating Window", "Open default floating player", "Floating", () => _windowManager.OpenFloat()),
            new("Open: YouTube", "Watch videos in floating window", "Favorites", () => _windowManager.OpenFloat(new FloatingWindowConfig { Title = "YouTube", Url = "https://www.youtube.com", Preset = WindowPreset.Compact })),
            new("Open: YouTube Music", "Stream audio in ultra-mini float", "Favorites", () => _windowManager.OpenFloat(new FloatingWindowConfig { Title = "YouTube Music", Url = "https://music.youtube.com", Preset = WindowPreset.UltraMini })),
            new("Open: Twitch", "Watch live stream in cinema mode", "Favorites", () => _windowManager.OpenFloat(new FloatingWindowConfig { Title = "Twitch", Url = "https://www.twitch.tv", Preset = WindowPreset.Cinema })),
            new("Toggle All Floats", "Hide or show all active floating windows", "Window", () => _windowManager.ToggleAllVisibility()),
            new("Toggle Always-on-Top", "Pin or unpin all floating windows", "Window", () => _windowManager.ToggleTopmostAll()),
            new("Toggle Click-Through Mode", "Pass mouse clicks to apps behind floats (Ctrl+Alt+K)", "Window", () => _windowManager.ToggleClickThroughAll()),
            new("Arrange: Side-by-Side", "Tile active floating windows horizontally", "Layout", () => _windowManager.ArrangeSideBySide()),
            new("Arrange: Stacked", "Stack active floating windows vertically on right edge", "Layout", () => _windowManager.ArrangeStacked()),
            new("Arrange: 3-Window Grid", "Arrange cinema video + secondary stack", "Layout", () => _windowManager.ArrangeGrid()),
            new("Workspace: Study Mode", "Load tutorial lecture and lo-fi music stack", "Workspace", () =>
            {
                var ws = SettingsService.GetDefaultWorkspaces().Find(w => w.Id == "study");
                if (ws != null) _windowManager.ApplyWorkspace(ws);
            }),
            new("Workspace: Coding Setup", "Load reference docs float in corner", "Workspace", () =>
            {
                var ws = SettingsService.GetDefaultWorkspaces().Find(w => w.Id == "coding");
                if (ws != null) _windowManager.ApplyWorkspace(ws);
            }),
            new("Workspace: Gaming Stream", "Load compact borderless stream", "Workspace", () =>
            {
                var ws = SettingsService.GetDefaultWorkspaces().Find(w => w.Id == "gaming");
                if (ws != null) _windowManager.ApplyWorkspace(ws);
            }),
            new("Toggle Privacy Shield", "Enable or disable ad/tracker network filtering", "Privacy", () =>
            {
                _privacyService.IsEnabled = !_privacyService.IsEnabled;
            }),
            new("Focus Mode: Start/Pause", "Start or pause the 25-minute Pomodoro timer", "Focus", () => _focusService.TogglePlayPause()),
            new("Focus Mode: Reset", "Reset timer to initial interval", "Focus", () => _focusService.Reset()),
            new("Settings", "Open application configuration", "App", () =>
            {
                NavSettings.IsChecked = true;
                SwitchView(ViewSettings);
            }),
            new("Close All Floating Windows", "Close all open floats and clear session", "Window", () => _windowManager.CloseAll()),
            new("Quit FloatView", "Safely save session and exit", "App", () => Application.Current.Shutdown())
        };

        var palette = new CommandPaletteWindow(commands);
        palette.ShowDialog();
    }

    private void OnCommandPaletteClicked(object sender, RoutedEventArgs e) => ShowCommandPalette();

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            OnMaximizeClicked(sender, e);
            return;
        }

        if (_hwnd != IntPtr.Zero)
        {
            WindowsApi.ReleaseCapture();
            WindowsApi.SendMessage(_hwnd, WindowsApi.WM_NCLBUTTONDOWN, (IntPtr)WindowsApi.HT_CAPTION, IntPtr.Zero);
        }
    }

    private void OnMinimizeClicked(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeClicked(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        // If floating windows are open, hide main window instead of terminating
        if (_windowManager.ActiveCount > 0)
        {
            Hide();
        }
        else
        {
            Close();
        }
    }

    private void OnNavChanged(object sender, RoutedEventArgs e)
    {
        if (NavDashboard.IsChecked == true) SwitchView(ViewDashboard);
        else if (NavBrowser.IsChecked == true) SwitchView(ViewBrowser);
        else if (NavWorkspaces.IsChecked == true) SwitchView(ViewWorkspaces);
        else if (NavNotes.IsChecked == true) SwitchView(ViewNotes);
        else if (NavPrivacy.IsChecked == true) SwitchView(ViewPrivacy);
        else if (NavSettings.IsChecked == true) SwitchView(ViewSettings);
    }

    private void SwitchView(UIElement view)
    {
        ViewDashboard.Visibility = Visibility.Collapsed;
        ViewBrowser.Visibility = Visibility.Collapsed;
        ViewWorkspaces.Visibility = Visibility.Collapsed;
        ViewNotes.Visibility = Visibility.Collapsed;
        ViewPrivacy.Visibility = Visibility.Collapsed;
        ViewSettings.Visibility = Visibility.Collapsed;

        view.Visibility = Visibility.Visible;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _hotkeyService.Dispose();
        _windowManager.CloseAll();
    }
}