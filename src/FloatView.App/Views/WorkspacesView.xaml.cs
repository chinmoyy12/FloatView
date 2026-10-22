using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FloatView.App.Floating;
using FloatView.App.Models;
using FloatView.App.Persistence;

namespace FloatView.App.Views;

public partial class WorkspacesView : UserControl
{
    private FloatingWindowManager? _windowManager;
    private SettingsService? _settingsService;
    private List<WorkspaceModel> _workspaces = new();

    public WorkspacesView()
    {
        InitializeComponent();
    }

    public void Initialize(FloatingWindowManager windowManager, SettingsService settingsService)
    {
        _windowManager = windowManager;
        _settingsService = settingsService;
        RefreshList();
    }

    private void RefreshList()
    {
        if (_settingsService == null) return;
        _workspaces = _settingsService.LoadWorkspaces();
        LstWorkspaces.ItemsSource = _workspaces;
    }

    private void OnLaunchWorkspaceClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WorkspaceModel ws)
        {
            _windowManager?.ApplyWorkspace(ws);
        }
    }

    private void OnDeleteWorkspaceClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WorkspaceModel ws && _settingsService != null)
        {
            _workspaces.Remove(ws);
            _settingsService.SaveWorkspaces(_workspaces);
            RefreshList();
        }
    }

    private void OnSaveCurrentLayoutClicked(object sender, RoutedEventArgs e)
    {
        if (_windowManager == null || _settingsService == null) return;

        var activeWindows = _windowManager.ActiveWindows;
        if (activeWindows.Count == 0)
        {
            MessageBox.Show("Open at least one floating window before saving a workspace layout.", "FloatView", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var name = string.IsNullOrWhiteSpace(TxtNewWorkspaceName.Text) ? "Custom Workspace" : TxtNewWorkspaceName.Text.Trim();
        var configs = activeWindows.Select(w => new FloatingWindowConfig
        {
            Title = w.Config.Title,
            Url = w.Config.Url,
            Left = w.Left,
            Top = w.Top,
            Width = w.Width,
            Height = w.Height,
            Preset = w.Config.Preset,
            Opacity = w.Opacity,
            IsTopmost = w.Topmost
        }).ToList();

        var newWs = new WorkspaceModel
        {
            Name = name,
            Description = $"Saved desktop layout with {configs.Count} windows",
            Windows = configs,
            LastUsedAt = DateTime.UtcNow
        };

        _workspaces.Insert(0, newWs);
        _settingsService.SaveWorkspaces(_workspaces);
        RefreshList();
        TxtNewWorkspaceName.Text = "My Workspace";
    }
}
