using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GameFinder.Controls
{
    public partial class SessionLobby : UserControl, INotifyPropertyChanged
    {
        private readonly ObservableCollection<string> _users = new();
        private readonly ObservableCollection<string> _admins = new();
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
            
            SetCurrentUser(Config.Username);
            App.Api.UserJoinedSession += AddUser;
            App.Api.UserLeftSession += RemoveUser;
            App.Api.SessionStarted += OnSessionStarted;
        }

        // Method for setting the current user
        public void SetCurrentUser(string username, bool isAdmin = false)
        {
            _currentUser = username;
            IsAdmin = isAdmin;
        }

        // Method for adding a new user
        public void AddUser(string username, bool admin = false)
        {
            if (username == _currentUser)
            {
                SetCurrentUser(username, admin);
            }

            if (!_users.Contains(username))
            {
                Dispatcher.Invoke(() => _users.Add(username));
            }

            if (admin && !_admins.Contains(username))
            {
                Dispatcher.Invoke(() => _admins.Add(username));
            }
        }

        // Method for removing a user
        public void RemoveUser(string username)
        {
            if (_users.Contains(username))
            {
                Dispatcher.Invoke(() => _users.Remove(username));
            }

            if (_admins.Contains(username))
            {
                Dispatcher.Invoke(() => _admins.Remove(username));
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
