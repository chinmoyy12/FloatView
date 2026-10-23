using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FloatView.App.Models;

namespace FloatView.App.Views;

public partial class MediaSourcePickerWindow : Window
{
    private readonly Func<MediaCandidate, Task> _onCandidateSelected;
    private bool _hasSelected = false;

    public MediaSourcePickerWindow(
        IReadOnlyList<MediaCandidate> candidates,
        Func<MediaCandidate, Task> onCandidateSelected)
    {
        InitializeComponent();
        _onCandidateSelected = onCandidateSelected;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        PopulateCandidates(candidates);
    }

    private void PopulateCandidates(IReadOnlyList<MediaCandidate> candidates)
    {
        PanelCandidates.Children.Clear();

        if (candidates == null || candidates.Count == 0)
        {
            BorderEmptyState.Visibility = Visibility.Visible;
            return;
        }

        BorderEmptyState.Visibility = Visibility.Collapsed;

        var buttonStyle = (Style)FindResource("CandidateCardButton");
        var accentBrush = (Brush)FindResource("BrushAccent");
        var textPrimaryBrush = (Brush)FindResource("BrushTextPrimary");
        var textSecondaryBrush = (Brush)FindResource("BrushTextSecondary");
        var iconPlay = (Geometry)FindResource("IconPlay");

        foreach (var candidate in candidates)
        {
            var btn = new Button
            {
                Style = buttonStyle,
                Tag = candidate
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon Container
            var iconBorder = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(6),
                Background = (Brush)FindResource("BrushAccentSubtle"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            var path = new Path
            {
                Data = iconPlay,
                Fill = accentBrush,
                Width = 10,
                Height = 10,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = path;
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // Title and Details Stack
            var textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };

            var txtTitle = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(candidate.Title) ? "Media Source" : candidate.Title,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = textPrimaryBrush,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var txtSub = new TextBlock
            {
                Text = candidate.DisplaySubtitle,
                FontSize = 11,
                Foreground = textSecondaryBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };

            textStack.Children.Add(txtTitle);
            textStack.Children.Add(txtSub);
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            // Float Now Pill
            var pillBorder = new Border
            {
                Background = accentBrush,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            var txtPill = new TextBlock
            {
                Text = "Float",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            pillBorder.Child = txtPill;
            Grid.SetColumn(pillBorder, 2);
            grid.Children.Add(pillBorder);

            btn.Content = grid;
            btn.Click += async (s, e) =>
            {
                if (_hasSelected) return;
                _hasSelected = true;
                Close();
                await _onCandidateSelected(candidate);
            };

            PanelCandidates.Children.Add(btn);
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
