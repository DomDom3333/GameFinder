# ArcadeMatch

This repository contains a cross-platform Avalonia application and an ASP.NET Core API used to create game sessions and match common games between Steam users.

The project is split into three parts:

- **ArcadeMatch.Avalonia** – the cross-platform Avalonia client.
- **ArcadeMatch.Server** – the backend API and SignalR hub.
- **SteamCookieFetcher** – a console utility for obtaining Steam cookies.

Both the client and server are included in `ArcadeMatch.sln`.

The Avalonia client is cross-platform and can be built on Windows, macOS, and Linux. A legacy WPF client (ArcadeMatch.Client) is also available but the Avalonia client is now the recommended option.

The API persistently caches fetched game details and throttles Steam requests with a token bucket limiter to avoid rate limits. On a fresh start the server will still queue incoming requests so the store is never overwhelmed.

## Obtaining Steam API credentials
1. Sign in to your Steam account and open <https://steamcommunity.com/dev/apikey>.
2. Enter any domain name ("localhost" works) and press **Register** to generate a key.
3. Copy the displayed key.
4. Find your numeric Steam ID by opening your profile and copying the number from the URL (use <https://steamidfinder.com/> if you have a custom URL).
5. Launch ArcadeMatch, open the **Config** tab, and paste your API key and Steam ID.
6. Click **Fetch via API** to load your games without using cookies.

## Releases
GitHub releases contain prebuilt binaries for the Avalonia client, the server and the
Steam cookie utility. The Avalonia client is provided for Windows, macOS, and Linux.
Server executables are provided for Windows and Linux (x64, arm64 and arm) as 
self‑contained binaries so they run without a local .NET installation. A Docker image 
of the server is also published on GitHub Container Registry for container deployments.
