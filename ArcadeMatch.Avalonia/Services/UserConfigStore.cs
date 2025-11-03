using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.Services;

public class UserConfigStore : IUserConfigStore
{
    private IList<string> _gameList = new List<string>();
    private IList<string> _wishlistGames = new List<string>();
    private IList<string> _commonGames = new List<string>();
    private string _username = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Username
    {
        get => _username;
        set
        {
            if (_username == value)
            {
                return;
            }

            _username = value;
            OnPropertyChanged();
        }
    }

    public string SteamApiKey { get; set; } = string.Empty;
    public string? SteamId { get; set; }
    public SteamProfile? UserProfile { get; set; }

    public IList<string> GameList
    {
        get => _gameList;
        set => _gameList = value ?? new List<string>();
    }

    public IList<string> WishlistGames
    {
        get => _wishlistGames;
        set => _wishlistGames = value ?? new List<string>();
    }

    public IList<string> CommonGames
    {
        get => _commonGames;
        set => _commonGames = value ?? new List<string>();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
