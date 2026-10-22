using System;
using FloatView.App.Browser;
using Xunit;

namespace FloatView.Tests;

public class BrowserServiceTests
{
    [Theory]
    [InlineData("https://www.youtube.com", "https://www.youtube.com")]
    [InlineData("http://example.com", "https://example.com")] // Forced HTTPS
    [InlineData("youtube.com", "https://youtube.com")]
    [InlineData("music.youtube.com/explore", "https://music.youtube.com/explore")]
    [InlineData("localhost:3000", "http://localhost:3000")]
    [InlineData("about:blank", "about:blank")]
    public void NormalizeUrl_ValidWebInputs_ProducesCorrectUrl(string input, string expected)
    {
        var result = BrowserService.NormalizeUrl(input, forceHttps: true);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeUrl_EmptyOrNull_ReturnsDefaultHomepage()
    {
        var result = BrowserService.NormalizeUrl(string.Empty);
        Assert.Equal("https://www.youtube.com", result);
    }

    [Fact]
    public void NormalizeUrl_ArbitrarySearchString_RedirectsToGoogleSearch()
    {
        var result = BrowserService.NormalizeUrl("lofi beats to study to");
        Assert.StartsWith("https://www.google.com/search?q=", result);
        Assert.Contains("lofi%20beats", result);
    }
}
