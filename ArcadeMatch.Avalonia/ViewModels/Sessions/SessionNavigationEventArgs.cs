namespace ArcadeMatch.Avalonia.ViewModels.Sessions;

public enum SessionViewType
{
    Start,
    Lobby,
    Swiping,
    Results
}

public sealed class SessionNavigationEventArgs : EventArgs
{
    public SessionNavigationEventArgs(SessionViewType destination, object? parameter = null)
    {
        Destination = destination;
        Parameter = parameter;
    }

    public SessionViewType Destination { get; }

    public object? Parameter { get; }
}
