using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using FloatView.App.Persistence;

namespace FloatView.App.Platform;

public class TrayIconService : IDisposable
{
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 101;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_LBUTTONUP = 0x0202;

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private readonly Window _ownerWindow;
    private HwndSource? _hwndSource;
    private IntPtr _hwnd = IntPtr.Zero;
    private bool _isCreated = false;
    private bool _disposed = false;

    public event Action? DoubleClicked;
    public event Action? RightClicked;

    public TrayIconService(Window ownerWindow)
    {
        _ownerWindow = ownerWindow;
    }

    public void Initialize(IntPtr hwnd)
    {
        if (_isCreated) return;
        _hwnd = hwnd;

        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(HwndHook);

        CreateTrayIcon();
    }

    private void CreateTrayIcon()
    {
        try
        {
            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1001,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                szTip = "FloatView - Floating Browser & Smart Media Player",
                hIcon = LoadAppIcon()
            };

            _isCreated = Shell_NotifyIcon(NIM_ADD, ref data);
            if (!_isCreated)
            {
                SettingsService.Log("Warning: Could not create system tray icon.");
            }
        }
        catch (Exception ex)
        {
            SettingsService.Log($"CreateTrayIcon error: {ex.Message}");
        }
    }

    private IntPtr LoadAppIcon()
    {
        try
        {
            // Use standard application icon as fallback
            const int IDI_APPLICATION = 32512;
            return LoadIcon(IntPtr.Zero, (IntPtr)IDI_APPLICATION);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAYICON)
        {
            int lparamVal = lParam.ToInt32();
            if (lparamVal == WM_LBUTTONDBLCLK)
            {
                DoubleClicked?.Invoke();
                handled = true;
            }
            else if (lparamVal == WM_RBUTTONUP)
            {
                RightClicked?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void ShowContextMenu(ContextMenu menu)
    {
        if (menu == null || _hwnd == IntPtr.Zero) return;
        SetForegroundWindow(_hwnd);
        menu.IsOpen = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isCreated && _hwnd != IntPtr.Zero)
        {
            try
            {
                var data = new NOTIFYICONDATA
                {
                    cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = _hwnd,
                    uID = 1001
                };
                Shell_NotifyIcon(NIM_DELETE, ref data);
            }
            catch {}
            _isCreated = false;
        }

        _hwndSource?.RemoveHook(HwndHook);
    }
}
