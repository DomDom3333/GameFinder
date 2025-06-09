# Repository Guidelines for ArcadeMatch

This file provides repository-specific instructions for agents working in this codebase.

## Commit Guidelines
- Use concise commit messages that summarize the change.
- Prefer the present tense (e.g., "Add feature" rather than "Added feature").

## Code Style
- Projects target **.NET 8**; use the same version for builds.
- Indent C# code with **4 spaces**.
- Use `PascalCase` for public types and methods.
- Ensure all files end with a newline.

## Programmatic Checks
Run the following commands from the repository root before committing changes to confirm the solution builds:

```bash
dotnet restore ArcadeMatch.sln
dotnet build --no-restore ArcadeMatch.sln
```

The solution includes a WPF project that requires the .NET Desktop SDK and will only build on Windows. If building on a non-Windows environment, you may build the API and console projects individually:

```bash
dotnet build ArcadeMatch.Server/ArcadeMatch.Server.csproj

dotnet build SteamCookieFetcher/SteamCookieFetcher.csproj
```

If the repository gains unit tests, run `dotnet test` as well.

## Pull Request Notes
Include a short summary of what changed and paste the output of the build commands in the PR description.
