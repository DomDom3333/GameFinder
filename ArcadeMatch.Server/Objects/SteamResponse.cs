using System.Text.Json.Serialization;

namespace GameFinder.Objects;

public class SteamGameResponse
{
    public bool Success { get; set; }
    public GameData Data { get; set; }
}

public class GameData
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("type")]
    public string AppType { get; set; }
    [JsonPropertyName("short_description")]
    public string ShortDescription { get; set; }
    [JsonPropertyName("header_image")]
    public string HeaderImage { get; set; }
    [JsonPropertyName("genres")]
    public List<Genre> Genres { get; set; }    
    [JsonPropertyName("categories")]
    public List<Category> Categories { get; set; }
    [JsonPropertyName("supported_languages")]
    public string SupportedLanguages { get; set; }
    [JsonPropertyName("price_overview")]
    public PriceOverview? PriceOverview { get; set; }
    [JsonPropertyName("release_date")]
    public ReleaseDate? ReleaseDate { get; set; }
    [JsonPropertyName("developers")]
    public List<string>? Developers { get; set; }
    [JsonPropertyName("publishers")]
    public List<string>? Publishers { get; set; }
    public Recommendations Recommendations { get; set; }
    [JsonPropertyName("metacritic")]
    public Metacritic? Metacritic { get; set; }
    [JsonIgnore]
    public ReviewSummary? ReviewSummary { get; set; }
}

public class PriceOverview
{
    [JsonPropertyName("final_formatted")]
    public string? FinalFormatted { get; set; }
}

public class ReleaseDate
{
    [JsonPropertyName("coming_soon")]
    public bool ComingSoon { get; set; }
    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

public class Category
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class Genre
{
    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class Recommendations
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class Metacritic
{
    [JsonPropertyName("score")]
    public int Score { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class ReviewSummary
{
    [JsonPropertyName("review_score_desc")]
    public string ReviewScoreDesc { get; set; }
    [JsonPropertyName("total_reviews")]
    public int TotalReviews { get; set; }
}