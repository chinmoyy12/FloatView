using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using FloatView.App.Browser;
using FloatView.App.Media;
using FloatView.App.Models;
using FloatView.App.Persistence;
using FloatView.App.Platform;
using FloatView.App.Privacy;
using Microsoft.Web.WebView2.Core;

namespace FloatView.App.Floating;

public partial class FloatingWindow : Window
{
    private IntPtr _hwnd = IntPtr.Zero;
    private readonly FloatingWindowConfig _config;
    private readonly PrivacyFilterService? _privacyService;
    private readonly AudioVisualizerService _visualizerService = new();
    private bool _isClickThrough = false;
    private bool _isPlaying = false;
    private bool _isMuted = false;
    private double _currentTime = 0;
    private string _currentCaption = string.Empty;
    private bool _isEdgeHidden = false;
    private double _normalLeft;
    private double _normalTop;

    public event Action<FloatingWindow, FloatingWindowConfig>? ConfigChanged;
    public event Action<FloatingWindow>? ClosedRequested;
    public event Action<CaptionNote>? NoteSaved;

    public FloatingWindowConfig Config => _config;

    public FloatingWindow(FloatingWindowConfig config, PrivacyFilterService? privacyService = null)
    {
        InitializeComponent();
        _config = config;
        _privacyService = privacyService;

        // Apply config dimensions and position
        Left = _config.Left;
        Top = _config.Top;
        Width = _config.Width;
        Height = _config.Height;
        Opacity = _config.Opacity;
        Topmost = _config.IsTopmost;
        TxtUrl.Text = _config.Url;
        SliderOpacity.Value = _config.Opacity;

        _normalLeft = Left;
        _normalTop = Top;

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closing += OnClosing;

        // Hover toolbar interactions
        BrowserContainer.MouseEnter += (_, _) =>
        {
            if (!_isClickThrough)
            {
                var sb = (Storyboard)FindResource("ShowToolbar");
                sb.Begin();
            }
        };

        BrowserContainer.MouseLeave += (_, _) =>
        {
            var sb = (Storyboard)FindResource("HideToolbar");
            sb.Begin();
        };

        _visualizerService.VisualizerFrame += OnVisualizerFrame;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        WindowsApi.ApplyImmersiveDarkMode(_hwnd);

        if (_config.IsClickThrough)
        {
            SetClickThrough(true);
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var env = await BrowserService.GetOrCreateEnvironmentAsync();
            await WebViewControl.EnsureCoreWebView2Async(env);

            var core = WebViewControl.CoreWebView2;
            if (core != null)
            {
                // Register Privacy Shield Request Filter
                if (_privacyService != null)
                {
                    core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                    core.WebResourceRequested += OnWebResourceRequested;
                }

                // Inject Media Detection script
                await core.AddScriptToExecuteOnDocumentCreatedAsync(MediaDetectionService.InjectedScript);
                core.WebMessageReceived += OnWebMessageReceived;

                core.NavigationStarting += (_, _) => LoadingOverlay.Visibility = Visibility.Visible;
                core.NavigationCompleted += (_, _) =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    TxtTitle.Text = core.DocumentTitle;
                };

                // Navigate to initial URL
                var initialUrl = BrowserService.NormalizeUrl(_config.Url);
                core.Navigate(initialUrl);
            }
        }
        catch (Exception ex)
        {
            SettingsService.Log($"WebView2 Init error: {ex.Message}");
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void OnWebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (_privacyService == null || !_privacyService.IsEnabled) return;

        try
        {
            if (Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var reqUri))
            {
                string? topLevelHost = null;
                if (Uri.TryCreate(WebViewControl.Source?.ToString(), UriKind.Absolute, out var pageUri))
                {
                    topLevelHost = pageUri.Host;
                }

                if (_privacyService.ShouldBlockUri(reqUri, topLevelHost, out _))
                {
                    var response = WebViewControl.CoreWebView2.Environment.CreateWebResourceResponse(
                        null, 403, "Blocked by FloatView Privacy Shield", null);
                    e.Response = response;
                }
            }
        }
        catch
        {
            // Don't crash on unparseable URIs
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "FLOATVIEW_MEDIA_STATE")
            {
                _isPlaying = root.GetProperty("isPlaying").GetBoolean();
                _currentTime = root.GetProperty("currentTime").GetDouble();
                _isMuted = root.GetProperty("isMuted").GetBoolean();
                var title = root.GetProperty("title").GetString();
                _currentCaption = root.GetProperty("currentCaption").GetString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(title))
                {
                    TxtTitle.Text = title;
                }

