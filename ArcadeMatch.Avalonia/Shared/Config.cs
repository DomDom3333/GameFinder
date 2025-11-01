using System.Collections.Generic;

namespace ArcadeMatch.Avalonia;

public static class Config
{
    public static string Username { get; set; } = string.Empty;
    public static string SteamApiKey { get; set; } = string.Empty;
    public static string SteamId { get; set; } = string.Empty;
    public static List<string> GameList { get; set; } = new();
    public static List<string> WishlistGames { get; set; } = new();
    public static List<string> CommonGames { get; set; } = new();
}
