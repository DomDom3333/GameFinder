using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ArcadeMatch.Avalonia.Commands;
using ArcadeMatch.Avalonia.Services;
using ArcadeMatch.Avalonia.ViewModels;
using Avalonia.Threading;
using GameFinder.Objects;

namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class FriendsTabViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly FriendsService _friendsService;
    private readonly IUserConfigStore _configStore;
    private readonly ISessionApi _sessionApi;

    private readonly ObservableCollection<SteamFriend> _friends = new();
    private string? _statusMessage;
    private bool _isLoading;

    public FriendsTabViewModel(FriendsService friendsService, IUserConfigStore configStore, ISessionApi sessionApi)
    {
        _friendsService = friendsService;
        _configStore = configStore;
        _sessionApi = sessionApi;

        Friends = new ReadOnlyObservableCollection<SteamFriend>(_friends);

        RefreshCommand = new AsyncCommand(RefreshAsync);
        InviteCommand = new AsyncCommand(
            parameter => InviteFriendAsync(parameter as SteamFriend),
            parameter => parameter is SteamFriend);
        JoinCommand = new AsyncCommand(
            parameter => JoinFriendAsync(parameter as SteamFriend),
            parameter => parameter is SteamFriend friend && friend.InSession);

        _sessionApi.InviteReceived += OnInviteReceived;
        _sessionApi.ErrorOccurred += OnErrorOccurred;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MessageRequestedEventArgs>? MessageRequested;

    public ReadOnlyObservableCollection<SteamFriend> Friends { get; }

    public ICommand RefreshCommand { get; }

    public ICommand InviteCommand { get; }

    public ICommand JoinCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (string.Equals(_statusMessage, value, StringComparison.Ordinal))
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public void Dispose()
    {
        _sessionApi.InviteReceived -= OnInviteReceived;
        _sessionApi.ErrorOccurred -= OnErrorOccurred;
    }

    public async Task InitializeAsync()
    {
        if (_friends.Count == 0)
        {
            await RefreshAsync().ConfigureAwait(false);
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsLoading = true;
                StatusMessage = null;
            });

            IReadOnlyList<SteamFriend> friends = await _friendsService.GetFriendsAsync().ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateFriends(friends);
                if (friends.Count == 0)
                {
                    StatusMessage = "No friends found. Confirm your Steam account privacy settings.";
                }
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusMessage = ex.Message);
            MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Friends", ex.Message));
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    private void UpdateFriends(IReadOnlyList<SteamFriend> friends)
    {
        _friends.Clear();
        foreach (SteamFriend friend in friends)
        {
            _friends.Add(friend);
        }

        if (JoinCommand is AsyncCommand joinCommand)
        {
            joinCommand.RaiseCanExecuteChanged();
        }

        if (InviteCommand is AsyncCommand inviteCommand)
        {
            inviteCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task InviteFriendAsync(SteamFriend? friend)
    {
        if (friend == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_sessionApi.SessionId))
        {
            MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Invite", "Start or join a session before sending invites."));
            return;
        }

        try
        {
            await _sessionApi.InviteFriendAsync(friend.SteamId).ConfigureAwait(false);
            MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Invite", $"Invite sent to {friend.PersonaName}."));
        }
        catch (Exception ex)
        {
            MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Invite", ex.Message));
        }
    }

    private async Task JoinFriendAsync(SteamFriend? friend)
    {
        if (friend == null || string.IsNullOrWhiteSpace(friend.SessionCode))
        {
            return;
        }

        string? username = _configStore.Username;
        if (string.IsNullOrWhiteSpace(username))
        {
            username = _configStore.UserProfile?.SteamId ?? _configStore.SteamId;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Join", "Set a display name on the Session tab before joining."));
            return;
        }

        if (!string.IsNullOrWhiteSpace(_sessionApi.SessionId) &&
            !string.Equals(_sessionApi.SessionId, friend.SessionCode, StringComparison.Ordinal))
        {
            await _sessionApi.LeaveSessionAsync(username).ConfigureAwait(false);
        }

        try
        {
            await _sessionApi.JoinSessionAsync(
                friend.SessionCode,
                username,
                _configStore.GameList,
                _configStore.WishlistGames,
                _configStore.SteamId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Join", ex.Message));
        }
    }

    private void OnInviteReceived(string sessionCode, string inviter)
    {
        MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Invite Received", $"{inviter} invited you to join session {sessionCode}."));
    }

    private void OnErrorOccurred(string error)
    {
        MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Error", error));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
