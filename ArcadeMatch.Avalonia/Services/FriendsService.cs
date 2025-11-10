using System.Linq;
using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.Services;

public sealed class FriendsService
{
    private readonly CookieSteamFriendsProvider _cookieProvider;
    private readonly ApiSteamFriendsProvider _apiProvider;
    private readonly ISessionApi _sessionApi;
    private readonly IUserConfigStore _configStore;

    public FriendsService(
        CookieSteamFriendsProvider cookieProvider,
        ApiSteamFriendsProvider apiProvider,
        ISessionApi sessionApi,
        IUserConfigStore configStore)
    {
        _cookieProvider = cookieProvider;
        _apiProvider = apiProvider;
        _sessionApi = sessionApi;
        _configStore = configStore;
    }

    public async Task<IReadOnlyList<SteamFriend>> GetFriendsAsync(CancellationToken cancellationToken = default)
    {
        string? steamId = _configStore.SteamId;
        if (string.IsNullOrWhiteSpace(steamId))
        {
            throw new InvalidOperationException("Steam ID is required. Provide it on the Home tab and try again.");
        }

        IReadOnlyList<SteamFriend> friends = await FetchFriendsAsync(steamId, cancellationToken).ConfigureAwait(false);
        if (friends.Count == 0)
        {
            return friends;
        }

        var steamIds = friends.Select(friend => friend.SteamId).ToList();
        IDictionary<string, string> sessions = await _sessionApi.GetFriendsSessionsAsync(steamIds).ConfigureAwait(false);

        return friends
            .Select(friend => sessions.TryGetValue(friend.SteamId, out string? sessionCode)
                ? friend with { InSession = true, SessionCode = sessionCode }
                : friend with { InSession = false, SessionCode = null })
            .OrderByDescending(friend => friend.InSession)
            .ThenByDescending(friend => friend.IsOnline)
            .ThenBy(friend => friend.PersonaName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<SteamFriend>> FetchFriendsAsync(string steamId, CancellationToken cancellationToken)
    {
        string apiKey = _configStore.SteamApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return await _apiProvider.GetFriendsAsync(steamId, apiKey, cancellationToken).ConfigureAwait(false);
        }

        return await _cookieProvider.GetFriendsAsync(steamId, null, cancellationToken).ConfigureAwait(false);
    }
}
