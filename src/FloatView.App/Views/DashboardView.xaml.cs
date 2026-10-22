using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FloatView.App.Floating;
using FloatView.App.Focus;
using FloatView.App.Models;
using FloatView.App.Persistence;

namespace FloatView.App.Views;

public partial class DashboardView : UserControl
{
    private FloatingWindowManager? _windowManager;
    private FocusSessionService? _focusService;
    private SettingsService? _settingsService;

    public DashboardView()
    {
        InitializeComponent();
    }

    public void Initialize(FloatingWindowManager windowManager, FocusSessionService focusService, SettingsService settingsService)
    {
        _windowManager = windowManager;
        _focusService = focusService;
        _settingsService = settingsService;

        _windowManager.WindowsListChanged += UpdateWindowsList;
        _focusService.Tick += OnFocusTick;
        _focusService.SessionCompleted += OnSessionCompleted;

        UpdateWindowsList();
        PopulateFavorites();
    }

    private void UpdateWindowsList()
    {
        if (_windowManager == null || _settingsService == null) return;

        Dispatcher.Invoke(() =>
        {
            var windows = _windowManager.ActiveWindows.ToList();
            LstActiveWindows.ItemsSource = windows;
            TxtActiveFloatsCount.Text = $"{windows.Count} / {_settingsService.Settings.MaxActiveFloats} Active";
            TxtEmptyState.Visibility = windows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    private void PopulateFavorites()
    {
        if (_settingsService == null) return;
        GridFavorites.Children.Clear();

        var favorites = _settingsService.LoadFavorites();
        foreach (var fav in favorites)
        {
            var btn = new Button
            {
                Style = (Style)FindResource("BtnSecondary"),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(4),
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = fav.Title,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 3),
                Foreground = (System.Windows.Media.Brush)FindResource("BrushTextPrimary")
            });
            stack.Children.Add(new TextBlock
            {
                Text = fav.Url,
                FontSize = 11,
                Foreground = (System.Windows.Media.Brush)FindResource("BrushTextMuted"),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            btn.Content = stack;
            btn.Click += (_, _) =>
            {
                _windowManager?.OpenFloat(new FloatingWindowConfig
                {
                    Title = fav.Title,
                    Url = fav.Url,
                    Preset = fav.PreferredPreset,
                    Width = FloatingWindowConfig.GetPresetDimensions(fav.PreferredPreset).width,
                    Height = FloatingWindowConfig.GetPresetDimensions(fav.PreferredPreset).height
                });
            };

            GridFavorites.Children.Add(btn);
        }
    }

    private void OnNewFloatClicked(object sender, RoutedEventArgs e)
    {
        _windowManager?.OpenFloat();
    }

    private void OnToggleVisibilityClicked(object sender, RoutedEventArgs e)
    {
        _windowManager?.ToggleAllVisibility();
    }

    private void OnToggleTopmostClicked(object sender, RoutedEventArgs e)
    {
        _windowManager?.ToggleTopmostAll();
    }

    private void OnArrangeSideBySideClicked(object sender, RoutedEventArgs e)
    {
        _windowManager?.ArrangeSideBySide();
    }

    private void OnArrangeStackedClicked(object sender, RoutedEventArgs e)
    {
        _windowManager?.ArrangeStacked();
    }

    private void OnArrangeGridClicked(object sender, RoutedEventArgs e)
    {
        _windowManager?.ArrangeGrid();
    }

    private void OnFocusWindowClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FloatingWindow w)
        {
            w.Activate();
        }
    }

    private void OnCloseWindowClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FloatingWindow w)
        {
            _windowManager?.CloseWindow(w);
        }
    }

    private void OnFocusToggleClicked(object sender, RoutedEventArgs e)
    {
        if (_focusService == null || _settingsService == null) return;
        _focusService.TogglePlayPause(
            _settingsService.Settings.FocusDurationMinutes,
            _settingsService.Settings.FocusBreakDurationMinutes);
    }

    private void OnFocusResetClicked(object sender, RoutedEventArgs e)
    {
        _focusService?.Reset();
    }

    private void OnFocusTick(TimeSpan remaining, bool isBreak)
    {
        Dispatcher.Invoke(() =>
        {
            TxtFocusTimer.Text = remaining.ToString(@"mm\:ss");
            TxtFocusStatus.Text = isBreak ? "Break Interval" : "Work Interval";
            TxtFocusStatus.Foreground = isBreak
                ? (System.Windows.Media.Brush)FindResource("BrushSuccess")
                : (System.Windows.Media.Brush)FindResource("BrushAccent");
        });
    }

    private void OnSessionCompleted(bool isBreak)
    {
        Dispatcher.Invoke(() =>
        {
            if (_focusService != null)
            {
                TxtCompletedPomodoros.Text = $"Completed sessions today: {_focusService.CompletedSessions}";
            }
        });
    }
}
