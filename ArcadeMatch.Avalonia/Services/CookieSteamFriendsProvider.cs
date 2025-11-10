using System.Collections.Concurrent;
using System.Net;
using System.Xml.Linq;
using GameFinder.Objects;
using SeleniumCookie = OpenQA.Selenium.Cookie;

namespace ArcadeMatch.Avalonia.Services;

public sealed class CookieSteamFriendsProvider : ISteamFriendsProvider
{
    private static readonly Uri SteamCommunityBaseUri = new("https://steamcommunity.com/");
    private static readonly SemaphoreSlim Throttle = new(10);

    private readonly ISteamGameService _steamGameService;
    private readonly SteamFriendCache _cache;

    public CookieSteamFriendsProvider(ISteamGameService steamGameService, SteamFriendCache cache)
    {
        _steamGameService = steamGameService;
        _cache = cache;
    }

    public async Task<IReadOnlyList<SteamFriend>> GetFriendsAsync(
        string steamId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(steamId))
        {
            throw new InvalidOperationException("Steam ID is required to load friends.");
        }

        IReadOnlyCollection<SeleniumCookie>? cookies = _steamGameService.LoadCookies();
        if (cookies == null || cookies.Count == 0)
        {
            throw new InvalidOperationException("Steam login required to retrieve friends list.");
        }

        using HttpClient client = CreateHttpClient(cookies);
        XDocument profileDocument = await LoadProfileAsync(client, steamId, cancellationToken).ConfigureAwait(false);
        var friendIds = profileDocument
            .Descendants("friend")
            .Select(node => node.Value)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (friendIds.Count == 0)
        {
            return Array.Empty<SteamFriend>();
        }

        var friends = new ConcurrentBag<SteamFriend>();
        var tasks = friendIds.Select(id => FetchFriendAsync(client, id, friends, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);

        return friends
            .OrderByDescending(friend => friend.IsOnline)
            .ThenBy(friend => friend.PersonaName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HttpClient CreateHttpClient(IReadOnlyCollection<SeleniumCookie> cookies)
    {
        HttpClientHandler handler = new()
        {
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        foreach (SeleniumCookie cookie in cookies)
        {
            System.Net.Cookie netCookie = new(cookie.Name, cookie.Value, cookie.Path, cookie.Domain)
            {
                Secure = cookie.Secure,
                HttpOnly = cookie.IsHttpOnly
            };
            if (cookie.Expiry is DateTime expiry)
            {
                netCookie.Expires = expiry;
            }
            handler.CookieContainer.Add(SteamCommunityBaseUri, netCookie);
        }

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    private static async Task<XDocument> LoadProfileAsync(HttpClient client, string steamId, CancellationToken cancellationToken)
    {
        string url = $"https://steamcommunity.com/profiles/{steamId}/?xml=1";
        await using Stream stream = await client.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        return await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
    }

    private async Task FetchFriendAsync(HttpClient client, string friendId, ConcurrentBag<SteamFriend> friends, CancellationToken cancellationToken)
    {
        if (_cache.TryGet(friendId, out CachedSteamFriend cached))
        {
            friends.Add(new SteamFriend(friendId, cached.PersonaName, cached.AvatarUrl, cached.IsOnline ?? false));
            return;
        }

        await Throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            XDocument friendProfile = await LoadProfileAsync(client, friendId, cancellationToken).ConfigureAwait(false);
            XElement? profile = friendProfile.Element("profile");
            if (profile == null)
            {
                return;
            }

            string personaName = profile.Element("steamID")?.Value ?? friendId;
            string? avatar = profile.Element("avatarFull")?.Value;
            string? state = profile.Element("onlineState")?.Value;
            bool isOnline = !string.Equals(state, "offline", StringComparison.OrdinalIgnoreCase);

            CachedSteamFriend cacheEntry = new(personaName, avatar, isOnline);
            _cache.Set(friendId, cacheEntry);

            friends.Add(new SteamFriend(friendId, personaName, avatar, isOnline));
        }
        catch
        {
            friends.Add(new SteamFriend(friendId, friendId, null, false));
        }
        finally
        {
            Throttle.Release();
        }
    }
}
