using System;
using System.Windows;
using System.Windows.Threading;

namespace GameFinder.Helpers
{
    public static class UiHelper
    {
        public static void SafeInvoke(this UIElement element, Action action)
        {
            if (element.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                element.Dispatcher.Invoke(action);
            }
        }
    }
}