using System;
using System.Threading;
using FloatView.App.Focus;
using Xunit;

namespace FloatView.Tests;

public class FocusSessionTests
{
    [Fact]
    public void FocusSessionService_InitialState_MatchesConfiguredMinutes()
    {
        var service = new FocusSessionService(workMinutes: 25, breakMinutes: 5);

        Assert.False(service.IsRunning);
        Assert.False(service.IsBreak);
        Assert.Equal("25:00", service.FormattedRemainingTime);
        Assert.Equal(0, service.CompletedSessions);
    }

    [Fact]
    public void FocusSessionService_StartAndPause_MaintainsAccurateRemainingTime()
    {
        var service = new FocusSessionService(workMinutes: 10, breakMinutes: 2);

        service.Start(workMinutes: 10, breakMinutes: 2);
        Assert.True(service.IsRunning);

        Thread.Sleep(50); // Small interval
        service.Pause();

        Assert.False(service.IsRunning);
        Assert.True(service.RemainingTime < TimeSpan.FromMinutes(10));
        Assert.True(service.RemainingTime > TimeSpan.FromMinutes(9));

        var pausedTime = service.RemainingTime;
        Thread.Sleep(50);
        // While paused, time does not continue to decrease
        Assert.Equal(pausedTime, service.RemainingTime);
    }

    [Fact]
    public void FocusSessionService_Reset_RestoresFullDuration()
    {
        var service = new FocusSessionService(workMinutes: 20, breakMinutes: 5);
        service.Start(20, 5);
        Thread.Sleep(50);

        service.Reset();

        Assert.False(service.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(20), service.RemainingTime);
    }
}
