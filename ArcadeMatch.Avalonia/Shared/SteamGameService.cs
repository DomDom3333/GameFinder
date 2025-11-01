using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using GameFinder.Objects;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Cookie = OpenQA.Selenium.Cookie;

namespace ArcadeMatch.Avalonia.Shared;

/// <summary>
/// Service for retrieving owned games and wishlist from Steam using cookies or API.
/// </summary>
public class SteamGameService
{
    private const string SteamLoginUrl = "https://store.steampowered.com/login/";
    private const string UserDataUrl = "https://store.steampowered.com/dynamicstore/userdata/";
    private const string DefaultCookiesFilePath = "cookies.json";

    private readonly string _cookiesFilePath;

    public SteamGameService(string? cookiesFilePath = null)
    {
        _cookiesFilePath = cookiesFilePath ?? DefaultCookiesFilePath;
    }

    /// <summary>
    /// Retrieves owned games using the Steam Web API.
    /// </summary>
    /// <param name="apiKey">Steam API key.</param>
    /// <param name="steamId">Steam ID of the user.</param>
    /// <returns>List of game IDs if successful, null otherwise.</returns>
    public async Task<List<string>?> GetOwnedGamesViaApiAsync(string apiKey, string steamId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(steamId))
                return null;

            string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=0";
            using HttpClient client = new();
            string jsonResponse = await client.GetStringAsync(url);
            
            JsonNode? games = JsonNode.Parse(jsonResponse)?["response"]?["games"];
            if (games is not JsonArray arr)
                return null;

            List<string> ownedPackages = new();
            foreach (JsonNode? game in arr)
            {
                string? id = game?["appid"]?.ToString();
                if (!string.IsNullOrEmpty(id))
                    ownedPackages.Add(id);
            }

            return ownedPackages;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving owned games via API: {e.Message}");
            return null;
        }
    }


    /// <summary>
    /// Retrieves both owned games and wishlist games using saved cookies.
    /// </summary>
    /// <returns>Tuple containing owned games list and wishlist games list, or null if unsuccessful.</returns>
    public async Task<(List<string> OwnedGames, List<string> WishlistGames)?> GetOwnedAndWishlistGamesAsync()
    {
        try
        {
            if (!File.Exists(_cookiesFilePath))
                return null;

            var cookies = LoadCookies(_cookiesFilePath);
            if (cookies == null)
                return null;

            string jsonResponse = await FetchJsonResponseWithCookiesAsync(cookies);
            var ownedGames = ParseOwnedPackages(jsonResponse);
            var wishlistGames = ParseWishlistPackages(jsonResponse);
            
            return (ownedGames, wishlistGames);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving games: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Prompts the user to log in to Steam using a browser and saves the cookies.
    /// </summary>
    /// <returns>Collection of cookies from the login session.</returns>
    public IReadOnlyCollection<Cookie> PromptUserToLogin()
    {
        using ChromeDriver driver = new();
        try
        {
            driver.Navigate().GoToUrl(SteamLoginUrl);
            WebDriverWait wait = new(driver, TimeSpan.FromMinutes(2));
            wait.Until(drv => drv.Url != SteamLoginUrl);
            
            var cookies = driver.Manage().Cookies.AllCookies;
            SaveCookies(_cookiesFilePath, cookies);
            return cookies;
        }
        finally
        {
            driver.Quit();
        }
    }

    /// <summary>
    /// Saves cookies to a JSON file.
    /// </summary>
    /// <param name="path">File path to save cookies.</param>
    /// <param name="cookies">Collection of cookies to save.</param>
    public void SaveCookies(string path, IReadOnlyCollection<Cookie> cookies)
    {
        List<CookieData> data = new();
        foreach (var c in cookies)
            data.Add(new CookieData(c));

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Loads cookies from a JSON file.
    /// </summary>
    /// <param name="path">File path to load cookies from.</param>
    /// <returns>Collection of cookies if successful, null otherwise.</returns>
    public IReadOnlyCollection<Cookie>? LoadCookies(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            List<CookieData>? cookieDataList = JsonSerializer.Deserialize<List<CookieData>>(json);
            return cookieDataList?.ConvertAll(c => c.ToSeleniumCookie());
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading cookies: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks if saved cookies exist.
    /// </summary>
    /// <returns>True if cookies file exists, false otherwise.</returns>
    public bool HasSavedCookies()
    {
        return File.Exists(_cookiesFilePath);
    }

    private async Task<string> FetchJsonResponseWithCookiesAsync(IReadOnlyCollection<Cookie> cookies)
    {
        Uri baseUri = new(UserDataUrl);
        using HttpClientHandler handler = new() { CookieContainer = new CookieContainer() };
        
        foreach (Cookie cookie in cookies)
        {
            handler.CookieContainer.Add(
                baseUri, 
                new System.Net.Cookie(cookie.Name, HttpUtility.UrlEncode(cookie.Value, Encoding.UTF8), cookie.Path, cookie.Domain)
            );
        }

        using HttpClient client = new(handler);
        return await client.GetStringAsync(baseUri);
    }

    private static List<string> ParseOwnedPackages(string jsonResponse)
    {
        List<string> ownedPackagesList = new();
        JsonNode? json = JsonNode.Parse(jsonResponse)?["rgOwnedApps"];
        
        if (json is JsonArray packages)
        {
            foreach (JsonNode? package in packages)
            {
                if (package != null)
                    ownedPackagesList.Add(package.ToString());
            }
        }

        return ownedPackagesList;
    }
    private static List<string> ParseWishlistPackages(string jsonResponse)
    {
        List<string> wishlistPackagesList = new();
        JsonNode? json = JsonNode.Parse(jsonResponse)?["rgWishlist"];
        
        if (json is JsonArray packages)
        {
            foreach (JsonNode? package in packages)
            {
                if (package != null)
                    wishlistPackagesList.Add(package.ToString());
            }
        }

        return wishlistPackagesList;
    }
}

