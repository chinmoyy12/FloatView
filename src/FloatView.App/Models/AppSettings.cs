using System.Collections.Generic;

namespace FloatView.App.Models;

public class HotkeyBinding
{
    public string Action { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool ModCtrl { get; set; }
    public bool ModAlt { get; set; }
    public bool ModShift { get; set; }
    public bool ModWin { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class AppSettings
{
    // Appearance
    public string Theme { get; set; } = "Dark"; // "Dark", "Light", "System"
    public string AccentColor { get; set; } = "#8B5CF6"; // Modern violet
    public double DefaultWindowOpacity { get; set; } = 0.96;
    public bool EnableBackdropBlur { get; set; } = true;
    public bool ReducedMotion { get; set; } = false;
    public string UiDensity { get; set; } = "Default"; // "Compact", "Default", "Comfortable"

    // Floating Windows
    public int MaxActiveFloats { get; set; } = 3;
    public WindowPreset DefaultPreset { get; set; } = WindowPreset.Compact;
    public bool DefaultTopmost { get; set; } = true;
    public bool EnableWindowSnapping { get; set; } = true;
    public double SnappingDistance { get; set; } = 16.0;

    // Edge Activation & Docking
    public bool EdgeDockingEnabled { get; set; } = true;
    public int EdgeTriggerSize { get; set; } = 6;
    public int EdgeHoverDelayMs { get; set; } = 150;
    public int EdgeHideDelayMs { get; set; } = 600;
    public bool HotCornersEnabled { get; set; } = true;
    public bool EdgeActivationOnlyOnPrimary { get; set; } = false;

    // Browser
    public string HomepageUrl { get; set; } = "https://www.youtube.com";
    public bool ForceHttps { get; set; } = true;
    public bool BlockThirdPartyPopups { get; set; } = true;

    // Privacy Shield
    public bool PrivacyShieldEnabled { get; set; } = true;
    public List<string> PrivacyAllowlist { get; set; } = new()
    {
        "accounts.google.com",
        "login.microsoftonline.com"
    };

    // Media & Visualizer
    public bool AutoPopOutEnabled { get; set; } = false;
    public string VisualizerStyle { get; set; } = "Bars"; // "Off", "LowPower", "Bars", "Waveform"
    public bool PreferNativePip { get; set; } = true;

    // Focus Session Defaults
    public int FocusDurationMinutes { get; set; } = 25;
    public int FocusBreakDurationMinutes { get; set; } = 5;
    public bool AutoPauseMediaOnFocus { get; set; } = false;

    // Workspaces & Restoration
    public string DefaultWorkspaceId { get; set; } = "study";
    public string StartupRestoreOption { get; set; } = "LastWorkspace"; // "LastWorkspace", "LayoutOnly", "Clean"

    // Hotkeys
    public List<HotkeyBinding> Hotkeys { get; set; } = new()
    {
        new() { Action = "CommandPalette", Key = "Space", ModAlt = true, ModCtrl = false },
        new() { Action = "ToggleAllFloats", Key = "F", ModAlt = true, ModCtrl = true },
        new() { Action = "ToggleTopmost", Key = "T", ModAlt = true, ModCtrl = true },
        new() { Action = "ToggleClickThrough", Key = "K", ModAlt = true, ModCtrl = true },
        new() { Action = "MediaPlayPause", Key = "P", ModAlt = true, ModCtrl = true },
        new() { Action = "FocusToggle", Key = "O", ModAlt = true, ModCtrl = true }
    };
}
