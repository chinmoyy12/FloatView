using System;
using System.Diagnostics;
using System.Windows.Threading;
using FloatView.App.Models;

namespace FloatView.App.Focus;

public class FocusSessionService
{
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _sessionDuration;
    private TimeSpan _breakDuration;
    private TimeSpan _initialRemaining;
    private bool _isBreak = false;
    private int _completedSessions = 0;

    public event Action<TimeSpan, bool>? Tick;
    public event Action<bool>? SessionCompleted; // isBreak

    public FocusSessionService(int workMinutes = 25, int breakMinutes = 5)
    {
        _sessionDuration = TimeSpan.FromMinutes(workMinutes);
        _breakDuration = TimeSpan.FromMinutes(breakMinutes);
        _initialRemaining = _sessionDuration;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += OnTimerTick;
    }

    public bool IsRunning => _stopwatch.IsRunning;
    public bool IsBreak => _isBreak;
    public int CompletedSessions => _completedSessions;

    public TimeSpan RemainingTime
    {
        get
        {
            var remaining = _initialRemaining - _stopwatch.Elapsed;
            return remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }
    }

    public string FormattedRemainingTime => RemainingTime.ToString(@"mm\:ss");

    public void Start(int workMinutes, int breakMinutes)
    {
        _sessionDuration = TimeSpan.FromMinutes(workMinutes);
        _breakDuration = TimeSpan.FromMinutes(breakMinutes);
        _isBreak = false;
        _initialRemaining = _sessionDuration;
        _stopwatch.Restart();
        _timer.Start();
        Tick?.Invoke(RemainingTime, _isBreak);
    }

    public void Pause()
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
            _initialRemaining -= _stopwatch.Elapsed;
            _stopwatch.Reset();
            _timer.Stop();
            Tick?.Invoke(RemainingTime, _isBreak);
        }
    }

    public void Resume()
    {
        if (!_stopwatch.IsRunning && RemainingTime > TimeSpan.Zero)
        {
            _stopwatch.Restart();
            _timer.Start();
        }
    }

    public void TogglePlayPause(int workMinutes = 25, int breakMinutes = 5)
    {
        if (_stopwatch.IsRunning)
        {
            Pause();
        }
        else if (RemainingTime > TimeSpan.Zero)
        {
            Resume();
        }
        else
        {
            Start(workMinutes, breakMinutes);
        }
    }

    public void Reset()
    {
        _timer.Stop();
        _stopwatch.Reset();
        _isBreak = false;
        _initialRemaining = _sessionDuration;
        Tick?.Invoke(RemainingTime, _isBreak);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var remaining = RemainingTime;
        Tick?.Invoke(remaining, _isBreak);

        if (remaining <= TimeSpan.Zero)
        {
            _stopwatch.Reset();
            _timer.Stop();

            if (!_isBreak)
            {
                _completedSessions++;
                SessionCompleted?.Invoke(false);
                // Switch to break
                _isBreak = true;
                _initialRemaining = _breakDuration;
                _stopwatch.Restart();
                _timer.Start();
            }
            else
            {
                SessionCompleted?.Invoke(true);
                // Break ended, reset to work session
                _isBreak = false;
                _initialRemaining = _sessionDuration;
            }

            Tick?.Invoke(RemainingTime, _isBreak);
        }
    }
}
