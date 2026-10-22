using System;
using System.Collections.Generic;
using System.Windows;
using FloatView.App.Models;
using FloatView.App.Platform;

namespace FloatView.App.Floating;

public static class WindowPositionService
{
    public static (double left, double top) CalculateSnappedPosition(
        double currentLeft,
        double currentTop,
        double width,
        double height,
        double snapDistance,
        IReadOnlyList<Rect> otherWindows)
    {
        var monitor = DisplayService.GetCurrentMonitor(currentLeft, currentTop);
        double left = currentLeft;
        double top = currentTop;

        // Snap to Monitor Work Area Left / Right
        if (Math.Abs(left - monitor.WorkLeft) <= snapDistance)
        {
            left = monitor.WorkLeft;
        }
        else if (Math.Abs((left + width) - (monitor.WorkLeft + monitor.WorkWidth)) <= snapDistance)
        {
            left = monitor.WorkLeft + monitor.WorkWidth - width;
        }

        // Snap to Monitor Work Area Top / Bottom
        if (Math.Abs(top - monitor.WorkTop) <= snapDistance)
        {
            top = monitor.WorkTop;
        }
        else if (Math.Abs((top + height) - (monitor.WorkTop + monitor.WorkHeight)) <= snapDistance)
        {
            top = monitor.WorkTop + monitor.WorkHeight - height;
        }

        // Snap to other floating windows
        foreach (var other in otherWindows)
        {
            // Horizontal alignment with other window
            if (Math.Abs(left - other.Right) <= snapDistance && VerticalOverlap(top, height, other.Top, other.Height))
            {
                left = other.Right;
            }
            else if (Math.Abs(left + width - other.Left) <= snapDistance && VerticalOverlap(top, height, other.Top, other.Height))
            {
                left = other.Left - width;
            }

            // Vertical alignment with other window
            if (Math.Abs(top - other.Bottom) <= snapDistance && HorizontalOverlap(left, width, other.Left, other.Width))
            {
                top = other.Bottom;
            }
            else if (Math.Abs(top + height - other.Top) <= snapDistance && HorizontalOverlap(left, width, other.Left, other.Width))
            {
                top = other.Top - height;
            }
        }

        return (left, top);
    }

    private static bool VerticalOverlap(double y1, double h1, double y2, double h2) =>
        Math.Max(y1, y2) < Math.Min(y1 + h1, y2 + h2);

    private static bool HorizontalOverlap(double x1, double w1, double x2, double w2) =>
        Math.Max(x1, x2) < Math.Min(x1 + w1, x2 + w2);

    public static List<Rect> ArrangeSideBySide(MonitorArea monitor, int windowCount)
    {
        var list = new List<Rect>();
        if (windowCount <= 0) return list;

        double spacing = 16.0;
        double totalWidth = monitor.WorkWidth * 0.7;
        double slotWidth = (totalWidth - (spacing * (windowCount - 1))) / windowCount;
        double slotHeight = slotWidth * (9.0 / 16.0); // 16:9 ratio

        double startX = monitor.WorkLeft + (monitor.WorkWidth - totalWidth) / 2;
        double startY = monitor.WorkTop + (monitor.WorkHeight - slotHeight) / 2;

        for (int i = 0; i < windowCount; i++)
        {
            list.Add(new Rect(startX + i * (slotWidth + spacing), startY, slotWidth, slotHeight));
        }

        return list;
    }

    public static List<Rect> ArrangeStacked(MonitorArea monitor, int windowCount)
    {
        var list = new List<Rect>();
        if (windowCount <= 0) return list;

        double spacing = 16.0;
        double slotHeight = 260;
        double slotWidth = slotHeight * (16.0 / 9.0);
        double totalHeight = (slotHeight * windowCount) + (spacing * (windowCount - 1));

        double startX = monitor.WorkLeft + monitor.WorkWidth - slotWidth - 32;
        double startY = monitor.WorkTop + (monitor.WorkHeight - totalHeight) / 2;

        for (int i = 0; i < windowCount; i++)
        {
            list.Add(new Rect(startX, startY + i * (slotHeight + spacing), slotWidth, slotHeight));
        }

        return list;
    }

    public static List<Rect> ArrangeThreeWindowGrid(MonitorArea monitor)
    {
        var list = new List<Rect>();
        double spacing = 16.0;

        // Large Primary on the Left
        double leftWidth = monitor.WorkWidth * 0.55;
        double leftHeight = leftWidth * (9.0 / 16.0);
        double startX = monitor.WorkLeft + 32;
        double startY = monitor.WorkTop + (monitor.WorkHeight - leftHeight) / 2;
        list.Add(new Rect(startX, startY, leftWidth, leftHeight));

        // Two smaller stacked on the Right
        double rightX = startX + leftWidth + spacing;
        double rightWidth = monitor.WorkWidth * 0.35;
        double rightHeight = (leftHeight - spacing) / 2;

        list.Add(new Rect(rightX, startY, rightWidth, rightHeight));
        list.Add(new Rect(rightX, startY + rightHeight + spacing, rightWidth, rightHeight));

        return list;
    }
}
