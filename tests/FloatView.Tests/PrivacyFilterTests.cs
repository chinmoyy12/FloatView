using System;
using System.IO;
using FloatView.App.Persistence;
using FloatView.App.Privacy;
using Xunit;

namespace FloatView.Tests;

public class PrivacyFilterTests : IDisposable
{
    private readonly SettingsService _settingsService;

    public PrivacyFilterTests()
    {
        _settingsService = new SettingsService();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public void ShouldBlockUri_KnownAdDomain_ReturnsTrueAndMatchesDomain()
    {
        var filter = new PrivacyFilterService(_settingsService) { IsEnabled = true };
        var testUri = new Uri("https://ad.doubleclick.net/pagead/ads?id=123");

        bool blocked = filter.ShouldBlockUri(testUri, topLevelHost: "youtube.com", out var matchedDomain);

        Assert.True(blocked);
        Assert.Equal("doubleclick.net", matchedDomain);
        Assert.True(filter.TotalBlockedCount > 0);
    }

    [Fact]
    public void ShouldBlockUri_WhenShieldDisabled_ReturnsFalse()
    {
        var filter = new PrivacyFilterService(_settingsService) { IsEnabled = false };
        var testUri = new Uri("https://googleadservices.com/pagead/conversion");

        bool blocked = filter.ShouldBlockUri(testUri, topLevelHost: "youtube.com", out _);

        Assert.False(blocked);
    }

    [Fact]
    public void ShouldBlockUri_TopLevelHostInAllowlist_BypassesFilter()
    {
        var filter = new PrivacyFilterService(_settingsService) { IsEnabled = true };
        filter.AddToAllowlist("trusted-site.com");

        var testUri = new Uri("https://doubleclick.net/ad");
        bool blocked = filter.ShouldBlockUri(testUri, topLevelHost: "trusted-site.com", out _);

        Assert.False(blocked);
    }

    [Fact]
    public void ShouldBlockUri_AuthenticationEndpoint_IsNeverBlocked()
    {
        var filter = new PrivacyFilterService(_settingsService) { IsEnabled = true };
        var authUri = new Uri("https://accounts.google.com/signin/oauth");

        bool blocked = filter.ShouldBlockUri(authUri, topLevelHost: "youtube.com", out _);

        Assert.False(blocked);
    }

    [Fact]
    public void AddRule_And_RemoveRule_ModifiesFilterBehavior()
    {
        var filter = new PrivacyFilterService(_settingsService) { IsEnabled = true };
        var customDomain = "custom-tracker-" + Guid.NewGuid().ToString("N")[..6] + ".com";
        var customUri = new Uri($"https://sub.{customDomain}/telemetry");

        // Before adding
        Assert.False(filter.ShouldBlockUri(customUri, null, out _));

        // Add
        filter.AddRule(customDomain, isBlocked: true);
        Assert.True(filter.ShouldBlockUri(customUri, null, out var matched));
        Assert.Equal(customDomain, matched);

        // Remove
        filter.RemoveRule(customDomain);
        Assert.False(filter.ShouldBlockUri(customUri, null, out _));
    }
}
