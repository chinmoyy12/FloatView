using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FloatView.App.Models;
using FloatView.App.Persistence;
using Xunit;

namespace FloatView.Tests;

public class SettingsAndPersistenceTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsAndPersistenceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "FloatView_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        var settings = new AppSettings();
        Assert.Equal("Dark", settings.Theme);
        Assert.Equal(3, settings.MaxActiveFloats);
        Assert.True(settings.EdgeDockingEnabled);
        Assert.True(settings.EnableWindowSnapping);
        Assert.Equal(25, settings.FocusDurationMinutes);
        Assert.True(settings.PrivacyShieldEnabled);
    }

    [Fact]
    public void DefaultWorkspaces_ContainsCoreProductPresets()
    {
        var workspaces = SettingsService.GetDefaultWorkspaces();
        Assert.NotEmpty(workspaces);
        Assert.Contains(workspaces, w => w.Id == "study");
        Assert.Contains(workspaces, w => w.Id == "coding");
        Assert.Contains(workspaces, w => w.Id == "gaming");
        Assert.Contains(workspaces, w => w.Id == "night_music");

        var study = workspaces.Find(w => w.Id == "study")!;
        Assert.Equal(2, study.Windows.Count);
    }

    [Fact]
    public void WorkspaceModel_Serialization_RoundTripsAccurately()
    {
        var original = new WorkspaceModel
        {
            Id = "custom_test",
            Name = "Custom Test Workspace",
            Description = "Testing serialization integrity",
            Windows = new List<FloatingWindowConfig>
            {
                new() { Title = "Test 1", Url = "https://example.com", Left = 100, Top = 150, Width = 500, Height = 300, Preset = WindowPreset.Cinema, IsTopmost = true }
            }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<WorkspaceModel>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Single(deserialized.Windows);
        Assert.Equal("https://example.com", deserialized.Windows[0].Url);
        Assert.Equal(500, deserialized.Windows[0].Width);
    }

    [Fact]
    public void CaptionNote_FormattedTimestamp_FormatsSecondsProperly()
    {
        var note1 = new CaptionNote { TimestampSeconds = 65 };
        Assert.Equal("01:05", note1.FormattedTimestamp);

        var note2 = new CaptionNote { TimestampSeconds = 3600 };
        Assert.Equal("01:00:00", note2.FormattedTimestamp);

        var noteNegative = new CaptionNote { TimestampSeconds = -10 };
        Assert.Equal("00:00", noteNegative.FormattedTimestamp);
    }
}
