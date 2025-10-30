using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

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

        // Define events
        public event Action<string>? SessionCreated;
        public event Action<string, bool>? UserJoinedSession;
        public event Action<string>? UserLeftSession;
        public event Action<IEnumerable<string>>? SessionStarted;
        public event Action<string, string, bool>? UserSwiped;
        public event Action<string>? GameMatched;
        public event Action<string>? ErrorOccurred;
        public event Action<string?>? SessionEnded;
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
                if (username == _currentUser)
                {
                    IsCurrentUserAdmin = admin;
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

            connection.On<string?>("SessionEnded", (game) =>
            {
                Console.WriteLine($"Session ended. Matched game: {game}");
                SessionEnded?.Invoke(game);
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
    }
}