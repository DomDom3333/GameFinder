using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using GameFinder.Objects;

namespace GameFinder
{
    public class ApiHandler
    {
        public HubConnection? Connection { get; set; }
        public string SessionId { get; private set; } = string.Empty;
        private string _currentUser = string.Empty;
        public bool IsCurrentUserAdmin { get; private set; }
        private readonly List<string> _sessionRoster = new();
        public IReadOnlyList<string> SessionRoster => _sessionRoster.AsReadOnly();
        public string? CurrentAdminUser { get; private set; }

        private static readonly HttpClient HttpClient = new();
        private const string SteamMarketEndpoint = "http://127.0.0.1:5170/SteamMarketData/";

        // Define events
        public event Action<string>? SessionCreated;
        public event Action<string, bool>? UserJoinedSession;
        public event Action<string>? UserLeftSession;
        public event Action<IEnumerable<string>>? SessionStarted;
        public event Action<string, string, bool>? UserSwiped;
        public event Action<string>? GameMatched;
        public event Action<string>? ErrorOccurred;
        public event Action<IReadOnlyList<MatchedGame>>? SessionEnded;
        public event Action<IReadOnlyList<string>, string?>? SessionStateReceived;

        public async Task Connect(string[] args)
        {
            string hubUrl = "http://127.0.0.1:5170/matchinghub"; // Replace with your SignalR hub URL

            Connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();

            RegisterEventHandlers(Connection);

            await Connection.StartAsync();
            Console.WriteLine("Connected to the hub.");
        }

        private void RegisterEventHandlers(HubConnection connection)
        {
            connection.On<string>("SessionCreated", (sessionCode) =>
            {
                SessionId = sessionCode;
                Console.WriteLine($"Session created with code: {sessionCode}");
                SessionCreated?.Invoke(sessionCode);
            });

            connection.On<string, bool>("JoinedSession", (username, admin) =>
            {
                Console.WriteLine($"{username} joined");
                if (admin)
                {
                    Console.WriteLine("User is admin");
                }
                if (!_sessionRoster.Contains(username))
                {
                    _sessionRoster.Add(username);
                }
                if (admin)
                {
                    CurrentAdminUser = username;
                }
                if (!admin && string.Equals(CurrentAdminUser, username, StringComparison.Ordinal))
                {
                    CurrentAdminUser = null;
                }
                if (username == _currentUser)
                {
                    IsCurrentUserAdmin = admin;
                }
                else if (!string.IsNullOrEmpty(_currentUser))
                {
                    IsCurrentUserAdmin = string.Equals(CurrentAdminUser, _currentUser, StringComparison.Ordinal);
                }
                UserJoinedSession?.Invoke(username, admin);
            });

            connection.On<List<string>, string?>("SessionState", (users, adminUsername) =>
            {
                _sessionRoster.Clear();
                if (users != null)
                {
                    _sessionRoster.AddRange(users);
                }
                CurrentAdminUser = adminUsername;
                if (!string.IsNullOrEmpty(_currentUser))
                {
                    IsCurrentUserAdmin = string.Equals(CurrentAdminUser, _currentUser, StringComparison.Ordinal);
                }
                var snapshot = _sessionRoster.ToList();
                SessionStateReceived?.Invoke(snapshot, CurrentAdminUser);
            });

            // Assuming a "LeftSession" event exists in your SignalR hub
            connection.On<string>("LeftSession", (username) =>
            {
                Console.WriteLine($"{username} left");
                _sessionRoster.RemoveAll(user => string.Equals(user, username, StringComparison.Ordinal));
                if (string.Equals(CurrentAdminUser, username, StringComparison.Ordinal))
                {
                    CurrentAdminUser = null;
                }
                if (!string.IsNullOrEmpty(_currentUser))
                {
                    IsCurrentUserAdmin = string.Equals(CurrentAdminUser, _currentUser, StringComparison.Ordinal);
                }
                UserLeftSession?.Invoke(username);
            });

            connection.On<IEnumerable<string>>("SessionStarted", (commonGames) =>
            {
                Console.WriteLine("Session started. Common games:");
                Config.CommonGames = commonGames.ToList();
                SessionStarted?.Invoke(commonGames);
            });

            connection.On<string, string, bool>("UserSwiped", (userId, game, swipeRight) =>
            {
                Console.WriteLine($"User {userId} swiped {(swipeRight ? "right" : "left")} on {game}");
                UserSwiped?.Invoke(userId, game, swipeRight);
            });

            connection.On<string>("GameMatched", (game) =>
            {
                Console.WriteLine($"Game matched: {game}");
                GameMatched?.Invoke(game);
            });

            connection.On<List<MatchedGameSummary>?>("SessionEnded", async (games) =>
            {
                var resolved = await ResolveGamesAsync(games ?? Enumerable.Empty<MatchedGameSummary>()).ConfigureAwait(false);
                Console.WriteLine("Session ended. Matched games: " +
                    string.Join(", ", resolved.Select(g => $"{g.Data.Name} ({g.Likes}/{g.TotalParticipants})")));
                SessionEnded?.Invoke(resolved);
            });

            connection.On<string>("Error", (errorMessage) =>
            {
                Console.WriteLine($"Error occurred: {errorMessage}");
                ErrorOccurred?.Invoke(errorMessage);
            });
        }

