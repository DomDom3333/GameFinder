﻿using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GameFinder.Controls
{
    public partial class SessionLobby : UserControl
    {
        private ObservableCollection<string> _users = new ObservableCollection<string>();
        private ObservableCollection<string> _admins = new ObservableCollection<string>();
        private bool _isAdmin = false;
        private string _currentUser;
        public event EventHandler<string> StartButtonClicked;


        public SessionLobby()
        {
            InitializeComponent();
            DataContext = this;
            _users = new ObservableCollection<string>();
            UsersListBox.ItemsSource = _users;
            
            SetCurrentUser(Config.Username);
            AddUser(Config.Username);
            App.Api.UserJoinedSession += AddUser;
            App.Api.UserLeftSession += RemoveUser;
        }

        // Method for setting the current user
        public void SetCurrentUser(string username, bool isAdmin = false)
        {
            _currentUser = username;
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
                Dispatcher.Invoke(() => _users.Remove(username));
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
    }
}