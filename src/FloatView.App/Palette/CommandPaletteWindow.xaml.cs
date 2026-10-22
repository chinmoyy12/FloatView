using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using FloatView.App.Platform;

namespace FloatView.App.Palette;

public record PaletteCommand(
    string Title,
    string Subtitle,
    string Category,
    Action Action);

public partial class CommandPaletteWindow : Window
{
    private readonly List<PaletteCommand> _allCommands = new();
    private List<PaletteCommand> _filteredCommands = new();

    public CommandPaletteWindow(IEnumerable<PaletteCommand> commands)
    {
        InitializeComponent();
        _allCommands.AddRange(commands);
        _filteredCommands = _allCommands.ToList();
        LstCommands.ItemsSource = _filteredCommands;

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowsApi.ApplyImmersiveDarkMode(hwnd);
        };

        Loaded += (_, _) =>
        {
            TxtSearch.Focus();
            if (LstCommands.Items.Count > 0)
            {
                LstCommands.SelectedIndex = 0;
            }
        };

        Deactivated += (_, _) =>
        {
            // Close when focus lost
            Close();
        };
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = TxtSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            _filteredCommands = _allCommands.ToList();
        }
        else
        {
            _filteredCommands = _allCommands
                .Where(c => c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            c.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            c.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        LstCommands.ItemsSource = _filteredCommands;
        if (LstCommands.Items.Count > 0)
        {
            LstCommands.SelectedIndex = 0;
        }
    }

    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (LstCommands.SelectedIndex < LstCommands.Items.Count - 1)
            {
                LstCommands.SelectedIndex++;
                LstCommands.ScrollIntoView(LstCommands.SelectedItem);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            if (LstCommands.SelectedIndex > 0)
            {
                LstCommands.SelectedIndex--;
                LstCommands.ScrollIntoView(LstCommands.SelectedItem);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            ExecuteSelected();
            e.Handled = true;
        }
    }

    private void OnListKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ExecuteSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnListMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ExecuteSelected();
    }

    private void ExecuteSelected()
    {
        if (LstCommands.SelectedItem is PaletteCommand cmd)
        {
            Close();
            cmd.Action.Invoke();
        }
        else if (!string.IsNullOrWhiteSpace(TxtSearch.Text))
        {
            // If user typed custom URL or search and pressed enter, execute first matching or close
            Close();
        }
    }
}
