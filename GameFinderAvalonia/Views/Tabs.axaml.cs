using Avalonia.Controls;
using System;

namespace GameFinderAvalonia.Views;

public partial class Tabs : UserControl
{
    public Tabs()
    {
        InitializeComponent();
        ShowSessionStart();
        App.Api.SessionEnded += game => ShowResults(game);
    }

    public void ShowSessionStart()
    {
        var start = new SessionStart();
        start.SessionAction += (_, _) => ShowSessionLobby();
        SessionContent.Content = start;
    }

    public void ShowSessionLobby()
    {
        var lobby = new SessionLobby();
        lobby.StartClicked += (_, _) => ShowSwiping();
        SessionContent.Content = lobby;
    }

    public void ShowSwiping()
    {
        SessionContent.Content = new Swiping();
    }

    public void ShowResults(string? gameId)
    {
        var result = new MatchResult(gameId);
        result.BackClicked += (_, _) => ShowSessionStart();
        SessionContent.Content = result;
    }
}
