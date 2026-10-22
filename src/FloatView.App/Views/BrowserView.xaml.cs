using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FloatView.App.Browser;
using FloatView.App.Floating;
using FloatView.App.Models;
using FloatView.App.Persistence;
using FloatView.App.Privacy;

namespace FloatView.App.Views;

public partial class BrowserView : UserControl
{
    private FloatingWindowManager? _windowManager;
    private PrivacyFilterService? _privacyService;

    public BrowserView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void Initialize(FloatingWindowManager windowManager, PrivacyFilterService privacyService)
    {
        _windowManager = windowManager;
        _privacyService = privacyService;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var env = await BrowserService.GetOrCreateEnvironmentAsync();
            await MainWebView.EnsureCoreWebView2Async(env);

            var core = MainWebView.CoreWebView2;
            if (core != null)
            {
                if (_privacyService != null)
                {
                    core.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);
                    core.WebResourceRequested += (s, args) =>
                    {
                        if (!_privacyService.IsEnabled) return;
                        try
                        {
                            if (Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out var reqUri))
                            {
                                string? topLevelHost = null;
                                if (Uri.TryCreate(core.Source, UriKind.Absolute, out var pageUri))
                                {
                                    topLevelHost = pageUri.Host;
                                }
                                if (_privacyService.ShouldBlockUri(reqUri, topLevelHost, out _))
                                {
                                    args.Response = core.Environment.CreateWebResourceResponse(null, 403, "Blocked by FloatView Privacy Shield", null);
                                }
                            }
                        }
                        catch {}
                    };
                }

                await core.AddScriptToExecuteOnDocumentCreatedAsync(MediaDetectionService.InjectedScript);

                core.NavigationStarting += (_, _) => WebLoadingOverlay.Visibility = Visibility.Visible;
                core.NavigationCompleted += (_, _) =>
                {
                    WebLoadingOverlay.Visibility = Visibility.Collapsed;
                    TxtAddress.Text = core.Source;
                };

                core.Navigate("https://www.youtube.com");
            }
        }
        catch (Exception ex)
        {
            SettingsService.Log($"MainWebView load error: {ex.Message}");
            WebLoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void OnAddressKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateTo(TxtAddress.Text);
        }
    }

    private void OnGoClicked(object sender, RoutedEventArgs e) => NavigateTo(TxtAddress.Text);

    public void NavigateTo(string url)
    {
        var normalized = BrowserService.NormalizeUrl(url);
        TxtAddress.Text = normalized;
        if (MainWebView?.CoreWebView2 != null)
        {
            MainWebView.CoreWebView2.Navigate(normalized);
        }
    }

    private void OnBackClicked(object sender, RoutedEventArgs e)
    {
        if (MainWebView?.CanGoBack == true) MainWebView.GoBack();
    }

    private void OnForwardClicked(object sender, RoutedEventArgs e)
    {
        if (MainWebView?.CanGoForward == true) MainWebView.GoForward();
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        MainWebView?.Reload();
    }

    private void OnPopOutClicked(object sender, RoutedEventArgs e)
    {
        var currentUrl = MainWebView?.Source?.ToString() ?? TxtAddress.Text;
        _windowManager?.OpenFloat(new FloatingWindowConfig
        {
            Title = MainWebView?.CoreWebView2?.DocumentTitle ?? "Float Window",
            Url = currentUrl,
            Preset = WindowPreset.Compact
        });
    }

    private void OnOpenExternalClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = MainWebView?.Source?.ToString() ?? TxtAddress.Text;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SettingsService.Log($"OpenExternal failed: {ex.Message}");
        }
    }
}
