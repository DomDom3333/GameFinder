using System.Threading.Tasks;
using ArcadeMatch.Avalonia.Services;

namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class TabsViewModel
{
    public TabsViewModel(ISteamGameService steamGameService, IUserConfigStore userConfig)
    {
        Home = new HomeTabViewModel(steamGameService, userConfig);
    }

    public HomeTabViewModel Home { get; }

    public Task InitializeAsync()
    {
        return Home.InitializeAsync();
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
                await UpdateStatusAsync(cookies);
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
                await UpdateStatusAsync(cookies);
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
        Home.UpdateSteamStatusFromSession(isConnected);
    }
}
