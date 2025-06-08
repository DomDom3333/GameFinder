using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using GameFinder.Objects;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace GameFinder
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
            ApplicationAccentColorManager.Apply(Color.FromRgb(71, 126, 244), ApplicationTheme.Dark);
        }
    }
}
