using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GameFinder.Controls
{
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
                        OnPropertyChanged(nameof(DisplayName));
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
                        OnPropertyChanged(nameof(DisplayName));
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

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                    OnPropertyChanged(nameof(SessionId));
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
                    OnPropertyChanged(nameof(IsAdmin));
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

        // Method for setting the current user
        public void SetCurrentUser(string username, bool isAdmin = false)
        {
            _currentUser = username;
            IsAdmin = isAdmin;

            var existing = _users.FirstOrDefault(u => u.Name == username);
            if (existing == null)
            {
                Dispatcher.Invoke(() => _users.Insert(0, new UserEntry(username, isAdmin, true)));
            }
            else
            {
                existing.IsAdmin = isAdmin;
                existing.IsCurrent = true;
                if (_users.IndexOf(existing) != 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _users.Remove(existing);
                        _users.Insert(0, existing);
                    });
                }
            }
        }

        // Method for adding a new user
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
                Dispatcher.Invoke(() => _users.Add(new UserEntry(username, admin, false)));
            }
            else
            {
                existing.IsAdmin = admin;
            }
        }

        // Method for removing a user
        public void RemoveUser(string username)
        {
            var entry = _users.FirstOrDefault(u => u.Name == username);
            if (entry != null)
            {
                Dispatcher.Invoke(() => _users.Remove(entry));
            }
        }

        private async void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentUser))
            {
                await App.Api.LeaveSessionAsync(_currentUser);
                StartButtonClicked?.Invoke(this, "LeaveButton");
            }
        }

        private async void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            await App.Api.StartSession(App.Api.SessionId);
            StartButtonClicked?.Invoke(this, "StartButton");
        }

        private void OnSessionStarted(IEnumerable<string> _)
        {
            Dispatcher.Invoke(() => StartButtonClicked?.Invoke(this, "StartButton"));
        }

        private void SessionLobby_Unloaded(object sender, RoutedEventArgs e)
        {
            App.Api.UserJoinedSession -= AddUser;
            App.Api.UserLeftSession -= RemoveUser;
            App.Api.SessionStarted -= OnSessionStarted;
        }

        private void CopyCode_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(SessionId);
            }
            catch (Exception)
            {
                // ignore clipboard errors
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
