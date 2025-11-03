using System.Collections.Generic;
using OpenQA.Selenium;

namespace ArcadeMatch.Avalonia.Services;

public interface ISteamGameService
{
    Task<List<string>?> GetOwnedGamesViaApiAsync(string apiKey, string steamId);

    Task<(List<string> OwnedGames, List<string> WishlistGames)?> GetOwnedAndWishlistGamesAsync();

    IReadOnlyCollection<Cookie> PromptUserToLogin();

    IReadOnlyCollection<Cookie>? LoadCookies(string? path = null);

    void SaveCookies(string path, IReadOnlyCollection<Cookie> cookies);

    bool HasSavedCookies();

    void ParseCookiesForData(IReadOnlyCollection<Cookie> cookies);
}
