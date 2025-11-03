using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ArcadeMatch.Avalonia.Services;
using OpenQA.Selenium;

namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class TabsViewModel : INotifyPropertyChanged
{
    private readonly ISteamGameService _steamGameService;
    private readonly IUserConfigStore _userConfig;

    private bool _isLoggedIn;
    private string _steamStatusText = "Not Connected";
    private string _steamApiKey;
    private string _steamId;

    public TabsViewModel(
        ISteamGameService steamGameService,
        IUserConfigStore userConfig)
    {
        _steamGameService = steamGameService;
        _userConfig = userConfig;

        _steamApiKey = _userConfig.SteamApiKey;
        _steamId = _userConfig.SteamId ?? string.Empty;
        _isLoggedIn = _userConfig.GameList.Count > 0;
        _steamStatusText = _isLoggedIn ? "Connected" : "Not Connected";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
            SteamStatusText = value ? "Connected" : "Not Connected";
            OnPropertyChanged();
        }
    }

    public string SteamStatusText
    {
        get => _steamStatusText;
        private set
        {
            if (_steamStatusText == value)
            {
                return;
            }

            _steamStatusText = value;
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

    public async Task InitializeAsync()
    {
        if (_steamGameService.HasSavedCookies())
        {
            var cookies = _steamGameService.LoadCookies();
            if (cookies != null)
            {
                await UpdateStatusAsync(cookies).ConfigureAwait(false);
            }
        }

        IsLoggedIn = await TryGetGameListAsync().ConfigureAwait(false);
    }

    public async Task<LoginResult> LoginAsync()
    {
        try
        {
            IReadOnlyCollection<Cookie>? cookies = _steamGameService.PromptUserToLogin();
            if (cookies != null && cookies.Any())
            {
                await UpdateStatusAsync(cookies).ConfigureAwait(false);
                return LoginResult.CreateSuccess();
            }

            return LoginResult.CreateFailure("No cookies were returned from the Steam login process.");
        }
        catch (Exception ex)
        {
            return LoginResult.CreateFailure(ex.Message);
        }
    }

    public async Task<GameListResult> RefreshGamesAsync()
    {
        var result = await _steamGameService.GetOwnedAndWishlistGamesAsync().ConfigureAwait(false);
        if (result == null)
        {
            IsLoggedIn = false;
            return GameListResult.CreateFailure("Failed to retrieve cookies. Please log into Steam.");
        }

        _userConfig.GameList = result.Value.OwnedGames;
        _userConfig.WishlistGames = result.Value.WishlistGames;
        IsLoggedIn = result.Value.OwnedGames.Count > 0;
        return GameListResult.CreateSuccess();
    }

    public async Task<GameListResult> FetchGamesViaApiAsync(string apiKey, string steamId)
    {
        SteamApiKey = apiKey.Trim();
        SteamId = steamId.Trim();

        if (string.IsNullOrWhiteSpace(SteamApiKey) || string.IsNullOrWhiteSpace(SteamId))
        {
            IsLoggedIn = false;
            return GameListResult.CreateFailure("Please enter both API key and Steam ID.");
        }

        var games = await _steamGameService.GetOwnedGamesViaApiAsync(SteamApiKey, SteamId).ConfigureAwait(false);
        if (games != null)
        {
            _userConfig.GameList = games;
            IsLoggedIn = games.Count > 0;
            return GameListResult.CreateSuccess();
        }

        IsLoggedIn = false;
        return GameListResult.CreateFailure("No games were returned from the Steam API.");
    }

    public async Task<bool> TryGetGameListAsync()
    {
        try
        {
            var result = await _steamGameService.GetOwnedAndWishlistGamesAsync().ConfigureAwait(false);
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

    public void UpdateSteamStatusFromSession(bool isConnected)
    {
        IsLoggedIn = isConnected;
    }

    private async Task UpdateStatusAsync(IReadOnlyCollection<Cookie> cookies)
    {
        _steamGameService.ParseCookiesForData(cookies);
        var steamId = _userConfig.SteamId;
        _userConfig.UserProfile = steamId != null
            ? await SteamProfileFetcher.GetProfileAsync(steamId).ConfigureAwait(false)
            : null;
        _userConfig.Username = _userConfig.UserProfile?.SteamId ?? string.Empty;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public record LoginResult(bool Success, string? ErrorMessage)
    {
        public static LoginResult CreateSuccess() => new(true, null);
        public static LoginResult CreateFailure(string message) => new(false, message);
    }

    public record GameListResult(bool Success, string? ErrorMessage)
    {
        public static GameListResult CreateSuccess() => new(true, null);
        public static GameListResult CreateFailure(string message) => new(false, message);
    }
}
