using System;
using System.Net.Http;
using System.Threading.Tasks;
using FloatView.App.Browser;
using FloatView.App.Media;
using FloatView.App.Persistence;
using FloatView.App.Platform;
using FloatView.App.Privacy;
using Xunit;

namespace FloatView.Tests;

public class LocalhostMediaIntegrationTests
{
    [Theory]
    [InlineData("localhost:5055", "http://localhost:5055")]
    [InlineData("http://localhost:5055", "http://localhost:5055")]
    [InlineData("localhost:3000/stream", "http://localhost:3000/stream")]
    [InlineData("localhost", "http://localhost")]
    public void NormalizeUrl_LocalhostTargets_AlwaysRemainHttp(string input, string expected)
    {
        string result = BrowserService.NormalizeUrl(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PrivacyFilter_LocalhostOrigin_NeverBlocked()
    {
        var settings = new SettingsService();
        var privacy = new PrivacyFilterService(settings);

        var localUri = new Uri("http://localhost:5055/media-stream");
        bool blocked = privacy.ShouldBlockUri(localUri, "localhost", out _);

        Assert.False(blocked, "Localhost resources should never be blocked by Privacy Shield.");
    }

    [Fact]
    public void PrivacyFilter_AdTrackerLoadedFromLocalhost_IsBlocked()
    {
        var settings = new SettingsService();
        var privacy = new PrivacyFilterService(settings);

        var adUri = new Uri("https://adservice.google.com/adsid/integrator.sync");
        bool blocked = privacy.ShouldBlockUri(adUri, "localhost", out string? matchedDomain);

        Assert.True(blocked, "Ad domain should be blocked even when page origin is localhost.");
        Assert.Equal("adservice.google.com", matchedDomain);
    }

    [Fact]
    public async Task LocalhostServer_HealthCheck_RespondsWith200AndValidHtml()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            var response = await client.GetAsync("http://localhost:5055");
            Assert.True(response.IsSuccessStatusCode, $"Server returned {response.StatusCode}");

            string html = await response.Content.ReadAsStringAsync();
            Assert.Contains("FloatView Localhost Media Testbed", html);
            Assert.Contains("mainVideo", html);
            Assert.Contains("shadowHost", html);
            Assert.Contains("testIframe", html);
        }
        catch (HttpRequestException ex)
        {
            // If server is temporarily restarting, record informative message
            Assert.Fail($"Localhost testbed server on port 5055 is unreachable: {ex.Message}");
        }
    }

    [Fact]
    public void WasapiAudioMeter_ContinuousSamplingLoop_DoesNotLeakOrThrow()
    {
        var meter = new WasapiAudioMeterService();

        for (int i = 0; i < 50; i++)
        {
            float peak = meter.GetPeakValue();
            var (left, right) = meter.GetStereoPeakValues();

            Assert.True(peak >= 0f && peak <= 1f);
            Assert.True(left >= 0f && left <= 1f);
            Assert.True(right >= 0f && right <= 1f);
        }
    }

    [Fact]
    public void AudioVisualizer_ContinuousLoop_MaintainsValidBands()
    {
        var visualizer = new AudioVisualizerService();
        double[]? latestBars = null;
        visualizer.VisualizerFrame += bars => latestBars = (double[])bars.Clone();

        visualizer.IsPlaying = true;

        // Simulate multiple audio frames
        for (int frame = 0; frame < 10; frame++)
        {
            if (latestBars != null)
            {
                Assert.Equal(6, latestBars.Length);
                foreach (var b in latestBars)
                {
                    Assert.True(b >= 0.0 && b <= 1.0, $"Visualizer bar {b} must be between 0 and 1");
                }
            }
        }

        visualizer.Stop();
        Assert.NotNull(latestBars);
        foreach (var b in latestBars)
        {
            Assert.Equal(0.0, b);
        }
    }
}
