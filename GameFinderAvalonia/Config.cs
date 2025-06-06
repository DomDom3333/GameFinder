using System.Collections.Generic;

namespace GameFinderAvalonia;

public static class Config
{
    public static string Username { get; set; } = string.Empty;
    public static List<string> GameList = new List<string>();
    public static List<string> CommonGames = new List<string>();
}
