# GameFinder Overview

GameFinder is a cross-platform multiplayer game recommendation system. The project consists of a WPF client and an ASP.NET Core backend that work together to help groups of friends decide on a common game to play.

## How it Works
1. **Session Creation** - A user creates a new session on the server.
2. **Joining** - Others join the session using the generated code.
3. **Fetching Common Games** - The server looks at the Steam libraries of all participants and finds games that everyone owns.
4. **Swiping** - Each player swipes on the shared game list in the WPF client to indicate which titles they would like to play today.
5. **Match Result** - When everyone finishes swiping, the server broadcasts a final game that all participants agreed on.

## Components
- **GameFinder (WPF)** – The desktop client used for joining sessions and swiping through games.
- **GameFinderApi** – The ASP.NET Core application hosting the SignalR hub and session logic.
- **ConsoleApp1** – A helper tool for retrieving Steam cookies.

The solution requires the .NET Desktop Runtime and SDK for the WPF project. It can only be built on Windows.
