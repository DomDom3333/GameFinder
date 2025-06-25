using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MessageBox.Avalonia;
using ArcadeMatch.Avalonia;

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

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? StartButtonClicked;

    public string SessionId
    {
        get => _sessionId;
        private set
        {
            if (_sessionId != value)
            {
                _sessionId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SessionId)));
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
            }
        }
    }

    public SessionLobby()
    {
        InitializeComponent();
        DataContext = this;
        UsersListBox.ItemsSource = _users;
        SessionId = App.Api.SessionId;
        SetCurrentUser(Config.Username, App.Api.IsCurrentUserAdmin);
        App.Api.UserJoinedSession += AddUser;
        App.Api.UserLeftSession += RemoveUser;
        App.Api.SessionStarted += OnSessionStarted;
    }

    public void SetCurrentUser(string username, bool isAdmin = false)
    {
        _currentUser = username;
        IsAdmin = isAdmin;
        var existing = _users.FirstOrDefault(u => u.Name == username);
        if (existing == null)
        {
            Dispatcher.UIThread.Post(() => _users.Insert(0, new UserEntry(username, isAdmin, true)));
        }
        else
        {
            existing.IsAdmin = isAdmin;
            existing.IsCurrent = true;
            if (_users.IndexOf(existing) != 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _users.Remove(existing);
                    _users.Insert(0, existing);
                });
            }
        }
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
            Dispatcher.UIThread.Post(() => _users.Add(new UserEntry(username, admin, false)));
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
            Dispatcher.UIThread.Post(() => _users.Remove(entry));
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
        await App.Api.StartSession(App.Api.SessionId);
        StartButtonClicked?.Invoke(this, "StartButton");
    }

    void OnSessionStarted(IEnumerable<string> _)
    {
        Dispatcher.UIThread.Post(() => StartButtonClicked?.Invoke(this, "StartButton"));
    }

    void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
