using System.Text.Json.Serialization;

namespace GameFinder.Objects
{
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
        public Recommendations Recommendations { get; set; }
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
}