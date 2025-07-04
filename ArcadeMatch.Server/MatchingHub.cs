﻿using System;
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
        
        // Stores the admin username for each session code
        private static readonly ConcurrentDictionary<string, string> Admins = new();

        // Add a mapping between ConnectionId and Username
        private static readonly ConcurrentDictionary<string, string> ConnectionUserMapping = new();

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        // 2. Remove user mapping and update admin on disconnect/leave
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectionUserMapping.TryRemove(Context.ConnectionId, out string username))
            {
                foreach (var kvp in Sessions)
                {
                    string sessionCode = kvp.Key;
                    Session session = kvp.Value;
                    if (session.Users.Remove(Context.ConnectionId))
                    {
                        UserGames.TryRemove(Context.ConnectionId, out _);
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionCode);

                        foreach (var swipes in session.GameSwipes.Values)
                        {
                            swipes.TryRemove(Context.ConnectionId, out _);
                        }

                        session.MatchedGames.Clear();
                        foreach (var (gameId, swipes) in session.GameSwipes)
                        {
                            bool allRight = session.Users.All(id => swipes.TryGetValue(id, out bool right) && right);
                            if (allRight)
                            {
                                session.MatchedGames.Add(gameId);
                            }
                        }

                        if (session.Users.Count > 0)
                        {
                            var remainingLists = session.Users
                                .Where(id => UserGames.ContainsKey(id))
                                .Select(id => UserGames[id])
                                .ToList();
                            if (remainingLists.Count > 0)
                            {
                                var common = remainingLists.Aggregate((prev, next) => new HashSet<string>(prev.Intersect(next)));
                                session.CommonGames = common;
                            }
                            else
                            {
                                session.CommonGames.Clear();
                            }
                        }

                        await Clients.GroupExcept(sessionCode, Context.ConnectionId).SendAsync("LeftSession", username);

                        if (Admins.TryGetValue(sessionCode, out string admin) && admin == username)
                        {
                            if (session.Users.Count > 0)
                            {
                                string newAdminConn = session.Users.First();
                                if (ConnectionUserMapping.TryGetValue(newAdminConn, out string newAdminUser))
                                {
                                    Admins[sessionCode] = newAdminUser;
                                    await Clients.Client(newAdminConn).SendAsync("JoinedSession", newAdminUser, true);
                                    await Clients.GroupExcept(sessionCode, newAdminConn).SendAsync("JoinedSession", newAdminUser, true);
                                }
                                else
                                {
                                    Admins.TryRemove(sessionCode, out _);
                                }
                            }
                            else
                            {
                                Admins.TryRemove(sessionCode, out _);
                            }
                        }

                        if (!session.Users.Any())
                        {
                            Sessions.TryRemove(sessionCode, out _);
                        }
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
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
                bool isAdmin;
                if (!Admins.TryGetValue(sessionCode, out string? currentAdmin))
                {
                    isAdmin = true;
                    Admins[sessionCode] = username;
                }
                else
                {
                    isAdmin = currentAdmin == username;
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

                    // Remove user swipes from all games
                    foreach (var kvp in session.GameSwipes.Values)
                    {
                        kvp.TryRemove(Context.ConnectionId, out _);
                    }

                    // Recalculate matched games
                    session.MatchedGames.Clear();
                    foreach (var (gameId, swipes) in session.GameSwipes)
                    {
                        bool allRight = session.Users.All(id => swipes.TryGetValue(id, out bool right) && right);
                        if (allRight)
                        {
                            session.MatchedGames.Add(gameId);
                        }
                    }

                    // Recalculate common games for remaining users
                    if (session.Users.Count > 0)
                    {
                        var remainingLists = session.Users
                            .Where(id => UserGames.ContainsKey(id))
                            .Select(id => UserGames[id])
                            .ToList();
                        if (remainingLists.Count > 0)
                        {
                            var common = remainingLists.Aggregate((prev, next) => new HashSet<string>(prev.Intersect(next)));
                            session.CommonGames = common;
                        }
                        else
                        {
                            session.CommonGames.Clear();
                        }
                    }

                    await Clients.Client(Context.ConnectionId).SendAsync("LeftSession", username);
                    await Clients.GroupExcept(sessionCode, Context.ConnectionId).SendAsync("LeftSession", username);
                }

                if (Admins.TryGetValue(sessionCode, out string admin) && admin == username)
                {
                    if (session.Users.Count > 0)
                    {
                        string newAdminConn = session.Users.First();
                        if (ConnectionUserMapping.TryGetValue(newAdminConn, out string newAdminUser))
                        {
                            Admins[sessionCode] = newAdminUser;
                            await Clients.Client(newAdminConn).SendAsync("JoinedSession", newAdminUser, true);
                            await Clients.GroupExcept(sessionCode, newAdminConn).SendAsync("JoinedSession", newAdminUser, true);
                        }
                        else
                        {
                            Admins.TryRemove(sessionCode, out _);
                        }
                    }
                    else
                    {
                        Admins.TryRemove(sessionCode, out _);
                    }
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

        public async Task EndSession(string sessionCode)
        {
            if (Sessions.TryGetValue(sessionCode, out Session? session))
            {
                string? result = session.MatchedGames.FirstOrDefault();
                await Clients.Group(sessionCode).SendAsync("SessionEnded", result);
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