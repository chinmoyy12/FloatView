# Security & Privacy Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

---

## 🛡️ Privacy & Local Data Security Architecture

FloatView is designed with a **strict local-first, zero-telemetry architecture**:

1. **Zero External Servers**: FloatView does not operate any cloud backend, telemetry ingestion servers, analytics beacons, or remote database endpoints.
2. **Local Request Interception**: The Privacy Shield inspects HTTP/HTTPS requests at the WebView2 boundary (`WebResourceRequested`) locally in memory. Ad and tracking domains are blocked locally with an HTTP 403 response without external DNS routing or cloud inspection.
3. **Isolated Authentication Profile**: All browser cookies, cached sessions, and website logins are securely stored in the local `%LOCALAPPDATA%\FloatView\WebView2Profile` folder and never exported, transmitted, or logged.
4. **Memory & IPC Boundaries**: The companion browser extension and desktop named pipe communicate strictly over `127.0.0.1` and `\\.\pipe\FloatViewMediaBridge`. No network sockets are exposed to the public internet or local area network.

---

## 🚨 Reporting a Vulnerability

If you discover a security vulnerability or potential privacy leak in FloatView, please do **NOT** open a public issue.

Instead, please report the vulnerability privately:
1. Open a **Private Vulnerability Report** via [GitHub Security Advisories](https://github.com/chinmoyy12/FloatView/security/advisories/new).
2. Or contact the maintainer directly via GitHub profile.

Please include:
- A description of the issue and potential impact.
- Exact steps or proof-of-concept to reproduce the behavior.
- Operating system version and WebView2 runtime version.

We will acknowledge receipt within 48 hours and work with you on a swift fix and disclosure timeline.
