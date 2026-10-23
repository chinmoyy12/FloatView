using System;

namespace FloatView.App.Models;

public enum MediaSourceType
{
    Extension,
    AudioSessionFallback
}

public class MediaCandidate
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = "Unknown Media";
    public string Url { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string BrowserName { get; set; } = "Browser";
    public string BrowserProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public int TabId { get; set; } = -1;
    public double CurrentTime { get; set; }
    public double Duration { get; set; }
    public bool IsPlaying { get; set; }
    public float AudioPeak { get; set; }
    public MediaSourceType SourceType { get; set; } = MediaSourceType.Extension;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

    public string FormattedTime
    {
        get
        {
            if (CurrentTime <= 0 && Duration <= 0) return string.Empty;
            var currentSpan = TimeSpan.FromSeconds(Math.Max(0, CurrentTime));
            string cur = currentSpan.TotalHours >= 1 ? currentSpan.ToString(@"hh\:mm\:ss") : currentSpan.ToString(@"mm\:ss");
            if (Duration > 0)
            {
                var durSpan = TimeSpan.FromSeconds(Duration);
                string dur = durSpan.TotalHours >= 1 ? durSpan.ToString(@"hh\:mm\:ss") : durSpan.ToString(@"mm\:ss");
                return $"{cur} / {dur}";
            }
            return cur;
        }
    }

    public string DisplaySubtitle
    {
        get
        {
            string host = !string.IsNullOrWhiteSpace(Domain) ? Domain : BrowserName;
            string time = !string.IsNullOrWhiteSpace(FormattedTime) ? $" • {FormattedTime}" : string.Empty;
            return $"{host} • {BrowserName}{time}";
        }
    }
}

public class MediaEventMessage
{
    public string Type { get; set; } = string.Empty;
    public int TabId { get; set; } = -1;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double CurrentTime { get; set; }
    public double Duration { get; set; }
    public bool IsPlaying { get; set; }
    public bool IsMuted { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string BrowserName { get; set; } = string.Empty;
}
