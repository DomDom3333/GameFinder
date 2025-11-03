using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ArcadeMatch.Avalonia.Services;

namespace ArcadeMatch.Avalonia.ViewModels.Tabs;

public class SessionStartViewModel : INotifyPropertyChanged
{
    private readonly ISessionApi _sessionApi;
    private readonly IUserConfigStore _userConfig;

    private string _displayName;
    private string _sessionCode = string.Empty;

    public SessionStartViewModel(ISessionApi sessionApi, IUserConfigStore userConfig)
    {
        _sessionApi = sessionApi;
        _userConfig = userConfig;

        _displayName = _userConfig.Username;
        if (string.IsNullOrWhiteSpace(_displayName))
        {
            _displayName = _userConfig.UserProfile?.SteamId ?? string.Empty;
        }

        _userConfig.PropertyChanged += OnUserConfigPropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName == value)
            {
                return;
            }

            _displayName = value;
            _userConfig.Username = value ?? string.Empty;
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

    public async Task<SessionActionResult> StartNewSessionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            return SessionActionResult.CreateFailure("Please enter a valid Username");
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

        return SessionActionResult.CreateSuccess("StartNewSession");
    }

    public async Task<SessionActionResult> JoinSessionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SessionCode) || SessionCode.Length != 4)
        {
            return SessionActionResult.CreateFailure("Please enter a valid Session Code");
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            return SessionActionResult.CreateFailure("Please enter a valid Username");
        }

        await _sessionApi.JoinSessionAsync(
            SessionCode,
            DisplayName,
            _userConfig.GameList,
            _userConfig.WishlistGames).ConfigureAwait(false);

        return SessionActionResult.CreateSuccess("JoinSession");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public record SessionActionResult(bool Success, string? ErrorMessage, string? Action)
    {
        public static SessionActionResult CreateSuccess(string action) => new(true, null, action);
        public static SessionActionResult CreateFailure(string message) => new(false, message, null);
    }
}
