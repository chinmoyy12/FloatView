using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FloatView.App.Floating;
using FloatView.App.Media;
using FloatView.App.Models;
using FloatView.App.Persistence;
using FloatView.App.Platform;
using FloatView.App.Privacy;
using Xunit;

namespace FloatView.Tests;

public class SmartMediaDetectionTests
{
    [Fact]
    public void MediaCandidate_FormattedTime_HandlesBothDurationAndCurrentTime()
    {
        var candidate = new MediaCandidate
        {
            CurrentTime = 125, // 02:05
            Duration = 600    // 10:00
        };

        Assert.Equal("02:05 / 10:00", candidate.FormattedTime);

        // Long media (> 1 hour)
        var longCandidate = new MediaCandidate
        {
            CurrentTime = 3665, // 01:01:05
            Duration = 7200    // 02:00:00
        };

        Assert.Equal("01:01:05 / 02:00:00", longCandidate.FormattedTime);

        // Zero duration
        var streamCandidate = new MediaCandidate
        {
            CurrentTime = 45,
            Duration = 0
        };

        Assert.Equal("00:45", streamCandidate.FormattedTime);
    }

    [Fact]
    public void MediaCandidate_DisplaySubtitle_FormatsCleanly()
    {
        var candidate = new MediaCandidate
        {
            Domain = "youtube.com",
            BrowserName = "Google Chrome",
            CurrentTime = 90,
            Duration = 180
        };

        Assert.Equal("youtube.com • Google Chrome • 01:30 / 03:00", candidate.DisplaySubtitle);
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", 125.0, "https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=125s")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", 45.0, "https://youtu.be/dQw4w9WgXcQ?t=45s")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s", 125.0, "https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s")] // Already has timestamp
    [InlineData("https://twitch.tv/streamer", 120.0, "https://twitch.tv/streamer")] // Non-YouTube preserved
    public void SmartMediaCoordinator_BuildHandoffUrl_FormatsTimestampsCorrectly(string inputUrl, double currentTime, string expectedUrl)
    {
        var candidate = new MediaCandidate
        {
            Url = inputUrl,
            CurrentTime = currentTime,
            Domain = "youtube.com"
        };

        string target = SmartMediaCoordinator.BuildHandoffUrl(candidate);
        Assert.Equal(expectedUrl, target);
    }

    [Fact]
    public void SmartMediaCoordinator_BuildHandoffUrl_FallbackWhenUrlEmpty_GeneratesSearchOrHome()
    {
        var candidate = new MediaCandidate
        {
            Url = "",
            Title = "Rick Astley Never Gonna Give You Up"
        };

        string target = SmartMediaCoordinator.BuildHandoffUrl(candidate);
        Assert.Contains("google.com/search?q=Rick%20Astley", target);
    }

    [Fact]
    public void AudioSessionWatcher_ParseCandidateFromProcess_ExtractsTitleAndKnownDomains()
    {
        // 1. YouTube in Chrome
        var cand1 = AudioSessionWatcher.ParseCandidateFromProcess(
            1234,
            "chrome",
            "Google Chrome",
            "Lofi Hip Hop Radio - Beats to Study to - YouTube - Google Chrome",
            0.45f);

        Assert.NotNull(cand1);
        Assert.Equal("youtube.com", cand1.Domain);
        Assert.Equal("Lofi Hip Hop Radio - Beats to Study to", cand1.Title);
        Assert.Equal("Google Chrome", cand1.BrowserName);
        Assert.Equal(0.45f, cand1.AudioPeak);
        Assert.Equal(MediaSourceType.AudioSessionFallback, cand1.SourceType);

        // 2. Twitch in Edge
        var cand2 = AudioSessionWatcher.ParseCandidateFromProcess(
            5678,
            "msedge",
            "Microsoft Edge",
            "Shroud Playing FPS - Twitch - Microsoft Edge",
            0.2f);

        Assert.NotNull(cand2);
        Assert.Equal("twitch.tv", cand2.Domain);
        Assert.Equal("Shroud Playing FPS", cand2.Title);
        Assert.Equal("Microsoft Edge", cand2.BrowserName);

        // 3. Spotify app
        var cand3 = AudioSessionWatcher.ParseCandidateFromProcess(
            9999,
            "spotify",
            "Spotify",
            "Spotify Free",
            0.8f);

        Assert.NotNull(cand3);
        Assert.Equal("spotify.com", cand3.Domain);
    }

    [Fact]
    public void BrowserBridgeService_ProcessRawMessage_ParsesEventAndQueuesPause()
    {
        using var bridge = new BrowserBridgeService();
        MediaCandidate? receivedCandidate = null;
        MediaEventMessage? receivedMsg = null;

        bridge.MediaEventReceived += (cand, msg) =>
        {
            receivedCandidate = cand;
            receivedMsg = msg;
        };

        string json = @"{
            ""type"": ""MEDIA_PLAYING"",
            ""tabId"": 42,
            ""url"": ""https://www.youtube.com/watch?v=jfKfPfyJRdk"",
            ""title"": ""Lofi Girl Live"",
            ""domain"": ""youtube.com"",
            ""currentTime"": 154.2,
            ""duration"": 0,
            ""isPlaying"": true,
            ""isMuted"": false,
            ""browserName"": ""Google Chrome""
        }";

        bridge.ProcessRawMessage(json, out string responseJson);

        Assert.NotNull(receivedCandidate);
        Assert.NotNull(receivedMsg);
        Assert.Equal("Lofi Girl Live", receivedCandidate.Title);
        Assert.Equal(42, receivedCandidate.TabId);
        Assert.Equal("youtube.com", receivedCandidate.Domain);
        Assert.True(receivedCandidate.IsPlaying);
        Assert.Equal(154.2, receivedCandidate.CurrentTime);
        Assert.Equal(MediaSourceType.Extension, receivedCandidate.SourceType);

        // Test queuing pause command
        bridge.SendPauseCommand(42, "https://www.youtube.com/watch?v=jfKfPfyJRdk");

        // Next time tab reports an event, response should deliver the queued command
        bridge.ProcessRawMessage(json, out string responseWithCmd);
        Assert.Contains("\"command\":\"PAUSE\"", responseWithCmd);
    }

    [Fact]
    public void NativeMessagingRelay_ReadAndWriteFramedMessage_PreservesPayloadAndLengthPrefix()
    {
        using var ms = new MemoryStream();
        string originalJson = "{\"type\":\"TEST_PACKET\",\"data\":\"FloatView Real Handoff\"}";

        NativeMessagingRelay.WriteFramedMessage(ms, originalJson);

        // Seek back to start of stream
        ms.Position = 0;

        string? readJson = NativeMessagingRelay.ReadFramedMessage(ms);

        Assert.NotNull(readJson);
        Assert.Equal(originalJson, readJson);
    }

    [Fact]
    public void NativeMessagingRelay_IsNativeMessagingInvocation_DetectsChromeAndEdgeFlags()
    {
        Assert.True(NativeMessagingRelay.IsNativeMessagingInvocation(new[] { "--native-messaging-host" }));
        Assert.True(NativeMessagingRelay.IsNativeMessagingInvocation(new[] { "chrome-extension://abcdefghijklmnop/" }));
        Assert.True(NativeMessagingRelay.IsNativeMessagingInvocation(new[] { "edge-extension://abcdefghijklmnop/" }));
        Assert.False(NativeMessagingRelay.IsNativeMessagingInvocation(new[] { "--some-other-flag" }));
        Assert.False(NativeMessagingRelay.IsNativeMessagingInvocation(Array.Empty<string>()));
    }
}
