using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ArcadeMatch.Avalonia.Services;
using ArcadeMatch.Avalonia.Shared;
using ArcadeMatch.Avalonia.ViewModels;
using ArcadeMatch.Avalonia.ViewModels.Sessions;
using Avalonia.Threading;

namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class TabsViewModel : INotifyPropertyChanged
{
    private readonly SessionStartViewModel _sessionStart;
    private readonly SessionLobbyViewModel _sessionLobby;
    private readonly SwipingViewModel _swiping;
    private readonly MatchResultViewModel _matchResult;

    private object? _selectedSession;

    public TabsViewModel(ISteamGameService steamGameService, IUserConfigStore userConfig, ISessionApi sessionApi, ApiSettings settings)
    {
        Home = new HomeTabViewModel(steamGameService, userConfig);
        _sessionStart = new SessionStartViewModel(sessionApi, userConfig);
        _sessionLobby = new SessionLobbyViewModel(sessionApi, userConfig);
        _swiping = new SwipingViewModel(sessionApi, userConfig, settings);
        _matchResult = new MatchResultViewModel();

        SubscribeToMessages(_sessionStart, _sessionLobby, _swiping);
        _matchResult.NavigationRequested += OnNavigationRequested;

        _sessionStart.NavigationRequested += OnNavigationRequested;
        _sessionLobby.NavigationRequested += OnNavigationRequested;
        _swiping.NavigationRequested += OnNavigationRequested;

        SelectedSession = _sessionStart;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MessageRequestedEventArgs>? MessageRequested;

    public HomeTabViewModel Home { get; }

    public SessionStartViewModel SessionStart => _sessionStart;
    public SessionLobbyViewModel SessionLobby => _sessionLobby;
    public SwipingViewModel Swiping => _swiping;
    public MatchResultViewModel MatchResult => _matchResult;

    public object? SelectedSession
    {
        get => _selectedSession;
        private set
        {
            if (ReferenceEquals(_selectedSession, value))
            {
                return;
            }

            _selectedSession = value;
            OnPropertyChanged();
        }
    }

    public void UpdateSteamStatusFromSession(bool isConnected) => Home.UpdateSteamStatusFromSession(isConnected);

    public async Task InitializeAsync()
    {
        await Home.InitializeAsync().ConfigureAwait(false);
    }

    private void OnNavigationRequested(object? sender, SessionNavigationEventArgs e)
    {
        Dispatcher.UIThread.Post(() => HandleNavigation(e));
    }

    private void HandleNavigation(SessionNavigationEventArgs e)
    {
        switch (e.Destination)
        {
            case SessionViewType.Start:
                SelectedSession = _sessionStart;
                break;
            case SessionViewType.Lobby:
                _sessionLobby.Activate();
                SelectedSession = _sessionLobby;
                break;
            case SessionViewType.Swiping:
                _ = _swiping.InitializeAsync();
                SelectedSession = _swiping;
                break;
            case SessionViewType.Results:
                if (e.Parameter is IReadOnlyList<GameFinder.Objects.MatchedGame> matches)
                {
                    _ = _matchResult.LoadMatchesAsync(matches);
                }
                SelectedSession = _matchResult;
                break;
        }
    }

    private void SubscribeToMessages(params object[] viewModels)
    {
        foreach (var vm in viewModels)
        {
            if (vm is SessionStartViewModel start)
            {
                start.MessageRequested += (_, e) => MessageRequested?.Invoke(this, e);
            }
            else if (vm is SessionLobbyViewModel lobby)
            {
                lobby.MessageRequested += (_, e) => MessageRequested?.Invoke(this, e);
            }
            else if (vm is SwipingViewModel swiping)
            {
                swiping.MessageRequested += (_, e) => MessageRequested?.Invoke(this, e);
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
