using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ArcadeMatch.Avalonia.Commands;
using ArcadeMatch.Avalonia.Services;
using Avalonia.Threading;

namespace ArcadeMatch.Avalonia.ViewModels.Sessions;

public class SessionLobbyViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISessionApi _sessionApi;
    private readonly IUserConfigStore _userConfig;

    private readonly ObservableCollection<UserEntryViewModel> _users = new();

    private string _currentUser;
    private string _sessionId;

    public SessionLobbyViewModel(ISessionApi sessionApi, IUserConfigStore userConfig)
    {
        _sessionApi = sessionApi;
        _userConfig = userConfig;

        _currentUser = _userConfig.Username;
        _sessionId = _sessionApi.SessionId;
        Users = new ReadOnlyObservableCollection<UserEntryViewModel>(_users);

        LeaveCommand = new AsyncCommand(LeaveSessionAsync);
        StartCommand = new AsyncCommand(StartSessionAsync, () => IsAdmin && !string.IsNullOrWhiteSpace(SessionId));
        CopyCodeCommand = new RelayCommand(_ => OnCopyCodeRequested(), _ => !string.IsNullOrWhiteSpace(SessionId));

        _sessionApi.SessionCreated += OnSessionCreated;
        _sessionApi.UserJoinedSession += OnUserJoinedSession;
        _sessionApi.UserLeftSession += OnUserLeftSession;
        _sessionApi.SessionStarted += OnSessionStarted;
        _sessionApi.SessionStateReceived += OnSessionStateReceived;
        _sessionApi.ErrorOccurred += OnErrorOccurred;

        if (_sessionApi.SessionRoster.Count > 0)
        {
            ApplyRoster(_sessionApi.SessionRoster, _sessionApi.CurrentAdminUser);
        }
        else if (!string.IsNullOrWhiteSpace(_currentUser))
        {
            AddOrUpdateUser(_currentUser, _sessionApi.IsCurrentUserAdmin, true);
        }

        LobbyUserCount = _users.Count > 0 ? _users.Count : 1;
        IsAdmin = _sessionApi.IsCurrentUserAdmin;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MessageRequestedEventArgs>? MessageRequested;
    public event EventHandler<SessionNavigationEventArgs>? NavigationRequested;
    public event EventHandler<string>? CopyCodeRequested;

    public ICommand LeaveCommand { get; }

    public ICommand StartCommand { get; }

    public ICommand CopyCodeCommand { get; }

    public ReadOnlyObservableCollection<UserEntryViewModel> Users { get; }

    public int LobbyUserCount
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = Math.Max(1, value);
            OnPropertyChanged();
            (StartCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }
    }

    public int MinOwners
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    public int MinWishlisted
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    public bool IncludeWishlist
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWishlistSliderEnabled));
        }
    }

    public bool IsWishlistSliderEnabled => IsAdmin && IncludeWishlist;

    public string SessionId
    {
        get => _sessionId;
        private set
        {
            if (_sessionId == value)
            {
                return;
            }

            _sessionId = value;
            OnPropertyChanged();
            (CopyCodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StartCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool IsAdmin
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWishlistSliderEnabled));
            (StartCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }
    }

    public void Activate()
    {
        _currentUser = string.IsNullOrWhiteSpace(_userConfig.Username) ? _currentUser : _userConfig.Username;
        SessionId = _sessionApi.SessionId;

        if (_sessionApi.SessionRoster.Count > 0)
        {
            ApplyRoster(_sessionApi.SessionRoster, _sessionApi.CurrentAdminUser);
        }
    }

    public void Dispose()
    {
        _sessionApi.SessionCreated -= OnSessionCreated;
        _sessionApi.UserJoinedSession -= OnUserJoinedSession;
        _sessionApi.UserLeftSession -= OnUserLeftSession;
        _sessionApi.SessionStarted -= OnSessionStarted;
        _sessionApi.SessionStateReceived -= OnSessionStateReceived;
        _sessionApi.ErrorOccurred -= OnErrorOccurred;
    }

    private async Task LeaveSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentUser))
        {
            _currentUser = _userConfig.Username;
        }

        if (!string.IsNullOrWhiteSpace(_currentUser))
        {
            await _sessionApi.LeaveSessionAsync(_currentUser).ConfigureAwait(false);
        }

        NavigationRequested?.Invoke(this, new SessionNavigationEventArgs(SessionViewType.Start));
    }

    private async Task StartSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            return;
        }

        await _sessionApi.StartSession(SessionId, IncludeWishlist, MinOwners, MinWishlisted).ConfigureAwait(false);
    }

    private void OnCopyCodeRequested()
    {
        if (!string.IsNullOrWhiteSpace(SessionId))
        {
            CopyCodeRequested?.Invoke(this, SessionId);
        }
    }

    private void OnSessionCreated(string sessionCode)
    {
        SessionId = sessionCode;
    }

    private void OnUserJoinedSession(string username, bool isAdmin)
    {
        AddOrUpdateUser(username, isAdmin, string.Equals(username, _currentUser, StringComparison.Ordinal));
    }

    private void OnUserLeftSession(string username)
    {
        var entry = _users.FirstOrDefault(user => string.Equals(user.Name, username, StringComparison.Ordinal));
        if (entry == null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            _users.Remove(entry);
            LobbyUserCount = _users.Count;
        });
    }

    private void OnSessionStarted(IEnumerable<string> _)
    {
        NavigationRequested?.Invoke(this, new SessionNavigationEventArgs(SessionViewType.Swiping));
    }

    private void OnSessionStateReceived(IReadOnlyList<string> users, string? adminName)
    {
        ApplyRoster(users, adminName);
    }

    private void OnErrorOccurred(string message)
    {
        MessageRequested?.Invoke(this, new MessageRequestedEventArgs("Error", message));
    }

    private void ApplyRoster(IReadOnlyList<string>? users, string? adminName)
    {
        bool currentIsAdmin = string.Equals(adminName, _currentUser, StringComparison.Ordinal);

        var entries = new List<(string Name, bool IsAdmin, bool IsCurrent)>();
        foreach (string user in users ?? [])
        {
            bool isCurrent = string.Equals(user, _currentUser, StringComparison.Ordinal);
            bool isAdmin = string.Equals(user, adminName, StringComparison.Ordinal);
            entries.Add((user, isAdmin, isCurrent));
        }

        Dispatcher.UIThread.Post(() =>
        {
            _users.Clear();
            foreach ((string name, bool isAdmin, bool isCurrent) in entries
                         .OrderByDescending(entry => entry.IsCurrent)
                         .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
            {
                _users.Add(new UserEntryViewModel(name, isAdmin, isCurrent));
            }

            if (!string.IsNullOrWhiteSpace(_currentUser) && _users.All(u => u.Name != _currentUser))
            {
                _users.Insert(0, new UserEntryViewModel(_currentUser, currentIsAdmin, true));
            }

            LobbyUserCount = _users.Count;
        });

        IsAdmin = currentIsAdmin;
    }

    private void AddOrUpdateUser(string username, bool isAdmin, bool isCurrent)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var existing = _users.FirstOrDefault(user => string.Equals(user.Name, username, StringComparison.Ordinal));
            if (existing == null)
            {
                var entry = new UserEntryViewModel(username, isAdmin, isCurrent);
                if (isCurrent)
                {
                    _users.Insert(0, entry);
                }
                else
                {
                    _users.Add(entry);
                }
            }
            else
            {
                existing.IsAdmin = isAdmin;
                existing.IsCurrent = isCurrent;
            }

            LobbyUserCount = _users.Count;
        });

        if (isCurrent)
        {
            _currentUser = username;
            IsAdmin = isAdmin;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
