# FloatView Companion Extension (Chromium MV3)

The **FloatView Companion Extension** provides browser-side media detection and control for **Google Chrome**, **Microsoft Edge**, and **Brave Browser**.

It works seamlessly alongside the [FloatView Windows Desktop Application](https://github.com/chinmoyy12/FloatView) to deliver the **Smart Media Detection (Detect, Select, Float)** experience.

---

## Features

- **HTML5 & Shadow DOM Media Hooking**: Automatically detects active `<video>` and `<audio>` elements across frames, web components, and dynamic pages.
- **Deep YouTube & Streaming Integration**: Extracts exact video titles, canonical URLs, and real-time playback timestamps (`currentTime`).
- **Safe Media Handoff**: Listens for the `FLOATVIEW_PAUSE` command issued by FloatView after a floating window confirms playback, preventing dual-audio playback without prematurely interrupting the original tab.
- **Zero Cloud / Local-Only**: Sends media metadata strictly to FloatView via:
  1. Chromium Native Messaging (`com.floatview.bridge` stdio pipe).
  2. Local loopback HTTP bridge (`http://127.0.0.1:48123/api/media/event`).

---

## Installation & Setup

### 1. Register the Native Messaging Host (One-Time)
Run the registration script from PowerShell (writes to `HKCU`—no administrator rights needed):
```powershell
powershell -ExecutionPolicy Bypass -File scripts\register-extension-host.ps1
```
*Alternatively, in the FloatView app, go to **Settings** → **Smart Media Detection** → click **Register Native Host**.*

### 2. Load Unpacked in Chrome or Edge
1. Open your browser:
   - **Google Chrome**: Go to `chrome://extensions/`
   - **Microsoft Edge**: Go to `edge://extensions/`
   - **Brave**: Go to `brave://extensions/`
2. Enable **Developer mode** (toggle in the top-right corner).
3. Click **Load unpacked**.
4. Select this directory:
   ```
   FloatView\extension
   ```
   *(If installed via setup, select `C:\Program Files\FloatView\extension`)*
5. The extension **"FloatView Companion - Smart Media Detection"** will appear as active!

---

## File Architecture

- `manifest.json`: Manifest V3 configuration with `nativeMessaging` and `tabs` permissions.
- `content.js`: Injected into web pages to hook media events (`play`, `pause`, `timeupdate`, `ended`) and execute pause commands.
- `background.js`: Service worker forwarding telemetry to the desktop application over native messaging or local loopback.
- `com.floatview.bridge.json`: Native messaging host manifest specifying the `FloatView.App.exe` binary path.
