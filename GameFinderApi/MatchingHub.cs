using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace GameFinder
{
    public class MatchingHub : Hub
    {
        // To store ongoing sessions and the users in them
        private static readonly ConcurrentDictionary<string, Session> Sessions = new();

        // To store user connections and their game lists
        private static readonly ConcurrentDictionary<string, HashSet<string>> UserGames = new();
        
        // To store Sessions and their Admins
        private static readonly ConcurrentDictionary<string, HashSet<string>> Admins = new();

        // Add a mapping between ConnectionId and Username
        private static readonly ConcurrentDictionary<string, string> ConnectionUserMapping = new();

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        // 2. Remove user mapping and update admin on disconnect/leave
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Get the username from the mapping
            if (ConnectionUserMapping.TryRemove(Context.ConnectionId, out string username))
            {
                // Remove from all sessions
                foreach (Session session in Sessions.Values)
                {
                    session.Users.Remove(Context.ConnectionId);
                }
                // Remove from user games
                UserGames.TryRemove(Context.ConnectionId, out _);
                // Remove from admins if needed
                if (Admins.ContainsKey(username))
                {
                    Admins.TryRemove(username, out _);
                }
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task CreateSession()
        {
            string sessionCode = GenerateSessionCode();
            while (!Sessions.TryAdd(sessionCode, new Session()))
            {
                sessionCode = GenerateSessionCode();
            }
            await Clients.Client(Context.ConnectionId).SendAsync("SessionCreated", sessionCode);
            Console.WriteLine($"Session created with code: {sessionCode}");
        }

        public async Task JoinSession(string sessionCode, string username, List<string> gameList)
        {
            if (Sessions.TryGetValue(sessionCode, out Session? session))
            {
                // Determine if this user will be admin (first user in the session)
                bool isAdmin = session.Users.Count == 0;
                if (isAdmin)
                {
                    // Use the username as key for admin collection
                    Admins.TryAdd(username, new HashSet<string>());
                }
                // Store the mapping: connection id to user name
                ConnectionUserMapping[Context.ConnectionId] = username;
                session.Users.Add(Context.ConnectionId);

                // Store user's game list
                UserGames[Context.ConnectionId] = new HashSet<string>(gameList);

                Console.WriteLine($"User {username} joined session {sessionCode}");

                // Add the connection to the group immediately.
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode);

                // Notify the caller about their join and also notify the group about the new user.
                await Clients.Client(Context.ConnectionId).SendAsync("JoinedSession", username, isAdmin);
                await Clients.GroupExcept(sessionCode, Context.ConnectionId)
                    .SendAsync("JoinedSession", username, isAdmin);
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Error", "Session does not exist");
            }
        }

        public async Task LeaveSession(string sessionCode, string username)
        {
            if (Sessions.TryGetValue(sessionCode, out Session? session))
            {
                // Remove mapping
                ConnectionUserMapping.TryRemove(Context.ConnectionId, out _);
                if (UserGames.TryRemove(Context.ConnectionId, out _))
                {
                    session.Users.Remove(Context.ConnectionId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionCode);
                    await Clients.Client(Context.ConnectionId).SendAsync("LeaveSession", username);
                }
                // Cleanup Admin collection if this user was admin
                if (Admins.ContainsKey(username))
                {
                    Admins.TryRemove(username, out _);
                    // Optionally, reassign admin status to another user in the session.
                }
                if (!session.Users.Any())
                {
                    Sessions.TryRemove(sessionCode, out _);
                }
            }
        }

        // 3. Validate game list intersection in StartSession
        public async Task StartSession(string sessionCode)
        {
            if (Sessions.TryGetValue(sessionCode, out Session? session))
            {
                // Ensure there is at least one user; if only one user exists, handle it accordingly.
                if (session.Users.Count < 1)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("Error", "Not enough participants to start the session.");
                    return;
                }
                // Aggregate game lists from all users in the session with safety for single user scenarios.
                HashSet<string> commonGames = session.Users
                    .Where(id => UserGames.ContainsKey(id))
                    .Select(id => UserGames[id])
                    .Aggregate((previousList, nextList) =>
                        new HashSet<string>(previousList.Intersect(nextList).OrderBy(x => Guid.NewGuid())));

                session.CommonGames = commonGames;
                await Clients.Group(sessionCode).SendAsync("SessionStarted", commonGames);
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Error", "Session does not exist");
            }
        }

        public async Task Swipe(string sessionCode, string game, bool swipeRight)
        {
            if (Sessions.TryGetValue(sessionCode, out Session? session))
            {
                // Initialize swipe dictionary for the game if not exists
                if (!session.GameSwipes.TryGetValue(game, out ConcurrentDictionary<string, bool>? swipes))
                {
                    swipes = new ConcurrentDictionary<string, bool>();
                    session.GameSwipes[game] = swipes;
                }

                // Store the swipe
                swipes[Context.ConnectionId] = swipeRight;

                // Notify session group about the swipe
                await Clients.Group(sessionCode).SendAsync("UserSwiped", Context.ConnectionId, game, swipeRight);

                // Check if all users have swiped right for this game
                bool allSwipedRight = session.Users.All(user =>
                    session.GameSwipes.TryGetValue(game, out ConcurrentDictionary<string, bool>? userSwipes) &&
                    userSwipes.TryGetValue(user, out bool userSwipe) &&
                    userSwipe);

                if (allSwipedRight)
                {
                    session.MatchedGames.Add(game);
                    await Clients.Group(sessionCode).SendAsync("GameMatched", game);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Error", "Session does not exist");
            }
        }

        private string GenerateSessionCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    public class Session
    {
        public HashSet<string> Users { get; } = new();

        // Stores swipes for each game by each user
        public ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> GameSwipes { get; } = new();

        // Stores games that are matched by all users
        public HashSet<string> MatchedGames { get; } = new();

        // Stores common games when session starts
        public HashSet<string> CommonGames { get; set; } = new();
    }
}