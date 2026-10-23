<div align="center">

# 🪟 FloatView
### **Next-Generation Windows Floating Media, Browser & Productivity Workspace**

[![Release](https://img.shields.io/badge/Release-v1.0.0-7928CA?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/chinmoyy12/FloatView/releases/tag/v1.0.0)
[![.NET](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20x64-0078D6?style=for-the-badge&logo=windows11&logoColor=white)](https://www.microsoft.com/windows)
[![Tests](https://img.shields.io/badge/Tests-65%20Passed%20(100%25)-success?style=for-the-badge&logo=checkmarx&logoColor=white)](https://github.com/chinmoyy12/FloatView/actions)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)

<br/>

<p align="center">
  <b>Keep videos, music, tutorials, and documentation floating seamlessly above your games, IDEs, and workspaces.</b><br/>
  Zero visual clutter • Edge-triggered reveal tabs • WASAPI hardware audio visualizer • Mouse click-through passthrough • Local privacy shield
</p>

---

### ⚡ **Instant Direct Downloads (v1.0.0)**

<table>
  <tr>
    <td align="center">
      <a href="https://github.com/chinmoyy12/FloatView/releases/download/v1.0.0/FloatView-Setup-v1.0.0.exe">
        <img src="https://img.shields.io/badge/📥_Download_Windows_Installer-Setup.exe_(46_MB)-7928CA?style=for-the-badge" alt="Download Installer"/>
      </a>
      <br/>
      <sub>Recommended for Windows 10 & 11 • Start Menu Shortcuts • Auto-Uninstaller</sub>
    </td>
    <td align="center">
      <a href="https://github.com/chinmoyy12/FloatView/releases/download/v1.0.0/FloatView-v1.0.0-win-x64.zip">
        <img src="https://img.shields.io/badge/📦_Download_Portable_Archive-Portable.zip_(63_MB)-0078D6?style=for-the-badge" alt="Download Portable Zip"/>
      </a>
      <br/>
      <sub>Zero Installation Required • Extract & Run • Fully Self-Contained</sub>
    </td>
  </tr>
</table>

<p align="center">
  👉 <b><a href="https://github.com/chinmoyy12/FloatView/releases/tag/v1.0.0">View Release v1.0.0 Notes & Checksums</a></b>
</p>

</div>

---

## 📸 Overview & Visual Architecture

```
 ┌────────────────────────────────────────────────────────────────────────┐
 │  FloatView Workspace HUD                                 [—] [□] [✕]  │
 ├──────────┬─────────────────────────────────────────────────────────────┤
 │ 🏠 Home  │  ┌──────────────────────┐   ┌─────────────────────────────┐ │
 │ 🌐 Web   │  │ 📺 YouTube Tutorial  │   │ 🎵 Spotify / Lo-Fi Beats    │ │
 │ 🪟 Float │  │                      │   │  [ ▂▃▅▆▇  WASAPI Loopback ] │ │
 │ 📚 Work  │  │  [Ultra-Mini 320x180]│   │                             │ │
 │ 🛡️ Shield│  └──────────────────────┘   └─────────────────────────────┘ │
 │ ⚙️ Setup │        ▲ Click-Through Passthrough Active (Ctrl+Alt+K)      │
 │          │  ─────────────────────────────────────────────────────────  │
 │          │  🚀 Command Palette: [ Alt + Space ] to launch anything     │
 └──────────┴─────────────────────────────────────────────────────────────┘
```

---

## ✨ Features at a Glance

### 🚀 1. Smart Media Detection (Detect, Select, Float)
- **Background Watcher**: FloatView runs quietly in the Windows background and system tray, monitoring media playback across Google Chrome, Microsoft Edge, Brave, and Firefox.
- **Dual-Layer Detection Strategy**:
  - **High-Fidelity Extension**: Captures exact video title, canonical URL, elapsed timestamp, and duration via Chromium MV3 companion extension.
  - **Windows Core Audio (WASAPI) Fallback**: Uses `IAudioSessionManager2` and `IAudioMeterInformation` to detect media audio sessions and map PIDs to browser windows out of the box with zero configuration!
- **Unobtrusive Toast**: Shows an elegant notification `[ Use FloatView ]` or `[ Not now ]` when media plays for at least 2 seconds (debounced).
- **One-Click Float**: Selecting a detected candidate in the **"Choose what to float"** selector immediately initiates playback in a floating window—no secondary confirmation needed.
- **Safe Media Handoff**: Synchronizes timestamps (`#t=125s`) and automatically pauses the source browser tab only after the destination float confirms playback.
- **Full Documentation**: See [Setup Guide](SMART_MEDIA_DETECTION_SETUP.md), [Architecture Specification](SMART_MEDIA_DETECTION_IMPLEMENTATION.md), and [Test Report](SMART_MEDIA_DETECTION_TEST_REPORT.md).

### 🪟 2. Universal Borderless Float Mode
- **Always-on-Top Precision**: Hardware-accelerated WPF borderless windows layered over fullscreen games and IDEs.
- **Three One-Click Presets**:
  - `Ultra-Mini`: 320 × 180 compact media widget.
  - `Compact`: 480 × 270 optimal PiP aspect.
  - `Cinema`: 720 × 405 high-resolution presentation view.
- **Fluid Windows Dragging**: OS-level `WM_NCLBUTTONDOWN` message dispatching avoids WPF mouse-capture lag.
- **Per-Window Opacity**: Smooth native window transparency slider (25% to 100%).

### 🧲 2. Screen-Edge Docking & Reveal Tabs
- **Zero Screen Obstruction**: Dock floating windows against the **Left**, **Right**, **Top**, or **Bottom** screen boundaries.
- **Auto-Hide to Edge**: Smooth cubic-ease slide hides the player behind the screen edge, leaving an elegant 8px reveal tab.
- **Mouse Hover Slide-In**: Bringing the cursor near the edge trigger glides the window back into view with zero latency.
- **Multi-Monitor Awareness**: Accurately computes negative desktop virtual coordinates and per-display taskbar working areas.

### 👻 3. Mouse Click-Through Passthrough
- **Play While Coding/Gaming**: Enable click-through via toolbar or global shortcut `<kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>K</kbd>`.
- **Native Extended Styles**: Sets Win32 `WS_EX_TRANSPARENT | WS_EX_LAYERED` so mouse events hit the application behind the float.
- **Anti-Trap Visual Cues**: A bright amber `PASSTHROUGH` badge guarantees the user is never stranded.

### ⚡ 4. Spotlight Command Palette (`Alt + Space`)
- **Instant Keyboard Control**: Press `<kbd>Alt</kbd> + <kbd>Space</kbd>` anywhere on your desktop.
- **Fuzzy Search Engine**: Fast keyboard search over favorite websites, workspace layouts, window tiling, opacity adjustments, and timers.
- **Arrow-Key Navigation**: Navigate with `<kbd>↑</kbd>` `<kbd>↓</kbd>`, commit with `<kbd>Enter</kbd>`, or dismiss with `<kbd>Esc</kbd>`.

### 🎵 5. Real-Time Hardware Audio Visualizer (WASAPI Loopback)
- **Zero Drivers / Zero CORS**: Eliminates browser cross-origin audio limitations without installing virtual audio drivers!
- **Windows Core Audio API**: Queries `IAudioMeterInformation` (`C02216F6-0380-4D5B-9C00-DAC82BE3EF83`) on the master multimedia render endpoint.
- **Dynamic 6-Band Equalizer**: Accurately maps real hardware stereo peak levels into Sub-bass, Bass, Low-mids, High-mids, Presence, and Brilliance with authentic attack/decay physics.
- **Zero Battery Drain**: Automatically pauses and decays to baseline when audio playback stops.

### 🛡️ 6. Built-in Privacy Shield & Request Blocker
- **Network Level Interception**: Evaluates outgoing requests inside Microsoft Edge WebView2's `WebResourceRequested` filter.
- **Blocks Trackers & Ads**: Prevents ad delivery, telemetry, and tracking endpoints locally with zero cloud dependencies.
- **Allowlist & Custom Rules**: 1-click toggle, per-site bypass, and real-time blocked request counters.

### 📝 7. Floating Captions & Timestamped Notes
- **Deep Subtitle Extraction**: Automatically extracts live subtitles from YouTube (`.ytp-caption-segment`), Netflix (`.player-timedtext`), and HTML5 text tracks.
- **1-Click Timestamp Capture**: Saves timestamped study notes (`mm:ss` / `hh:mm:ss`) synchronized with current media playback.
- **Markdown Export**: Export your complete study session or research log directly to `.md` or copy to clipboard.

### 🍅 8. Monotonic Focus Engine (Pomodoro)
- **Stopwatch Precision**: Uses monotonic high-resolution hardware timestamps; immune to UI minimization or thread throttling.
- **Configurable Work & Break Intervals**: Default 25m work / 5m rest cycle with audio chime and workspace switching.

---

## ⌨️ Desktop Global Hotkeys

All hotkeys are registered system-wide via native Win32 `RegisterHotKey`:

| Hotkey | Action | Description |
|---|---|---|
| <kbd>Alt</kbd> + <kbd>Space</kbd> | **Command Palette** | Open Spotlight-style fuzzy search launcher |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>F</kbd> | **Toggle Floats** | Instantly hide or restore all open floating windows |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd> | **Toggle Topmost** | Pin or unpin all floating windows above other apps |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>K</kbd> | **Click-Through** | Toggle mouse click-through passthrough mode |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>P</kbd> | **Play / Pause** | Toggle playback in active floating media |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>O</kbd> | **Focus Timer** | Start or pause the Pomodoro focus session |
| `Double Click Header` | **Cycle Presets** | Cycle size between Ultra-Mini, Compact, and Cinema |
| <kbd>Escape</kbd> | **Dismiss** | Close active palette or overlay |

---

## 📥 Direct Downloads & Installation

| Release File | Type | Architecture | Size | Direct Link |
|---|---|---|---|---|
| **FloatView-Setup-v1.0.0.exe** | Windows Installer | x64 | ~46.0 MB | [**Download Installer**](https://github.com/chinmoyy12/FloatView/releases/download/v1.0.0/FloatView-Setup-v1.0.0.exe) |
| **FloatView-v1.0.0-win-x64.zip** | Standalone Portable | x64 | ~63.8 MB | [**Download Portable Zip**](https://github.com/chinmoyy12/FloatView/releases/download/v1.0.0/FloatView-v1.0.0-win-x64.zip) |

### System Prerequisites
- **OS**: Windows 10 (Build 1809+) or Windows 11 (64-bit).
- **Runtime**: Microsoft Edge WebView2 Evergreen Runtime (pre-installed on Windows 11; automatically verified by installer).

---

## 🛠️ Building from Source

FloatView is written in **C# 13** and built on **.NET 10 LTS** with native WPF.

### 1. Clone Repository
```powershell
git clone https://github.com/chinmoyy12/FloatView.git
cd FloatView
```

### 2. Build Release Binaries
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1
```

### 3. Run Test Suite (65 Unit Tests)
```powershell
powershell -ExecutionPolicy Bypass -File scripts\test.ps1
```

### 4. Create Standalone Publish Output
```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish-win-x64.ps1
```
Output executable: `publish\win-x64\FloatView.App.exe`

### 5. Package Windows Installer (Inno Setup)
```powershell
powershell -ExecutionPolicy Bypass -File scripts\package-installer.ps1
```
Output installer: `installer\output\FloatView-Setup-v1.0.0.exe`

---

## 🏛️ Architecture & Clean Design Principles

```
FloatView/
├── extension/             # Companion Chromium MV3 Extension (Chrome & Edge)
├── src/FloatView.App/
│   ├── Platform/          # Win32 User32/DWM, WASAPI Audio Sessions & Meter, System Tray
│   ├── Browser/           # WebView2 Environment, Media Detection, Deep Player Hooks
│   ├── Floating/          # FloatingWindow, WindowManager, EdgeActivation, Snapping
│   ├── Media/             # SmartMediaCoordinator, BrowserBridgeService, GlobalHotkeys
│   ├── Notifications/     # MediaDetectedNotificationWindow (Toast Prompt)
│   ├── Palette/           # Spotlight Command Palette Window (Alt + Space)
│   ├── Privacy/           # Privacy Shield Request Filter & Allowlist Store
│   ├── Focus/             # Monotonic Pomodoro Session Engine
│   ├── Persistence/       # Atomic File Swaps (settings, workspaces, notes)
│   └── Views/             # Dashboard, Browser, Workspaces, Notes, Privacy, Settings, MediaPicker
├── tests/FloatView.Tests/ # 65 Automated Unit Tests (xUnit)
├── installer/             # Inno Setup Script (FloatView.iss)
└── scripts/               # PowerShell Build, Test, Publish, Package Automations
```

- **Local-First & Zero Telemetry**: Absolutely no cloud databases, accounts, external API trackers, or telemetry pings.
- **Crash-Resistant Storage**: All configurations are serialized to `.tmp` files and atomically swapped via `File.Replace`.
- **Shared WebView2 Process Tree**: All floating instances share a thread-safe single `CoreWebView2Environment` targeting `%LOCALAPPDATA%\FloatView\WebView2Profile`, eliminating duplicate memory overhead.

---

## 📄 License

Distributed under the **MIT License**. See [LICENSE](LICENSE) for details.

<div align="center">
  <sub>Built with ❤️ for power users, developers, students, and gamers on Windows.</sub>
</div>
