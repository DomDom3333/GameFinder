using System;

namespace GameFinder.Objects;

public record MatchedGame(string Id, GameData Data, int Likes, int TotalParticipants)
{
    public Uri SteamUri => new($"https://store.steampowered.com/app/{Id}");

    public bool IsUnanimous => TotalParticipants > 0 && Likes >= TotalParticipants;

    public string LikesDisplay
    {
        get
        {
            if (TotalParticipants > 0)
            {
                if (Likes >= TotalParticipants)
                {
                    var playerWord = TotalParticipants == 1 ? "player" : "players";
                    return $"❤️ Liked by all {TotalParticipants} {playerWord}";
                }

                var likedWord = Likes == 1 ? "player" : "players";
                return $"❤️ Liked by {Likes} of {TotalParticipants} {likedWord}";
            }

            var fallbackWord = Likes == 1 ? "player" : "players";
            return $"❤️ Liked by {Likes} {fallbackWord}";
        }
    }

    public string ParticipantsDisplay
    {
        get
        {
            if (TotalParticipants <= 0)
            {
                return "👥 0 players";
            }

            return TotalParticipants == 1
                ? "👥 1 player"
                : $"👥 {TotalParticipants} players";
        }
    }
}
