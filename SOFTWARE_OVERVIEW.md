# ArcadeMatch Overview

ArcadeMatch is a cross-platform multiplayer game recommendation system. The project consists of an Avalonia client and an ASP.NET Core backend that work together to help groups of friends decide on a common game to play.

## How it Works
1. **Session Creation** - A user creates a new session on the server.
2. **Joining** - Others join the session using the generated code.
3. **Fetching Common Games** - The server looks at the Steam libraries of all participants and finds games that everyone owns.
4. **Swiping** - Each player swipes on the shared game list in the client to indicate which titles they would like to play today.
5. **Match Result** - When everyone finishes swiping, the server broadcasts a final game that all participants agreed on.

## Components
- **ArcadeMatch.Avalonia** – The cross-platform Avalonia client used for joining sessions and swiping through games. Runs on Windows, macOS, and Linux.
- **ArcadeMatch.Server** – The ASP.NET Core application hosting the SignalR hub and session logic.
- **SteamCookieFetcher** – A helper tool for retrieving Steam cookies.

The Avalonia client is cross-platform and can be built on any platform that supports .NET 8.
