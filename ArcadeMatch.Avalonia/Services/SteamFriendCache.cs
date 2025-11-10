using Microsoft.Extensions.Caching.Memory;

namespace ArcadeMatch.Avalonia.Services;

public sealed class SteamFriendCache
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _memoryCache;

    public SteamFriendCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public bool TryGet(string steamId, out CachedSteamFriend friend)
    {
        if (_memoryCache.TryGetValue(steamId, out CachedSteamFriend? cached) && cached != null)
        {
            friend = cached;
            return true;
        }

        friend = default!;
        return false;
    }

    public void Set(string steamId, CachedSteamFriend friend)
    {
        _memoryCache.Set(
            steamId,
            friend,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultExpiration
            });
    }
}

public sealed record class CachedSteamFriend(string PersonaName, string? AvatarUrl, bool? IsOnline);
