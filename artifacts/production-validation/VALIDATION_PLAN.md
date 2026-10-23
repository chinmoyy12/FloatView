# FloatView v1.0.0 — Production Validation Plan & Engineering Test Matrix

## 1. Executive Summary & Objective
This document outlines the end-to-end verification plan, feature test matrix, and automated regression suite for **FloatView** (Windows 10/11 x64, .NET 10 LTS, WebView2). The objective is to achieve true production readiness through empirical testing, real-world media playback verification, defect remediation, and release packaging.

---

## 2. Test Environment Specification
- **Operating System**: Windows 10/11 x64 (OS Build 10.0.26300)
- **.NET SDK & Runtime**: .NET 10.0.402 SDK / .NET 10.0.13 Desktop Runtime
- **WebView2 Evergreen Runtime**: Version 155.0.4283.62
- **Audio Subsystem**: Windows Core Audio / WASAPI Master Multimedia Endpoint (`IAudioMeterInformation`)
- **Display Configuration**: 1920 × 1080 (Primary Monitor, 60Hz, 100% DPI Scaling)
- **Localhost Testbed Server**: Python 3.12 HTTP Server on `http://localhost:5055`
- **Inno Setup Compiler**: Inno Setup 6.4.4 (`C:\Users\chinmoy\AppData\Local\Programs\Inno Setup 6\ISCC.exe`)

---

## 3. Autonomous Execution Roadmap

```
Baseline Verification & Defect Identification
   │
   ▼
Source Code Repairs & Enhancements (Hotkeys, Markdown, Atomic Writes, Corrupt Recovery)
   │
   ▼
Automated Regression Test Suite Expansion (50+ Unit & Integration Tests)
   │
   ▼
Real-World Media Playback & Localhost Verification (YouTube, Streams, Localhost 5055)
   │
   ▼
Native Windows Desktop Feature Testing (Snap, Click-Through, WASAPI, Edge Docking)
   │
   ▼
Performance, Memory & Stability Validation (10m Playback & Resource Monitoring)
   │
   ▼
Windows Release Build, Publish & Installer Packaging (Self-Contained, Portable Zip, Setup.exe)
   │
   ▼
Deliverables: PRODUCTION_READINESS_REPORT.md, FEATURE_TEST_MATRIX.md, BUG_FIX_LOG.md
```

---

## 4. Feature Testing Scope & Verification Gates

| Phase | Feature / Component | Target Verification | Gate |
|---|---|---|---|
| **Phase 4** | Real YouTube / Media Playback | Progression, Audio, Volume, Seek, Pause/Resume, Source Switching | Gate B |
| **Phase 5** | Borderless Always-on-Top Floats | Pinning, Presets (320x180, 480x270, 720x405), Dragging, Opacity | Gate C |
| **Phase 5** | Edge Docking & Reveal Tabs | Left, Right, Top, Bottom docking with 10px reveal strip & smooth cubic ease | Gate C |
| **Phase 5** | Click-Through Mode | `Ctrl+Alt+K`, `WS_EX_TRANSPARENT`, amber badge cue, safe return | Gate C |
| **Phase 6** | Global Hotkeys & Command Palette | `Alt+Space`, `Ctrl+Alt+F`, `Ctrl+Alt+T`, `Ctrl+Alt+K`, `Ctrl+Alt+P`, `Ctrl+Alt+O` | Gate D |
| **Phase 7** | WASAPI Hardware Audio Visualizer | 6-band real stereo meter sampling (`IAudioMeterInformation`) | Gate D |
| **Phase 8** | Privacy Shield & Request Blocker | Tracker filtering, per-site allowlist, 403 synthetic response | Gate D |
| **Phase 9** | Captions & Note Taking | Live extraction, timestamped note capture, Markdown export | Gate D |
| **Phase 10**| Monotonic Pomodoro Focus Engine | High-res stopwatch timing, break transitions, audio alert | Gate D |
| **Phase 11**| Settings & Persistence | Atomic file write, corrupt config backup recovery, session restore | Gate E |
| **Phase 12**| Performance & Memory | Stability, zero handle leaks, low CPU visualizer throttling | Gate E |
| **Phase 15**| Build, Publish, Zip & Installer | Self-contained win-x64, portable archive, Inno Setup `.exe` | Gate F |
