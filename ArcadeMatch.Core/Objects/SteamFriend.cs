namespace GameFinder.Objects;

public record class SteamFriend(
    string SteamId,
    string PersonaName,
    string? AvatarUrl,
    bool IsOnline)
{
    public bool InSession { get; init; }

    public string? SessionCode { get; init; }
}
