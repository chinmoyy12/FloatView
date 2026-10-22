using System;
using System.Collections.Generic;

namespace FloatView.App.Models;

public enum WindowPreset
{
    Compact,     // 480x270 (16:9)
    Cinema,      // 720x405 (16:9)
    UltraMini,   // 320x180 (16:9)
    Custom
}

public enum DockEdge
{
    None,
    Left,
    Right,
    Top,
    Bottom
}

public class FloatingWindowConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = "FloatView Window";
    public string Url { get; set; } = "https://www.youtube.com";
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 480;
    public double Height { get; set; } = 270;
    public double Opacity { get; set; } = 1.0;
    public bool IsTopmost { get; set; } = true;
    public bool IsClickThrough { get; set; } = false;
    public DockEdge DockEdge { get; set; } = DockEdge.None;
    public bool IsEdgeHidden { get; set; } = false;
    public WindowPreset Preset { get; set; } = WindowPreset.Compact;
    public bool AutoPopOut { get; set; } = false;

    public static (double width, double height) GetPresetDimensions(WindowPreset preset) => preset switch
    {
        WindowPreset.UltraMini => (320, 180),
        WindowPreset.Cinema => (720, 405),
        WindowPreset.Compact => (480, 270),
        _ => (480, 270)
    };
}
