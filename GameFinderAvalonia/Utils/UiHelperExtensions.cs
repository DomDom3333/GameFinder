using System;
using Avalonia.Threading;

namespace GameFinderAvalonia.Helpers
{
    public static class UiHelper
    {
        public static void SafeInvoke(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.UIThread.Post(action);
            }
        }
    }
}
