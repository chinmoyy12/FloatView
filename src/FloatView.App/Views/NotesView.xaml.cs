using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FloatView.App.Models;
using FloatView.App.Persistence;
using Microsoft.Win32;

namespace FloatView.App.Views;

public partial class NotesView : UserControl
{
    private SettingsService? _settingsService;
    private List<CaptionNote> _notes = new();

    public NotesView()
    {
        InitializeComponent();
    }

    public void Initialize(SettingsService settingsService)
    {
        _settingsService = settingsService;
        RefreshNotes();
    }

    public void AddNote(CaptionNote note)
    {
        _notes.Insert(0, note);
        _settingsService?.SaveNotes(_notes);
        ApplyFilter();
    }

    private void RefreshNotes()
    {
        if (_settingsService == null) return;
        _notes = _settingsService.LoadNotes();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = TxtSearchNotes.Text.Trim();
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _notes
            : _notes.Where(n => n.PageTitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                n.NoteText.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                n.ExtractedCaption.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        LstNotes.ItemsSource = filtered;
        TxtNoNotes.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnSearchNotesChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void OnCopyNoteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CaptionNote note)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{note.FormattedTimestamp}] {note.PageTitle}");
            if (!string.IsNullOrWhiteSpace(note.ExtractedCaption))
                sb.AppendLine($"Caption: \"{note.ExtractedCaption}\"");
            sb.AppendLine(note.NoteText);
            sb.AppendLine($"Source: {note.Url}");
            Clipboard.SetText(sb.ToString());
            MessageBox.Show("Note copied to clipboard!", "FloatView", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnDeleteNoteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CaptionNote note && _settingsService != null)
        {
            _notes.Remove(note);
            _settingsService.SaveNotes(_notes);
            ApplyFilter();
        }
    }

    private void OnClearAllClicked(object sender, RoutedEventArgs e)
    {
        if (_notes.Count == 0) return;
        var result = MessageBox.Show("Clear all saved notes?", "FloatView", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes && _settingsService != null)
        {
            _notes.Clear();
            _settingsService.SaveNotes(_notes);
            ApplyFilter();
        }
    }

    private void OnExportMarkdownClicked(object sender, RoutedEventArgs e)
    {
        if (_notes.Count == 0)
        {
            MessageBox.Show("No notes to export.", "FloatView", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt",
            FileName = $"FloatView_Notes_{DateTime.Now:yyyyMMdd}.md"
        };

        if (sfd.ShowDialog() == true)
        {
            var markdown = CaptionNote.ExportToMarkdown(_notes);
            File.WriteAllText(sfd.FileName, markdown, Encoding.UTF8);
            MessageBox.Show($"Exported {_notes.Count} notes to {sfd.FileName}", "FloatView", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
