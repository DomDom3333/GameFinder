using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GameFinder.Objects;

namespace GameFinder.Controls;

public partial class MatchResult : UserControl
{
    public event Action? BackClicked;

    private readonly ObservableCollection<MatchedGame> _matches = new();

    public MatchResult() : this(Array.Empty<MatchedGame>())
    {
    }

    public MatchResult(IReadOnlyList<MatchedGame> matches)
    {
        InitializeComponent();
        MatchesList.ItemsSource = _matches;

        if (matches != null)
        {
            foreach (var match in matches
                         .OrderByDescending(m => m.Likes)
                         .ThenBy(m => m.Data.Name, StringComparer.OrdinalIgnoreCase))
            {
                _matches.Add(match);
            }
        }

        UpdateView();
    }

    void UpdateView()
    {
        bool hasMatches = _matches.Count > 0;
        MatchesScrollViewer.Visibility = hasMatches ? Visibility.Visible : Visibility.Collapsed;
        ShareButton.Visibility = hasMatches ? Visibility.Visible : Visibility.Collapsed;
        ShareButton.IsEnabled = hasMatches;
        NoMatchesPanel.Visibility = hasMatches ? Visibility.Collapsed : Visibility.Visible;

        if (hasMatches)
        {
            ResultText.Text = "🎯 Top crew picks";
            SubtitleText.Text = "Sorted by how many friends liked each game.";
        }
        else
        {
            ResultText.Text = "No liked games yet";
            SubtitleText.Text = "Nobody swiped right this round. Try another session!";
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        BackClicked?.Invoke();
    }

    private void CopyMatches_Click(object sender, RoutedEventArgs e)
    {
        if (_matches.Count == 0)
        {
            return;
        }

        var lines = _matches.Select(match => $"{match.Data.Name} — {match.LikesDisplay} — {match.SteamUri}");
        try
        {
            Clipboard.SetText(string.Join(Environment.NewLine, lines));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to copy match list: {ex.Message}");
        }
    }

    private void OpenSteam_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not MatchedGame match)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = match.SteamUri.ToString(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open Steam page: {ex.Message}");
        }
    }
}
