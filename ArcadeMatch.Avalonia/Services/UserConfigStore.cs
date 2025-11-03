using System.ComponentModel;
using System.Runtime.CompilerServices;
using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.Services;

public class UserConfigStore : IUserConfigStore
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Username
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    public string SteamApiKey { get; set; } = string.Empty;
    public string? SteamId { get; set; }
    public SteamProfile? UserProfile { get; set; }

    public IList<string> GameList { get; set; } = new List<string>();

    public IList<string> WishlistGames { get; set; } = new List<string>();

    public IList<string> CommonGames { get; set; } = new List<string>();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
