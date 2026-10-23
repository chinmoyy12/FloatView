# FloatView Smart Media Detection — Setup & Usage Guide

## Overview

FloatView Smart Media Detection operates with zero mandatory setup out of the box using Windows Core Audio (WASAPI) session monitoring. For enhanced precision—including exact video titles, current timestamps, canonical URLs, and safe handoff pausing—you can load the lightweight companion browser extension for Google Chrome and Microsoft Edge.

---

## Method 1: Out-of-the-Box Usage (Zero Setup Required)

FloatView includes a built-in **Windows Core Audio Session Watcher**. Even if you never install a browser extension:

1. Launch **FloatView**.
2. Close the FloatView window to the system tray (it remains active in the background).
3. Open **Google Chrome**, **Microsoft Edge**, **Brave**, or **Mozilla Firefox** and play any audio or video stream.
4. FloatView detects that the browser is outputting media audio.
5. A notification appears:
   ```
   Media detected
   YouTube • Google Chrome
   [ Use FloatView ]  [ Not now ]
   ```
6. Click **Use FloatView** to open the media picker, and click the source to float it immediately.

---

## Method 2: High-Fidelity Companion Extension (Chrome & Edge)

Installing the companion extension enables:
- Exact video and page titles
- Exact URL and elapsed timestamp synchronization (e.g. resuming YouTube at `03:42`)
- Safe Media Handoff (FloatView automatically pauses the source browser tab once the float starts playing)

### Step 1: Register Native Messaging Host (One Click)
You can register the host in either of two ways:
- **Option A (UI)**: Open FloatView -> **Settings** -> **Smart Media Detection** -> click **Register Native Host**.
- **Option B (PowerShell)**: In a PowerShell terminal, run:
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts\register-extension-host.ps1
  ```
*Note: Registration writes to `HKCU:\Software\Google\Chrome\NativeMessagingHosts` and `HKCU:\Software\Microsoft\Edge\NativeMessagingHosts`, requiring no administrator privileges.*

### Step 2: Load the Extension into Google Chrome or Microsoft Edge
1. Open your browser:
   - For **Google Chrome**: Navigate to `chrome://extensions/`
   - For **Microsoft Edge**: Navigate to `edge://extensions/`
2. Enable **Developer mode** (toggle in the top-right corner of the extensions page).
3. Click **Load unpacked**.
4. Select the `extension` folder inside the FloatView directory:
   ```
   c:\Users\chinmoy\.gemini\antigravity-ide\scratch\FloatView\extension
   ```
5. The extension **"FloatView Companion - Smart Media Detection"** will appear in your extensions list.

---

## Verifying the End-to-End Workflow

1. Ensure FloatView is running (it can be minimized or closed to the system tray).
2. Open Chrome or Edge and start playing any YouTube video (e.g., `https://www.youtube.com/watch?v=jfKfPfyJRdk`).
3. Let it play for at least 2 seconds (debounce threshold).
4. An unobtrusive notification toast will slide in at the bottom-right of your screen:
   ```
   Media detected
   Lofi Girl - Beats to Relax/Study to
   youtube.com • Google Chrome • 01:24
   [ Use FloatView ]  [ Not now ]
   ```
5. Click **Use FloatView**.
6. The **Choose what to float** picker appears displaying the video details and current timestamp.
7. Click the entry card.
8. **One-Click Launch**: The video immediately opens in a FloatView floating window at the current timestamp, and the external browser tab is safely paused.

---

## System Tray Controls

Look for the FloatView icon in the Windows notification area (system tray):
- **Double-click**: Restores and focuses the FloatView main dashboard.
- **Right-click**:
  - **FloatView**: Restores the main window.
  - **Choose Media to Float (N)**: Opens the media picker to select from currently active browser sources.
  - **Smart Media Detection**: Toggle background detection on or off on the fly.
  - **Exit FloatView**: Safely saves your session and shuts down all background services.
