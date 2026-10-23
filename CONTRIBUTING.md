# Contributing to FloatView

Thank you for your interest in contributing to **FloatView**! We welcome contributions from developers, designers, and testers.

---

## 🛠️ Development Setup

### Prerequisites
- **Operating System**: Windows 10 (Build 1809+) or Windows 11 (64-bit).
- **.NET SDK**: .NET 10.0 SDK (or later).
- **WebView2**: Microsoft Edge WebView2 Evergreen Runtime (pre-installed on Windows 11).
- **Inno Setup 6** (Optional, for packaging installers): [Inno Setup Download](https://jrsoftware.org/isdl.php).

### Clone and Build
```powershell
# Clone the repository
git clone https://github.com/chinmoyy12/FloatView.git
cd FloatView

# Build Debug / Release
powershell -ExecutionPolicy Bypass -File scripts\build.ps1

# Run the automated test suite (65 tests)
powershell -ExecutionPolicy Bypass -File scripts\test.ps1
```

---

## 🏛️ Code Architecture & Guidelines

FloatView is organized into clean, decoupled layers:

1. **`Platform/`**: Direct native Win32 and COM interop (`User32`, `DwmApi`, `Shell32`, WASAPI `AudioClient` / `IAudioSessionManager2`). Keep P/Invoke declarations isolated here.
2. **`Browser/`**: Microsoft Edge WebView2 integration, environment caching, script injection, and player DOM hooks.
3. **`Floating/`**: Floating window coordinates, multi-monitor bounds resolution, snapping logic, and edge docking animations.
4. **`Media/`**: Background coordinator, browser bridge (HTTP & Named Pipe), audio visualizer, and global hotkey message pump.
5. **`Persistence/`**: Atomic file writes with `.tmp` staging and `File.Replace` swaps. Never perform raw unbuffered direct file overwrites.
6. **`Privacy/`**: WebView2 request filter, URL normalization, and tracking domain blocking rules.

---

## 🧪 Testing Guidelines

Before opening a pull request, ensure all tests pass:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\test.ps1
```

When adding new features:
- Add comprehensive unit tests in `tests/FloatView.Tests/`.
- Ensure tests run cleanly in headless CI environments without requiring active physical audio or display hardware.
- Maintain zero compiler warnings and zero test failures.

---

## 📬 Pull Request Process

1. Fork the repository and create a descriptive branch:
   ```powershell
   git checkout -b feat/my-new-feature
   ```
2. Commit your changes with clear, semantic commit messages (`feat: ...`, `fix: ...`, `docs: ...`).
3. Push to your fork and submit a Pull Request targeting `main`.
4. Ensure the GitHub Actions CI pipeline completes with a green checkmark.

---

## 📄 License
By contributing to FloatView, you agree that your contributions will be licensed under the project's [MIT License](LICENSE).
