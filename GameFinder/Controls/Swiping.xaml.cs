using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using GameFinder.Objects;
using Microsoft.VisualBasic;

namespace GameFinder.Controls;

public partial class Swiping : UserControl
{
    private readonly List<string> _gameIds;
    private int currentIndex = 0;
    private Random rand = new();
    private readonly HttpClient httpClient = new();
    private GameData? currentGameData = null;
    private GameData? nextGameData = null;
    private HashSet<string> seenGameIds = new();

    public Swiping()
    {
        _gameIds = Config.CommonGames;
        //_gameIds = gameIds;
        InitializeComponent();
        this.Loaded += async (_, _) => await LoadFirstGameAsync();
    }

    private async Task LoadFirstGameAsync()
        {
            DisableButtons();
            currentGameData = await PreloadNextGameDetails();
            if (currentGameData != null)
            {
                DisplayGameDetails(currentGameData);
                nextGameData =
                    await PreloadNextGameDetails(); // Preload the next game right after displaying the current one
            }

            EnableButtons();
        }

        private async Task<GameData?> PreloadNextGameDetails()
        {
            Console.WriteLine($"Preloading next game details; remaining games: {_gameIds.Count - currentIndex}");

            while (_gameIds.Count > currentIndex)
            {
                var gameId = _gameIds[currentIndex];
                _gameIds.Remove(gameId);

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
                        return gameData;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load game details: {ex.Message}");
                }
            }

            //MessageBox.Show("No more game profiles to show.");
            return null;
        }

        private void DisplayGameDetails(GameData? game)
        {
            if (game == null) return;

            try
            {
                Console.WriteLine($"Displaying game details for: {game.Name}");
                GameNameTextBlock.Text = game.Name;
                DescriptionTextBlock.Text = game.ShortDescription;
                GameImage.Source = new BitmapImage(new Uri(game.HeaderImage));
                GenresTextBlock.Text = string.Join(", ", game.Genres.Select(genre => genre.Description));
                LanguagesTextBlock.Text = game.SupportedLanguages?.Replace(",", ", ");
                PriceTextBlock.Text = game?.Recomendations?.Total.ToString();
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
                await App.Api.Swipe(App.Api.SessionId, _gameIds[currentIndex], like);
                AnimateCard(ProfileCard, like ? 500 : -500);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in HandleSwipe: {ex.Message}");
            }
        }

        private void AnimateCard(UIElement element, double toValue)
        {
            var animation = new DoubleAnimation
            {
                To = toValue,
                Duration = TimeSpan.FromSeconds(0.5),
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += async (_, _) =>
            {
                Trace.WriteLine("Card animation completed.");
                ResetCardPosition(element);
                currentGameData = nextGameData;
                DisplayGameDetails(currentGameData);
                nextGameData = await PreloadNextGameDetails(); // Preload the subsequent game after swipe completion
                EnableButtons();
                currentIndex++;
            };

            element.RenderTransform = new TranslateTransform();
            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation,
                new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
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
}