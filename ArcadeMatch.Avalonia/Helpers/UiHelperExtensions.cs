using System;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ArcadeMatch.Avalonia.Helpers;

public static class UiHelperExtensions
{
    public static void SafeInvoke(this Control _, Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }
}
