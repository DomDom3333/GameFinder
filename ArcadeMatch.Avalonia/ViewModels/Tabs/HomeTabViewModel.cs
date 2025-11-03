using System.ComponentModel;
using System.Runtime.CompilerServices;
using ArcadeMatch.Avalonia.Commands;
using ArcadeMatch.Avalonia.Services;
using OpenQA.Selenium;
using System.Windows.Input;
using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class HomeTabViewModel : INotifyPropertyChanged
{
    private readonly ISteamGameService _steamGameService;
    private readonly IUserConfigStore _userConfig;

    private bool _isLoggedIn;
    private string _connectionStatus;
    private string _steamApiKey;
    private string _steamId;

    public HomeTabViewModel(ISteamGameService steamGameService, IUserConfigStore userConfig)
    {
        _steamGameService = steamGameService;
        _userConfig = userConfig;

        _steamApiKey = _userConfig.SteamApiKey;
        _steamId = _userConfig.SteamId ?? string.Empty;
        _isLoggedIn = _userConfig.GameList.Count > 0;
        _connectionStatus = _isLoggedIn ? "Connected" : "Not Connected";

        LoginCommand = new AsyncCommand(ExecuteLoginAsync);
        RefreshCommand = new AsyncCommand(ExecuteRefreshAsync);
        FetchViaApiCommand = new AsyncCommand(ExecuteFetchViaApiAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MessageRequestedEventArgs>? MessageRequested;

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set
        {
            if (_isLoggedIn == value)
            {
                return;
            }

            _isLoggedIn = value;
            ConnectionStatus = value ? "Connected" : "Not Connected";
            OnPropertyChanged();
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set
        {
            if (_connectionStatus == value)
            {
                return;
            }

            _connectionStatus = value;
            OnPropertyChanged();
        }
    }

    public string SteamApiKey
    {
        get => _steamApiKey;
        set
        {
            if (_steamApiKey == value)
            {
                return;
            }

            _steamApiKey = value;
            _userConfig.SteamApiKey = value;
            OnPropertyChanged();
        }
    }

    public string SteamId
    {
        get => _steamId;
        set
        {
            if (_steamId == value)
            {
                return;
            }

            _steamId = value;
            _userConfig.SteamId = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoginCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand FetchViaApiCommand { get; }

    public async Task InitializeAsync()
    {
        if (_steamGameService.HasSavedCookies())
        {
            var cookies = _steamGameService.LoadCookies();
            if (cookies != null)
            {
                await UpdateStatusAsync(cookies);
            }
        }

        IsLoggedIn = await TryGetGameListAsync();
    }

    public async Task<bool> TryGetGameListAsync()
    {
        try
        {
            var result = await _steamGameService.GetOwnedAndWishlistGamesAsync();
            if (result != null)
            {
                _userConfig.GameList = result.Value.OwnedGames;
                _userConfig.WishlistGames = result.Value.WishlistGames;
                return result.Value.OwnedGames.Count > 0;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task ExecuteLoginAsync()
    {
        try
        {
            IReadOnlyCollection<Cookie>? cookies = _steamGameService.PromptUserToLogin();
            if (cookies != null && cookies.Any())
            {
                await UpdateStatusAsync(cookies);
                return;
            }

            OnMessageRequested("Error", "No cookies were returned from the Steam login process.");
        }
        catch (Exception ex)
        {
            OnMessageRequested("Error", ex.Message);
        }
    }

    private async Task ExecuteRefreshAsync()
    {
        var result = await _steamGameService.GetOwnedAndWishlistGamesAsync();
        if (result == null)
        {
            IsLoggedIn = false;
            OnMessageRequested("Error", "Failed to retrieve cookies. Please log into Steam.");
            return;
        }

        _userConfig.GameList = result.Value.OwnedGames;
        _userConfig.WishlistGames = result.Value.WishlistGames;
        IsLoggedIn = result.Value.OwnedGames.Count > 0;
    }

    private async Task ExecuteFetchViaApiAsync()
    {
        string trimmedApiKey = SteamApiKey.Trim();
        string trimmedSteamId = SteamId.Trim();

        SteamApiKey = trimmedApiKey;
        SteamId = trimmedSteamId;

        if (string.IsNullOrWhiteSpace(SteamApiKey) || string.IsNullOrWhiteSpace(SteamId))
        {
            IsLoggedIn = false;
            OnMessageRequested("Error", "Please enter both API key and Steam ID.");
            return;
        }

        var games = await _steamGameService.GetOwnedGamesViaApiAsync(SteamApiKey, SteamId);
        if (games != null)
        {
            _userConfig.GameList = games;
            IsLoggedIn = games.Count > 0;
            return;
        }

        IsLoggedIn = false;
        OnMessageRequested("Error", "No games were returned from the Steam API.");
    }

    private async Task UpdateStatusAsync(IReadOnlyCollection<Cookie> cookies)
    {
        _steamGameService.ParseCookiesForData(cookies);
        string? steamId = _userConfig.SteamId;
        _userConfig.UserProfile = steamId != null
            ? await SteamProfileFetcher.GetProfileAsync(steamId)
            : null;
        _userConfig.Username = _userConfig.UserProfile?.SteamId ?? string.Empty;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnMessageRequested(string title, string message)
    {
        MessageRequested?.Invoke(this, new MessageRequestedEventArgs(title, message));
    }
}
