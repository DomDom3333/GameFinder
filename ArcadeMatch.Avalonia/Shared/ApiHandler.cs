using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace ArcadeMatch.Avalonia;

public class ApiHandler
{
    public HubConnection? Connection { get; set; }
    public string SessionId { get; private set; } = string.Empty;
    private string _currentUser = string.Empty;
    public bool IsCurrentUserAdmin { get; private set; }

    public event Action<string>? SessionCreated;
    public event Action<string, bool>? UserJoinedSession;
    public event Action<string>? UserLeftSession;
    public event Action<IEnumerable<string>>? SessionStarted;
    public event Action<string, string, bool>? UserSwiped;
    public event Action<string>? GameMatched;
    public event Action<string>? ErrorOccurred;
    public event Action<string?>? SessionEnded;

    public async Task Connect(string[] args)
    {
        string hubUrl = "http://127.0.0.1:5170/matchinghub";
        Connection = new HubConnectionBuilder().WithUrl(hubUrl).Build();
        RegisterEventHandlers(Connection);
        await Connection.StartAsync();
    }

    void RegisterEventHandlers(HubConnection connection)
    {
        connection.On<string>("SessionCreated", sessionCode =>
        {
            SessionId = sessionCode;
            SessionCreated?.Invoke(sessionCode);
        });
        connection.On<string, bool>("JoinedSession", (username, admin) =>
        {
            if (username == _currentUser)
                IsCurrentUserAdmin = admin;
            UserJoinedSession?.Invoke(username, admin);
        });
        connection.On<string>("LeftSession", username => UserLeftSession?.Invoke(username));
        connection.On<IEnumerable<string>>("SessionStarted", commonGames =>
        {
            Config.CommonGames = commonGames.ToList();
            SessionStarted?.Invoke(commonGames);
        });
        connection.On<string, string, bool>("UserSwiped", (userId, game, swipeRight) => UserSwiped?.Invoke(userId, game, swipeRight));
        connection.On<string>("GameMatched", game => GameMatched?.Invoke(game));
        connection.On<string?>("SessionEnded", game => SessionEnded?.Invoke(game));
        connection.On<string>("Error", msg => ErrorOccurred?.Invoke(msg));
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
            await Connection.InvokeAsync("JoinSession", sessionCode, username, gameList);
    }

    public async Task LeaveSessionAsync(string username)
    {
        if (Connection != null)
            await Connection.InvokeAsync("LeaveSession", SessionId, username);
        SessionId = string.Empty;
        _currentUser = string.Empty;
        IsCurrentUserAdmin = false;
        Config.CommonGames.Clear();
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
        if (Connection != null) await Connection.InvokeAsync("EndSession", sessionCode);
        SessionId = string.Empty;
        _currentUser = string.Empty;
        IsCurrentUserAdmin = false;
    }
}
