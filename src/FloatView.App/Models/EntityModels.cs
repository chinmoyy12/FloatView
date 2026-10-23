using System;
using System.Collections.Generic;

namespace FloatView.App.Models;

public class WorkspaceModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "Workspace";
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "Layers";
    public List<FloatingWindowConfig> Windows { get; set; } = new();
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    public bool IsBuiltIn { get; set; } = false;
}

public class FavoriteItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = "Media"; // "Media", "Study", "Productivity", "Social"
    public string FaviconUrl { get; set; } = string.Empty;
    public WindowPreset PreferredPreset { get; set; } = WindowPreset.Compact;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class CaptionNote
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Url { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public double TimestampSeconds { get; set; }
    public string FormattedTimestamp
    {
        get
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, TimestampSeconds));
            return ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");
        }
    }
    public string NoteText { get; set; } = string.Empty;
    public string ExtractedCaption { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static string ExportToMarkdown(IEnumerable<CaptionNote> notes)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# FloatView Study Notes & Captions");
        sb.AppendLine($"Exported: {DateTime.Now:f}\n");

        foreach (var note in notes)
        {
            string safeTitle = string.IsNullOrWhiteSpace(note.PageTitle) ? "Untitled Media" : note.PageTitle.Replace("[", "\\[").Replace("]", "\\]");
            sb.AppendLine($"### [{note.FormattedTimestamp}] {safeTitle}");
            sb.AppendLine($"- **Source**: {note.Url}");
            if (!string.IsNullOrWhiteSpace(note.ExtractedCaption))
            {
                string safeCaption = note.ExtractedCaption.Replace("*", "\\*");
                sb.AppendLine($"- **Caption**: *\"{safeCaption}\"*");
            }
            sb.AppendLine($"- **Note**: {note.NoteText}\n");
        }

        return sb.ToString();
    }
}

public class PrivacyRule
{
    public string Domain { get; set; } = string.Empty;
    public bool IsBlocked { get; set; } = true;
    public long BlockCount { get; set; } = 0;
    public DateTime LastBlockedAt { get; set; } = DateTime.UtcNow;
}

public class FocusSessionModel
{
    public int TotalMinutes { get; set; } = 25;
    public int RemainingSeconds { get; set; } = 25 * 60;
    public bool IsRunning { get; set; } = false;
    public bool IsPaused { get; set; } = false;
    public bool IsBreak { get; set; } = false;
    public int CompletedSessions { get; set; } = 0;
    public string ActiveWorkspaceId { get; set; } = string.Empty;
}
