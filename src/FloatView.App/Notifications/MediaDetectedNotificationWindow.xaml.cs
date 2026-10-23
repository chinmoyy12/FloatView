using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FloatView.App.Models;

namespace FloatView.App.Notifications;

public partial class MediaDetectedNotificationWindow : Window
{
    private readonly MediaCandidate _candidate;
    private readonly Action<MediaCandidate> _onUseFloatView;
    private readonly Action<MediaCandidate> _onDismiss;
    private readonly DispatcherTimer _autoCloseTimer;
    private bool _actionHandled = false;

    public MediaCandidate Candidate => _candidate;

    public MediaDetectedNotificationWindow(
        MediaCandidate candidate,
        Action<MediaCandidate> onUseFloatView,
        Action<MediaCandidate> onDismiss)
    {
        InitializeComponent();
        _candidate = candidate;
        _onUseFloatView = onUseFloatView;
        _onDismiss = onDismiss;

        TxtTitle.Text = string.IsNullOrWhiteSpace(candidate.Title) ? "Media detected" : candidate.Title;
        TxtSubtitle.Text = candidate.DisplaySubtitle;

        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(8)
        };
        _autoCloseTimer.Tick += OnAutoCloseTick;

        Loaded += OnLoaded;
        MouseEnter += (_, _) => _autoCloseTimer.Stop();
        MouseLeave += (_, _) => _autoCloseTimer.Start();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Position at bottom-right of primary work area
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 16;
        Top = workArea.Bottom - ActualHeight - 16;

        _autoCloseTimer.Start();
    }

    private void OnAutoCloseTick(object? sender, EventArgs e)
    {
        _autoCloseTimer.Stop();
        Dismiss();
    }

    private void OnUseFloatViewClicked(object sender, RoutedEventArgs e)
    {
        if (_actionHandled) return;
        _actionHandled = true;
        _autoCloseTimer.Stop();
        Close();
        _onUseFloatView.Invoke(_candidate);
    }

    private void OnDismissClicked(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    private void Dismiss()
    {
        if (_actionHandled) return;
        _actionHandled = true;
        _autoCloseTimer.Stop();
        Close();
        _onDismiss.Invoke(_candidate);
    }
}
