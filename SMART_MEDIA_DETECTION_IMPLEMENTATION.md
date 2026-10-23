# FloatView Smart Media Detection — Architecture & Implementation

## Executive Summary

**FloatView Smart Media Detection (Detect, Select, Float)** enables FloatView to remain quietly active in the background, detect media playback initiated across external browsers (such as Google Chrome, Microsoft Edge, Brave, and Mozilla Firefox), notify the user with an unobtrusive popup, present active media candidates, and launch a chosen source into a floating window with **one single click**.

The architecture implements a **dual-layer detection strategy**:
1. **Layer 1 — High-Fidelity Companion Extension & Native Bridge**: Captures exact media metadata (real video title, canonical URL, playback position, duration, and tab identifier) via a Manifest V3 companion extension communicating over Chromium Native Messaging and an HTTP loopback bridge.
2. **Layer 2 — Windows Core Audio (WASAPI) Session Enumeration**: System-level audio session monitoring via COM interfaces (`IAudioSessionManager2`, `IAudioSessionEnumerator`, `IAudioSessionControl2`, `IAudioMeterInformation`) that detects external audio sessions and links PIDs to browser windows even if the user has not installed or loaded any browser extension.

---

## 1. Architectural Component Overview

```
+-----------------------------------------------------------------------------------+
|                            EXTERNAL BROWSER PLAYBACK                              |
|   (Google Chrome, Microsoft Edge, Brave, Firefox, YouTube, Twitch, Local Streams) |
+----------------------------------------+------------------------------------------+
                                         |
               +-------------------------+-------------------------+
               | Layer 1: Extension                                | Layer 2: Core Audio
               v                                                   v
    +----------------------+                            +----------------------+
    | extension/content.js |                            | AudioSessionWatcher  |
    | HTML5 / YouTube Hook |                            | WASAPI Session Enum  |
    +----------+-----------+                            +----------+-----------+
               |                                                   |
               v                                                   | (PID / Peak / Window)
    +----------------------+                                       |
    |  extension/bg.js     |                                       |
    | (Native / HTTP Post) |                                       |
    +----------+-----------+                                       |
               |                                                   |
               +-------------------------+                         |
                                         v                         v
                       +--------------------------------------------------+
                       |             FloatView Desktop App                |
                       |                                                  |
                       |  [ BrowserBridgeService ] (HTTP & Named Pipe)    |
                       |                      |                           |
                       |                      v                           |
                       |         [ SmartMediaCoordinator ]                |
                       |   - Debounce Filter (1-5s active playing)        |
                       |   - Cooldown Suppression (30s per source)        |
                       |   - Self-Floating URL Deduplication              |
                       |                      |                           |
                       |                      +------------------------+  |
                       |                      |                        |  |
                       |                      v                        v  |
                       |          [ Notification Toast ]      [ Tray Icon ]|
                       |          "Media detected" (WPF)      (Shell API)  |
                       |                      |                           |
                       |                      v                           |
                       |          [ MediaSourcePicker ]                   |
                       |         "Choose what to float"                   |
                       |                      | (One-Click Launch)        |
                       |                      v                           |
                       |         [ FloatingWindowManager ]                |
                       |         Launches Float + Safe Handoff            |
                       +--------------------------------------------------+
```

---

## 2. Core Components & Implementation Details

### A. Data Models (`SmartDetectionModels.cs`)
- **`MediaCandidate`**: Encapsulates media title, URL, domain, browser name, process ID, tab ID, playback timestamp (`CurrentTime`), duration, audio peak, and source type (`Extension` vs `AudioSessionFallback`).
  - Includes helper properties: `FormattedTime` (displays `mm:ss / mm:ss` or `hh:mm:ss`) and `DisplaySubtitle`.
- **`MediaEventMessage`**: Standardized JSON payload schema for IPC exchange between the browser extension and FloatView (`type`, `tabId`, `url`, `title`, `currentTime`, `duration`, `isPlaying`, `isMuted`, `domain`, `browserName`).

### B. Windows Core Audio Session Watcher (`AudioSessionWatcher.cs`)
- Communicates directly with WASAPI via COM interfaces:
  - `IMMDeviceEnumerator` and `IMMDevice` for default audio rendering endpoints.
  - `IAudioSessionManager2` (`77AA99A0-1BD6-447F-8F77-44042EC15EF5`) to obtain `IAudioSessionEnumerator`.
  - `IAudioSessionControl2` (`bfb7ff88-7239-4fc9-8fa2-07c950be9c31`) to inspect `AudioSessionState` and retrieve the owning process ID (`GetProcessId`).
  - `IAudioMeterInformation` (`C02216F6-0380-4D5B-9C00-DAC82BE3EF83`) to measure real-time peak audio amplitude.
