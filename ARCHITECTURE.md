# FloatView Architecture & Technical Specification

## 1. Overview & Core Philosophy

**FloatView** is a high-performance, keyboard-first Windows utility engineered in C# and .NET 10 LTS with Windows Presentation Foundation (WPF) and Microsoft Edge WebView2. It provides compact floating media windows, picture-in-picture capabilities, desktop-wide global hotkeys, screen-edge docking and reveal triggers, an embedded command palette, local privacy request filtering, Pomodoro focus sessions, and multi-window workspace presets.

---

## 2. Component Hierarchy

```
FloatView.slnx
├── extension/                 -> Companion Chromium MV3 Extension (manifest, content.js, background.js)
├── src/FloatView.App/
│   ├── Platform/
│   │   ├── WindowsApi.cs      -> Win32 User32 & DWM interop, click-through, EnumWindows, process window title resolution
│   │   ├── DisplayService.cs  -> Multi-monitor enumeration, work-area bounding (taskbars), DPI recovery
│   │   ├── StartupService.cs  -> Run at Windows startup via HKCU Run registry
│   │   ├── TrayIconService.cs -> Win32 Shell_NotifyIconW tray icon with double-click and context menu
│   │   └── NativeMessagingRelay.cs -> Chromium Native Messaging 4-byte length prefix stdio relay
│   ├── Persistence/
│   │   ├── StoragePaths.cs    -> AppData (settings/workspaces/notes/privacy) and LocalAppData (WebView2 profile)
│   │   └── SettingsService.cs -> Atomic file writes (.tmp -> File.Replace) & corruption fallback
│   ├── Models/
│   │   ├── AppSettings.cs     -> Configuration for theme, opacity, hotkeys, snapping, smart media detection
│   │   ├── SmartDetectionModels.cs -> MediaCandidate, MediaEventMessage, MediaSourceType
│   │   ├── FloatingWindowConfig.cs -> Presets (UltraMini, Compact, Cinema), coordinates, topmost, dock edge
│   │   └── EntityModels.cs    -> WorkspaceModel, FavoriteItem, CaptionNote, PrivacyRule, FocusSessionModel
│   ├── Floating/
│   │   ├── FloatingWindow.xaml -> Sleek borderless window, custom header, media controls, captions, visualizer
│   │   ├── FloatingWindowManager.cs -> Orchestrator for active floats, master show/hide, sessions
│   │   ├── WindowPositionService.cs -> Magnetic edge snapping to screen borders & neighboring floats, layouts
│   │   └── EdgeActivationService.cs -> Edge docking math & slide-out/in cubic ease animations
│   ├── Browser/
│   │   ├── BrowserService.cs  -> CoreWebView2Environment reuse, URL normalization (HTTPS/search fallback)
│   │   └── MediaDetectionService.cs -> JavaScript bridge for <video>/<audio> playback, native PiP, and caption cues
│   ├── Notifications/
│   │   └── MediaDetectedNotificationWindow.xaml -> Modern bottom-right toast with [Use FloatView] / [Not now]
│   ├── Media/
│   │   ├── SmartMediaCoordinator.cs -> Multi-candidate aggregator, debounce filter, cooldown, safe handoff
│   │   ├── BrowserBridgeService.cs -> Dual transport (HTTP 48123 & Named Pipe FloatViewMediaBridge)
│   │   ├── GlobalHotkeyService.cs -> Win32 RegisterHotKey message pump hook with conflict detection
│   │   └── AudioVisualizerService.cs -> Dynamic playback-reactive audio activity visualizer
│   ├── Privacy/
│   │   └── PrivacyFilterService.cs -> CoreWebView2.WebResourceRequested blocker for ad/tracking domains with allowlist
│   ├── Focus/
│   │   └── FocusSessionService.cs -> Monotonic Stopwatch Pomodoro timer (work/break cycles)
│   ├── Palette/
│   │   └── CommandPaletteWindow.xaml -> Alt+Space spotlight launcher with keyboard fuzzy search
│   ├── Themes/
│   │   ├── Colors.xaml        -> Deep graphite (#0D0F15, #161923), soft borders, violet accents
│   │   ├── Typography.xaml    -> Segoe UI Variable Display & Cascadia Code typography
│   │   ├── Icons.xaml         -> Crisp vector geometry paths
│   │   └── Controls.xaml      -> Button, slider, and input templates
│   └── Views/
│       ├── DashboardView.xaml -> Live active floats card, layout arrangements, focus timer, favorites
│       ├── BrowserView.xaml   -> Embedded mini browser with 1-click Pop Out to Float
│       ├── WorkspacesView.xaml -> Study Mode, Coding Setup, Gaming, Night Music presets & custom saving
│       ├── NotesView.xaml     -> Timestamped video captions, search filter, and Markdown export
│       ├── PrivacyView.xaml   -> Shield status, total blocked count, allowlist, and block rule manager
│       ├── SettingsView.xaml  -> Complete preferences, diagnostics, and data reset
│       └── MediaSourcePickerWindow.xaml -> "Choose what to float" modal with one-click float launch
└── tests/FloatView.Tests/     -> 65 automated unit tests covering Smart Media Detection, debouncing, WASAPI parsing,
                                  URL normalization, bounds math, snapping, workspaces round-trip, and privacy shield
```

---

## 3. Native Windows Window Management

### Dragging & Chrome Performance
Rather than using WPF `DragMove()` which can introduce jitter under heavy compositor load or during active video rendering, FloatView uses native Win32 message dispatch:
```csharp
WindowsApi.ReleaseCapture();
WindowsApi.SendMessage(hwnd, WindowsApi.WM_NCLBUTTONDOWN, (IntPtr)WindowsApi.HT_CAPTION, IntPtr.Zero);
```
This gives OS-native smoothness and directly integrates with Windows snap assists and multi-monitor window managers.

