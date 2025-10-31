using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.Controls;

public partial class MatchResult : UserControl
{
    public event Action? BackClicked;

    private static readonly HttpClient HttpClient = new();
    private readonly ObservableCollection<MatchResultItem> _items = new();

    public MatchResult() : this(Array.Empty<MatchedGame>())
    {
    }

    public MatchResult(IReadOnlyList<MatchedGame> matches)
    {
        InitializeComponent();
        MatchesList.ItemsSource = _items;
        _ = LoadMatchesAsync(matches ?? Array.Empty<MatchedGame>());
    }

    async Task LoadMatchesAsync(IReadOnlyList<MatchedGame> matches)
    {
        if (matches.Count == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(UpdateView);
            return;
        }

        foreach (var match in matches
                     .OrderByDescending(m => m.Likes)
                     .ThenBy(m => m.Data.Name, StringComparer.OrdinalIgnoreCase))
        {
            var item = new MatchResultItem(match);
            await Dispatcher.UIThread.InvokeAsync(() => _items.Add(item));
            await LoadCoverAsync(item);
        }

        await Dispatcher.UIThread.InvokeAsync(UpdateView);
    }

    async Task LoadCoverAsync(MatchResultItem item)
    {
        try
        {
            var imageUrl = item.Game.Data.HeaderImage;
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            var bytes = await HttpClient.GetByteArrayAsync(imageUrl).ConfigureAwait(false);
            await using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            await Dispatcher.UIThread.InvokeAsync(() => item.Cover = bitmap);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load cover for {item.Game.Id}: {ex.Message}");
        }
    }

    void UpdateView()
    {
        bool hasMatches = _items.Count > 0;
        MatchesScrollViewer.IsVisible = hasMatches;
        ShareButton.IsVisible = hasMatches;
        ShareButton.IsEnabled = hasMatches;
        NoMatchesPanel.IsVisible = !hasMatches;

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

    void Back_Click(object? sender, RoutedEventArgs e)
    {
        BackClicked?.Invoke();
    }

    async void CopyMatches_Click(object? sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard == null)
        {
            return;
        }

        var lines = _items.Select(item => $"{item.Game.Data.Name} — {item.Game.LikesDisplay} — {item.Game.SteamUri}");
        try
        {
            await topLevel.Clipboard.SetTextAsync(string.Join(Environment.NewLine, lines));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to copy match list: {ex.Message}");
        }
    }

    void OpenSteam_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not MatchResultItem item)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.Game.SteamUri.ToString(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to open Steam page: {ex.Message}");
        }
    }

    private sealed class MatchResultItem : INotifyPropertyChanged
    {
        public MatchResultItem(MatchedGame game)
        {
            Game = game;
        }

        public MatchedGame Game { get; }

        private Bitmap? _cover;
        public Bitmap? Cover
        {
            get => _cover;
            set
            {
                if (_cover != value)
                {
                    _cover = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Cover)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
