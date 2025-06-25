using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ArcadeMatch.Avalonia.Helpers;
using GameFinder.Objects;
using ArcadeMatch.Avalonia;
using GameFinder;
using Avalonia.VisualTree;
using MessageBox.Avalonia;

namespace ArcadeMatch.Avalonia.Controls;

public partial class Swiping : UserControl
{
    private readonly Queue<string> _gameQueue;
    private readonly HttpClient httpClient = new();
    private GameData? currentGameData = null;
    private string? currentGameId = null;
    private GameData? nextGameData = null;
    private string? nextGameId = null;
    private readonly HashSet<string> seenGameIds = new();

    public event Action? LeaveClicked;

    public Swiping()
    {
        _gameQueue = new Queue<string>(Config.CommonGames);
        InitializeComponent();
        App.Api.GameMatched += OnGameMatched;
        this.Unloaded += Swiping_Unloaded;
        this.Loaded += async (_, _) => await LoadFirstGameAsync();
    }

    async Task LoadFirstGameAsync()
    {
        DisableButtons();
        (currentGameData, currentGameId) = await PreloadNextGameDetails();
        if (currentGameData != null)
        {
            DisplayGameDetails(currentGameData);
            (nextGameData, nextGameId) = await PreloadNextGameDetails();
        }
        else
        {
            await App.Api.EndSession(App.Api.SessionId);
        }
        EnableButtons();
    }

    async Task<(GameData? gameData, string? gameId)> PreloadNextGameDetails()
    {
        while (_gameQueue.Count > 0)
        {
            var gameId = _gameQueue.Dequeue();
            if (seenGameIds.Contains(gameId))
                continue;
            if (GameDataCache.TryGet(gameId, out GameData? cached))
            {
                seenGameIds.Add(gameId);
                return (cached, gameId);
            }
            var apiUrl = $"http://127.0.0.1:5170/SteamMarketData/{gameId}";
            try
            {
                var jsonData = await httpClient.GetStringAsync(apiUrl);
                if (string.IsNullOrEmpty(jsonData))
                    continue;
                var gameData = JsonSerializer.Deserialize<GameData>(jsonData);
                if (gameData != null && gameData.AppType == "game" && gameData.Categories.Any(x => x.Id == 1))
                {
                    seenGameIds.Add(gameId);
                    await GameDataCache.SetAsync(gameId, gameData);
                    return (gameData, gameId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load game details: {ex.Message}");
            }
        }
        return (null, null);
    }

    void DisplayGameDetails(GameData game)
    {
        GameNameTextBlock.SafeInvoke(() => GameNameTextBlock.Text = game.Name);
        DescriptionTextBlock.Text = game.ShortDescription;
        GameImage.Source = new Bitmap(game.HeaderImage);
        GenresTextBlock.Text = string.Join(", ", game.Genres.Select(g => g.Description));
        LanguagesTextBlock.Text = game.SupportedLanguages?.Replace(",", ", ");
        PriceTextBlock.Text = game.Recommendations?.Total.ToString();
    }

    async void OnLikeButtonClick(object? sender, RoutedEventArgs e)
    {
        await HandleSwipe(true);
    }

    async void OnDislikeButtonClick(object? sender, RoutedEventArgs e)
    {
        await HandleSwipe(false);
    }

    async Task HandleSwipe(bool like)
    {
        try
        {
            DisableButtons();
            if (!string.IsNullOrEmpty(currentGameId))
            {
                await App.Api.Swipe(App.Api.SessionId, currentGameId, like);
            }
            currentGameData = nextGameData;
            currentGameId = nextGameId;
            if (currentGameData == null)
            {
                await App.Api.EndSession(App.Api.SessionId);
            }
            else
            {
                DisplayGameDetails(currentGameData);
                (nextGameData, nextGameId) = await PreloadNextGameDetails();
                EnableButtons();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in HandleSwipe: {ex.Message}");
        }
    }

    void DisableButtons()
    {
        Like.IsEnabled = false;
        Dislike.IsEnabled = false;
    }

    void EnableButtons()
    {
        Like.IsEnabled = true;
        Dislike.IsEnabled = true;
    }

    void OnGameMatched(string gameId)
    {
        Dispatcher.UIThread.Post(async () =>
            await MessageBoxManager.GetMessageBoxStandardWindow("Match", $"All players liked {gameId}!").ShowDialog((Window)this.GetVisualRoot()));
    }

    void Swiping_Unloaded(object? sender, RoutedEventArgs e)
    {
        App.Api.GameMatched -= OnGameMatched;
    }

    async void OnLeaveButtonClick(object? sender, RoutedEventArgs e)
    {
        await App.Api.LeaveSessionAsync(Config.Username);
        LeaveClicked?.Invoke();
    }
}
