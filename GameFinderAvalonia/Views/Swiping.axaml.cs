using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GameFinderAvalonia.Objects;

namespace GameFinderAvalonia.Views;

public partial class Swiping : UserControl
{
    private Queue<string> _queue;
    private HttpClient _http = new HttpClient();
    private string? _current;

    public Swiping()
    {
        InitializeComponent();
        _queue = new Queue<string>(Config.CommonGames);
        Loaded += async (_, _) => await LoadNext();
    }

    private async Task LoadNext()
    {
        if (_queue.Count == 0)
        {
            App.Api.EndSession(App.Api.SessionId);
            return;
        }
        _current = _queue.Dequeue();
        var json = await _http.GetStringAsync($"http://127.0.0.1:5170/SteamMarketData/{_current}");
        var data = JsonSerializer.Deserialize<SteamGameResponse>(json)?.Data;
        if (data != null)
        {
            GameName.Text = data.Name;
            Description.Text = data.ShortDescription;
            GameImage.Source = new Bitmap(data.HeaderImage);
        }
    }

    private async void Like(object? sender, RoutedEventArgs e)
    {
        if (_current != null) await App.Api.Swipe(App.Api.SessionId, _current, true);
        await LoadNext();
    }

    private async void Dislike(object? sender, RoutedEventArgs e)
    {
        if (_current != null) await App.Api.Swipe(App.Api.SessionId, _current, false);
        await LoadNext();
    }
}
