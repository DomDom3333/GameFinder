using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcadeMatch.Avalonia.Commands;
using ArcadeMatch.Avalonia.Services;
using ArcadeMatch.Avalonia.ViewModels;

namespace ArcadeMatch.Avalonia.ViewModels.Sessions;

public class SessionStartViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ISessionApi _sessionApi;
    private readonly IUserConfigStore _userConfig;

    private string _displayName;
    private string _sessionCode = string.Empty;

    public SessionStartViewModel(ISessionApi sessionApi, IUserConfigStore userConfig)
    {
        _sessionApi = sessionApi;
        _userConfig = userConfig;

        _displayName = string.IsNullOrWhiteSpace(_userConfig.Username)
            ? _userConfig.UserProfile?.SteamId ?? string.Empty
            : _userConfig.Username;

        _userConfig.PropertyChanged += OnUserConfigPropertyChanged;

        StartNewSessionCommand = new AsyncCommand(() => ExecuteStartNewSessionAsync());
        JoinSessionCommand = new AsyncCommand(() => ExecuteJoinSessionAsync());
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MessageRequestedEventArgs>? MessageRequested;
    public event EventHandler<SessionNavigationEventArgs>? NavigationRequested;

    public ICommand StartNewSessionCommand { get; }

    public ICommand JoinSessionCommand { get; }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName == value)
            {
                return;
            }

            _displayName = value ?? string.Empty;
            _userConfig.Username = _displayName;
            OnPropertyChanged();
        }
    }

    public string SessionCode
    {
        get => _sessionCode;
        set
        {
            if (_sessionCode == value)
            {
                return;
            }

            _sessionCode = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public async Task ExecuteStartNewSessionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            OnMessageRequested("Error", "Please enter a valid Username");
            return;
        }

        await _sessionApi.CreateSessionAsync().ConfigureAwait(false);
        while (string.IsNullOrWhiteSpace(_sessionApi.SessionId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        }

        await _sessionApi.JoinSessionAsync(
            _sessionApi.SessionId!,
            DisplayName,
            _userConfig.GameList,
            _userConfig.WishlistGames).ConfigureAwait(false);

        OnNavigationRequested(SessionViewType.Lobby);
    }

    public async Task ExecuteJoinSessionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SessionCode) || SessionCode.Length != 4)
        {
            OnMessageRequested("Error", "Please enter a valid Session Code");
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            OnMessageRequested("Error", "Please enter a valid Username");
            return;
        }

        await _sessionApi.JoinSessionAsync(
            SessionCode,
            DisplayName,
            _userConfig.GameList,
            _userConfig.WishlistGames).ConfigureAwait(false);

        OnNavigationRequested(SessionViewType.Lobby);
    }

    public void Dispose()
    {
        _userConfig.PropertyChanged -= OnUserConfigPropertyChanged;
    }

    private void OnUserConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IUserConfigStore.Username))
        {
            if (!string.IsNullOrWhiteSpace(_userConfig.Username) && string.IsNullOrWhiteSpace(_displayName))
            {
                _displayName = _userConfig.Username;
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnMessageRequested(string title, string message)
    {
        MessageRequested?.Invoke(this, new MessageRequestedEventArgs(title, message));
    }

    private void OnNavigationRequested(SessionViewType destination, object? parameter = null)
    {
        NavigationRequested?.Invoke(this, new SessionNavigationEventArgs(destination, parameter));
    }
}
