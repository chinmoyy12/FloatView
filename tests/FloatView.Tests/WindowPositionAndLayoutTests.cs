using System.Collections.Generic;
using System.Windows;
using FloatView.App.Floating;
using FloatView.App.Models;
using FloatView.App.Platform;
using Xunit;

namespace FloatView.Tests;

public class WindowPositionAndLayoutTests
{
    private readonly MonitorArea _mockMonitor = new(
        DeviceLeft: 0, DeviceTop: 0, DeviceWidth: 1920, DeviceHeight: 1080,
        WorkLeft: 0, WorkTop: 0, WorkWidth: 1920, WorkHeight: 1040,
        IsPrimary: true);

    [Fact]
    public void CalculateSnappedPosition_CloseToScreenEdge_SnapsToBoundary()
    {
        // 8px from left edge with snapDistance 16px -> should snap to 0
        var (left, top) = WindowPositionService.CalculateSnappedPosition(
            currentLeft: 8,
            currentTop: 100,
            width: 480,
            height: 270,
            snapDistance: 16.0,
            otherWindows: new List<Rect>());

        Assert.Equal(0, left);
        Assert.Equal(100, top);
    }

    [Fact]
    public void CalculateSnappedPosition_CloseToAdjacentWindow_SnapsToNeighbor()
    {
        var neighbor = new Rect(100, 100, 480, 270);
        var others = new List<Rect> { neighbor };

        // Placed at left 585 (5px gap from neighbor.Right = 580)
        var (left, top) = WindowPositionService.CalculateSnappedPosition(
            currentLeft: 585,
            currentTop: 100,
            width: 480,
            height: 270,
            snapDistance: 16.0,
            otherWindows: others);

        Assert.Equal(580, left); // Snapped cleanly to neighbor right
    }

    [Fact]
    public void ArrangeSideBySide_GeneratesNonOverlappingSlots()
    {
        var slots = WindowPositionService.ArrangeSideBySide(_mockMonitor, 2);
        Assert.Equal(2, slots.Count);

        // Slot 1 should be to the left of Slot 2
        Assert.True(slots[0].Right <= slots[1].Left);
        // Both should fit within work area
        Assert.True(slots[0].Left >= _mockMonitor.WorkLeft);
        Assert.True(slots[1].Right <= _mockMonitor.WorkLeft + _mockMonitor.WorkWidth);
    }

    [Fact]
    public void ArrangeStacked_GeneratesStackedVerticalSlots()
    {
        var slots = WindowPositionService.ArrangeStacked(_mockMonitor, 2);
        Assert.Equal(2, slots.Count);

        // Slot 0 should be vertically above Slot 1
        Assert.True(slots[0].Bottom <= slots[1].Top);
        // Both slots should have the same X (right aligned)
        Assert.Equal(slots[0].Left, slots[1].Left);
    }

    [Fact]
    public void ArrangeThreeWindowGrid_AllocatesOnePrimaryAndTwoSecondary()
    {
        var slots = WindowPositionService.ArrangeThreeWindowGrid(_mockMonitor);
        Assert.Equal(3, slots.Count);

        var primary = slots[0];
        var secTop = slots[1];
        var secBottom = slots[2];

        // Primary is on the left
        Assert.True(primary.Right < secTop.Left);
        // Secondaries are stacked vertically
        Assert.True(secTop.Bottom < secBottom.Top);
    }

    [Fact]
    public void EnsureVisible_RecoversCompletelyOffscreenCoordinates()
    {
        // Placed far off-screen (e.g. disconnected second monitor at X=5000)
        var safe = DisplayService.EnsureVisible(5000, 5000, 480, 270);

        Assert.True(safe.Left < 3000);
        Assert.True(safe.Top < 2000);
        Assert.Equal(480, safe.Width);
        Assert.Equal(270, safe.Height);
    }

    [Fact]
    public void EdgeActivationService_GetHiddenCoordinates_CalculatesCorrectOffsets()
    {
        var (rightHiddenX, _) = EdgeActivationService.GetHiddenCoordinates(
            DockEdge.Right, _mockMonitor, 100, 100, 480, 270);

        // Should be at work area right minus the 10px reveal strip
        Assert.Equal(_mockMonitor.WorkLeft + _mockMonitor.WorkWidth - 10, rightHiddenX);

        var (leftHiddenX, _) = EdgeActivationService.GetHiddenCoordinates(
            DockEdge.Left, _mockMonitor, 100, 100, 480, 270);

        // Should be offscreen to the left except 10px strip
        Assert.Equal(_mockMonitor.WorkLeft - 480 + 10, leftHiddenX);

        var (_, topHiddenY) = EdgeActivationService.GetHiddenCoordinates(
            DockEdge.Top, _mockMonitor, 100, 100, 480, 270);
        Assert.Equal(_mockMonitor.WorkTop - 270 + 10, topHiddenY);

        var (_, bottomHiddenY) = EdgeActivationService.GetHiddenCoordinates(
            DockEdge.Bottom, _mockMonitor, 100, 100, 480, 270);
        Assert.Equal(_mockMonitor.WorkTop + _mockMonitor.WorkHeight - 10, bottomHiddenY);
    }

    [Fact]
    public void MultiMonitor_NegativeCoordinates_SnappingAndDockingCalculationsAccurate()
    {
        // Secondary monitor positioned to the left of the primary display
        var leftMonitor = new MonitorArea(
            DeviceLeft: -1920, DeviceTop: 0, DeviceWidth: 1920, DeviceHeight: 1080,
            WorkLeft: -1920, WorkTop: 0, WorkWidth: 1920, WorkHeight: 1040,
            IsPrimary: false);

        // 1. Edge docking on negative coordinate screen
        var (dockedLeft, dockedTop) = EdgeActivationService.GetDockedCoordinates(
            DockEdge.Left, leftMonitor, -1500, 100, 480, 270);
        Assert.Equal(-1920, dockedLeft);
        Assert.Equal(100, dockedTop);

        var (dockedRight, _) = EdgeActivationService.GetDockedCoordinates(
            DockEdge.Right, leftMonitor, -1500, 100, 480, 270);
        Assert.Equal(-1920 + 1920 - 480, dockedRight); // -480

        // 2. Hidden coordinates on negative coordinate screen
        var (hiddenLeft, _) = EdgeActivationService.GetHiddenCoordinates(
            DockEdge.Left, leftMonitor, -1500, 100, 480, 270);
        Assert.Equal(-1920 - 480 + 10, hiddenLeft); // -2390

        var (hiddenRight, _) = EdgeActivationService.GetHiddenCoordinates(
            DockEdge.Right, leftMonitor, -1500, 100, 480, 270);
        Assert.Equal(-1920 + 1920 - 10, hiddenRight); // -10

        // 3. Side-by-side arrangement on negative coordinate monitor
        var slots = WindowPositionService.ArrangeSideBySide(leftMonitor, 2);
        Assert.Equal(2, slots.Count);
        Assert.True(slots[0].Left < 0);
        Assert.True(slots[1].Right <= 0);
        Assert.True(slots[0].Right <= slots[1].Left);
    }
}
