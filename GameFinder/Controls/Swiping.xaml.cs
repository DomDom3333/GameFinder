using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using GameFinder.Helpers;
using GameFinder.Objects;
using Microsoft.VisualBasic;

namespace GameFinder.Controls;

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
        Unloaded += Swiping_Unloaded;
        Loaded += async (_, _) => await LoadFirstGameAsync();
    }

    private async Task LoadFirstGameAsync()
        {
            DisableButtons();
            (currentGameData, currentGameId) = await PreloadNextGameDetails();
            if (currentGameData != null)
            {
                DisplayGameDetails(currentGameData);
                (nextGameData, nextGameId) = await PreloadNextGameDetails(); // Preload the next game right after displaying the current one
            }
            else
            {
                await App.Api.EndSession(App.Api.SessionId);
            }

            EnableButtons();
        }

        private async Task<(GameData? gameData, string? gameId)> PreloadNextGameDetails()
        {
            Console.WriteLine($"Preloading next game details; remaining games: {_gameQueue.Count}");

            while (_gameQueue.Count > 0)
            {
                var gameId = _gameQueue.Dequeue();

                if (seenGameIds.Contains(gameId))
                {
                    Console.WriteLine($"Skipping already seen game ID: {gameId}");
                    continue;
                }

                var apiUrl = $"http://127.0.0.1:5170/SteamMarketData/{gameId}";
                try
                {
                    var jsonData = await httpClient.GetStringAsync(apiUrl);
                    if (string.IsNullOrEmpty(jsonData))
                    {
                        Trace.WriteLine($"No data for game ID: {gameId}, continuing...");
                        continue;
                    }

                    var gameData = JsonSerializer.Deserialize<GameData>(jsonData);
                    if (gameData != null && gameData.AppType == "game" &&
                        gameData.Categories.Any(x => x.Id == 1))
                    {
                        seenGameIds.Add(gameId);
                        return (gameData, gameId);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load game details: {ex.Message}");
                }
            }

            //MessageBox.Show("No more game profiles to show.");
            return (null, null);
        }

        private void DisplayGameDetails(GameData? game)
        {
            if (game == null) return;

            try
            {
                Console.WriteLine($"Displaying game details for: {game.Name}");
                GameNameTextBlock.SafeInvoke(() => GameNameTextBlock.Text = game.Name);
                DescriptionTextBlock.Text = game.ShortDescription;
                GameImage.Source = new BitmapImage(new Uri(game.HeaderImage));
                GenresTextBlock.Text = string.Join(", ", game.Genres.Select(genre => genre.Description));
                LanguagesTextBlock.Text = game.SupportedLanguages?.Replace(",", ", ");
                PriceTextBlock.Text = game?.Recommendations?.Total.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception in DisplayGameDetails: {ex.Message}");
            }
        }
    
    private async void OnLikeButtonClick(object sender, RoutedEventArgs e)
        {
            await HandleSwipe(true);
        }

        private async void OnDislikeButtonClick(object sender, RoutedEventArgs e)
        {
            await HandleSwipe(false);
        }

        private async Task HandleSwipe(bool like)
        {
            try
            {
                DisableButtons();
                var swipeAction = like ? "Liking" : "Disliking";
                Console.WriteLine($"{swipeAction} game: {currentGameData?.Name}");
                if (!string.IsNullOrEmpty(currentGameId))
                {
                    await App.Api.Swipe(App.Api.SessionId, currentGameId, like);
                }
                AnimateCard(ProfileCard, like ? 500 : -500);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in HandleSwipe: {ex.Message}");
            }
        }

        private void AnimateCard(UIElement element, double toValue)
        {
            var slide = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromSeconds(0.4),
                FillBehavior = FillBehavior.Stop
            };

            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.4),
                FillBehavior = FillBehavior.Stop
            };

            slide.Completed += async (_, _) =>
            {
                Trace.WriteLine("Card animation completed.");
                ResetCardPosition(element);
                element.Opacity = 0;
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

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };

            element.RenderTransform = new TranslateTransform();
            Storyboard.SetTarget(slide, element);
            Storyboard.SetTargetProperty(slide,
                new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            Storyboard.SetTarget(fadeOut, element);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));

            var storyboard = new Storyboard();
            storyboard.Children.Add(slide);
            storyboard.Children.Add(fadeOut);
            storyboard.Begin();
        }

        private void ResetCardPosition(UIElement element)
        {
            if (element.RenderTransform is TranslateTransform translateTransform)
            {
                translateTransform.X = 0;
            }
        }

        private void DisableButtons()
        {
            Like.IsEnabled = false;
            Dislike.IsEnabled = false;
        }

        private void EnableButtons()
        {
            Like.IsEnabled = true;
            Dislike.IsEnabled = true;
        }

        private void Window_Keydown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
                OnDislikeButtonClick(sender, e);
            else if (e.Key == Key.Right)
                OnLikeButtonClick(sender, e);
        }

        private void OnGameMatched(string gameId)
        {
            Dispatcher.Invoke(() =>
                MessageBox.Show($"All players liked {gameId}!", "Match", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Swiping_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Api.GameMatched -= OnGameMatched;
            Unloaded -= Swiping_Unloaded;
        }

        private async void OnLeaveButtonClick(object sender, RoutedEventArgs e)
        {
            await App.Api.LeaveSessionAsync(Config.Username);
            LeaveClicked?.Invoke();
        }
}