- Excludes FloatView's own process (`Environment.ProcessId`) and child WebView2 processes.
- Correlates audio-producing PIDs with top-level visible windows using Win32 API (`EnumWindows`, `GetWindowThreadProcessId`, `GetWindowText`).
- Cleans and formats titles by stripping browser suffixes (`" - Google Chrome"`, `" - Microsoft Edge"`) and infers service domains (`youtube.com`, `twitch.tv`, `spotify.com`).

### C. Browser Bridge & Native Messaging Relay (`BrowserBridgeService.cs` & `NativeMessagingRelay.cs`)
- **Dual Communication Transport**:
  1. **HTTP Loopback Listener (`http://127.0.0.1:48123/api/media/event`)**: Provides instantaneous zero-configuration connectivity for unpacked Chrome/Edge extensions with full CORS headers.
  2. **Named Pipe Bridge (`\\.\pipe\FloatViewMediaBridge`)**: Handles high-performance native inter-process communication.
- **Native Messaging Relay Mode**:
  - `FloatView.App.exe` handles `--native-messaging-host` and `chrome-extension://` command-line flags on startup (`OnStartup`).
  - Reads and writes 4-byte little-endian length-prefixed JSON packets conforming to the Chromium Native Messaging specification.
  - Transparently forwards browser events to the main running FloatView instance via Named Pipe.

### D. Smart Media Coordinator (`SmartMediaCoordinator.cs`)
- **Playback Debounce Logic**:
  - Media must be actively playing for a configurable interval (`SmartDetectionDebounceSeconds`, default 2 seconds) before a notification is triggered.
  - Prevents alert spam when users rapidly seek or skip through videos.
- **Cooldown Suppression**:
  - Remembers notifications per source URL or domain for a configurable cooldown window (`SmartDetectionCooldownSeconds`, default 30 seconds).
  - Dismissing an alert ("Not now") enforces cooldown so the user is not repeatedly interrupted.
- **Self-Float Deduplication**:
  - Cross-references incoming candidate URLs with `FloatingWindowManager.ActiveWindows`. If FloatView is already floating that exact URL, candidate notification is suppressed.
- **Safe Handoff Execution**:
  - Computes exact timestamp target URLs:
    - For YouTube: Appends `#t={seek}s` or `&t={seek}s` to resume playback at the exact elapsed second.
  - Opens float window via `FloatingWindowManager.OpenFloat()`.
  - Once destination playback is confirmed, issues an asynchronous `PAUSE` command to the external browser tab via `BrowserBridgeService.SendPauseCommand()`.
  - Ensures source media is **never destroyed or paused prematurely** if the destination faces autoplay blocks or network errors.

### E. User Interface Windows
1. **`MediaDetectedNotificationWindow.xaml`**:
   - Sleek, unobtrusive toast window positioned at the bottom-right of the primary display work area.
   - Shows real media title, domain, and browser name.
   - Features `[ Use FloatView ]` (accent purple) and `[ Not now ]` (dismiss) action buttons.
   - Auto-dismiss countdown (8 seconds) that automatically pauses when the cursor hovers over the card.
2. **`MediaSourcePickerWindow.xaml`**:
   - Modal selection dialog titled **"Choose what to float"**.
   - Displays all detected media candidates with domain, title, timestamp, and visual indicator badge.
   - **One-Click Float**: Clicking any card immediately starts the float and closes the picker without requiring a secondary confirmation or Start button.

### F. System Tray & Application Lifecycle (`TrayIconService.cs`)
- Uses native Win32 `Shell_NotifyIconW` via P/Invoke with zero third-party dependencies.
- Handles double-click to restore/focus MainWindow.
- Right-click context menu provides:
  - Open FloatView
  - Choose Media to Float (with live active count)
  - Toggle Smart Media Detection
  - Exit FloatView (clean shutdown)
- When MainWindow is closed, if `MinimizeToTrayOnClose` or `SmartMediaDetectionEnabled` is true, the window is hidden (`Hide()`) rather than terminating, keeping background detection alive.

---

## 3. Configuration & Settings Additions

In `AppSettings.cs` and `SettingsView.xaml`:
- `SmartMediaDetectionEnabled` (bool, default: `true`): Master toggle for external media detection.
- `SmartDetectionShowToast` (bool, default: `true`): Controls notification popup visibility.
- `SmartDetectionDebounceSeconds` (int, default: `2`): Playback confirmation threshold (1 to 5 seconds).
- `SmartDetectionCooldownSeconds` (int, default: `30`): Minimum seconds between repeated alerts for the same source.
- `SmartDetectionSafeHandoff` (bool, default: `true`): Safely pauses the external browser tab once float starts.
- `SmartDetectionAudioFallbackEnabled` (bool, default: `true`): Enables WASAPI audio session detection.
- `MinimizeToTrayOnClose` (bool, default: `true`): Keeps FloatView in tray when dashboard is closed.
- Extension helper controls: Displays bridge port (`48123`) and a one-click button to register the Native Messaging Host in the Windows registry.
