using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media;
using MessageBox.Avalonia;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ArcadeMatch.Avalonia;
using GameFinder.Objects;
using Avalonia.VisualTree;
using Cookie = OpenQA.Selenium.Cookie;

namespace ArcadeMatch.Avalonia.Controls;

public partial class Tabs : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isLoggedIn;
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (_isLoggedIn != value)
            {
                _isLoggedIn = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoggedIn)));
                UpdateStatus();
            }
        }
    }

    readonly string SteamLoginUrl = "https://store.steampowered.com/login/";
    readonly string cookiesFilePath = "cookies.json";
    readonly string UserDataUrl = "https://store.steampowered.com/dynamicstore/userdata/";

    public Tabs()
    {
        InitializeComponent();
        DataContext = this;
        ShowSessionStart();
        App.Api.SessionEnded += OnSessionEnded;
    }

    public async override void EndInit()
    {
        base.EndInit();
        IsLoggedIn = await TryGetGameListAsync();
        UpdateStatus();
    }

    async void ShowMessage(string message)
    {
        var window = (Window)this.GetVisualRoot();
        await MessageBoxManager.GetMessageBoxStandardWindow("Info", message).ShowDialog(window);
    }

    void LoginButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ShowLogin();
    }

    void ShowLogin()
    {
        IReadOnlyCollection<Cookie> cookies = PromptUserToLogin();
        SaveCookies(cookiesFilePath, cookies);
    }

    async void ApiFetchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Config.SteamApiKey = ApiKeyBox.Text.Trim();
        Config.SteamId = SteamIdBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(Config.SteamApiKey) || string.IsNullOrWhiteSpace(Config.SteamId))
        {
            await MessageBoxManager.GetMessageBoxStandardWindow("Error", "Please enter both API key and Steam ID.").ShowDialog((Window)this.GetVisualRoot());
            return;
        }
        IsLoggedIn = await TryGetGameListViaApiAsync(Config.SteamApiKey, Config.SteamId);
    }

    async void RefreshButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var cookies = LoadCookies(cookiesFilePath);
        if (cookies == null)
        {
            await MessageBoxManager.GetMessageBoxStandardWindow("Error", "Failed to retrieve cookies. Please log into Steam.").ShowDialog((Window)this.GetVisualRoot());
            return;
        }
        string jsonResponse = await FetchJsonResponseWithCookies(cookies);
        List<string> ownedPackages = ParseOwnedPackages(jsonResponse);
        Config.GameList = ownedPackages;
        IsLoggedIn = ownedPackages.Count > 0;
    }

    async Task<bool> TryGetGameListAsync()
    {
        try
        {
            if (!File.Exists(cookiesFilePath))
                return false;
            IReadOnlyCollection<Cookie>? cookies = LoadCookies(cookiesFilePath);
            if (cookies == null)
                return false;
            string jsonResponse = await FetchJsonResponseWithCookies(cookies);
            List<string> ownedPackages = ParseOwnedPackages(jsonResponse);
            Config.GameList = ownedPackages;
            return ownedPackages.Count > 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    async Task<bool> TryGetGameListViaApiAsync(string apiKey, string steamId)
    {
        try
        {
            string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo=0";
            using HttpClient client = new();
            string jsonResponse = await client.GetStringAsync(url);
            JsonNode? games = JsonNode.Parse(jsonResponse)?["response"]?["games"];
            if (games is not JsonArray arr)
                return false;
            List<string> ownedPackages = new();
            foreach (JsonNode? game in arr)
            {
                string? id = game?["appid"]?.ToString();
                if (!string.IsNullOrEmpty(id))
                    ownedPackages.Add(id);
            }
            Config.GameList = ownedPackages;
            return ownedPackages.Count > 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    IReadOnlyCollection<Cookie> PromptUserToLogin()
    {
        using ChromeDriver driver = new();
        try
        {
            driver.Navigate().GoToUrl(SteamLoginUrl);
            WebDriverWait wait = new(driver, TimeSpan.FromMinutes(2));
            wait.Until(drv => drv.Url != SteamLoginUrl);
            return driver.Manage().Cookies.AllCookies;
        }
        finally
        {
            SaveCookies(cookiesFilePath, driver.Manage().Cookies.AllCookies);
            driver.Quit();
        }
    }

    async Task<string> FetchJsonResponseWithCookies(IReadOnlyCollection<Cookie> cookies)
    {
        Uri baseUri = new(UserDataUrl);
        using HttpClientHandler handler = new() { CookieContainer = new CookieContainer() };
        foreach (Cookie cookie in cookies)
        {
            handler.CookieContainer.Add(baseUri, new System.Net.Cookie(cookie.Name, HttpUtility.UrlEncode(cookie.Value, Encoding.UTF8), cookie.Path, cookie.Domain));
        }
        using HttpClient client = new(handler);
        return await client.GetStringAsync(baseUri);
    }

    static List<string> ParseOwnedPackages(string jsonResponse)
    {
        List<string> ownedPackagesList = new();
        JsonNode? json = JsonNode.Parse(jsonResponse)?["rgOwnedApps"];
        if (json is JsonArray packages)
        {
            foreach (JsonNode? package in packages)
                ownedPackagesList.Add(package.ToString());
        }
        return ownedPackagesList;
    }

    static void SaveCookies(string path, IReadOnlyCollection<Cookie> cookies)
    {
        List<CookieData> data = new();
        foreach (var c in cookies)
            data.Add(new CookieData(c));
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    static IReadOnlyCollection<Cookie>? LoadCookies(string path)
    {
        if (!File.Exists(path)) return null;
        string json = File.ReadAllText(path);
        List<CookieData>? cookieDataList = JsonSerializer.Deserialize<List<CookieData>>(json);
        return cookieDataList?.ConvertAll(c => c.ToSeleniumCookie());
    }

    void OnSessionButtonClicked(object? sender, string action)
    {
        switch (action)
        {
            case "StartNewSession":
            case "JoinSession":
                ShowSessionLobby();
                break;
            case "StartButton":
                ShowSwiping();
                break;
            case "LeaveButton":
                ShowSessionStart();
                break;
        }
    }

    internal void ShowSessionStart()
    {
        var control = new SessionStart();
        control.SessionButtonClicked += OnSessionButtonClicked;
        SessionContentControl.Content = control;
    }

    internal void ShowSessionLobby()
    {
        var control = new SessionLobby();
        control.StartButtonClicked += OnSessionButtonClicked;
        SessionContentControl.Content = control;
    }

    internal void ShowSwiping()
    {
        var swiping = new Swiping();
        swiping.LeaveClicked += () => ShowSessionStart();
        SessionContentControl.Content = swiping;
    }

    internal void ShowResults(string? game)
    {
        var result = new MatchResult(game);
        result.BackClicked += () => ShowSessionStart();
        SessionContentControl.Content = result;
    }

    void OnSessionEnded(string? game)
    {
        Dispatcher.UIThread.Post(() => ShowResults(game));
    }

    void UpdateStatus()
    {
        if (StatusBorder != null && StatusTextBlock != null)
        {
            if (IsLoggedIn)
            {
                StatusBorder.Background = new SolidColorBrush(Color.Parse("#44AA44"));
                StatusTextBlock.Text = "Connected";
            }
            else
            {
                StatusBorder.Background = new SolidColorBrush(Color.Parse("#FF4444"));
                StatusTextBlock.Text = "Not Connected";
            }
        }
    }
}

