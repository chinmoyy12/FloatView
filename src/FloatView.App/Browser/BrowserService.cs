using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FloatView.App.Persistence;
using Microsoft.Web.WebView2.Core;

namespace FloatView.App.Browser;

public class BrowserService
{
    private static CoreWebView2Environment? _sharedEnvironment;
    private static readonly object EnvLock = new();

    public static async Task<CoreWebView2Environment> GetOrCreateEnvironmentAsync()
    {
        if (_sharedEnvironment != null) return _sharedEnvironment;

        lock (EnvLock)
        {
            if (_sharedEnvironment != null) return _sharedEnvironment;
        }

        var profilePath = StoragePaths.WebView2UserDataFolder;
        Directory.CreateDirectory(profilePath);

        var options = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = "--enable-features=WidevineCdM,AudioServiceOutOfProcess,HardwareMediaKeyHandling " +
                                         "--autoplay-policy=no-user-gesture-required " +
                                         "--enable-gpu-rasterization --enable-zero-copy"
        };
        var env = await CoreWebView2Environment.CreateAsync(null, profilePath, options);

        lock (EnvLock)
        {
            _sharedEnvironment ??= env;
        }

        return _sharedEnvironment;
    }

    public static string NormalizeUrl(string input, bool forceHttps = true)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "https://www.youtube.com";
        }

        input = input.Trim();

        if (input.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return forceHttps ? "https://" + input.Substring(7) : input;
        }

        if (input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return input;
        }

        // Check if looks like a domain (e.g. youtube.com or sub.domain.com/path)
        var domainRegex = new Regex(@"^[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}(/.*)?$");
        if (domainRegex.IsMatch(input))
        {
            return "https://" + input;
        }

        // If it's localhost:port
        if (input.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return "http://" + input;
        }

        // Otherwise treat as a search query
        return "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
    }

    public static async Task ClearBrowsingDataAsync(CoreWebView2 webView)
    {
        try
        {
            if (webView.Profile != null)
            {
                await webView.Profile.ClearBrowsingDataAsync();
            }
        }
        catch (Exception ex)
        {
            SettingsService.Log($"ClearBrowsingData failed: {ex.Message}");
        }
    }
}
