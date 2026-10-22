using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace FloatView.App.Platform;

public record MonitorArea(
    int DeviceLeft,
    int DeviceTop,
    int DeviceWidth,
    int DeviceHeight,
    int WorkLeft,
    int WorkTop,
    int WorkWidth,
    int WorkHeight,
    bool IsPrimary);

public static class DisplayService
{
    private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref WindowsApi.RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    public static List<MonitorArea> GetMonitors()
    {
        var monitors = new List<MonitorArea>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref WindowsApi.RECT lprcMonitor, IntPtr dwData) =>
        {
            var mi = new WindowsApi.MONITORINFO();
            mi.cbSize = Marshal.SizeOf(typeof(WindowsApi.MONITORINFO));
            if (WindowsApi.GetMonitorInfo(hMonitor, ref mi))
            {
                bool isPrimary = (mi.dwFlags & 1) != 0;
                monitors.Add(new MonitorArea(
                    mi.rcMonitor.Left,
                    mi.rcMonitor.Top,
                    mi.rcMonitor.Width,
                    mi.rcMonitor.Height,
                    mi.rcWork.Left,
                    mi.rcWork.Top,
                    mi.rcWork.Width,
                    mi.rcWork.Height,
                    isPrimary));
            }
            return true;
        }, IntPtr.Zero);

        if (monitors.Count == 0)
        {
            // Fallback to primary screen
            monitors.Add(new MonitorArea(0, 0, 1920, 1080, 0, 0, 1920, 1040, true));
        }

        return monitors;
    }

    public static MonitorArea GetCurrentMonitor(double logicalX, double logicalY)
    {
        var pt = new WindowsApi.POINT { X = (int)logicalX, Y = (int)logicalY };
        var hMon = WindowsApi.MonitorFromPoint(pt, WindowsApi.MONITOR_DEFAULTTONEAREST);
        
        var mi = new WindowsApi.MONITORINFO();
        mi.cbSize = Marshal.SizeOf(typeof(WindowsApi.MONITORINFO));
        if (WindowsApi.GetMonitorInfo(hMon, ref mi))
        {
            return new MonitorArea(
                mi.rcMonitor.Left,
                mi.rcMonitor.Top,
                mi.rcMonitor.Width,
                mi.rcMonitor.Height,
                mi.rcWork.Left,
                mi.rcWork.Top,
                mi.rcWork.Width,
                mi.rcWork.Height,
                (mi.dwFlags & 1) != 0);
        }

        return GetMonitors()[0];
    }

    /// <summary>
    /// Ensures window coordinates (left, top, width, height) are safely visible within an active monitor.
    /// If outside (e.g. disconnected monitor or resolution changed), clamps or moves to the nearest visible area.
    /// </summary>
    public static Rect EnsureVisible(double left, double top, double width, double height)
    {
        var monitors = GetMonitors();
        bool isPartiallyVisible = false;

        var targetRect = new Rect(left, top, width, height);

        foreach (var m in monitors)
        {
            var workRect = new Rect(m.WorkLeft, m.WorkTop, m.WorkWidth, m.WorkHeight);
            var intersection = Rect.Intersect(targetRect, workRect);
            if (!intersection.IsEmpty && intersection.Width >= 60 && intersection.Height >= 40)
            {
                isPartiallyVisible = true;
                break;
            }
        }

        if (isPartiallyVisible)
        {
            return targetRect;
        }

        // Recover to primary monitor work area
        var primary = monitors.Find(m => m.IsPrimary) ?? monitors[0];
        double safeWidth = Math.Min(width, primary.WorkWidth * 0.9);
        double safeHeight = Math.Min(height, primary.WorkHeight * 0.9);
        double safeLeft = primary.WorkLeft + (primary.WorkWidth - safeWidth) / 2;
        double safeTop = primary.WorkTop + (primary.WorkHeight - safeHeight) / 2;

        return new Rect(safeLeft, safeTop, safeWidth, safeHeight);
    }
}
