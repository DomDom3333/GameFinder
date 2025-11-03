using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcadeMatch.Avalonia.Commands;
using GameFinder.Objects;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace ArcadeMatch.Avalonia.ViewModels.Sessions;

public class MatchResultViewModel : INotifyPropertyChanged
{
    private static readonly HttpClient HttpClient = new();

    private readonly ObservableCollection<MatchResultItemViewModel> _matches = new();
    private readonly ReadOnlyObservableCollection<MatchResultItemViewModel> _readOnlyMatches;
    private bool _hasMatches;
    private string _resultTitle = "🎯 Top crew picks";
    private string _subtitle = "Sorted by how many friends liked each game.";

    public MatchResultViewModel()
    {
        _readOnlyMatches = new ReadOnlyObservableCollection<MatchResultItemViewModel>(_matches);

        BackCommand = new RelayCommand(_ => OnBackRequested());
        CopyMatchesCommand = new RelayCommand(_ => OnCopyMatchesRequested(), _ => HasMatches);
        OpenSteamCommand = new RelayCommand(OpenSteamPage);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<SessionNavigationEventArgs>? NavigationRequested;
    public event EventHandler<string>? CopyMatchesRequested;

    public ICommand BackCommand { get; }
    public ICommand CopyMatchesCommand { get; }
    public ICommand OpenSteamCommand { get; }

    public ReadOnlyObservableCollection<MatchResultItemViewModel> Matches => _readOnlyMatches;

    public bool HasMatches
    {
        get => _hasMatches;
        private set
        {
            if (_hasMatches == value)
            {
                return;
            }

            _hasMatches = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEmpty));
            (CopyMatchesCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool IsEmpty => !HasMatches;

    public string ResultTitle
    {
        get => _resultTitle;
        private set
        {
            if (_resultTitle == value)
            {
                return;
            }

            _resultTitle = value;
            OnPropertyChanged();
        }
    }

    public string Subtitle
    {
        get => _subtitle;
        private set
        {
            if (_subtitle == value)
            {
                return;
            }

            _subtitle = value;
            OnPropertyChanged();
        }
    }

    public async Task LoadMatchesAsync(IReadOnlyList<MatchedGame> matches)
    {
        _matches.Clear();

        if (matches == null || matches.Count == 0)
        {
            HasMatches = false;
            ResultTitle = "No liked games yet";
            Subtitle = "Nobody swiped right this round. Try another session!";
            return;
        }

        foreach (var match in matches
                     .OrderByDescending(m => m.Likes)
                     .ThenBy(m => m.Data.Name, StringComparer.OrdinalIgnoreCase))
        {
            var item = new MatchResultItemViewModel(match);
            _matches.Add(item);
            await LoadCoverAsync(item).ConfigureAwait(false);
        }

        HasMatches = _matches.Count > 0;
        ResultTitle = "🎯 Top crew picks";
        Subtitle = "Sorted by how many friends liked each game.";
    }

    private async Task LoadCoverAsync(MatchResultItemViewModel item)
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
            Dispatcher.UIThread.Post(() => item.Cover = bitmap);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load cover for {item.Game.Id}: {ex.Message}");
        }
    }

    private void OnBackRequested()
    {
        NavigationRequested?.Invoke(this, new SessionNavigationEventArgs(SessionViewType.Start));
    }

    private void OnCopyMatchesRequested()
    {
        if (_matches.Count == 0)
        {
            return;
        }

        var lines = _matches.Select(item => $"{item.Game.Data.Name} — {item.Game.LikesDisplay} — {item.Game.SteamUri}");
        CopyMatchesRequested?.Invoke(this, string.Join(Environment.NewLine, lines));
    }

    private void OpenSteamPage(object? parameter)
    {
        if (parameter is not MatchResultItemViewModel item)
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
            Debug.WriteLine($"Unable to open Steam page: {ex.Message}");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class MatchResultItemViewModel : INotifyPropertyChanged
    {
        private Bitmap? _cover;

        public MatchResultItemViewModel(MatchedGame game)
        {
            Game = game;
        }

        public MatchedGame Game { get; }

        public Bitmap? Cover
        {
            get => _cover;
            set
            {
                if (Equals(_cover, value))
                {
                    return;
                }

                _cover = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
