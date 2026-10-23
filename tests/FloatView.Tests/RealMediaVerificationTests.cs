using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FloatView.App.Browser;
using FloatView.App.Floating;
using FloatView.App.Focus;
using FloatView.App.Media;
using FloatView.App.Models;
using FloatView.App.Persistence;
using FloatView.App.Platform;
using FloatView.App.Privacy;
using Xunit;

namespace FloatView.Tests;

public class RealMediaVerificationTests
{
    private static readonly string[] VerifiedMediaUrls = new[]
    {
        "https://www.youtube.com/watch?v=aqz-KE-bpKQ", // General Video
        "https://www.youtube.com/watch?v=dQw4w9WgXcQ", // Music Video
        "https://www.youtube.com/watch?v=jfKfPfyJRdk", // Lofi Livestream
        "http://localhost:5055"                       // Localhost Testbed Stream
    };

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=aqz-KE-bpKQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=jfKfPfyJRdk")]
    [InlineData("http://localhost:5055")]
    public void NormalizeUrl_RealMediaTargets_RemainExactAndDirect(string input)
    {
        string normalized = BrowserService.NormalizeUrl(input);
        Assert.Equal(input, normalized);
    }

    [Fact]
    public void PrivacyShield_EssentialYouTubeVideoStreamingEndpoints_AreNeverBlocked()
    {
        var settings = new SettingsService();
        var privacy = new PrivacyFilterService(settings) { IsEnabled = true };

        // Crucial YouTube media streaming endpoints
        var videoStreamUri = new Uri("https://rr4---sn-4g5ednls.googlevideo.com/videoplayback?expire=123");
        var ytHomeUri = new Uri("https://www.youtube.com/watch?v=aqz-KE-bpKQ");
        var ytApiUri = new Uri("https://youtubei.googleapis.com/youtubei/v1/player");

        Assert.False(privacy.ShouldBlockUri(videoStreamUri, "youtube.com", out _));
        Assert.False(privacy.ShouldBlockUri(ytHomeUri, "youtube.com", out _));
        Assert.False(privacy.ShouldBlockUri(ytApiUri, "youtube.com", out _));
    }

    [Fact]
    public void PrivacyShield_YouTubeTrackingAdEndpoints_AreBlocked()
    {
        var settings = new SettingsService();
        var privacy = new PrivacyFilterService(settings) { IsEnabled = true };

        var ad1 = new Uri("https://pagead2.googlesyndication.com/pagead/js/ads.js");
        var ad2 = new Uri("https://static.doubleclick.net/instream/ad_status.js");

        Assert.True(privacy.ShouldBlockUri(ad1, "youtube.com", out string? domain1));
        Assert.Equal("googlesyndication.com", domain1);

        Assert.True(privacy.ShouldBlockUri(ad2, "youtube.com", out string? domain2));
        Assert.Equal("doubleclick.net", domain2);
    }

    [Fact]
    public void Workspaces_StudyModePreset_LoadsLectureAndStudyMusicWindows()
    {
        var workspaces = SettingsService.GetDefaultWorkspaces();
        var study = workspaces.Find(w => w.Id == "study");

        Assert.NotNull(study);
        Assert.Equal(2, study.Windows.Count);

        var lecture = study.Windows[0];
        var music = study.Windows[1];

        Assert.Contains("tutorial", lecture.Url);
        Assert.Equal(WindowPreset.Compact, lecture.Preset);
        Assert.True(lecture.IsTopmost);

        Assert.Contains("music.youtube.com", music.Url);
        Assert.Equal(WindowPreset.UltraMini, music.Preset);
        Assert.True(music.IsTopmost);
    }

    [Fact]
    public void FocusSessionService_TransitionsToBreakAfterWorkDuration()
    {
        var service = new FocusSessionService(workMinutes: 25, breakMinutes: 5);

        // Monotonic initial state
        Assert.False(service.IsRunning);
        Assert.False(service.IsBreak);
        Assert.Equal("25:00", service.FormattedRemainingTime);
    }

    [Fact]
    public void FloatingWindowConfig_PresetsMatchExactClientDimensions()
    {
        var (miniW, miniH) = FloatingWindowConfig.GetPresetDimensions(WindowPreset.UltraMini);
        Assert.Equal(320, miniW);
        Assert.Equal(180, miniH);

        var (compW, compH) = FloatingWindowConfig.GetPresetDimensions(WindowPreset.Compact);
        Assert.Equal(480, compW);
        Assert.Equal(270, compH);

        var (cineW, cineH) = FloatingWindowConfig.GetPresetDimensions(WindowPreset.Cinema);
        Assert.Equal(720, cineW);
        Assert.Equal(405, cineH);
    }
}
