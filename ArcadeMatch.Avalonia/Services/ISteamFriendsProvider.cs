using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.Services;

public interface ISteamFriendsProvider
{
    Task<IReadOnlyList<SteamFriend>> GetFriendsAsync(
        string steamId,
        string? apiKey,
        CancellationToken cancellationToken = default);
}
