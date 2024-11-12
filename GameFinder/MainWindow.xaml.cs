using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Input;
using GameFinderApi.Objects;

namespace GameFinderApi
{
    public partial class MainWindow : Window
    {
        private List<string> gameIds = new(Config.CommonGames);
        private int currentIndex = 0;
        private Random rand = new();
        private readonly HttpClient httpClient = new();
        private GameData? currentGameData = null;
        private GameData? nextGameData = null;
        private HashSet<string> seenGameIds = new();

        public MainWindow()
        {
            InitializeComponent();
            DisableButtons();
            gameIds = new(Config.CommonGames.OrderBy(x => rand.Next()));
            this.Loaded += async (s, e) => await LoadFirstGameAsync();
        }

        private async Task LoadFirstGameAsync()
        {
            try
            {
                Trace.WriteLine("Loading first game...");
                await PreloadNextGameDetails();
                if (nextGameData != null)
                {
                    currentGameData = nextGameData;
                    seenGameIds.Add(currentGameData.Name);
                    nextGameData = null; // Clear the preloaded cache
                    Trace.WriteLine($"Displaying first game: {currentGameData.Name}");
                    DisplayGameDetails(currentGameData);
                }
                else
                {
                    MessageBox.Show("Failed to load initial game details.");
                }

                EnableButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LoadFirstGameAsync: {ex.Message}");
            }
        }

        private async void OnLikeButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine($"Liking game: {gameIds[currentIndex]} {currentGameData.Name}");
                await App.Api.Swipe(App.Api.SessionId, gameIds[currentIndex], true);
                AnimateCard(ProfileCard, 500);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in OnLikeButtonClick: {ex.Message}");
            }
        }
        private async void OnDislikeButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine($"Disliking game: {gameIds[currentIndex]} {currentGameData.Name}");
                await App.Api.Swipe(App.Api.SessionId, gameIds[currentIndex], false);
                AnimateCard(ProfileCard, -500);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in OnDislikeButtonClick: {ex.Message}");
            }
        }

        private void AnimateCard(UIElement element, double toValue)
        {
            Storyboard storyboard = new Storyboard();
            TranslateTransform translateTransform = new TranslateTransform();
            element.RenderTransform = translateTransform;

            DoubleAnimation animation = new DoubleAnimation
            {
                To = toValue,
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                FillBehavior = FillBehavior.Stop
            };

            // Event handler for animation completion
            animation.Completed += async (s, e) =>
            {
                Trace.WriteLine("Card animation completed.");

                // Reset card position
                ResetCardPosition(element);

                // Load and display next game details
                currentIndex++;
                await LoadNextGame();
            };

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation,
                new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

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

        private async Task LoadNextGame()
        {
            try
            {
                Trace.WriteLine("Loading next game...");

                // Assign and display the preloaded game data
                if (nextGameData != null)
                {
                    currentGameData = nextGameData;
                    seenGameIds.Add(currentGameData.Name);
                    nextGameData = null;
                    Trace.WriteLine($"Displaying game: {currentGameData.Name}");
                    Dispatcher.Invoke(() => DisplayGameDetails(currentGameData));
                }

                // Preload the next game details for smooth transition
                await PreloadNextGameDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LoadNextGame: {ex.Message}");
            }
        }

        private async Task PreloadNextGameDetails()
        {
            Trace.WriteLine($"Preloading next game details; remaining games: {gameIds.Count - currentIndex}");

            while (gameIds.Count > currentIndex)
            {
                string gameId = gameIds[currentIndex];
                gameIds.Remove(gameId);

                // Skip if the gameId has already been seen/used
                if (seenGameIds.Contains(gameId))
                {
                    Trace.WriteLine($"Skipping already seen game ID: {gameId}");
                    continue;
                }

                string apiUrl = $"http://127.0.0.1:5170/SteamMarketData/{gameId}";

                try
                {
                    string jsonData = await httpClient.GetStringAsync(apiUrl);
                    if (string.IsNullOrEmpty(jsonData))
                    {
                        Trace.WriteLine($"No data for game ID: {gameId}, continuing...");
                        continue; // Skip this game and try the next one
                    }

                    nextGameData = JsonSerializer.Deserialize<GameData>(jsonData);
                    Trace.WriteLine($"Preloaded game ID: {gameId} with name: {nextGameData.Name}");

                    // Ensure we only take valid games
                    if (nextGameData is { AppType: "game" } && nextGameData.Categories.Any(x => x.Id == 1))
                    {
                        seenGameIds.Add(gameId); // Mark this game as seen
                        break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load game details: {ex.Message}");
                }
            }

            // No more games available check
            if (nextGameData == null && gameIds.Count <= currentIndex)
            {
                MessageBox.Show("No more game profiles to show.");
            }
        }

        private void DisplayGameDetails(GameData game)
        {
            if (game == null)
            {
                MessageBox.Show("Attempted to display null game data.");
                return;
            }

            try
            {
                Trace.WriteLine($"Displaying game details for: {game.Name}");
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
            switch (e.Key)
            {
                case Key.Left:
                    // Action for Left Arrow key
                    OnDislikeButtonClick(sender, e);
                    break;
                case Key.Right:
                    // Action for Right Arrow key
                    OnLikeButtonClick(sender, e);
                    break;
            }        
        }
    }
}