        public async Task CreateSessionAsync()
        {
            if (Connection != null) await Connection.InvokeAsync("CreateSession");
        }

        public async Task JoinSessionAsync(string sessionCode, string username, List<string> gameList)
        {
            SessionId = sessionCode;
            _currentUser = username;
            if (Connection != null)
            {
                await Connection.InvokeAsync("JoinSession", sessionCode, username, gameList);
            }
        }
        
        public async Task LeaveSessionAsync(string username)
        {
            if (Connection != null)
            {
                await Connection.InvokeAsync("LeaveSession", SessionId, username);
            }
            SessionId = string.Empty;
            _currentUser = string.Empty;
            IsCurrentUserAdmin = false;
            Config.CommonGames.Clear();
            _sessionRoster.Clear();
            CurrentAdminUser = null;
        }

        public async Task StartSession(string sessionCode)
        {
            if (Connection != null) await Connection.InvokeAsync("StartSession", sessionCode);
        }

        public async Task Swipe(string sessionCode, string game, bool swipeRight)
        {
            if (Connection != null) await Connection.InvokeAsync("Swipe", sessionCode, game, swipeRight);
        }

        public async Task EndSession(string sessionCode)
        {
            if (Connection != null)
            {
                await Connection.InvokeAsync("EndSession", sessionCode);
            }
            SessionId = string.Empty;
            _currentUser = string.Empty;
            IsCurrentUserAdmin = false;
            _sessionRoster.Clear();
            CurrentAdminUser = null;
        }

        public async Task<IReadOnlyList<MatchedGame>> ResolveGamesAsync(IEnumerable<MatchedGameSummary> gameSummaries)
        {
            var resolved = new List<MatchedGame>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var summary in gameSummaries ?? Enumerable.Empty<MatchedGameSummary>())
            {
                var id = summary.Id;

                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                {
                    continue;
                }

                if (GameDataCache.TryGet(id, out GameData? cached) && cached != null)
                {
                    resolved.Add(new MatchedGame(id, cached, summary.Likes, summary.TotalParticipants));
                    continue;
                }

                try
                {
                    var json = await HttpClient.GetStringAsync($"{SteamMarketEndpoint}{id}").ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        continue;
                    }

                    var data = JsonSerializer.Deserialize<GameData>(json);
                    if (data == null)
                    {
                        continue;
                    }

                    await GameDataCache.SetAsync(id, data).ConfigureAwait(false);
                    resolved.Add(new MatchedGame(id, data, summary.Likes, summary.TotalParticipants));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to resolve game {id}: {ex.Message}");
                }
            }

            return resolved;
        }

        public async Task<MatchedGame?> ResolveGameAsync(string gameId, int likes = 0, int totalParticipants = 0)
        {
            if (totalParticipants <= 0)
            {
                totalParticipants = _sessionRoster.Count;
            }

            if (likes <= 0 && totalParticipants > 0)
            {
                likes = totalParticipants;
            }

            var games = await ResolveGamesAsync(new[] { new MatchedGameSummary(gameId, likes, totalParticipants) }).ConfigureAwait(false);
            return games.FirstOrDefault();
        }
    }
}