using Microsoft.AspNetCore.SignalR.Client;

namespace GameFinder;

public class ApiHandler
{
    public HubConnection? Connection { get; set; }
    public string SessionId { get; set; }
    public async Task Connect(string[] args)
        {
            string hubUrl = "http://127.0.0.1:5170/matchinghub"; // Replace with your SignalR hub URL

            Connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();

            RegisterEventHandlers(Connection);

            await Connection.StartAsync();
            Console.WriteLine("Connected to the hub.");

            // Example: Create a session
            await Connection.InvokeAsync("CreateSession");
        }

        private void RegisterEventHandlers(HubConnection connection)
        {
            connection.On<string>("SessionCreated", (sessionCode) =>
            {
                SessionId = sessionCode;
                Console.WriteLine($"Session created with code: {sessionCode}");
                JoinSession(SessionId, Config.GameList).Wait();
            });

            connection.On<string>("JoinedSession", (sessionCode) =>
            {
                Console.WriteLine($"Joined session with code: {sessionCode}");
                StartSession(SessionId).Wait();
            });

            connection.On<IEnumerable<string>>("SessionStarted", (commonGames) =>
            {
                Console.WriteLine("Session started. Common games:");
                Config.CommonGames = commonGames.ToList();
            });

            connection.On<string, string, bool>("UserSwiped", (userId, game, swipeRight) =>
            {
                Console.WriteLine($"User {userId} swiped {(swipeRight ? "right" : "left")} on {game}");
            });

            connection.On<string>("GameMatched", (game) =>
            {
                Console.WriteLine($"Game matched: {game}");
            });

            connection.On<string>("Error", (errorMessage) =>
            {
                Console.WriteLine($"Error occurred: {errorMessage}");
            });
        }

        public async Task JoinSession(string sessionCode, List<string> gameList)
        {
            if (Connection != null) await Connection.InvokeAsync("JoinSession", sessionCode, gameList);
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