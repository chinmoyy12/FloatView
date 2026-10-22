using System;
using System.Windows;
using System.Windows.Media.Animation;
using FloatView.App.Models;
using FloatView.App.Platform;

namespace FloatView.App.Floating;

public class EdgeActivationService
{
    private const double EdgeStripWidth = 10.0;

    public static (double hiddenLeft, double hiddenTop) GetHiddenCoordinates(
        DockEdge edge,
        MonitorArea monitor,
        double normalLeft,
        double normalTop,
        double width,
        double height) => edge switch
    {
        DockEdge.Left => (monitor.WorkLeft - width + EdgeStripWidth, normalTop),
        DockEdge.Right => (monitor.WorkLeft + monitor.WorkWidth - EdgeStripWidth, normalTop),
        DockEdge.Top => (normalLeft, monitor.WorkTop - height + EdgeStripWidth),
        DockEdge.Bottom => (normalLeft, monitor.WorkTop + monitor.WorkHeight - EdgeStripWidth),
        _ => (normalLeft, normalTop)
    };

    public static (double dockedLeft, double dockedTop) GetDockedCoordinates(
        DockEdge edge,
        MonitorArea monitor,
        double normalLeft,
        double normalTop,
        double width,
        double height) => edge switch
    {
        DockEdge.Left => (monitor.WorkLeft, normalTop),
        DockEdge.Right => (monitor.WorkLeft + monitor.WorkWidth - width, normalTop),
        DockEdge.Top => (normalLeft, monitor.WorkTop),
        DockEdge.Bottom => (normalLeft, monitor.WorkTop + monitor.WorkHeight - height),
        _ => (normalLeft, normalTop)
    };

    public static void AnimateWindowPosition(
        Window window,
        double targetLeft,
        double targetTop,
        int durationMs = 250,
        Action? onCompleted = null)
    {
        var leftAnim = new DoubleAnimation
        {
            To = targetLeft,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var topAnim = new DoubleAnimation
        {
            To = targetTop,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        if (onCompleted != null)
        {
            leftAnim.Completed += (_, _) => onCompleted();
        }

        window.BeginAnimation(Window.LeftProperty, leftAnim);
        window.BeginAnimation(Window.TopProperty, topAnim);
    }
}
