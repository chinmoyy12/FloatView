using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;
using FloatView.App.Models;
using FloatView.App.Persistence;
using FloatView.App.Platform;

namespace FloatView.App.Media;

public class GlobalHotkeyService : IDisposable
{
    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource? _hwndSource;
    private readonly Dictionary<int, (string action, Action handler)> _hotkeyActions = new();
    private int _currentId = 9000;
    private bool _isDisposed = false;

    public event Action<string, bool, string>? HotkeyRegistrationStatusChanged;

    public void Initialize(IntPtr windowHandle)
    {
        _hwnd = windowHandle;
        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(HwndHook);
    }

    public bool RegisterAction(string actionName, Key key, bool ctrl, bool alt, bool shift, bool win, Action handler)
    {
        if (_hwnd == IntPtr.Zero) return false;

        uint modifiers = (uint)WindowsApi.KeyModifiers.NoRepeat;
        if (ctrl) modifiers |= (uint)WindowsApi.KeyModifiers.Control;
        if (alt) modifiers |= (uint)WindowsApi.KeyModifiers.Alt;
        if (shift) modifiers |= (uint)WindowsApi.KeyModifiers.Shift;
        if (win) modifiers |= (uint)WindowsApi.KeyModifiers.Windows;

        int vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0) return false;

        int id = ++_currentId;
        bool success = WindowsApi.RegisterHotKey(_hwnd, id, modifiers, (uint)vk);

        if (success)
        {
            _hotkeyActions[id] = (actionName, handler);
            HotkeyRegistrationStatusChanged?.Invoke(actionName, true, "Registered successfully");
            return true;
        }
        else
        {
            var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            SettingsService.Log($"Failed to register hotkey {actionName} (Key={key}): Win32 Error {err}");
            HotkeyRegistrationStatusChanged?.Invoke(actionName, false, $"Conflict or error ({err})");
            return false;
        }
    }

    public void UnregisterAll()
    {
        if (_hwnd == IntPtr.Zero) return;

        foreach (var id in _hotkeyActions.Keys)
        {
            WindowsApi.UnregisterHotKey(_hwnd, id);
        }
        _hotkeyActions.Clear();
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WindowsApi.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var pair))
            {
                pair.handler.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            UnregisterAll();
            _hwndSource?.RemoveHook(HwndHook);
        }
    }
}
