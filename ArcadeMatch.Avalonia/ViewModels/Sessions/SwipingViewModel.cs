using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using ArcadeMatch.Avalonia.Commands;
using ArcadeMatch.Avalonia.Services;
using ArcadeMatch.Avalonia.Shared;
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

    private readonly AsyncCommand _likeCommand;
    private readonly AsyncCommand _dislikeCommand;

    // Disposal state flag
    private bool _disposed;

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
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            _likeCommand.RaiseCanExecuteChanged();
            _dislikeCommand.RaiseCanExecuteChanged();
        }
    } = true;

    public string GameName
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string Description
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public Bitmap? GameImage
    {
        get;
        private set
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    public string Genres
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string Languages
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string Developers
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string ReleaseDate
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string Price
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string Metacritic
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string Reviews
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

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

    #region IDisposable
    ~SwipingViewModel()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Unsubscribe from events and dispose managed resources
            _sessionApi.GameMatched -= OnGameMatched;
            _sessionApi.SessionEnded -= OnSessionEnded;
            _sessionApi.ErrorOccurred -= OnErrorOccurred;
            _httpClient.Dispose();
        }

        _disposed = true;
    }
    #endregion

    private async Task LoadFirstGameAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => IsInteractionEnabled = false);
        (var gameData, string? gameId) = await PreloadNextGameDetailsAsync().ConfigureAwait(false);
        _currentGameData = gameData;
        _currentGameId = gameId;

        if (_currentGameData != null && _currentGameId != null)
        {
            await DisplayGameDetailsAsync(_currentGameData).ConfigureAwait(false);
            (_nextGameData, _nextGameId) = await PreloadNextGameDetailsAsync().ConfigureAwait(false);
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
            string gameId = _gameQueue.Dequeue();
            if (!_seenGameIds.Add(gameId))
            {
                continue;
            }

            if (GameDataCache.TryGet(gameId, out GameData? cached) && cached != null)
            {
                return (cached, gameId);
            }

            string apiUrl = $"{_settings.Server.SteamMarketDataUrl}{gameId}";
            try
            {
                string jsonData = await _httpClient.GetStringAsync(apiUrl).ConfigureAwait(false);
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
        string genres = string.Join(", ", game.Genres.Select(g => g.Description));
        string languages = game.SupportedLanguages?.Replace(",", ", ") ?? string.Empty;
        string releaseDate = game.ReleaseDate?.Date ?? "Unknown";
        string developers = game.Developers != null ? string.Join(", ", game.Developers) : string.Empty;
        string price = game.PriceOverview?.FinalFormatted ?? "Free";
        string metacritic = game.Metacritic != null ? $"{game.Metacritic.Score}/100" : "N/A";
        string reviews = game.ReviewSummary != null
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
                byte[] bytes = await _httpClient.GetByteArrayAsync(game.HeaderImage).ConfigureAwait(false);
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
            int totalPlayers = _sessionApi.SessionRoster.Count;
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
