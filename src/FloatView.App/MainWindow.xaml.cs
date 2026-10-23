using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using FloatView.App.Floating;
using FloatView.App.Focus;
using FloatView.App.Media;
using FloatView.App.Models;
using FloatView.App.Notifications;
using FloatView.App.Palette;
using FloatView.App.Persistence;
using FloatView.App.Platform;
using FloatView.App.Privacy;
using FloatView.App.Views;

namespace FloatView.App;

public partial class MainWindow : Window
{
    private IntPtr _hwnd = IntPtr.Zero;
    private readonly SettingsService _settingsService;
    private readonly PrivacyFilterService _privacyService;
    private readonly FloatingWindowManager _windowManager;
    private readonly FocusSessionService _focusService;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly BrowserBridgeService _browserBridge;
    private readonly AudioSessionWatcher _audioWatcher;
    private readonly SmartMediaCoordinator _smartCoordinator;
    private readonly TrayIconService _trayService;

    private MediaDetectedNotificationWindow? _activeNotification;
    private bool _forceShutdown = false;

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

        _browserBridge = new BrowserBridgeService();
        _audioWatcher = new AudioSessionWatcher(autoStart: true);
        _smartCoordinator = new SmartMediaCoordinator(_settingsService, _windowManager, _browserBridge, _audioWatcher);
        _trayService = new TrayIconService(this);

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

        // Wire Smart Media Detection Notification
        _smartCoordinator.NotificationRequested += candidate =>
        {
            Dispatcher.Invoke(() => ShowSmartMediaNotification(candidate));
        };

        // Wire Tray Icon Actions
        _trayService.DoubleClicked += () =>
        {
            Dispatcher.Invoke(RestoreMainWindow);
        };
        _trayService.RightClicked += () =>
        {
            Dispatcher.Invoke(ShowTrayContextMenu);
        };

        SwitchView(ViewDashboard);

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

        // Initialize System Tray
        _trayService.Initialize(_hwnd);

        // Start Browser Bridge
        _browserBridge.Start();
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
        // Alt + Space: Command Palette (with fallback to Ctrl + Alt + Space if contested)
        bool registered = _hotkeyService.RegisterAction("CommandPalette", Key.Space, ctrl: false, alt: true, shift: false, win: false, () =>
        {
            Dispatcher.Invoke(ShowCommandPalette);
        });

        if (!registered)
        {
            _hotkeyService.RegisterAction("CommandPaletteFallback", Key.Space, ctrl: true, alt: true, shift: false, win: false, () =>
            {
                Dispatcher.Invoke(ShowCommandPalette);
            });
            SettingsService.Log("Alt+Space conflict handled: registered Ctrl+Alt+Space fallback for Command Palette.");
        }

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

        // Ctrl + Alt + P: Media Play / Pause
        _hotkeyService.RegisterAction("MediaPlayPause", Key.P, ctrl: true, alt: true, shift: false, win: false, () =>
        {
            Dispatcher.Invoke(async () => await _windowManager.TogglePlayPauseActiveAsync());
        });

