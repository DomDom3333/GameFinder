using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia;
using ArcadeMatch.Avalonia;
using ArcadeMatch.Avalonia.Shared;

namespace ArcadeMatch.Avalonia.Controls;

public partial class SessionLobby : UserControl, INotifyPropertyChanged
{
    private class UserEntry : INotifyPropertyChanged
    {
        public string Name { get; }
        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                if (_isAdmin != value)
                {
                    _isAdmin = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                }
            }
        }
        private bool _isCurrent;
        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                if (_isCurrent != value)
                {
                    _isCurrent = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
                }
            }
        }
        public string DisplayName => $"{Name}{(IsCurrent ? " (You)" : string.Empty)}{(IsAdmin ? " (Admin)" : string.Empty)}";
        public event PropertyChangedEventHandler? PropertyChanged;
        public UserEntry(string name, bool admin, bool current)
        {
            Name = name;
            _isAdmin = admin;
            _isCurrent = current;
        }
    }

    private readonly ObservableCollection<UserEntry> _users = new();
    private bool _isAdmin;
    private string _currentUser = string.Empty;
    private string _sessionId = string.Empty;
    private int _lobbyUserCount = 1;
    private bool _includeWishlist;
    private int _minOwners = 0;
    private int _minWishlisted = 0;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? StartButtonClicked;

    public int LobbyUserCount
    {
        get => _lobbyUserCount;
        private set
        {
            if (_lobbyUserCount != value)
            {
                _lobbyUserCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LobbyUserCount)));
            }
        }
    }

    public int MinOwners
    {
        get => _minOwners;
        set
        {
            if (_minOwners != value)
            {
                _minOwners = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinOwners)));
            }
        }
    }

    public int MinWishlisted
    {
        get => _minWishlisted;
        set
        {
            if (_minWishlisted != value)
            {
                _minWishlisted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinWishlisted)));
            }
        }
    }

    public bool IncludeWishlist
    {
        get => _includeWishlist;
        set
        {
            if (_includeWishlist != value)
            {
                _includeWishlist = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IncludeWishlist)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWishlistSliderEnabled)));
            }
        }
    }

    public bool IsWishlistSliderEnabled => _isAdmin && _includeWishlist;

    public string SessionId
    {
        get => _sessionId;
        private set
        {
            if (_sessionId != value)
            {
                _sessionId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SessionId)));
                UpdateSessionCode();
            }
        }
    }

    public bool IsAdmin
    {
        get => _isAdmin;
        private set
        {
            if (_isAdmin != value)
            {
                _isAdmin = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAdmin)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWishlistSliderEnabled)));
            }
        }
    }

    public SessionLobby()
    {
        InitializeComponent();
        DataContext = this;
        UsersListBox.ItemsSource = _users;
        SessionId = App.Api.SessionId;
        UpdateSessionCode();
        SetCurrentUser(Config.Username, App.Api.IsCurrentUserAdmin);
        App.Api.UserJoinedSession += AddUser;
        App.Api.UserLeftSession += RemoveUser;
        App.Api.SessionStarted += OnSessionStarted;
        App.Api.SessionStateReceived += OnSessionStateReceived;

        if (App.Api.SessionRoster.Count > 0)
            ApplyRoster(App.Api.SessionRoster, App.Api.CurrentAdminUser);
    }

    public void SetCurrentUser(string username, bool isAdmin = false)
    {
        _currentUser = username;
        IsAdmin = isAdmin;
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var user in _users)
            {
                bool isCurrent = user.Name == username;
                user.IsCurrent = isCurrent;
                if (isCurrent)
                    user.IsAdmin = isAdmin;
            }

            var existing = _users.FirstOrDefault(u => u.Name == username);
            if (existing != null && _users.IndexOf(existing) != 0)
            {
                _users.Remove(existing);
                _users.Insert(0, existing);
            }
        });
    }

    public void AddUser(string username, bool admin = false)
    {
        if (username == _currentUser)
        {
            SetCurrentUser(username, admin);
            return;
        }
        var existing = _users.FirstOrDefault(u => u.Name == username);
        if (existing == null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _users.Add(new UserEntry(username, admin, false));
                LobbyUserCount = _users.Count;
            });
        }
        else
        {
            existing.IsAdmin = admin;
        }
    }

    public void RemoveUser(string username)
    {
        var entry = _users.FirstOrDefault(u => u.Name == username);
        if (entry != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _users.Remove(entry);
                LobbyUserCount = _users.Count;
            });
        }
    }

    async void LeaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentUser))
        {
            await App.Api.LeaveSessionAsync(_currentUser);
            StartButtonClicked?.Invoke(this, "LeaveButton");
        }
    }

    async void StartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        int minOwners = MinOwners;
        int minWishlisted = MinWishlisted;

        await App.Api.StartSession(App.Api.SessionId, IncludeWishlist, minOwners, minWishlisted);
        StartButtonClicked?.Invoke(this, "StartButton");
    }

    async void CopyCode_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            try
            {
                await clipboard.SetTextAsync(SessionId);
            }
            catch
            {
                // ignore clipboard errors
            }
        }
    }

    void OnSessionStarted(IEnumerable<string> _)
    {
        Dispatcher.UIThread.Post(() => StartButtonClicked?.Invoke(this, "StartButton"));
    }

    void UpdateSessionCode()
    {
        if (SessionCodeText != null)
            SessionCodeText.Text = $"Session Code: {SessionId}";
    }

    void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    void OnSessionStateReceived(IReadOnlyList<string> users, string? adminName)
    {
        ApplyRoster(users, adminName);
    }

    void ApplyRoster(IReadOnlyList<string> users, string? adminName)
    {
        bool currentIsAdmin = string.Equals(adminName, _currentUser, StringComparison.Ordinal);

        var entries = new List<(string Name, bool IsAdmin, bool IsCurrent)>();
        foreach (var user in users ?? Array.Empty<string>())
        {
            bool isCurrent = string.Equals(user, _currentUser, StringComparison.Ordinal);
            bool isAdmin = string.Equals(user, adminName, StringComparison.Ordinal);
            entries.Add((user, isAdmin, isCurrent));
        }

        var ordered = entries
            .OrderByDescending(entry => entry.IsCurrent)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Dispatcher.UIThread.Post(() =>
        {
            _users.Clear();
            foreach (var (name, isAdmin, isCurrent) in ordered)
                _users.Add(new UserEntry(name, isAdmin, isCurrent));

            if (!string.IsNullOrEmpty(_currentUser) && _users.All(u => u.Name != _currentUser))
                _users.Insert(0, new UserEntry(_currentUser, currentIsAdmin, true));
            
            LobbyUserCount = _users.Count;
        });

        IsAdmin = currentIsAdmin;
    }
}
