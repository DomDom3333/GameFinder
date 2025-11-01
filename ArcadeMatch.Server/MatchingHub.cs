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
        // To store user wishlists
        private static readonly ConcurrentDictionary<string, HashSet<string>> UserWishlists = new();
        
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
                        UserWishlists.TryRemove(Context.ConnectionId, out _);
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
                            await PromoteNextAdminAsync(sessionCode, session);
                        }

                        if (session.Users.Any())
                        {
                            await BroadcastSessionStateAsync(sessionCode, session);
                        }
                        else
                        {
                            Sessions.TryRemove(sessionCode, out _);
                            Admins.TryRemove(sessionCode, out _);
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

        public async Task JoinSession(string sessionCode, string username, List<string> gameList, List<string> wishlist)
        {
            if (Sessions.TryGetValue(sessionCode, out Session? session))
            {
                // Determine if this user will be admin (first user in the session)
                bool isAdmin;
                if (!Admins.TryGetValue(sessionCode, out string currentAdmin))
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

                // Store user's game list and wishlist
                UserGames[Context.ConnectionId] = new HashSet<string>(gameList ?? new List<string>());
                UserWishlists[Context.ConnectionId] = new HashSet<string>(wishlist ?? new List<string>());

                Console.WriteLine($"User {username} joined session {sessionCode}");

                // Add the connection to the group immediately.
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode);

                var (roster, adminUsername) = BuildSessionSnapshot(sessionCode, session);
                await Clients.Client(Context.ConnectionId).SendAsync("SessionState", roster, adminUsername);

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
                    UserWishlists.TryRemove(Context.ConnectionId, out _);
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
                    await PromoteNextAdminAsync(sessionCode, session);
                }

                if (session.Users.Any())
                {
                    await BroadcastSessionStateAsync(sessionCode, session);
                }
                else
                {
                    Sessions.TryRemove(sessionCode, out _);
                    Admins.TryRemove(sessionCode, out _);
                }
            }
        }

        // 3. Validate game list intersection in StartSession
        public async Task StartSession(string sessionCode, bool includeWishlist, int minOwners, int minWishlisted)
        {
            if (!Sessions.TryGetValue(sessionCode, out Session? session))
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Error", "Session does not exist");
                return;
            }

            if (!ConnectionUserMapping.TryGetValue(Context.ConnectionId, out string? username))
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Error", "User is not part of the session");
                return;
            }

            if (!Admins.TryGetValue(sessionCode, out string? adminUsername) ||
                !string.Equals(username, adminUsername, StringComparison.Ordinal))
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Error", "Only the session admin can start the game");
                return;
            }

            if (session.Users.Count < 1)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("Error", "Not enough participants to start the session.");
                return;
            }

            // Build counts
            var ownerCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var wishCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var userId in session.Users)
            {
                if (UserGames.TryGetValue(userId, out var owned))
                {
                    foreach (var g in owned)
                    {
                        ownerCounts[g] = ownerCounts.TryGetValue(g, out var c) ? c + 1 : 1;
                    }
                }
                if (includeWishlist && UserWishlists.TryGetValue(userId, out var wished))
                {
                    foreach (var g in wished)
                    {
                        wishCounts[g] = wishCounts.TryGetValue(g, out var c) ? c + 1 : 1;
                    }
                }
            }

            // Union of all candidates (owners and optionally wishlists)
            var candidates = new HashSet<string>(ownerCounts.Keys, StringComparer.Ordinal);
            if (includeWishlist)
            {
                foreach (var g in wishCounts.Keys) candidates.Add(g);
            }

            bool ownersActive = minOwners > 0;
            bool wishlistActive = includeWishlist && minWishlisted > 0;

            IEnumerable<string> filtered = candidates.Where(g =>
            {
                bool ownersOk = ownersActive ? (ownerCounts.TryGetValue(g, out var oc) && oc >= minOwners) : false;
                bool wishlistOk = wishlistActive ? (wishCounts.TryGetValue(g, out var wc) && wc >= minWishlisted) : false;

                if (ownersActive && wishlistActive)
                    return ownersOk || wishlistOk; // either condition is enough
                if (ownersActive)
                    return ownersOk; // only owners threshold applies
                if (wishlistActive)
                    return wishlistOk; // only wishlist threshold applies

                // No thresholds active: default to intersection of owned games by all users
                if (session.Users.All(id => UserGames.TryGetValue(id, out var set) && set.Contains(g)))
                    return true;
                return false;
            });

            // Randomize order for fairness
            var randomized = filtered.OrderBy(_ => Guid.NewGuid()).ToHashSet(StringComparer.Ordinal);

            session.CommonGames = randomized;
            await Clients.Group(sessionCode).SendAsync("SessionStarted", randomized);
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
                int totalParticipants = Math.Max(
                    session.Users.Count,
                    session.GameSwipes
                        .SelectMany(swipes => swipes.Value.Keys)
                        .Distinct()
                        .Count());

                var results = session.GameSwipes
                    .Select(kvp =>
                    {
                        int likes = kvp.Value.Values.Count(swiped => swiped);
                        return new MatchedGameSummary(kvp.Key, likes, totalParticipants);
                    })
                    .Where(summary => summary.Likes > 0)
                    .OrderByDescending(summary => summary.Likes)
                    .ThenBy(summary => summary.Id, StringComparer.Ordinal)
                    .ToList();

                await Clients.Group(sessionCode).SendAsync("SessionEnded", results);
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

        private static (List<string> Roster, string? AdminUsername) BuildSessionSnapshot(string sessionCode, Session session)
        {
            var roster = new List<string>();
            var staleConnections = new List<string>();

            foreach (var connectionId in session.Users)
            {
                if (ConnectionUserMapping.TryGetValue(connectionId, out string username))
                {
                    roster.Add(username);
                }
                else
                {
                    staleConnections.Add(connectionId);
                }
            }

            if (staleConnections.Count > 0)
            {
                foreach (var stale in staleConnections)
                {
                    session.Users.Remove(stale);
                }
            }

            string? adminUsername = null;
            if (Admins.TryGetValue(sessionCode, out string storedAdmin))
            {
                if (roster.Contains(storedAdmin))
                {
                    adminUsername = storedAdmin;
                }
                else
                {
                    Admins.TryRemove(sessionCode, out _);
                }
            }

            return (roster, adminUsername);
        }

        private async Task BroadcastSessionStateAsync(string sessionCode, Session session)
        {
            var (roster, adminUsername) = BuildSessionSnapshot(sessionCode, session);
            await Clients.Group(sessionCode).SendAsync("SessionState", roster, adminUsername);
        }

        private async Task PromoteNextAdminAsync(string sessionCode, Session session)
        {
            string? newAdminConn = session.Users.FirstOrDefault(conn => ConnectionUserMapping.ContainsKey(conn));
            if (newAdminConn == null)
            {
                Admins.TryRemove(sessionCode, out _);
                return;
            }

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

    public record MatchedGameSummary(string Id, int Likes, int TotalParticipants);
}