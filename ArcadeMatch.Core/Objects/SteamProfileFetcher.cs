using System.Xml.Linq;

namespace GameFinder.Objects;

public class SteamProfile
{
    public string SteamId { get; set; }
    public string SteamId64 { get; set; }
    public string CustomURL { get; set; }
    public string OnlineState { get; set; }
    public string StateMessage { get; set; }
    public string PrivacyState { get; set; }
    public string VisibilityState { get; set; }
    public string AvatarIcon { get; set; }
    public string AvatarMedium { get; set; }
    public string AvatarFull { get; set; }
    public string Headline { get; set; }
    public string Location { get; set; }
    public string RealName { get; set; }
    public string MemberSince { get; set; }
    public string SteamRating { get; set; }
    public string HoursPlayed2Wk { get; set; }
    public string MostPlayedGameName { get; set; }
    public string MostPlayedGameHours { get; set; }
    public string MostPlayedGameLink { get; set; }
    public string Groups { get; set; }
}

public static class SteamProfileFetcher
{
    private static readonly HttpClient _client = new HttpClient();

    public static async Task<SteamProfile?> GetProfileAsync(string steam64Id)
    {
        if (string.IsNullOrWhiteSpace(steam64Id))
            throw new ArgumentException("SteamID64 cannot be null or empty.");

        string url = $"https://steamcommunity.com/profiles/{steam64Id}/?xml=1";
        string xml = await _client.GetStringAsync(url);

        var doc = XDocument.Parse(xml);
        var profile = doc.Element("profile");
        if (profile == null) return null;

        var mostPlayedGame = profile.Element("mostPlayedGame");

        return new SteamProfile
        {
            SteamId = (string?)profile.Element("steamID"),
            SteamId64 = (string?)profile.Element("steamID64"),
            CustomURL = (string?)profile.Element("customURL"),
            OnlineState = (string?)profile.Element("onlineState"),
            StateMessage = (string?)profile.Element("stateMessage"),
            PrivacyState = (string?)profile.Element("privacyState"),
            VisibilityState = (string?)profile.Element("visibilityState"),
            AvatarIcon = (string?)profile.Element("avatarIcon"),
            AvatarMedium = (string?)profile.Element("avatarMedium"),
            AvatarFull = (string?)profile.Element("avatarFull"),
            Headline = (string?)profile.Element("headline"),
            Location = (string?)profile.Element("location"),
            RealName = (string?)profile.Element("realname"),
            MemberSince = (string?)profile.Element("memberSince"),
            SteamRating = (string?)profile.Element("steamRating"),
            HoursPlayed2Wk = (string?)profile.Element("hoursPlayed2Wk"),
            MostPlayedGameName = (string?)mostPlayedGame?.Element("gameName"),
            MostPlayedGameHours = (string?)mostPlayedGame?.Element("hoursPlayed"),
            MostPlayedGameLink = (string?)mostPlayedGame?.Element("gameLink"),
            Groups = string.Join(", ",
                profile.Element("groups")?.Elements("group")?.Elements("groupName") ?? Array.Empty<XElement>())
        };
    }
}