                // Update Play/Pause Button Icon
                IconPlayPausePath.Data = (System.Windows.Media.Geometry)FindResource(_isPlaying ? "IconPause" : "IconPlay");
                _visualizerService.IsPlaying = _isPlaying;

                // Update Caption Overlay
                if (!string.IsNullOrWhiteSpace(_currentCaption))
                {
                    TxtCaption.Text = _currentCaption;
                    CaptionOverlay.Visibility = Visibility.Visible;
                }
                else
                {
                    CaptionOverlay.Visibility = Visibility.Collapsed;
                }

                // Update Mute Icon
                IconMutePath.Data = (System.Windows.Media.Geometry)FindResource(_isMuted ? "IconVolumeOff" : "IconVolume");
            }
        }
        catch
        {
            // Message format mismatch ignored
        }
    }

    private void OnVisualizerFrame(double[] bars)
    {
        Dispatcher.Invoke(() =>
        {
            if (bars.Length >= 6)
            {
                Bar0.Height = Math.Max(2, bars[0] * 18);
                Bar1.Height = Math.Max(2, bars[1] * 22);
                Bar2.Height = Math.Max(2, bars[2] * 26);
                Bar3.Height = Math.Max(2, bars[3] * 26);
                Bar4.Height = Math.Max(2, bars[4] * 22);
                Bar5.Height = Math.Max(2, bars[5] * 18);
            }
        });
    }

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            OnCyclePresetClicked(sender, e);
            return;
        }

        if (_hwnd != IntPtr.Zero)
        {
            WindowsApi.ReleaseCapture();
            WindowsApi.SendMessage(_hwnd, WindowsApi.WM_NCLBUTTONDOWN, (IntPtr)WindowsApi.HT_CAPTION, IntPtr.Zero);
            
            // Snap to screen edge when dragging is completed
            _normalLeft = Left;
            _normalTop = Top;
            SnapPosition();
            NotifyConfigUpdated();
        }
    }

    public void SnapPosition()
    {
        var snapped = WindowPositionService.CalculateSnappedPosition(
            Left, Top, ActualWidth, ActualHeight, 16.0, new List<Rect>());
        Left = snapped.left;
        Top = snapped.top;
        _normalLeft = Left;
        _normalTop = Top;
    }

    public void SetClickThrough(bool enabled)
    {
        _isClickThrough = enabled;
        _config.IsClickThrough = enabled;
        BadgeClickThrough.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

        if (_hwnd != IntPtr.Zero)
        {
            if (enabled)
            {
                WindowsApi.EnableClickThrough(_hwnd);
                Opacity = Math.Min(Opacity, 0.85); // Slight visual cue
            }
            else
            {
                WindowsApi.DisableClickThrough(_hwnd);
            }
        }
        NotifyConfigUpdated();
    }

    public void ToggleClickThrough() => SetClickThrough(!_isClickThrough);

    private void OnToggleClickThroughClicked(object sender, RoutedEventArgs e) => ToggleClickThrough();

    private void OnToggleTopmostClicked(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        _config.IsTopmost = Topmost;
        IconPinPath.Data = (System.Windows.Media.Geometry)FindResource(Topmost ? "IconPin" : "IconPinOff");
        IconPinPath.Fill = Topmost ? (System.Windows.Media.Brush)FindResource("BrushAccent") : (System.Windows.Media.Brush)FindResource("BrushTextMuted");
        NotifyConfigUpdated();
    }

    private void OnCyclePresetClicked(object sender, RoutedEventArgs e)
    {
        var nextPreset = _config.Preset switch
        {
            WindowPreset.UltraMini => WindowPreset.Compact,
            WindowPreset.Compact => WindowPreset.Cinema,
            WindowPreset.Cinema => WindowPreset.UltraMini,
            _ => WindowPreset.Compact
        };

        ApplyPreset(nextPreset);
    }

    public void ApplyPreset(WindowPreset preset)
    {
        _config.Preset = preset;
        var (w, h) = FloatingWindowConfig.GetPresetDimensions(preset);
        Width = w;
        Height = h;
        NotifyConfigUpdated();
    }

    private void OnEdgeDockClicked(object sender, RoutedEventArgs e)
    {
        var monitor = DisplayService.GetCurrentMonitor(Left, Top);
        if (!_isEdgeHidden)
        {
            // Slide to Right Edge
            var (hiddenX, hiddenY) = EdgeActivationService.GetHiddenCoordinates(
                DockEdge.Right, monitor, Left, Top, ActualWidth, ActualHeight);
            
            EdgeActivationService.AnimateWindowPosition(this, hiddenX, hiddenY, 200, () =>
            {
                _isEdgeHidden = true;
                _config.DockEdge = DockEdge.Right;
                _config.IsEdgeHidden = true;
            });
        }
        else
        {
            // Restore from edge
            EdgeActivationService.AnimateWindowPosition(this, _normalLeft, _normalTop, 200, () =>
            {
                _isEdgeHidden = false;
                _config.DockEdge = DockEdge.None;
                _config.IsEdgeHidden = false;
            });
        }
    }

    private void OnOpacitySliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Opacity = e.NewValue;
        _config.Opacity = e.NewValue;
        NotifyConfigUpdated();
    }

    private void OnUrlKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateTo(TxtUrl.Text);
        }
    }

    private void OnNavigateClicked(object sender, RoutedEventArgs e) => NavigateTo(TxtUrl.Text);

    public void NavigateTo(string url)
    {
        var normalized = BrowserService.NormalizeUrl(url);
        TxtUrl.Text = normalized;
        _config.Url = normalized;
        if (WebViewControl?.CoreWebView2 != null)
        {
            WebViewControl.CoreWebView2.Navigate(normalized);
        }
        NotifyConfigUpdated();
    }

    private async void OnPlayPauseClicked(object sender, RoutedEventArgs e)
    {
        if (WebViewControl?.CoreWebView2 != null)
        {
            await MediaDetectionService.TogglePlayPauseAsync(WebViewControl.CoreWebView2);
        }
    }

    private async void OnRewindClicked(object sender, RoutedEventArgs e)
    {
        if (WebViewControl?.CoreWebView2 != null)
        {
            await MediaDetectionService.SeekRelativeAsync(WebViewControl.CoreWebView2, -10);
        }
    }

    private async void OnForwardClicked(object sender, RoutedEventArgs e)
    {
        if (WebViewControl?.CoreWebView2 != null)
        {
            await MediaDetectionService.SeekRelativeAsync(WebViewControl.CoreWebView2, 10);
        }
    }

    private async void OnMuteClicked(object sender, RoutedEventArgs e)
    {
        if (WebViewControl?.CoreWebView2 != null)
        {
            await MediaDetectionService.ToggleMuteAsync(WebViewControl.CoreWebView2);
        }
    }

    private async void OnPipClicked(object sender, RoutedEventArgs e)
    {
        if (WebViewControl?.CoreWebView2 != null)
        {
            bool success = await MediaDetectionService.RequestNativePipAsync(WebViewControl.CoreWebView2);
            if (!success)
            {
                // Fallback: Cycle preset to UltraMini float
                ApplyPreset(WindowPreset.UltraMini);
            }
        }
    }

    private void OnAddNoteClicked(object sender, RoutedEventArgs e)
    {
        var note = new CaptionNote
        {
            Url = _config.Url,
            PageTitle = TxtTitle.Text,
            TimestampSeconds = _currentTime,
            NoteText = "Captured from " + TxtTitle.Text,
            ExtractedCaption = _currentCaption,
            CreatedAt = DateTime.UtcNow
        };
        NoteSaved?.Invoke(note);
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        ClosedRequested?.Invoke(this);
        Close();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _visualizerService.Stop();
    }

    private void NotifyConfigUpdated()
    {
        _config.Left = Left;
        _config.Top = Top;
        _config.Width = Width;
        _config.Height = Height;
        _config.Opacity = Opacity;
        _config.IsTopmost = Topmost;
        ConfigChanged?.Invoke(this, _config);
    }
}