        // Ctrl + Alt + O: Focus Mode Toggle
        _hotkeyService.RegisterAction("FocusToggle", Key.O, ctrl: true, alt: true, shift: false, win: false, () =>
        {
            Dispatcher.Invoke(() => _focusService.TogglePlayPause());
        });
    }

    public void ShowSmartMediaNotification(MediaCandidate candidate)
    {
        if (_activeNotification != null)
        {
            try { _activeNotification.Close(); } catch { }
        }

        _activeNotification = new MediaDetectedNotificationWindow(
            candidate,
            onUseFloatView: cand =>
            {
                ShowMediaSourcePicker();
            },
            onDismiss: cand =>
            {
                _smartCoordinator.DismissCandidate(cand);
            });

        _activeNotification.Show();
    }

    public void ShowMediaSourcePicker()
    {
        var candidates = _smartCoordinator.ActiveCandidates;
        var picker = new MediaSourcePickerWindow(candidates, async cand =>
        {
            await _smartCoordinator.InitiateFloatAsync(cand);
        });
        picker.ShowDialog();
    }

    public void RestoreMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ShowTrayContextMenu()
    {
        var menu = new ContextMenu();

        var itemOpen = new MenuItem { Header = "FloatView", FontWeight = FontWeights.Bold };
        itemOpen.Click += (_, _) => RestoreMainWindow();
        menu.Items.Add(itemOpen);

        int count = _smartCoordinator.ActiveCandidates.Count;
        var itemMedia = new MenuItem
        {
            Header = count > 0 ? $"Choose Media to Float ({count})..." : "Choose Media to Float..."
        };
        itemMedia.Click += (_, _) => ShowMediaSourcePicker();
        menu.Items.Add(itemMedia);

        menu.Items.Add(new Separator());

        var itemSmartToggle = new MenuItem
        {
            Header = "Smart Media Detection",
            IsCheckable = true,
            IsChecked = _settingsService.Settings.SmartMediaDetectionEnabled
        };
        itemSmartToggle.Click += (_, _) =>
        {
            _settingsService.Settings.SmartMediaDetectionEnabled = itemSmartToggle.IsChecked;
            _settingsService.SaveSettings(_settingsService.Settings);
        };
        menu.Items.Add(itemSmartToggle);

        menu.Items.Add(new Separator());

        var itemExit = new MenuItem { Header = "Exit FloatView" };
        itemExit.Click += (_, _) =>
        {
            _forceShutdown = true;
            Application.Current.Shutdown();
        };
        menu.Items.Add(itemExit);

        _trayService.ShowContextMenu(menu);
    }

    private void ShowCommandPalette()
    {
        var commands = new List<PaletteCommand>
        {
            new("New Floating Window", "Open default floating player", "Floating", () => _windowManager.OpenFloat()),
            new("Choose Media to Float", "Open detected external browser media selector", "Media", ShowMediaSourcePicker),
            new("Toggle Smart Media Detection", "Enable or disable external browser playback detection", "Media", () =>
            {
                _settingsService.Settings.SmartMediaDetectionEnabled = !_settingsService.Settings.SmartMediaDetectionEnabled;
                _settingsService.SaveSettings(_settingsService.Settings);
            }),
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
            new("Quit FloatView", "Safely save session and exit", "App", () =>
            {
                _forceShutdown = true;
                Application.Current.Shutdown();
            })
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
        // If minimize to tray is enabled or floats are open, hide main window instead of terminating
        if (_settingsService.Settings.MinimizeToTrayOnClose || _settingsService.Settings.SmartMediaDetectionEnabled || _windowManager.ActiveCount > 0)
        {
            Hide();
        }
        else
        {
            _forceShutdown = true;
            Close();
        }
    }

    private void OnNavChanged(object sender, RoutedEventArgs e)
    {
        if (ViewDashboard == null) return;

        if (NavDashboard?.IsChecked == true) SwitchView(ViewDashboard);
        else if (NavBrowser?.IsChecked == true) SwitchView(ViewBrowser);
        else if (NavWorkspaces?.IsChecked == true) SwitchView(ViewWorkspaces);
        else if (NavNotes?.IsChecked == true) SwitchView(ViewNotes);
        else if (NavPrivacy?.IsChecked == true) SwitchView(ViewPrivacy);
        else if (NavSettings?.IsChecked == true) SwitchView(ViewSettings);
    }

    private void SwitchView(UIElement? view)
    {
        if (view == null || ViewDashboard == null) return;

        if (ViewDashboard != null) ViewDashboard.Visibility = Visibility.Collapsed;
        if (ViewBrowser != null) ViewBrowser.Visibility = Visibility.Collapsed;
        if (ViewWorkspaces != null) ViewWorkspaces.Visibility = Visibility.Collapsed;
        if (ViewNotes != null) ViewNotes.Visibility = Visibility.Collapsed;
        if (ViewPrivacy != null) ViewPrivacy.Visibility = Visibility.Collapsed;
        if (ViewSettings != null) ViewSettings.Visibility = Visibility.Collapsed;

        view.Visibility = Visibility.Visible;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceShutdown && (_settingsService.Settings.MinimizeToTrayOnClose || _settingsService.Settings.SmartMediaDetectionEnabled || _windowManager.ActiveCount > 0))
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _trayService.Dispose();
        _smartCoordinator.Dispose();
        _browserBridge.Dispose();
        _audioWatcher.Dispose();
        _hotkeyService.Dispose();
        _windowManager.CloseAll();
    }
}
