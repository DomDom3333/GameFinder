using System.Text.Json;
using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.Services;

public sealed class ApiSteamFriendsProvider : ISteamFriendsProvider
{
    private const int BatchSize = 100;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private readonly SteamFriendCache _cache;

    public ApiSteamFriendsProvider(SteamFriendCache cache)
    {
        _cache = cache;
    }

    public async Task<IReadOnlyList<SteamFriend>> GetFriendsAsync(
        string steamId,
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Steam API key is required to load friends via the Web API.");
        }

        if (string.IsNullOrWhiteSpace(steamId))
        {
            throw new InvalidOperationException("Steam ID is required to load friends via the Web API.");
        }

        string validApiKey = apiKey!;

        IReadOnlyList<string> friendIds = await GetFriendIdsAsync(validApiKey, steamId, cancellationToken).ConfigureAwait(false);
        if (friendIds.Count == 0)
        {
            return Array.Empty<SteamFriend>();
        }

        var friends = new List<SteamFriend>(friendIds.Count);
        for (int i = 0; i < friendIds.Count; i += BatchSize)
        {
            var batch = friendIds.Skip(i).Take(BatchSize).ToList();
            var summaries = await GetPlayerSummariesAsync(validApiKey, batch, cancellationToken).ConfigureAwait(false);
            friends.AddRange(summaries);
        }

        return friends
            .OrderByDescending(friend => friend.IsOnline)
            .ThenBy(friend => friend.PersonaName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<IReadOnlyList<string>> GetFriendIdsAsync(string apiKey, string steamId, CancellationToken cancellationToken)
    {
        string url = $"https://api.steampowered.com/ISteamUser/GetFriendList/v0001/?key={apiKey}&steamid={steamId}&relationship=friend";
        using HttpResponseMessage response = await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument json = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!json.RootElement.TryGetProperty("friendslist", out JsonElement friendsList) ||
            !friendsList.TryGetProperty("friends", out JsonElement friendsArray))
        {
            return Array.Empty<string>();
        }

        return friendsArray
            .EnumerateArray()
            .Select(element => element.TryGetProperty("steamid", out JsonElement idElement) ? idElement.GetString() : null)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<IReadOnlyList<SteamFriend>> GetPlayerSummariesAsync(string apiKey, IReadOnlyList<string> steamIds, CancellationToken cancellationToken)
    {
        string joinedIds = string.Join(',', steamIds);
        string url = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids={joinedIds}";
        using HttpResponseMessage response = await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using JsonDocument json = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!json.RootElement.TryGetProperty("response", out JsonElement root) ||
            !root.TryGetProperty("players", out JsonElement players))
        {
            return Array.Empty<SteamFriend>();
        }

        List<SteamFriend> friends = new();
        foreach (JsonElement player in players.EnumerateArray())
        {
            string? steamId = player.TryGetProperty("steamid", out JsonElement idElement) ? idElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(steamId))
            {
                continue;
            }

            string personaName = player.TryGetProperty("personaname", out JsonElement nameElement)
                ? nameElement.GetString() ?? steamId
                : steamId;

            string? avatar = player.TryGetProperty("avatarfull", out JsonElement avatarElement)
                ? avatarElement.GetString()
                : null;

            bool isOnline = player.TryGetProperty("personastate", out JsonElement stateElement) && stateElement.GetInt32() != 0;

            friends.Add(new SteamFriend(steamId, personaName, avatar, isOnline));
            _cache.Set(steamId, new CachedSteamFriend(personaName, avatar, isOnline));
        }

        return friends;
    }
}
