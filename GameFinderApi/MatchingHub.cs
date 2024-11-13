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

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Remove user from all sessions they are part of
            foreach (Session session in Sessions.Values)
            {
                session.Users.Remove(Context.ConnectionId);
            }

            UserGames.TryRemove(Context.ConnectionId, out _);
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
        }

        public async Task JoinSession(string sessionCode, string username, List<string> gameList)
        {
            if (Sessions.TryGetValue(sessionCode, out Session? session))
            {
                bool isAdmin = !session.Users.Any();
                if (isAdmin)
                {
                    Admins.TryAdd(username, new HashSet<string>());
                }

                session.Users.Add(Context.ConnectionId);

                // Store user's game list
                UserGames[Context.ConnectionId] = new HashSet<string>(gameList);

                // Notify other members if necessary
                await Clients.Group(sessionCode).SendAsync("JoinedSession", username, isAdmin);
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode);
                foreach (string sessionUser in session.Users)
                {
                    if (username == sessionUser)
                    {
                        continue;
                    }
                    await Clients.Caller.SendAsync("JoinedSession", sessionUser, Admins.ContainsKey(sessionUser));
                }
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
                if (UserGames.TryRemove(Context.ConnectionId, out _))
                {
                    session.Users.Remove(Context.ConnectionId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionCode);
                    await Clients.Client(Context.ConnectionId).SendAsync("LeaveSession", username);
                }

                if (!session.Users.Any())
                {
                    Sessions.TryRemove(sessionCode, out _);
                }
            }
        }

        public async Task StartSession(string sessionCode)
        {
            if (Sessions.TryGetValue(sessionCode, out Session? session))
            {
                // Get the intersection of game lists from all users in the session
                HashSet<string> commonGames = session.Users
                    .Where(UserGames.ContainsKey)
                    .Select(connId => UserGames[connId])
                    .Aggregate((previousList, nextList) => new HashSet<string>(previousList.Intersect(nextList)));

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