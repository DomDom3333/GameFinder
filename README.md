# GameFinder

This repository contains a WPF application and an ASP.NET Core API used to create game sessions and match common games between Steam users.

The project is split into three parts:

- **GameFinder** – the WPF client.
- **GameFinderApi** – the backend API and SignalR hub.
- **ConsoleApp1** – a console utility for obtaining Steam cookies.

Both the client and server are included in `Solution1.sln`.

The WPF client uses `Microsoft.NET.Sdk.WindowsDesktop`. Building the solution requires the .NET Desktop Runtime and SDK, which are available only on Windows.