### Mouse Click-Through Mode
When enabled via UI or `Ctrl + Alt + K`:
1. `WindowsApi.EnableClickThrough(hwnd)` queries extended styles via `GetWindowLongPtr(hwnd, GWL_EXSTYLE)`.
2. Sets `WS_EX_TRANSPARENT | WS_EX_LAYERED`.
3. Clicks pass completely through to whatever IDE, game, or document is behind the float.
4. An amber `PASSTHROUGH` badge appears on the floating header to alert the user.
5. The global hotkey `Ctrl + Alt + K` or the Command Palette instantly disables the mode without trapping the user.

### Screen-Edge Docking & Reveal Tabs
When a window is docked to the right, left, top, or bottom:
1. `EdgeActivationService.GetHiddenCoordinates` calculates the work-area boundary minus an 8px activation strip.
2. An animated slide moves the window just off-screen using a cubic ease function.
3. Mouse hover or click triggers a smooth slide-in back to its active desktop coordinates.

---

## 4. WebView2 Lifecycle & Shared Environment

1. **Lazy Initialization**: Browser runtimes are never initialized on app startup until a floating window or the browser view is opened.
2. **Environment Reuse**: `BrowserService.GetOrCreateEnvironmentAsync()` maintains a thread-safe singleton `CoreWebView2Environment` targeting `%LOCALAPPDATA%\FloatView\WebView2Profile`. All floating windows share this optimized runtime profile, avoiding duplicate browser process trees.
3. **Safe Interception**: CoreWebView2's `WebResourceRequested` event runs the request host against `PrivacyFilterService`. If matched against known ad/tracking domains (and not in the user's allowlist), FloatView responds with an HTTP 403 response locally, preventing network latency and ad display without system-wide certificate injection.
4. **Media State Bridge**: An injected JavaScript snippet polls for active `<video>` and `<audio>` elements, hooks `play`, `pause`, and `timeupdate` events, and posts state back via `window.chrome.webview.postMessage`.

---

## 5. Security & Privacy

- **Local-First**: FloatView has zero remote tracking, telemetry, accounts, or cloud databases.
- **Atomic Persistence**: Configurations (`settings.json`, `workspaces.json`, etc.) are written to `.tmp` files and atomically swapped using `File.Replace`, protecting against sudden power outages or crashes.
- **Isolated Authentication**: Web credentials and cookies are confined to WebView2's isolated application profile and never leaked into plain text files or exported workspace JSONs.

---

## 6. Real-Time Audio (WASAPI) & Media Interop Architecture

1. **Hardware Loopback Audio Metering (WASAPI)**:
   - Implemented in `WasapiAudioMeterService` via native COM interfaces (`IMMDeviceEnumerator`, `IMMDevice`, `IAudioMeterInformation`).
   - Directly samples the physical hardware audio render endpoint (`eRender`, `eMultimedia`) at 30 fps without installing virtual audio drivers or requiring administrative access.
   - Completely bypasses browser Cross-Origin Resource Sharing (CORS) limits by reading the system-mixed audio level directly from the OS audio engine.
   - Stereo peak channels are decoupled into 6 dynamic frequency bands (Sub-bass, Bass, Low-mids, High-mids, Presence, Brilliance) with organic attack and musical decay physics.

2. **Protected Streams & Deep Player Injections**:
   - `CoreWebView2EnvironmentOptions` specifies `--enable-features=WidevineCdM,AudioServiceOutOfProcess,HardwareMediaKeyHandling` and `--autoplay-policy=no-user-gesture-required`.
   - `MediaDetectionService` leverages recursive Shadow DOM traversal (`deepQueryAll`) to locate nested video components across web components.
   - Injects platform-specific hooks for YouTube (`movie_player`), Netflix (`window.netflix.appContext.state.playerApp`), and Spotify Web Player.
   - Unblocks restricted PiP attributes (`disablepictureinpicture`, `controlslist="nopictureinpicture"`) and injects `allow="picture-in-picture; autoplay"` into all iframes. When native browser PiP is restricted by host policy, FloatView automatically executes a seamless fallback to its borderless Ultra-Mini floating window.


---

## 7. Smart Media Detection (Detect, Select, Float) Subsystem

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

1. **Dual-Layer Architecture**:
   - **Layer 1 (Companion Extension)**: Observes `<video>` and `<audio>` events across Chrome and Edge, capturing exact canonical URLs, clean titles, playback timestamps, and duration. Communicates via Chromium Native Messaging (`com.floatview.bridge`) and local HTTP loopback (`127.0.0.1:48123`).
   - **Layer 2 (Core Audio WASAPI)**: `AudioSessionWatcher` continuously queries `IAudioSessionManager2` to discover media audio output from browser processes without requiring any extension installation. Window titles are resolved via Win32 `EnumWindows` and mapped to domain sources.
2. **Coordinator & Debouncing**:
   - `SmartMediaCoordinator` coordinates candidate states. Playback must be continuously active for a configurable debounce threshold (default 2s) to prevent notification spam during playlist scrubbing.
   - 30-second cooldown suppression prevents repeated prompts for the same media.
   - Cross-references open floating windows to eliminate self-detection loops.
3. **One-Click Launch & Safe Handoff**:
   - `MediaSourcePickerWindow` lists all detected candidates. Clicking any item immediately opens the floating window at the current timestamp (`#t={seconds}s`).
   - Safe handoff policy waits until destination playback is verified, then dispatches an asynchronous `PAUSE` command to the external browser tab to prevent dual-audio without risking premature destruction of the source session.
