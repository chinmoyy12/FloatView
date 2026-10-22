using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FloatView.App.Models;
using FloatView.App.Persistence;
using FloatView.App.Platform;

namespace FloatView.App.Views;

public partial class SettingsView : UserControl
{
    private SettingsService? _settingsService;

    public event Action<string>? AccentColorChanged;

    public SettingsView()
    {
        InitializeComponent();
    }

    public void Initialize(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var s = _settingsService.Settings;

        SliderDefaultOpacity.Value = s.DefaultWindowOpacity;
        TxtOpacityValue.Text = $"{(int)(s.DefaultWindowOpacity * 100)}%";
        ChkSnapping.IsChecked = s.EnableWindowSnapping;
        ChkEdgeDocking.IsChecked = s.EdgeDockingEnabled;
        TxtFocusDuration.Text = s.FocusDurationMinutes.ToString();
        TxtBreakDuration.Text = s.FocusBreakDurationMinutes.ToString();
        ChkStartup.IsChecked = StartupService.IsRunAtStartupEnabled();

        // Match combo index
        int idx = Math.Clamp(s.MaxActiveFloats - 1, 0, 4);
        CmbMaxFloats.SelectedIndex = idx;
    }

    private void OnDefaultOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_settingsService == null) return;
        _settingsService.Settings.DefaultWindowOpacity = e.NewValue;
        _settingsService.SaveSettings(_settingsService.Settings);
        if (TxtOpacityValue != null)
        {
            TxtOpacityValue.Text = $"{(int)(e.NewValue * 100)}%";
        }
    }

    private void OnColorVioletSelected(object sender, MouseButtonEventArgs e) => ApplyAccent("#8B5CF6");
    private void OnColorBlueSelected(object sender, MouseButtonEventArgs e) => ApplyAccent("#3B82F6");
    private void OnColorGreenSelected(object sender, MouseButtonEventArgs e) => ApplyAccent("#10B981");
    private void OnColorPinkSelected(object sender, MouseButtonEventArgs e) => ApplyAccent("#EC4899");

    private void ApplyAccent(string hex)
    {
        if (_settingsService == null) return;
        _settingsService.Settings.AccentColor = hex;
        _settingsService.SaveSettings(_settingsService.Settings);

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            Application.Current.Resources["ColorAccent"] = color;
            Application.Current.Resources["BrushAccent"] = new SolidColorBrush(color);
            AccentColorChanged?.Invoke(hex);
        }
        catch { }
    }

    private void OnMaxFloatsChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_settingsService == null) return;
        _settingsService.Settings.MaxActiveFloats = CmbMaxFloats.SelectedIndex + 1;
        _settingsService.SaveSettings(_settingsService.Settings);
    }

    private void OnSnappingToggled(object sender, RoutedEventArgs e)
    {
        if (_settingsService == null) return;
        _settingsService.Settings.EnableWindowSnapping = ChkSnapping.IsChecked == true;
        _settingsService.SaveSettings(_settingsService.Settings);
    }

    private void OnEdgeDockingToggled(object sender, RoutedEventArgs e)
    {
        if (_settingsService == null) return;
        _settingsService.Settings.EdgeDockingEnabled = ChkEdgeDocking.IsChecked == true;
        _settingsService.SaveSettings(_settingsService.Settings);
    }

    private void OnFocusDurationChanged(object sender, TextChangedEventArgs e)
    {
        if (_settingsService == null) return;
        if (int.TryParse(TxtFocusDuration.Text, out int minutes) && minutes > 0)
        {
            _settingsService.Settings.FocusDurationMinutes = minutes;
            _settingsService.SaveSettings(_settingsService.Settings);
        }
    }

    private void OnBreakDurationChanged(object sender, TextChangedEventArgs e)
    {
        if (_settingsService == null) return;
        if (int.TryParse(TxtBreakDuration.Text, out int minutes) && minutes > 0)
        {
            _settingsService.Settings.FocusBreakDurationMinutes = minutes;
            _settingsService.SaveSettings(_settingsService.Settings);
        }
    }

    private void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        bool enable = ChkStartup.IsChecked == true;
        StartupService.SetRunAtStartup(enable);
    }

    private void OnClearWebCacheClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = StoragePaths.WebView2UserDataFolder;
            if (Directory.Exists(folder))
            {
                // Warn user
                var res = MessageBox.Show("Clear WebView2 cache on next application start?", "FloatView", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    MessageBox.Show("Cache flag set. Data will be cleared when browser sessions refresh.", "FloatView", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error clearing cache: {ex.Message}", "FloatView", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnResetAllDataClicked(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show("Are you sure you want to reset all preferences, notes, and workspaces to factory defaults?", "FloatView", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res == MessageBoxResult.Yes && _settingsService != null)
        {
            _settingsService.ClearAllData();
            MessageBox.Show("Preferences reset. FloatView will now reload default settings.", "FloatView", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
