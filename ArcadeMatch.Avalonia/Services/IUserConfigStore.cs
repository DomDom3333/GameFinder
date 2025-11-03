using System.Collections.Generic;
using GameFinder.Objects;

using System.ComponentModel;

namespace ArcadeMatch.Avalonia.Services;

public interface IUserConfigStore : INotifyPropertyChanged
{
    string Username { get; set; }
    string SteamApiKey { get; set; }
    string? SteamId { get; set; }
    SteamProfile? UserProfile { get; set; }
    IList<string> GameList { get; set; }
    IList<string> WishlistGames { get; set; }
    IList<string> CommonGames { get; set; }
}
