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

        // Define events
        public event Action<string>? SessionCreated;
        public event Action<string, bool>? UserJoinedSession;
        public event Action<string>? UserLeftSession;
        public event Action<IEnumerable<string>>? SessionStarted;
        public event Action<string, string, bool>? UserSwiped;
        public event Action<string>? GameMatched;
        public event Action<string>? ErrorOccurred;

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
                UserJoinedSession?.Invoke(username, admin);
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
            if (Connection != null) await Connection.InvokeAsync("JoinSession", sessionCode, username, gameList);
        }
        
        public async Task LeaveSessionAsync(string username)
        {
            if (Connection != null) await Connection.InvokeAsync("LeaveSession", SessionId, username);
        }

        public async Task StartSession(string sessionCode)
        {
            if (Connection != null) await Connection.InvokeAsync("StartSession", sessionCode);
        }

        public async Task Swipe(string sessionCode, string game, bool swipeRight)
        {
            if (Connection != null) await Connection.InvokeAsync("Swipe", sessionCode, game, swipeRight);
        }
    }
}