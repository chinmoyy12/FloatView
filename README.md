<div align="center">

# 🪟 FloatView
### **Next-Generation Windows Floating Media, Browser & Productivity Workspace**

[![Release](https://img.shields.io/badge/Release-v1.0.0-7928CA?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/chinmoyy12/FloatView/releases/tag/v1.0.0)
[![Build & Test](https://img.shields.io/badge/Build-Passing%20(65%2F65%20Tests)-success?style=for-the-badge&logo=dotnet&logoColor=white)](https://github.com/chinmoyy12/FloatView)
[![.NET](https://img.shields.io/badge/.NET-10.0%20LTS-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20x64-0078D6?style=for-the-badge&logo=windows11&logoColor=white)](https://www.microsoft.com/windows)
[![Tests](https://img.shields.io/badge/Tests-65%20Passed%20(100%25)-success?style=for-the-badge&logo=checkmarx&logoColor=white)](https://github.com/chinmoyy12/FloatView/actions)
[![Privacy](https://img.shields.io/badge/Privacy-100%25%20Local%20%7C%20Zero%20Telemetry-10B981?style=for-the-badge&logo=shield&logoColor=white)](#-security--privacy-policy)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)

<br/>

<p align="center">
  <b>Keep videos, music, tutorials, and documentation floating seamlessly above your games, IDEs, and workspaces.</b><br/>
  Smart Media Detection • Edge-triggered reveal tabs • WASAPI hardware audio visualizer • Mouse click-through passthrough • Local privacy shield
</p>

---

### ⚡ **Instant Direct Downloads (v1.0.0 LTS)**

<table>
  <tr>
    <td align="center">
      <a href="https://github.com/chinmoyy12/FloatView/releases/download/v1.0.0/FloatView-Setup-v1.0.0.exe">
        <img src="https://img.shields.io/badge/📥_Download_Windows_Installer-Setup.exe_(46_MB)-7928CA?style=for-the-badge" alt="Download Installer"/>
      </a>
      <br/>
      <sub><b>Recommended for Windows 10 & 11</b> • Start Menu Shortcuts • Uninstaller • Auto-Bundled Extension</sub>
    </td>
    <td align="center">
      <a href="https://github.com/chinmoyy12/FloatView/releases/download/v1.0.0/FloatView-v1.0.0-win-x64.zip">
        <img src="https://img.shields.io/badge/📦_Download_Portable_Archive-Portable.zip_(63_MB)-0078D6?style=for-the-badge" alt="Download Portable Zip"/>
      </a>
      <br/>
      <sub><b>Zero Installation Required</b> • Extract & Run • Fully Self-Contained x64 .NET 10 Bundle</sub>
    </td>
  </tr>
</table>

<p align="center">
  👉 <b><a href="https://github.com/chinmoyy12/FloatView/releases/tag/v1.0.0">View Release v1.0.0 Changelog & Notes</a></b>
</p>

</div>

---

## 📸 Visual Workspace Overview

```
 ┌────────────────────────────────────────────────────────────────────────────────────────┐
 │  FloatView Workspace HUD                                                 [—] [□] [✕]  │
 ├──────────┬─────────────────────────────────────────────────────────────────────────────┤
 │ 🏠 Home  │  ┌───────────────────────────┐    ┌───────────────────────────────────────┐ │
 │ 🌐 Web   │  │ 📺 Programming Tutorial   │    │ 🎵 Lo-Fi Beats / Spotify Web          │ │
 │ 🪟 Float │  │                           │    │  [ ▂▃▅▆▇  WASAPI Loopback Meter ]    │ │
 │ 📚 Work  │  │  [Ultra-Mini 320x180]     │    │                                       │ │
 │ 🛡️ Shield│  └───────────────────────────┘    └───────────────────────────────────────┘ │
 │ ⚙️ Setup │          ▲ Passthrough Active (Ctrl+Alt+K)    ▲ Screen-Edge Docked Reveal  │
 │          │  ─────────────────────────────────────────────────────────────────────────  │
 │          │  🚀 Command Palette: [ Alt + Space ]  •  🔔 Smart Media Detection Active    │
 └──────────┴─────────────────────────────────────────────────────────────────────────────┘
```

---

## ✨ Features Deep-Dive

### 🚀 1. Smart Media Detection (Detect, Select, Float)
- **Quiet Background Watcher**: FloatView runs quietly in the Windows background and system tray, automatically detecting media playing in external browsers (Chrome, Edge, Brave, Firefox).
- **Dual-Layer Detection Strategy**:
  - **High-Fidelity Companion Extension (MV3)**: Captures exact media titles, canonical URLs, elapsed timestamps (`currentTime`), and duration.
  - **Windows Core Audio (WASAPI) Fallback**: Uses `IAudioSessionManager2` and `IAudioMeterInformation` to monitor media audio sessions and map PIDs to browser windows out of the box with **zero configuration required**!
- **Non-Intrusive Toast Prompt**: When external playback is confirmed (debounced by 2 seconds to eliminate playlist scrubbing noise), an unobtrusive toast appears:
  ```
  Media detected
  YouTube tutorial detected in Google Chrome.
  [ Use FloatView ]  [ Not now ]
  ```
- **Instant One-Click Float**: Clicking **Use FloatView** opens the **"Choose what to float"** selector. Clicking any detected source immediately initiates the float—**no unnecessary second confirmation or Start button**.
- **Safe Media Handoff**: Synchronizes playback position (`#t={seconds}s`) and pauses the original external browser tab only after FloatView confirms that destination playback has begun.

```
External Browser ──> Extension / Core Audio ──> FloatView Toast ──> One-Click Float ──> Floating Window + Safe Pause
```

### 🪟 2. Universal Borderless Float Engine
- **Always-on-Top Precision**: Hardware-accelerated WPF borderless windows layered smoothly over fullscreen games, code editors, and office apps.
- **Three One-Click Aspect Presets**:
  - `Ultra-Mini`: 320 × 180 compact media widget.
  - `Compact`: 480 × 270 optimal Picture-in-Picture aspect.
  - `Cinema`: 720 × 405 high-resolution presentation view.
- **Fluid Windows Dragging**: OS-level `WM_NCLBUTTONDOWN` message dispatching avoids WPF mouse-capture lag under heavy GPU load.
- **Smooth Opacity Control**: Adjust transparency on the fly from 25% to 100% via the hover toolbar.

### 🧲 3. Screen-Edge Docking & Reveal Tabs
- **Zero Screen Obstruction**: Dock floating windows against the **Left**, **Right**, **Top**, or **Bottom** screen boundaries.
- **Auto-Hide to Edge**: Smooth cubic-ease slide hides the player behind the screen edge, leaving an elegant 8px reveal handle.
- **Mouse Hover Slide-In**: Bringing the cursor near the edge trigger glides the window back into view with zero latency.
- **Multi-Monitor Awareness**: Accurately computes negative desktop virtual coordinates and per-display taskbar working areas.

### 👻 4. Mouse Click-Through Passthrough Mode
- **Play While Coding or Gaming**: Enable click-through via the floating toolbar or global shortcut `<kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>K</kbd>`.
- **Native Extended Styles**: Sets Win32 `WS_EX_TRANSPARENT | WS_EX_LAYERED` so mouse events hit the application behind the float.
- **Anti-Trap Visual Cues**: A bright amber `PASSTHROUGH` badge guarantees the user is never stranded.

### ⚡ 5. Spotlight Command Palette (`Alt + Space`)
- **Instant Keyboard Control**: Press `<kbd>Alt</kbd> + <kbd>Space</kbd>` anywhere on your desktop.
- **Contested Hotkey Conflict Fallback**: If another app (e.g. PowerToys) holds `Alt + Space`, FloatView automatically registers `<kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>Space</kbd>`.
- **Fuzzy Search Engine**: Fast keyboard search over favorite websites, workspace layouts, window tiling, opacity adjustments, and timers.
- **Arrow-Key Navigation**: Navigate with `<kbd>↑</kbd>` `<kbd>↓</kbd>`, commit with `<kbd>Enter</kbd>`, or dismiss with `<kbd>Esc</kbd>`.

### 🎵 6. Real-Time Hardware Audio Visualizer (WASAPI Loopback)
- **Zero Drivers / Zero CORS**: Eliminates browser cross-origin audio limitations without installing virtual audio drivers!
- **Windows Core Audio API**: Queries `IAudioMeterInformation` (`C02216F6-0380-4D5B-9C00-DAC82BE3EF83`) on the master multimedia render endpoint.
- **Dynamic 6-Band Equalizer**: Maps real hardware stereo peak levels into Sub-bass, Bass, Low-mids, High-mids, Presence, and Brilliance with authentic attack/decay physics.
- **Zero Battery Drain**: Automatically pauses and decays to baseline when audio playback stops.

### 🛡️ 7. Built-in Local Privacy Shield
- **Network Level Interception**: Evaluates outgoing requests inside Microsoft Edge WebView2's `WebResourceRequested` filter.
- **Blocks Trackers & Ads**: Prevents ad delivery, telemetry, and tracking endpoints locally with zero cloud dependencies.
- **Allowlist & Custom Rules**: 1-click toggle, per-site bypass, and real-time blocked request counters.

### 📝 8. Floating Captions & Timestamped Notes
- **Deep Subtitle Extraction**: Automatically extracts live subtitles from YouTube (`.ytp-caption-segment`), Netflix (`.player-timedtext`), and HTML5 text tracks.
- **1-Click Timestamp Capture**: Saves timestamped study notes (`mm:ss` / `hh:mm:ss`) synchronized with current media playback.
- **Sanitized Markdown Export**: Export your complete study session or research log directly to clean `.md` or copy to clipboard.

### 🍅 9. Monotonic Focus Engine (Pomodoro)
- **Stopwatch Precision**: Uses monotonic high-resolution hardware timestamps; immune to UI minimization or thread throttling.
- **Configurable Work & Break Intervals**: Default 25m work / 5m rest cycle with audio chime and workspace switching.

### 🔔 10. System Tray Background Watcher
- **Native Win32 Shell Icon**: Integrated via `Shell_NotifyIconW` with double-click restore, media picker launch, and background minimization.
- **Safe Session Shutdown**: Cleanly preserves open workspaces and active sessions on exit.

---

## ⌨️ Desktop Global Hotkeys Cheat Sheet

All hotkeys are registered system-wide via native Win32 `RegisterHotKey`:

| Hotkey | Action | Description |
|---|---|---|
| <kbd>Alt</kbd> + <kbd>Space</kbd> | **Command Palette** | Open Spotlight-style fuzzy search launcher (Fallback: `Ctrl+Alt+Space`) |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>F</kbd> | **Toggle Floats** | Instantly hide or restore all open floating windows |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>T</kbd> | **Toggle Topmost** | Pin or unpin all floating windows above other apps |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>K</kbd> | **Click-Through** | Toggle mouse click-through passthrough mode |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>P</kbd> | **Play / Pause** | Toggle playback in active floating media |
| <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>O</kbd> | **Focus Timer** | Start or pause the Pomodoro focus session |
| `Double Click Header` | **Cycle Presets** | Cycle size between Ultra-Mini, Compact, and Cinema |
| <kbd>Escape</kbd> | **Dismiss** | Close active palette, picker, or overlay |

---

## 🧩 Companion Browser Extension Setup

FloatView works out of the box without any extension using WASAPI audio session monitoring. For exact video titles, canonical URLs, and safe handoff auto-pausing, load the lightweight companion extension:

1. **Register Native Messaging Host** (One-click):
   - In FloatView: Go to **Settings** → **Smart Media Detection** → click **Register Native Host**.
   - *Or in PowerShell:* `powershell -ExecutionPolicy Bypass -File scripts\register-extension-host.ps1`
2. **Load Unpacked in Chrome or Edge**:
   - Navigate to `chrome://extensions/` or `edge://extensions/`.
   - Toggle **Developer mode** on (top-right).
   - Click **Load unpacked** and select the `extension` folder in your FloatView installation directory.
   - For full details, see the [Extension Guide](extension/README.md) and [Setup Guide](SMART_MEDIA_DETECTION_SETUP.md).

---

## 📥 Direct Downloads & System Requirements

| Release File | Type | Architecture | Size | Direct Download Link |
|---|---|---|---|---|
| **FloatView-Setup-v1.0.0.exe** | Windows Installer | x64 | ~46.1 MB | [**Download Installer**](https://github.com/chinmoyy12/FloatView/releases/download/v1.0.0/FloatView-Setup-v1.0.0.exe) |
| **FloatView-v1.0.0-win-x64.zip** | Standalone Portable | x64 | ~63.8 MB | [**Download Portable Zip**](https://github.com/chinmoyy12/FloatView/releases/download/v1.0.0/FloatView-v1.0.0-win-x64.zip) |

### System Prerequisites
- **Operating System**: Windows 10 (Build 1809+) or Windows 11 (64-bit).
- **Runtime**: Microsoft Edge WebView2 Evergreen Runtime (pre-installed on Windows 11; automatically verified by installer).

---

## 🛠️ Building & Testing from Source

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

### 3. Run Complete Test Suite (65 Unit & Integration Tests)
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
Output portable zip: `installer\output\FloatView-v1.0.0-win-x64.zip`

---

## 🏛️ Project Architecture

```
FloatView/
├── .github/workflows/ci.yml # GitHub Actions Automated CI & Test Suite
├── extension/               # Companion Chromium MV3 Extension (Chrome, Edge, Brave)
│   ├── manifest.json        # MV3 manifest with Native Messaging & Tabs permissions
│   ├── content.js           # HTML5 media hooks & pause listener
│   ├── background.js        # Native Messaging & HTTP loopback service worker
│   ├── com.floatview.bridge.json # Native Messaging Host manifest
│   └── README.md            # Browser extension installation walkthrough
├── src/FloatView.App/
│   ├── Platform/            # Win32 User32/DWM, WASAPI Core Audio, TrayIconService, NativeMessagingRelay
│   ├── Browser/             # WebView2 Environment, MediaDetectionService, Player DOM Hooks
│   ├── Floating/            # FloatingWindow, WindowManager, WindowPositionService, EdgeActivation
│   ├── Media/               # SmartMediaCoordinator, BrowserBridgeService, GlobalHotkeys, AudioVisualizer
│   ├── Notifications/       # MediaDetectedNotificationWindow (Toast Prompt)
│   ├── Palette/             # Spotlight Command Palette Window (Alt + Space)
│   ├── Privacy/             # PrivacyFilterService Request Interceptor & Allowlist Store
│   ├── Focus/               # Monotonic Pomodoro Session Engine
│   ├── Persistence/         # Atomic File Swaps (.tmp -> File.Replace) & Crash Recovery
│   └── Views/               # Dashboard, Browser, Workspaces, Notes, Privacy, Settings, MediaSourcePicker
├── tests/FloatView.Tests/   # 65 Automated Unit & Integration Tests (xUnit)
│   ├── SmartMediaDetectionTests.cs  # Debounce, timestamp handoff, Native Messaging framing
│   ├── RealMediaVerificationTests.cs # YouTube endpoints, Privacy Shield allowlisting
│   ├── LocalhostMediaIntegrationTests.cs # Local server stream & visualizer loops
│   ├── MediaAndAudioTests.cs        # WASAPI meter ranges & hotkey unregistering
│   ├── WindowPositionAndLayoutTests.cs # Magnetic snapping math & multi-monitor bounds
│   └── SettingsAndPersistenceTests.cs  # Thread-safe atomic writes & corrupt file recovery
├── installer/               # Inno Setup Script (FloatView.iss)
└── scripts/                 # PowerShell Build, Test, Publish, Package & Register Automations
```

---

## 🛡️ Security & Privacy Policy

- **Local-First & Zero Telemetry**: FloatView has **zero remote tracking, analytics beacons, accounts, or cloud databases**.
- **Crash-Resistant Storage**: All configurations are serialized to `.tmp` files and atomically swapped via `File.Replace`. If corruption occurs, corrupted configs are safely backed up to `.corrupt.bak`.
- **Shared WebView2 Process Tree**: All floating instances share a thread-safe single `CoreWebView2Environment` targeting `%LOCALAPPDATA%\FloatView\WebView2Profile`, eliminating duplicate memory overhead.
- **For more information**, see [SECURITY.md](SECURITY.md).

---

## 📄 License

Distributed under the **MIT License**. See [LICENSE](LICENSE) for details.

<div align="center">
  <sub>Built with ❤️ for developers, power users, students, and gamers on Windows.</sub>
</div>
