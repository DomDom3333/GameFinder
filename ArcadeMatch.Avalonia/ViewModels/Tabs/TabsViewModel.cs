using System.ComponentModel;
using System.Runtime.CompilerServices;
using ArcadeMatch.Avalonia.Services;
using ArcadeMatch.Avalonia.Shared;
using ArcadeMatch.Avalonia.ViewModels.Sessions;
using Avalonia.Threading;

namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class TabsViewModel : INotifyPropertyChanged
{
    public TabsViewModel(
        ISteamGameService steamGameService,
        IUserConfigStore userConfig,
        ISessionApi sessionApi,
        ApiSettings settings,
        FriendsService friendsService)
    {
        Home = new HomeTabViewModel(steamGameService, userConfig);
        Settings = new SettingsTabViewModel(Home);
        Friends = new FriendsTabViewModel(friendsService, userConfig, sessionApi);
        SessionStart = new SessionStartViewModel(sessionApi, userConfig);
        SessionLobby = new SessionLobbyViewModel(sessionApi, userConfig);
        Swiping = new SwipingViewModel(sessionApi, userConfig, settings);
        MatchResult = new MatchResultViewModel();

        SubscribeToMessages(SessionStart, SessionLobby, Swiping);
        Friends.MessageRequested += (_, e) => MessageRequested?.Invoke(this, e);
        MatchResult.NavigationRequested += OnNavigationRequested;

        SessionStart.NavigationRequested += OnNavigationRequested;
        SessionLobby.NavigationRequested += OnNavigationRequested;
        Swiping.NavigationRequested += OnNavigationRequested;

        SelectedSession = SessionStart;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MessageRequestedEventArgs>? MessageRequested;

    public HomeTabViewModel Home { get; }

    public SettingsTabViewModel Settings { get; }

    public FriendsTabViewModel Friends { get; }

    private SessionStartViewModel SessionStart { get; }

    private SessionLobbyViewModel SessionLobby { get; }

    private SwipingViewModel Swiping { get; }

    private MatchResultViewModel MatchResult { get; }

    public object? SelectedSession
    {
        get;
        private set
        {
            if (ReferenceEquals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        await Home.InitializeAsync().ConfigureAwait(false);
        await Friends.InitializeAsync().ConfigureAwait(false);
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
                SelectedSession = SessionStart;
                break;
            case SessionViewType.Lobby:
                SessionLobby.Activate();
                SelectedSession = SessionLobby;
                break;
            case SessionViewType.Swiping:
                _ = Swiping.InitializeAsync();
                SelectedSession = Swiping;
                break;
            case SessionViewType.Results:
                if (e.Parameter is IReadOnlyList<GameFinder.Objects.MatchedGame> matches)
                {
                    _ = MatchResult.LoadMatchesAsync(matches);
                }
                SelectedSession = MatchResult;
                break;
        }
    }

    private void SubscribeToMessages(params object[] viewModels)
    {
        foreach (object vm in viewModels)
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
