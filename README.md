# ArcadeMatch

This repository contains a WPF application and an ASP.NET Core API used to create game sessions and match common games between Steam users.

The project is split into three parts:

- **ArcadeMatch.Client** – the WPF client.
- **ArcadeMatch.Server** – the backend API and SignalR hub.
- **SteamCookieFetcher** – a console utility for obtaining Steam cookies.

Both the client and server are included in `ArcadeMatch.sln`.

The WPF client uses `Microsoft.NET.Sdk.WindowsDesktop`. Building the solution requires the .NET Desktop Runtime and SDK, which are available only on Windows.

The API persistently caches fetched game details and throttles Steam requests with a token bucket limiter to avoid rate limits. On a fresh start the server will still queue incoming requests so the store is never overwhelmed.

## Obtaining Steam API credentials
1. Sign in to your Steam account and open <https://steamcommunity.com/dev/apikey>.
2. Enter any domain name ("localhost" works) and press **Register** to generate a key.
3. Copy the displayed key.
4. Find your numeric Steam ID by opening your profile and copying the number from the URL (use <https://steamidfinder.com/> if you have a custom URL).
5. Launch ArcadeMatch, open the **Config** tab, and paste your API key and Steam ID.
6. Click **Fetch via API** to load your games without using cookies.
