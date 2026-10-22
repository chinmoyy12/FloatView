using System.Threading.Tasks;
using FloatView.App.Browser;
using FloatView.App.Media;
using FloatView.App.Platform;
using Xunit;

namespace FloatView.Tests;

public class MediaAndAudioTests
{
    [Fact]
    public void WasapiAudioMeterService_GetPeakValue_DoesNotThrowAndReturnsValidRange()
    {
        var meter = new WasapiAudioMeterService();
        float peak = meter.GetPeakValue();

        Assert.True(peak >= 0f && peak <= 1f, $"Peak value must be between 0 and 1, was {peak}");
    }

    [Fact]
    public void WasapiAudioMeterService_GetStereoPeakValues_ReturnsValidRange()
    {
        var meter = new WasapiAudioMeterService();
        var (left, right) = meter.GetStereoPeakValues();

        Assert.True(left >= 0f && left <= 1f);
        Assert.True(right >= 0f && right <= 1f);
    }

    [Fact]
    public void AudioVisualizerService_InitialStateAndStop_ResetsBarsToZero()
    {
        var visualizer = new AudioVisualizerService();
        double[]? receivedBars = null;
        visualizer.VisualizerFrame += bars => receivedBars = (double[])bars.Clone();

        visualizer.Stop();

        Assert.NotNull(receivedBars);
        Assert.Equal(6, receivedBars.Length);
        foreach (var bar in receivedBars)
        {
            Assert.Equal(0.0, bar);
        }
    }

    [Fact]
    public void MediaDetectionService_InjectedScript_ContainsDrmAndDeepPlayerIntegrations()
    {
        string script = MediaDetectionService.InjectedScript;

        Assert.Contains("deepQueryAll", script);
        Assert.Contains("shadowRoot", script);
        Assert.Contains("movie_player", script); // YouTube Player API
        Assert.Contains("netflix", script); // Netflix Player API
        Assert.Contains("disablepictureinpicture", script); // PiP unblocker
        Assert.Contains("unblockPictureInPicture", script);
        Assert.Contains("ytp-caption-segment", script); // YouTube live captions
        Assert.Contains("player-timedtext", script); // Netflix subtitles
    }
}
