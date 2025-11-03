using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcadeMatch.Avalonia.Commands;
using ArcadeMatch.Avalonia.Services;
using ArcadeMatch.Avalonia.ViewModels;
using Avalonia.Threading;

namespace ArcadeMatch.Avalonia.ViewModels.Sessions;

public class SessionLobbyViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISessionApi _sessionApi;
    private readonly IUserConfigStore _userConfig;

    private readonly ObservableCollection<UserEntryViewModel> _users = new();
    private readonly ReadOnlyObservableCollection<UserEntryViewModel> _readOnlyUsers;

    private bool _isAdmin;
    private string _currentUser = string.Empty;
    private string _sessionId = string.Empty;
    private int _lobbyUserCount = 1;
    private bool _includeWishlist;
    private int _minOwners;
    private int _minWishlisted;

    public SessionLobbyViewModel(ISessionApi sessionApi, IUserConfigStore userConfig)
    {
        _sessionApi = sessionApi;
        _userConfig = userConfig;

        _currentUser = _userConfig.Username;
        _sessionId = _sessionApi.SessionId;
        _readOnlyUsers = new ReadOnlyObservableCollection<UserEntryViewModel>(_users);

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

    public ReadOnlyObservableCollection<UserEntryViewModel> Users => _readOnlyUsers;

    public int LobbyUserCount
    {
        get => _lobbyUserCount;
        private set
        {
            if (_lobbyUserCount == value)
            {
                return;
            }

            _lobbyUserCount = Math.Max(1, value);
            OnPropertyChanged();
            (StartCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }
    }

    public int MinOwners
    {
        get => _minOwners;
        set
        {
            if (_minOwners == value)
            {
                return;
            }

            _minOwners = value;
            OnPropertyChanged();
        }
    }

    public int MinWishlisted
    {
        get => _minWishlisted;
        set
        {
            if (_minWishlisted == value)
            {
                return;
            }

            _minWishlisted = value;
            OnPropertyChanged();
        }
    }

    public bool IncludeWishlist
    {
        get => _includeWishlist;
        set
        {
            if (_includeWishlist == value)
            {
                return;
            }

            _includeWishlist = value;
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

            _sessionId = value ?? string.Empty;
            OnPropertyChanged();
            (CopyCodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StartCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        }
    }

    public bool IsAdmin
    {
        get => _isAdmin;
        private set
        {
            if (_isAdmin == value)
            {
                return;
            }

            _isAdmin = value;
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

    private void ApplyRoster(IReadOnlyList<string> users, string? adminName)
    {
        var currentIsAdmin = string.Equals(adminName, _currentUser, StringComparison.Ordinal);

        var entries = new List<(string Name, bool IsAdmin, bool IsCurrent)>();
        foreach (var user in users ?? Array.Empty<string>())
        {
            bool isCurrent = string.Equals(user, _currentUser, StringComparison.Ordinal);
            bool isAdmin = string.Equals(user, adminName, StringComparison.Ordinal);
            entries.Add((user, isAdmin, isCurrent));
        }

        Dispatcher.UIThread.Post(() =>
        {
            _users.Clear();
            foreach (var (name, isAdmin, isCurrent) in entries
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

    public sealed class UserEntryViewModel : INotifyPropertyChanged
    {
        private bool _isAdmin;
        private bool _isCurrent;

        public UserEntryViewModel(string name, bool isAdmin, bool isCurrent)
        {
            Name = name;
            _isAdmin = isAdmin;
            _isCurrent = isCurrent;
        }

        public string Name { get; }

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                if (_isAdmin == value)
                {
                    return;
                }

                _isAdmin = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                if (_isCurrent == value)
                {
                    return;
                }

                _isCurrent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string DisplayName => $"{Name}{(IsCurrent ? " (You)" : string.Empty)}{(IsAdmin ? " (Admin)" : string.Empty)}";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
