# 🪟 FloatView v1.0.0 — Production Release (Updated with Smart Media Detection)

FloatView is a next-generation Windows floating media, browser & productivity workspace engineered with **C# 13, .NET 10 LTS, WPF, Microsoft Edge WebView2, and Windows Core Audio APIs**.

### 🚀 What's New: Smart Media Detection (Detect, Select, Float)
- **Background Watcher**: Runs quietly in the background and Windows system tray, automatically detecting media playback across external browsers (Google Chrome, Microsoft Edge, Brave, Mozilla Firefox).
- **Dual-Layer Detection Strategy**:
  - **High-Fidelity Companion Extension (MV3)**: Captures exact media title, canonical URL, elapsed timestamp (`currentTime`), and duration.
  - **Windows Core Audio (WASAPI) Fallback**: Uses `IAudioSessionManager2` and `IAudioMeterInformation` to detect media audio sessions and map PIDs to browser windows out of the box with zero configuration!
- **Unobtrusive Toast**: Shows an elegant notification with `[ Use FloatView ]` and `[ Not now ]` when media plays for at least 2 seconds (debounced).
- **One-Click Float**: Selecting a candidate in the **"Choose what to float"** selector immediately opens the floating window at the current timestamp—no secondary confirmation needed.
- **Safe Media Handoff**: Synchronizes timestamps (`#t=125s`) and automatically pauses the source browser tab only after the destination float confirms playback.
- **Windows System Tray**: Native Win32 `Shell_NotifyIconW` tray icon with double-click restore, media picker launch, and background minimization.

### 🛡️ Production Hardening & Bug Fixes
- **Atomic Config Persistence**: Thread-safe atomic file writes using unique temp files and atomic replacement, with automatic backup preservation of corrupted configs (`.corrupt.bak`).
- **Global Hotkey Conflicts**: Handled `Alt + Space` contention with automatic fallback to `Ctrl + Alt + Space`.
- **Sanitized Markdown Exports**: Study notes and video captions are cleanly escaped and exported to Markdown without formatting corruption.
- **Click-Through Anti-Trap**: High-visibility amber `PASSTHROUGH` badge prevents users from getting stranded in mouse pass-through mode.
- **65 Automated Unit & Integration Tests**: 100% test pass rate across all layout, bounds, snapping, media, privacy shield, debounce, and WASAPI suites.

### 📥 Downloads & Assets
- **`FloatView-Setup-v1.0.0.exe`** (46.1 MB): Windows Installer with Start Menu shortcuts and uninstaller.
- **`FloatView-v1.0.0-win-x64.zip`** (63.8 MB): Fully standalone portable archive (Zero installation required, extract and run).
