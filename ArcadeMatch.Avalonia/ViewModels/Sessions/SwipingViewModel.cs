using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcadeMatch.Avalonia.Commands;
using ArcadeMatch.Avalonia.Services;
using ArcadeMatch.Avalonia.Shared;
using ArcadeMatch.Avalonia.ViewModels;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GameFinder;
using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.ViewModels.Sessions;

public class SwipingViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISessionApi _sessionApi;
    private readonly IUserConfigStore _userConfig;
    private readonly ApiSettings _settings;
    private readonly HttpClient _httpClient = new();

    private Queue<string> _gameQueue = new();
    private GameData? _currentGameData;
    private string? _currentGameId;
    private GameData? _nextGameData;
    private string? _nextGameId;
    private readonly HashSet<string> _seenGameIds = new(StringComparer.Ordinal);
    private bool _isInteractionEnabled = true;

    private string _gameName = string.Empty;
    private string _description = string.Empty;
    private Bitmap? _gameImage;
    private string _genres = string.Empty;
    private string _languages = string.Empty;
    private string _developers = string.Empty;
    private string _releaseDate = string.Empty;
    private string _price = string.Empty;
    private string _metacritic = string.Empty;
    private string _reviews = string.Empty;

    private readonly AsyncCommand _likeCommand;
    private readonly AsyncCommand _dislikeCommand;

    public SwipingViewModel(ISessionApi sessionApi, IUserConfigStore userConfig, ApiSettings settings)
    {
        _sessionApi = sessionApi;
        _userConfig = userConfig;
        _settings = settings;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        _likeCommand = new AsyncCommand(() => HandleSwipeAsync(true), () => IsInteractionEnabled);
        _dislikeCommand = new AsyncCommand(() => HandleSwipeAsync(false), () => IsInteractionEnabled);
        LeaveCommand = new AsyncCommand(LeaveSessionAsync);

        _sessionApi.GameMatched += OnGameMatched;
        _sessionApi.SessionEnded += OnSessionEnded;
        _sessionApi.ErrorOccurred += OnErrorOccurred;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MessageRequestedEventArgs>? MessageRequested;
    public event EventHandler<SessionNavigationEventArgs>? NavigationRequested;

    public ICommand LikeCommand => _likeCommand;
    public ICommand DislikeCommand => _dislikeCommand;
    public ICommand LeaveCommand { get; }

    public bool IsInteractionEnabled
    {
        get => _isInteractionEnabled;
        private set
        {
            if (_isInteractionEnabled == value)
            {
                return;
            }

            _isInteractionEnabled = value;
            OnPropertyChanged();
            _likeCommand.RaiseCanExecuteChanged();
            _dislikeCommand.RaiseCanExecuteChanged();
        }
    }

    public string GameName
    {
        get => _gameName;
        private set
        {
            if (_gameName == value)
            {
                return;
            }

            _gameName = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        private set
        {
            if (_description == value)
            {
                return;
            }

            _description = value;
            OnPropertyChanged();
        }
    }

    public Bitmap? GameImage
    {
        get => _gameImage;
        private set
        {
            if (Equals(_gameImage, value))
            {
                return;
            }

            _gameImage = value;
            OnPropertyChanged();
        }
    }

    public string Genres
    {
        get => _genres;
        private set
        {
            if (_genres == value)
            {
                return;
            }

            _genres = value;
            OnPropertyChanged();
        }
    }

    public string Languages
    {
        get => _languages;
        private set
        {
            if (_languages == value)
            {
                return;
            }

            _languages = value;
            OnPropertyChanged();
        }
    }

    public string Developers
    {
        get => _developers;
        private set
        {
            if (_developers == value)
            {
                return;
            }

            _developers = value;
            OnPropertyChanged();
        }
    }

    public string ReleaseDate
    {
        get => _releaseDate;
        private set
        {
            if (_releaseDate == value)
            {
                return;
            }

            _releaseDate = value;
            OnPropertyChanged();
        }
    }

    public string Price
    {
        get => _price;
        private set
        {
            if (_price == value)
            {
                return;
            }

            _price = value;
            OnPropertyChanged();
        }
    }

    public string Metacritic
    {
        get => _metacritic;
        private set
        {
            if (_metacritic == value)
            {
                return;
            }

            _metacritic = value;
            OnPropertyChanged();
        }
    }

    public string Reviews
    {
        get => _reviews;
        private set
        {
            if (_reviews == value)
            {
                return;
            }

            _reviews = value;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        _gameQueue = new Queue<string>(_userConfig.CommonGames ?? Array.Empty<string>());
        _seenGameIds.Clear();
        _currentGameData = null;
        _currentGameId = null;
        _nextGameData = null;
        _nextGameId = null;

        await LoadFirstGameAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        _sessionApi.GameMatched -= OnGameMatched;
        _sessionApi.SessionEnded -= OnSessionEnded;
        _sessionApi.ErrorOccurred -= OnErrorOccurred;
        _httpClient.Dispose();
    }

    private async Task LoadFirstGameAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => IsInteractionEnabled = false);
        (var gameData, var gameId) = await PreloadNextGameDetailsAsync().ConfigureAwait(false);
        _currentGameData = gameData;
        _currentGameId = gameId;

        if (_currentGameData != null && _currentGameId != null)
        {
            await DisplayGameDetailsAsync(_currentGameData).ConfigureAwait(false);
            ( _nextGameData, _nextGameId ) = await PreloadNextGameDetailsAsync().ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => IsInteractionEnabled = true);
        }
        else if (!string.IsNullOrWhiteSpace(_sessionApi.SessionId))
        {
            await _sessionApi.EndSession(_sessionApi.SessionId).ConfigureAwait(false);
        }
    }

    private async Task<(GameData? Game, string? GameId)> PreloadNextGameDetailsAsync()
    {
        while (_gameQueue.Count > 0)
        {
            var gameId = _gameQueue.Dequeue();
            if (!_seenGameIds.Add(gameId))
            {
                continue;
            }

            if (GameDataCache.TryGet(gameId, out GameData? cached) && cached != null)
            {
                return (cached, gameId);
            }

            var apiUrl = $"{_settings.Server.SteamMarketDataUrl}{gameId}";
            try
            {
                var jsonData = await _httpClient.GetStringAsync(apiUrl).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    continue;
                }

                var gameData = JsonSerializer.Deserialize<GameData>(jsonData);
                if (gameData == null || gameData.AppType != "game" || !gameData.Categories.Any(x => x.Id == 1))
                {
                    continue;
                }

                await GameDataCache.SetAsync(gameId, gameData).ConfigureAwait(false);
                return (gameData, gameId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load game details: {ex.Message}");
            }
        }

        return (null, null);
    }

    private async Task DisplayGameDetailsAsync(GameData game)
    {
        var genres = string.Join(", ", game.Genres.Select(g => g.Description));
        var languages = game.SupportedLanguages?.Replace(",", ", ") ?? string.Empty;
        var releaseDate = game.ReleaseDate?.Date ?? "Unknown";
        var developers = game.Developers != null ? string.Join(", ", game.Developers) : string.Empty;
        var price = game.PriceOverview?.FinalFormatted ?? "Free";
        var metacritic = game.Metacritic != null ? $"{game.Metacritic.Score}/100" : "N/A";
        var reviews = game.ReviewSummary != null
            ? $"{game.ReviewSummary.ReviewScoreDesc} ({game.ReviewSummary.TotalReviews} reviews)"
            : "No reviews";

        Dispatcher.UIThread.Post(() =>
        {
            GameName = game.Name;
            Description = game.ShortDescription;
            Genres = genres;
            Languages = languages;
            ReleaseDate = releaseDate;
            Developers = developers;
            Price = price;
            Metacritic = metacritic;
            Reviews = reviews;
            GameImage = null;
        });

        if (!string.IsNullOrWhiteSpace(game.HeaderImage))
        {
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(game.HeaderImage).ConfigureAwait(false);
                await using var ms = new MemoryStream(bytes);
                var bitmap = new Bitmap(ms);
                Dispatcher.UIThread.Post(() => GameImage = bitmap);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load image: {ex.Message}");
                Dispatcher.UIThread.Post(() => GameImage = null);
            }
        }
    }

    private async Task HandleSwipeAsync(bool like)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsInteractionEnabled = false);
            if (!string.IsNullOrEmpty(_currentGameId))
            {
                await _sessionApi.Swipe(_sessionApi.SessionId, _currentGameId, like).ConfigureAwait(false);
            }

            _currentGameData = _nextGameData;
            _currentGameId = _nextGameId;

            if (_currentGameData == null || _currentGameId == null)
            {
                if (!string.IsNullOrWhiteSpace(_sessionApi.SessionId))
                {
                    await _sessionApi.EndSession(_sessionApi.SessionId).ConfigureAwait(false);
                }
                return;
            }

            await DisplayGameDetailsAsync(_currentGameData).ConfigureAwait(false);
            (_nextGameData, _nextGameId) = await PreloadNextGameDetailsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in HandleSwipeAsync: {ex.Message}");
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsInteractionEnabled = true);
        }
    }

    private async Task LeaveSessionAsync()
    {
        if (!string.IsNullOrWhiteSpace(_userConfig.Username))
        {
            await _sessionApi.LeaveSessionAsync(_userConfig.Username).ConfigureAwait(false);
        }

        NavigationRequested?.Invoke(this, new SessionNavigationEventArgs(SessionViewType.Start));
    }

    private async void OnGameMatched(string gameId)
    {
        try
        {
            var totalPlayers = _sessionApi.SessionRoster.Count;
            var match = await _sessionApi.ResolveGameAsync(gameId, totalPlayers, totalPlayers).ConfigureAwait(false);
            string displayName = match?.Data.Name ?? gameId;
            string likesDisplay = match?.LikesDisplay ?? "Everyone liked this pick!";
            MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Match", $"{displayName}\n{likesDisplay}"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to resolve matched game: {ex.Message}");
        }
    }

    private void OnSessionEnded(IReadOnlyList<MatchedGame> games)
    {
        NavigationRequested?.Invoke(this, new SessionNavigationEventArgs(SessionViewType.Results, games));
    }

    private void OnErrorOccurred(string message)
    {
        MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Error", message));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
