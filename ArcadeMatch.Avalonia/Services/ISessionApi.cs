using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.Services;

public interface ISessionApi
{
    string SessionId { get; }
    bool IsCurrentUserAdmin { get; }
    IReadOnlyList<string> SessionRoster { get; }
    string? CurrentAdminUser { get; }

    event Action<string>? SessionCreated;
    event Action<string, bool>? UserJoinedSession;
    event Action<string>? UserLeftSession;
    event Action<IEnumerable<string>>? SessionStarted;
    event Action<string, string, bool>? UserSwiped;
    event Action<string>? GameMatched;
    event Action<string>? ErrorOccurred;
    event Action<IReadOnlyList<MatchedGame>>? SessionEnded;
    event Action<IReadOnlyList<string>, string?>? SessionStateReceived;
    event Action<string, string>? InviteReceived;

    Task Connect(string[] args);
    Task CreateSessionAsync();
    Task JoinSessionAsync(
        string sessionCode,
        string username,
        IEnumerable<string> gameList,
        IEnumerable<string> wishlist,
        string? steamId = null);
    Task LeaveSessionAsync(string username);
    Task StartSession(string sessionCode, bool includeWishlist, int minOwners, int minWishlisted);
    Task Swipe(string sessionCode, string game, bool swipeRight);
    Task EndSession(string sessionCode);
    Task<IReadOnlyList<MatchedGame>> ResolveGamesAsync(IEnumerable<MatchedGameSummary> gameSummaries);
    Task<MatchedGame?> ResolveGameAsync(string gameId, int likes = 0, int totalParticipants = 0);
    Task<IDictionary<string, string>> GetFriendsSessionsAsync(IEnumerable<string> friendSteamIds);
    Task InviteFriendAsync(string friendSteamId);
}
